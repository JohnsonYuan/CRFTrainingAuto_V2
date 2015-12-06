//----------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Language Data Compiler
// </summary>
//----------------------------------------------------------------------------

namespace LangDataCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.FlowEngine;
    using Microsoft.Tts.Offline.Frontend;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.LangData;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Class of Language Data Compiler.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The resolver to resolve the inside configuration.
        /// </summary>
        /// <param name="sender">The Configuration object to send this event.</param>
        /// <param name="eventArgs">The event arguments.</param>
        /// <returns>The stream contains the inside configuration file.</returns>
        public static Stream ConfigurationResolver(Configuration sender, ResolveEventArgs eventArgs)
        {
            string configFile = "LangDataCompiler." + eventArgs.Name;
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(configFile);
            return stream;
        }

        /// <summary>
        /// Compile all language data files.
        /// </summary>
        /// <param name="config">LangDataCompilerConfig.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet CompileAll(LangDataCompilerConfig config)
        {
            return CompileAll(config, null);
        }

        /// <summary>
        /// Compile all language data files.
        /// </summary>
        /// <param name="config">LangDataCompilerConfig.</param>
        /// <param name="deltaConfig">CreateDeltaConfig.</param>
        /// <returns>Error set.</returns>
        public static ErrorSet CompileAll(LangDataCompilerConfig config, CreateDeltaConfig deltaConfig)
        {
            // Validate the arguments
            config.ApplyRootDataDir();
            ValidateArgument(config);

            ErrorSet errorSet = new ErrorSet();
            Dictionary<string, Collection<LanguageData>> allDataToCompile = config.GetAllLanguageDataInDomains();
            Dictionary<string, DataHandlerList> dataHandlerLists = PrepareDataHandlerLists(config, errorSet);
            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) > 0)
            {
                Helper.PrintColorMessage(ErrorSeverity.MustFix, "Lexicon errors detected, Please fix them first before compiling.");
            }
            else
            {
                foreach (string domain in config.OutputPaths.Keys)
                {
                    int mustFixCountBeforeCompiling = errorSet.GetSeverityCount(ErrorSeverity.MustFix);

                    if (dataHandlerLists.ContainsKey(domain) && allDataToCompile.ContainsKey(domain))
                    {
                        Console.WriteLine("Compiling data for \"{0}\" domain ... ", domain);

                        // Initialize the compiler
                        DataCompiler compiler = PrepareDataCompiler(config, dataHandlerLists[domain]);

                        // Compile each language module data if having external binary file
                        foreach (LanguageData languageData in allDataToCompile[domain])
                        {
                            if (!languageData.Compile && !string.IsNullOrEmpty(languageData.Path))
                            {
                                Console.Write("Compiling \"{0}\" ... ", languageData.Name);
                                ErrorSet compilingErrorSet = null;

                                if (File.Exists(languageData.Path))
                                {
                                    if (languageData.IsCustomer || !string.IsNullOrEmpty(languageData.FormatGuid))
                                    {
                                        compilingErrorSet = compiler.Compile(languageData.Name, languageData.Path, languageData.Guid, languageData.FormatGuid);
                                    }
                                    else
                                    {
                                        compilingErrorSet = compiler.Compile(languageData.Name, languageData.Path);
                                    }
                                }
                                else
                                {
                                    compilingErrorSet = new ErrorSet();
                                    compilingErrorSet.Add(new Error(DataCompilerError.InvalidBinaryData, languageData.Path) { Severity = ErrorSeverity.MustFix });
                                }

                                ReportCompilingErrorSet(compilingErrorSet, config.LogFilePath, languageData.Name);
                                errorSet.Merge(compilingErrorSet);
                            }
                        }

                        // Compile each language module data if no external binary file
                        // Those data load with composite process also needs to be compiled here.
                        foreach (LanguageData languageData in allDataToCompile[domain])
                        {
                            if (languageData.Compile)
                            {
                                if (string.IsNullOrEmpty(languageData.InnerCompilingXml))
                                {
                                    Console.Write("Compiling \"{0}\" ... ", languageData.Name);
                                    ErrorSet compilingErrorSet = Compile(compiler, languageData);
                                    ReportCompilingErrorSet(compilingErrorSet, config.LogFilePath, languageData.Name);
                                    errorSet.Merge(compilingErrorSet);
                                }
                            }
                        }

                        // Do pre-handling for the composite data
                        foreach (LanguageData languageData in allDataToCompile[domain])
                        {
                            if (languageData.Compile && !string.IsNullOrEmpty(languageData.InnerCompilingXml))
                            {
                                try
                                {
                                    Console.Write("Compiling \"{0}\" ... ", languageData.Name);
                                    if (languageData.Name.Equals(ModuleDataName.Lexicon))
                                    {
                                        Configuration flowEngineConfig = new Configuration();
                                        flowEngineConfig.ConfigurationResolve += ConfigurationResolver;
                                        Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(languageData.InnerCompilingXml));
                                        stream.Seek(0, SeekOrigin.Begin);

                                        flowEngineConfig.Load(stream);
                                        Dictionary<string, ConfigurationModule> modules = flowEngineConfig.GetAllModules();
                                        FlowEngine engine = new FlowEngine("Composite Lexicon Compiler", modules, config.LogFilePath);
                                        FlowItem item = engine.FindItem("env");
                                        if (item != null)
                                        {
                                            LangDataCompilerEnvironment env = item.Handler as LangDataCompilerEnvironment;
                                            env.InCompiler = compiler;
                                            if (string.IsNullOrEmpty(env.InWorkingDirectory))
                                            {
                                                env.InWorkingDirectory = config.BinRootDir;
                                            }
                                            else if (!Path.IsPathRooted(env.InWorkingDirectory))
                                            {
                                                env.InWorkingDirectory = Path.Combine(config.BinRootDir, env.InWorkingDirectory);
                                            }
                                        }

                                        engine.Execute();
                                        item = engine.FindItem("SetLexicon");
                                        if (item != null)
                                        {
                                            LangDataCompilerEnvironment setLexicon = item.Handler as LangDataCompilerEnvironment;
                                            if (setLexicon.OutCompilerLexicon != null)
                                            {
                                                compiler.SetObject(RawDataName.Lexicon, setLexicon.OutCompilerLexicon);
                                                dataHandlerLists[domain].Datas[RawDataName.Lexicon].SetObject(setLexicon.OutCompilerLexicon);
                                            }
                                        }
                                    }

                                    ErrorSet compilingErrorSet = Compile(compiler, languageData);
                                    ReportCompilingErrorSet(compilingErrorSet, config.LogFilePath, languageData.Name);
                                    errorSet.Merge(compilingErrorSet);
                                }
                                catch (InvalidDataException invalidDataException)
                                {
                                    string message = Helper.BuildExceptionMessage(invalidDataException);
                                    Error error = new Error(DataCompilerError.CompositeCompilingFail, "languageData.Name", message);
                                    ErrorSet compilingErrorSet = new ErrorSet();
                                    compilingErrorSet.Add(error);
                                    ReportCompilingErrorSet(compilingErrorSet, config.LogFilePath, languageData.Name);
                                    errorSet.Merge(compilingErrorSet);
                                }
                            }
                        }

                        if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) > mustFixCountBeforeCompiling)
                        {
                            Helper.PrintColorMessage(ErrorSeverity.MustFix, "Failed to Build Engine data file: '{0}'.", config.OutputPaths[domain]);
                        }
                        else
                        {
                            // Merge all the binary data into one language data file    
                            ErrorSet combinationErrorSet = compiler.CombineDataFile(config.OutputPaths[domain], domain);
                            foreach (Error tmpError in combinationErrorSet.Errors)
                            {
                                Helper.PrintColorMessage(tmpError.Severity, tmpError.ToString());
                            }

                            if (combinationErrorSet.Contains(ErrorSeverity.MustFix))
                            {
                                Error error = new Error(DataCompilerError.CombinationHalt);
                                Helper.PrintColorMessage(error.Severity, error.ToString());
                                combinationErrorSet.Add(error);
                            }
                            else
                            {
                                Helper.PrintSuccessMessage("Successfully Build Engine data file: '{0}'.", config.OutputPaths[domain]);
                            }

                            PrintLog(config.LogFilePath, "Combination information for engine data", combinationErrorSet);
                            errorSet.Merge(combinationErrorSet);
                        }
                    }
                    else
                    {
                        Error error = new Error(DataCompilerError.AllDomainRawDataNotFound, domain);
                        errorSet.Add(error);
                        Helper.PrintColorMessage(error.Severity, error.ToString());
                    }
                }

                // Process Create Delta
                if (deltaConfig != null)
                {
                    ProcessCreateLxaDelta(config, allDataToCompile[DomainItem.GeneralDomain], dataHandlerLists[DomainItem.GeneralDomain], deltaConfig, errorSet);
                    ProcessCreateDatDelta(config, deltaConfig, errorSet);
                }
            }

            // Print the summary information
            {
                int errorNumber = errorSet.GetSeverityCount(ErrorSeverity.MustFix);
                int warningNumber = errorSet.GetSeverityCount(ErrorSeverity.Warning);
                string message = Helper.NeutralFormat("There are {0} distinct error{1} and {2} distinct warning{3}.",
                    errorNumber, errorNumber == 1 ? string.Empty : "s",
                    warningNumber, warningNumber == 1 ? string.Empty : "s");
                Helper.PrintColorMessage(errorNumber > 0 || warningNumber > 0 ? ErrorSeverity.Warning : ErrorSeverity.NoError,
                    message);
                if (!string.IsNullOrEmpty(config.LogFilePath))
                {
                    using (TextWriter writer = new StreamWriter(config.LogFilePath, true, Encoding.Unicode))
                    {
                        string titleMessage = MakeTitle("Summary");
                        writer.WriteLine(titleMessage);
                        writer.WriteLine(message);
                    }

                    Helper.PrintColorMessage(ErrorSeverity.NoError,
                        "Log file could be found from '{0}'.", config.LogFilePath);
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Main of LangDataCompiler.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>0:Succeeded; -1:Catch exception.</returns>
        private static int Main(string[] args)
        {
            if (!args.Contains("-mode"))
            {
                args = new string[] { "-mode", "Normal" }.Append(args);
            }

            return ConsoleApp<Arguments>.Run(args, Process);
        }

        /// <summary>
        /// Process Language Data.
        /// </summary>
        /// <param name="arguments">Argument strings from command line.</param>
        /// <returns>Return exitcode.</returns>
        private static int Process(Arguments arguments)
        {
            LangDataCompilerConfig config = new LangDataCompilerConfig();
            config.Load(arguments.ConfigFilePath);
            ApplyForcedArguments(config, arguments);
            foreach (string output in config.OutputPaths.Values)
            {
                string errorMessage = Helper.TestWritableWithoutException(output);
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    throw new ArgumentException(errorMessage);
                }
            }

            CreateDeltaConfig createDeltaConfig = null;
            if (arguments.Mode.ToLower() == "createdelta")
            {
                Language language = config.Language;
                string originalDataDir = SetRootedPath(arguments.OriginalDataDir);
                string outputDeltaDir = SetRootedPath(arguments.OutputDeltaDir);
                Helper.CheckFolderNotEmpty(originalDataDir);
                Helper.EnsureFolderExist(outputDeltaDir);
                createDeltaConfig = new CreateDeltaConfig(originalDataDir, outputDeltaDir, language);
                if (!string.IsNullOrEmpty(arguments.OutputDirPath) && SetRootedPath(arguments.OutputDirPath) == outputDeltaDir)
                {
                    throw new ArgumentNullException("output dir path equals output delta dir.");
                }
            }

            ErrorSet errorSet = CompileAll(config, createDeltaConfig);
            if (errorSet.GetSeverityCount(ErrorSeverity.MustFix) > 0)
            {
                throw new InvalidDataException("There are must fix errors when compiling data!");
            }

            return ExitCode.NoError;
        }

        /// <summary>
        /// Report the compiling errors.
        /// </summary>
        /// <param name="compilingErrorSet">Compiling errors.</param>
        /// <param name="logFilePath">Log file path.</param>
        /// <param name="languageDataName">Language data name.</param>
        private static void ReportCompilingErrorSet(ErrorSet compilingErrorSet, string logFilePath, string languageDataName)
        {
            if (compilingErrorSet == null)
            {
                throw new ArgumentNullException("compilingErrorSet");
            }

            if (!compilingErrorSet.Contains(ErrorSeverity.MustFix))
            {
                int warningNumber = compilingErrorSet.GetSeverityCount(ErrorSeverity.Warning);
                if (warningNumber > 0)
                {
                    Console.WriteLine();
                    Helper.PrintColorMessage(ErrorSeverity.Warning,
                        "Successfully with {0} warning{1}.",
                        warningNumber, warningNumber == 1 ? string.Empty : "s");
                }
                else
                {
                    Helper.PrintSuccessMessage("Successfully.");
                }
            }
            else
            {
                foreach (Error tmpError in compilingErrorSet.Errors)
                {
                    if (tmpError.Severity == ErrorSeverity.MustFix)
                    {
                        Helper.PrintColorMessage(tmpError.Severity, tmpError.ToString());
                    }
                }

                int errorNumber = compilingErrorSet.GetSeverityCount(ErrorSeverity.MustFix);
                int warningNumber = compilingErrorSet.GetSeverityCount(ErrorSeverity.Warning);
                Helper.PrintColorMessage(ErrorSeverity.MustFix,
                    "There are {0} error{1} and {2} warning{3}.",
                    errorNumber, errorNumber == 1 ? string.Empty : "s",
                    warningNumber, warningNumber == 1 ? string.Empty : "s");

                // in case some words(like Chinese words) can't be displayed well in console
                // remind users to check log file to see detailed messages
                Helper.PrintColorMessage(ErrorSeverity.MustFix, string.Format("please see details in log file: {0}", logFilePath));
            }

            PrintLog(logFilePath,
                "Compiling information for [" + languageDataName + "]",
                compilingErrorSet);
        }

        /// <summary>
        /// Compile one language data.
        /// </summary>
        /// <param name="compiler">Compiler.</param>
        /// <param name="languageData">Language data.</param>
        /// <returns>Error set.</returns>
        private static ErrorSet Compile(DataCompiler compiler, LanguageData languageData)
        {
            ErrorSet compilingErrorSet = new ErrorSet();
            if (languageData.Compile)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    compilingErrorSet = compiler.Build(languageData.Name, memoryStream, AppConfig.Instance.IsEnableValidModule(languageData.Name), languageData.FormatGuid);
                    compilingErrorSet.Merge(compiler.Compile(languageData.Name, memoryStream, languageData.Guid, languageData.FormatGuid));

                    // Save to external binary path for the module data
                    if (!string.IsNullOrEmpty(languageData.Path))
                    {
                        string message = null;
                        try
                        {
                            Helper.EnsureFolderExistForFile(languageData.Path);
                            File.WriteAllBytes(languageData.Path, memoryStream.ToArray());
                        }
                        catch (Exception ex)
                        {
                            message = Helper.BuildExceptionMessage(ex);
                            if (string.IsNullOrEmpty(message))
                            {
                                throw;
                            }
                        }

                        if (!string.IsNullOrEmpty(message))
                        {
                            Error error = new Error(DataCompilerError.SaveBinaryFileFail,
                                languageData.Name, message);
                            compilingErrorSet.Add(error);
                        }
                    }
                }
            }

            return compilingErrorSet;
        }

        /// <summary>
        /// Prepare DataHandlerList for different domains.
        /// </summary>
        /// <param name="config">LangDataCompilerConfig.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>DataHandlerList Dictionary.</returns>
        private static Dictionary<string, DataHandlerList> PrepareDataHandlerLists(
            LangDataCompilerConfig config, ErrorSet errorSet)
        {
            Dictionary<string, DataHandlerList> lists = new Dictionary<string, DataHandlerList>();

            List<string> domains = config.GetAllDomains();
            foreach (string domain in domains)
            {
                DataHandlerList dataHandlerList = new DataHandlerList(domain);
                dataHandlerList.SetLanguage(config.Language);
                dataHandlerList.PrepareDataPath(config.RawRootDir, config.RawDataList);
                lists.Add(domain, dataHandlerList);
            }

            SplitLanguageData(config, lists, errorSet);
            using (StreamWriter sw = new StreamWriter(config.LogFilePath, true, Encoding.Unicode))
            {
                foreach (Error tmpError in errorSet.Errors)
                {
                    if (tmpError.Severity == ErrorSeverity.MustFix)
                    {
                        sw.WriteLine(tmpError.ToString());
                    }
                }
            }

            return lists;
        }

        /// <summary>
        /// Split LanguageData for unified files such as lexicon, polyphony rule.
        /// </summary>
        /// <param name="config">LangDataCompilerConfig.</param>
        /// <param name="dataHandlerLists">DataHandlerList dictionary.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>ErrorSet.</returns>
        private static ErrorSet SplitLanguageData(LangDataCompilerConfig config,
            Dictionary<string, DataHandlerList> dataHandlerLists, ErrorSet errorSet)
        {
            Console.WriteLine("Split language data ...");
            Dictionary<string, Lexicon> lexicons = new Dictionary<string, Lexicon>();
            Dictionary<string, RuleFile> polyphonyRuleFiles = new Dictionary<string, RuleFile>();
            Dictionary<string, RuleFile> sentDetectFiles = new Dictionary<string, RuleFile>();

            SP.ServiceProvider sp = null;
            if (config.IsServiceProviderRequired)
            {
                sp = InitializeServiceProvider(config.Language);
            }

            foreach (string domain in dataHandlerLists.Keys)
            {
                foreach (DataHandler handler in dataHandlerLists[domain].Datas.Values)
                {
                    try
                    {
                        switch (handler.Name)
                        {
                            case RawDataName.Lexicon:
                                LanguageData lexiconData = config.GetLangDataInDomain(ModuleDataName.Lexicon, domain);

                                if (lexiconData != null)
                                {
                                    // validate case-sensitive lexicon before splitting general domain lexicon
                                    if (domain == "general")
                                    {
                                        DataHandlerList dataHandlerList = dataHandlerLists[domain];
                                        LexicalAttributeSchema schema = null;
                                        TtsPhoneSet phoneSet = null;

                                        if (dataHandlerList.Datas.ContainsKey(RawDataName.LexicalAttributeSchema))
                                        {
                                            schema = (LexicalAttributeSchema)dataHandlerList.Datas[RawDataName.LexicalAttributeSchema].GetObject(errorSet);
                                        }

                                        if (dataHandlerList.Datas.ContainsKey(RawDataName.PhoneSet))
                                        {
                                            phoneSet = (TtsPhoneSet)dataHandlerList.Datas[RawDataName.PhoneSet].GetObject(errorSet);
                                        }

                                        if (schema != null && phoneSet != null)
                                        {
                                            Lexicon lexicon = new Lexicon();
                                            Lexicon.ContentControler lexiconControler = new Lexicon.ContentControler { IsCaseSensitive = true };
                                            lexicon.Load(handler.Path, lexiconControler);
                                            lexicon.Validate(phoneSet, schema);
                                            errorSet.Merge(lexicon.ErrorSet);

                                            ReportCompilingErrorSet(lexicon.ErrorSet, config.LogFilePath, handler.Name);
                                        }
                                    }

                                    SplitLexicon(lexicons, lexiconData, domain, handler, sp, errorSet);
                                }

                                break;
                            case RawDataName.PolyphoneRule:
                                LanguageData polyphoneRuleData = config.GetLangDataInDomain(ModuleDataName.PolyphoneRule, domain);
                                if (polyphoneRuleData != null)
                                {
                                    SplitRuleFile(polyphonyRuleFiles, polyphoneRuleData, domain, handler, errorSet);
                                }

                                break;
                            case RawDataName.SentenceDetectRule:
                                LanguageData sentDetectData = config.GetLangDataInDomain(ModuleDataName.SentenceDetector, domain);
                                if (sentDetectData != null)
                                {
                                    SplitRuleFile(sentDetectFiles, sentDetectData, domain, handler, errorSet);
                                }

                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Type exceptionType = ex.GetType();
                        if (exceptionType.Equals(typeof(FileNotFoundException)) ||
                            exceptionType.Equals(typeof(ArgumentNullException)) ||
                            exceptionType.Equals(typeof(XmlException)) ||
                            exceptionType.Equals(typeof(InvalidDataException)))
                        {
                            errorSet.Add(DataCompilerError.RawDataNotFound, handler.Name,
                                Helper.BuildExceptionMessage(ex));
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            return errorSet;
        }

        /// <summary>
        /// Initialize Engine setting and return service provider.
        /// </summary>
        /// <param name="language">Language.</param>
        /// <returns>ServiceProvider.</returns>
        private static SP.ServiceProvider InitializeServiceProvider(Language language)
        {
            SP.Language langId = (SP.Language)language;
            SP.TtsEngineSetting setting = new SP.TtsEngineSetting(langId);
            setting.PipelineMode = SP.ModulePipelineMode.PM_TEXT_ANALYSIS;

            return new SP.ServiceProvider(setting);
        }

        /// <summary>
        /// Split lexicon file in domains.
        /// </summary>
        /// <param name="lexicons">Domain lexicons.</param>
        /// <param name="lexiconData">Lexicon data.</param>
        /// <param name="domain">Domain.</param>
        /// <param name="handler">DataHandler.</param>
        /// <param name="sp">ServiceProvider.</param>
        /// <param name="errorSet">ErrorSet.</param>
        private static void SplitLexicon(Dictionary<string, Lexicon> lexicons,
            LanguageData lexiconData, string domain, DataHandler handler, SP.ServiceProvider sp, ErrorSet errorSet)
        {
            Helper.ThrowIfNull(lexicons);
            if (lexicons.Count == 0)
            {
                Lexicon lexicon = new Lexicon();
                lexicon.Load(handler.Path);
                if (string.IsNullOrEmpty(lexicon.DomainTag))
                {
                    Lexicon[] domainlexicons = lexicon.SplitIntoDomainLexicons(sp, errorSet);

                    foreach (Lexicon lex in domainlexicons)
                    {
                        lexicons.Add(lex.DomainTag, lex);
                    }
                }
                else
                {
                    lexicons.Add(lexicon.DomainTag, lexicon);
                }
            }

            handler.RelativePath = null;
            if (lexicons.ContainsKey(domain) && lexicons.Count > 1)
            {
                string domainPath = Path.Combine(Path.GetDirectoryName(lexiconData.Path),
                    string.Format("{0}.{1}{2}", Path.GetFileNameWithoutExtension(handler.Path), domain, Path.GetExtension(handler.Path)));
                lexicons[domain].Save(domainPath);
                handler.Path = domainPath;
            }
            else if (!lexicons.ContainsKey(domain))
            {
                handler.Path = null;
                errorSet.Add(DataCompilerError.NoDomainDataInRawData, domain, handler.Name);
            }
        }

        /// <summary>
        /// Split rule file in domains, such as polyphony rule, sentence detect rule.
        /// </summary>
        /// <param name="files">RuleFile Dictionary.</param>
        /// <param name="langData">LanguageData.</param>
        /// <param name="domain">Domain.</param>
        /// <param name="handler">DataHandler.</param>
        /// <param name="errorSet">ErrorSet.</param>
        private static void SplitRuleFile(Dictionary<string, RuleFile> files, LanguageData langData, string domain, DataHandler handler, ErrorSet errorSet)
        {
            Helper.ThrowIfNull(files);
            if (files.Count == 0)
            {
                RuleFile ruleFile = new RuleFile();
                ruleFile.Load(handler.Path);
                RuleFile[] ruleFiles = ruleFile.Split();
                foreach (RuleFile file in ruleFiles)
                {
                    files.Add(file.DomainTag, file);
                }
            }

            handler.RelativePath = null;
            if (files.ContainsKey(domain) && files.Count > 1)
            {
                string domainPath = Path.Combine(Path.GetDirectoryName(langData.Path),
                    string.Format("{0}.{1}{2}", Path.GetFileNameWithoutExtension(handler.Path), domain, Path.GetExtension(handler.Path)));
                files[domain].Save(domainPath);
                handler.Path = domainPath;
            }
            else if (!files.ContainsKey(domain))
            {
                handler.Path = null;
                errorSet.Add(DataCompilerError.NoDomainDataInRawData, domain, handler.Name);
            }
        }

        /// <summary>
        /// Prepare DataCompiler.
        /// </summary>
        /// <param name="config">LangDataCompilerConfig.</param>
        /// <param name="dataHandlerList">Data handler list.</param>
        /// <returns>DataCompiler.</returns>
        private static DataCompiler PrepareDataCompiler(LangDataCompilerConfig config, DataHandlerList dataHandlerList)
        {
            DataCompiler compiler = new DataCompiler();
            compiler.DataHandlerList = dataHandlerList;
            compiler.SetLanguage(config.Language);
            config.LanguageDataList.ForEach(langData =>
            {
                // Tell the compiler which datas will be built.
                if (langData.Domain == dataHandlerList.Domain)
                {
                    Guid guid = new Guid(langData.Guid);
                    LangDataObject dataObject = new LangDataObject(
                            guid, SP.TtsDataTag.Find(guid), null);
                    compiler.ModuleDataSet.Add(langData.Name, dataObject);
                }
            });

            if (!string.IsNullOrEmpty(config.ToolDir))
            {
                compiler.ToolDir = config.ToolDir;
            }

            return compiler;
        }

        /// <summary>
        /// Apply forced arguments from command line.
        /// </summary>
        /// <param name="config">Configuration for LangDataCompiler.</param>
        /// <param name="arguments">Arguments.</param>
        private static void ApplyForcedArguments(LangDataCompilerConfig config, Arguments arguments)
        {
            if (!string.IsNullOrEmpty(arguments.OutputDirPath))
            {
                foreach (string key in config.OutputPaths.Keys.ToArray())
                {
                    config.OutputPaths[key] = Path.Combine(SetRootedPath(arguments.OutputDirPath),
                        Path.GetFileName(config.OutputPaths[key]));
                }
            }

            if (!string.IsNullOrEmpty(arguments.RawDataDirPath))
            {
                config.RawRootDir = SetRootedPath(arguments.RawDataDirPath);
            }

            if (!string.IsNullOrEmpty(arguments.BinRootDirPath))
            {
                config.BinRootDir = SetRootedPath(arguments.BinRootDirPath);
            }

            if (!string.IsNullOrEmpty(arguments.CustomerDataRootDirPath))
            {
                config.CustomerRootDir = SetRootedPath(arguments.CustomerDataRootDirPath);
            }

            if (!string.IsNullOrEmpty(arguments.ToolDirPath))
            {
                config.ToolDir = SetRootedPath(arguments.ToolDirPath);
            }

            if (!string.IsNullOrEmpty(arguments.LogFilePath))
            {
                config.LogFilePath = SetRootedPath(arguments.LogFilePath);
            }

            config.ValidateDirPath();
        }

        /// <summary>
        /// Set rooted path.
        /// </summary>
        /// <param name="originalPath">Original path.</param>
        /// <returns>Applied path.</returns>
        private static string SetRootedPath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
            {
                throw new ArgumentNullException("originalPath");
            }

            string appliedPath = originalPath;
            if (!Path.IsPathRooted(originalPath))
            {
                appliedPath = Path.Combine(Directory.GetCurrentDirectory(),
                    originalPath);
            }

            return appliedPath;
        }

        /// <summary>
        /// Validate the arguments.
        /// </summary>
        /// <param name="config">Configuration.</param>
        private static void ValidateArgument(LangDataCompilerConfig config)
        {
            foreach (string output in config.OutputPaths.Values)
            {
                Helper.EnsureFolderExistForFile(output);
            }

            if (!string.IsNullOrEmpty(config.LogFilePath))
            {
                Helper.EnsureFolderExistForFile(config.LogFilePath);
            }

            if (File.Exists(config.LogFilePath))
            {
                File.Delete(config.LogFilePath);
            }
        }

        /// <summary>
        /// Print the log into file.
        /// </summary>
        /// <param name="logFilePath">Path of log file.</param>
        /// <param name="title">Title.</param>
        /// <param name="errorSet">Error set.</param>
        private static void PrintLog(string logFilePath, string title, ErrorSet errorSet)
        {
            if (!string.IsNullOrEmpty(logFilePath))
            {
                using (TextWriter writer = new StreamWriter(logFilePath, true, Encoding.Unicode))
                {
                    string titleMessage = MakeTitle(title);
                    writer.WriteLine(titleMessage);
                    if (errorSet != null && errorSet.Count > 0)
                    {
                        errorSet.Export(writer);
                    }
                    else
                    {
                        writer.WriteLine("No error");
                    }

                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// Make the title enclosing with "=" and with same width;
        /// Example: change "title" to "==== title ====".
        /// </summary>
        /// <param name="title">Title.</param>
        /// <returns>Full title enclosing with "=".</returns>
        private static string MakeTitle(string title)
        {
            string titleMessage = string.Empty;
            int fullTitleLength = 79;
            if (title.Length < fullTitleLength - 10)
            {
                int rightNumber = (fullTitleLength - title.Length - 2) / 2;
                int leftNumber = fullTitleLength - title.Length - 2 - rightNumber;
                for (int i = 0; i < leftNumber; i++)
                {
                    titleMessage += "=";
                }

                titleMessage += " " + title + " ";
                for (int i = 0; i < rightNumber; i++)
                {
                    titleMessage += "=";
                }
            }
            else
            {
                titleMessage = string.Format(CultureInfo.InvariantCulture, "==== {0} ====", title);
            }

            return titleMessage;
        }

        /// <summary>
        /// Process Create Delta.
        /// </summary>
        /// <param name="config">Language data compiler config.</param>
        /// <param name="newGeneralDataToCompile">New general data to compile.</param>
        /// <param name="newGeneralDataHandlerList">New data handler lists.</param>
        /// <param name="deltaConfig">Create delta config.</param>
        /// <param name="errorSet">Error set.</param>
        private static void ProcessCreateLxaDelta(LangDataCompilerConfig config,
            Collection<LanguageData> newGeneralDataToCompile,
            DataHandlerList newGeneralDataHandlerList,
            CreateDeltaConfig deltaConfig,
            ErrorSet errorSet)
        {
            IEnumerable<LanguageData> lexiconDatas = newGeneralDataToCompile.Where((data) => data.Name == RawDataName.Lexicon);
            switch (lexiconDatas.Count())
            {
                case 0:
                    return;
                case 1:
                    break;
                default:
                    errorSet.Add(DataCompilerError.CompilingLogWithError, RawDataName.Lexicon, "Two or more lexicon in general data set.");
                    return;
            }

            // Creating hot lexicon
            ErrorSet createErrorSet = new ErrorSet();
            Console.Write("Creating hot lexicon ...");
            LanguageData languageData = languageData = lexiconDatas.First();
            string oldPath = deltaConfig.GeneralLexiconPath;
            string newPath = newGeneralDataHandlerList.Datas[RawDataName.Lexicon].Path;
            Lexicon originalLexicon = new Lexicon(config.Language);
            Lexicon newLexicon = new Lexicon(config.Language);
            originalLexicon.Load(oldPath, new Lexicon.ContentControler() { IsCaseSensitive = true, IsHistoryCheckingMode = false });

            // Reload data handler lexicon.
            // tempNewLexicon is not always equals to newLexicon.
            string tempPath = Path.GetTempFileName();
            try
            {
                Lexicon tempNewLexicon = (Lexicon)newGeneralDataHandlerList.Datas[RawDataName.Lexicon].GetObject(createErrorSet);
                tempNewLexicon.SaveAs(tempPath);
                newLexicon.Load(tempPath, new Lexicon.ContentControler() { IsCaseSensitive = true, IsHistoryCheckingMode = false });
            }
            finally 
            {
                Helper.SafeDelete(tempPath);
            }

            Lexicon hotLexicon = CreateHotLexicon(originalLexicon, newLexicon, createErrorSet);
            ReportCompilingErrorSet(createErrorSet, config.LogFilePath, "Create hot lexicon");
            errorSet.Merge(createErrorSet);

            // Compiling hot lexicon
            if (!createErrorSet.Contains(ErrorSeverity.MustFix) && hotLexicon != null && hotLexicon.Items.Count > 0)
            {
                hotLexicon.SaveAs(Path.ChangeExtension(newPath, ".hot.xml"));
                Console.Write("Compiling hot lexicon ...");
                DataHandlerList updateDataHandlerList = new DataHandlerList(DomainItem.GeneralDomain);
                updateDataHandlerList.SetLanguage(config.Language);
                updateDataHandlerList.PrepareDataPath(config.RawRootDir, config.RawDataList);
                updateDataHandlerList.SetObject(RawDataName.Lexicon, hotLexicon);
                DataCompiler compiler = PrepareDataCompiler(config, updateDataHandlerList);
                LanguageData hotData = new LanguageData();
                hotData.Compile = true;
                hotData.InnerCompilingXml = languageData.InnerCompilingXml;
                hotData.IsCustomer = languageData.IsCustomer;
                hotData.Path = Helper.GetFullPath(deltaConfig.OutputDeltaDir, Path.GetFileNameWithoutExtension(languageData.Path) + ".hot.lxa");
                if (languageData.Domain != null)
                {
                    hotData.Domain = languageData.Domain;
                }

                if (languageData.Name != null)
                {
                    hotData.Name = languageData.Name;
                }

                if (languageData.Guid != null)
                {
                    hotData.Guid = languageData.Guid;
                }

                if (languageData.FormatGuid != null)
                {
                    hotData.FormatGuid = languageData.FormatGuid;
                }

                ErrorSet compileErrorSet = Compile(compiler, hotData);
                if (!compileErrorSet.Contains(ErrorSeverity.MustFix))
                {
                    MSDelta.CreateDeltaBasedOnEmptyFile(hotData.Path, hotData.Path + ".delta");
                }

                ReportCompilingErrorSet(compileErrorSet, config.LogFilePath, "Create hot lexicon");
                errorSet.Merge(compileErrorSet);
            }
        }

        /// <summary>
        /// Create hot lexicon.
        /// </summary>
        /// <param name="originalLexicon">Original lexicon.</param>
        /// <param name="newLexicon">New lexicon.</param>
        /// <param name="errorSet">Error set.</param>
        /// <returns>Hot lexicon.</returns>
        private static Lexicon CreateHotLexicon(Lexicon originalLexicon, Lexicon newLexicon, ErrorSet errorSet)
        {
            ErrorSet subErrorSet = new ErrorSet();
            if (originalLexicon.DomainTag != newLexicon.DomainTag ||
                originalLexicon.Language != newLexicon.Language)
            {
                throw new ArgumentException("Some properties are not equals.");
            }

            IDictionary<string, LexicalItem> newItems = newLexicon.Items;
            IDictionary<string, LexicalItem> originalItems = originalLexicon.Items;
            foreach (string originalKey in originalItems.Keys)
            {
                if (!newItems.ContainsKey(originalKey))
                {
                    subErrorSet.Add(DataCompilerError.CompilingLogWithError, "Create Hot Lexicon", "\"" + originalKey + "\" Not found in new Lexicon.");
                }
            }

            errorSet.Merge(subErrorSet);
            if (subErrorSet.Contains(ErrorSeverity.MustFix))
            {
                return null;
            }

            Lexicon deltaLexicon = new Lexicon();
            if (newLexicon.DomainTag != null) 
            {
                deltaLexicon.DomainTag = newLexicon.DomainTag;
            }

            deltaLexicon.Encoding = newLexicon.Encoding;
            deltaLexicon.Language = newLexicon.Language;
            deltaLexicon.LexicalAttributeSchema = newLexicon.LexicalAttributeSchema;
            deltaLexicon.PhoneSet = newLexicon.PhoneSet;
            deltaLexicon.PosSet = newLexicon.PosSet;

            foreach (KeyValuePair<string, LexicalItem> newPair in newItems)
            {
                string newKey = newPair.Key;
                if (!originalItems.ContainsKey(newKey) || !LexicalItem.Equals(newPair.Value, originalItems[newKey]))
                {
                    deltaLexicon.Items.Add(newKey, newPair.Value);
                }
            }

            return deltaLexicon;
        }

        /// <summary>
        /// Process create DAT delta.
        /// </summary>
        /// <param name="config">Language data compiler config.</param>
        /// <param name="deltaConfig">Create delta config.</param>
        /// <param name="errorSet">ErrorSet.</param>
        private static void ProcessCreateDatDelta(LangDataCompilerConfig config, CreateDeltaConfig deltaConfig, ErrorSet errorSet)
        {
            // Create Dat Delta
            Guid lexiconGuid = Guid.Parse(LanguageDataHelper.GetReservedGuid(ModuleDataName.Lexicon));
            foreach (string domain in config.GetAllDomains())
            {
                ErrorSet subErrorSet = new ErrorSet();
                Console.Write("Create delta for {0} domain ... ", domain);
                string newPath = config.OutputPaths[domain];
                string updatePath = Helper.GetFullPath(deltaConfig.OutputDeltaDir, Path.GetFileName(newPath));
                if (deltaConfig.OriginalDataPaths.ContainsKey(domain))
                {
                    using (LangDataFile originalFile = new LangDataFile())
                    using (LangDataFile newFile = new LangDataFile())
                    using (LangDataFile updateFile = new LangDataFile())
                    {
                        string originalPath = deltaConfig.OriginalDataPaths[domain];
                        if (!originalFile.Load(originalPath))
                        {
                            subErrorSet.Add(new Error(DataCompilerError.InvalidBinaryData, originalPath) { Severity = ErrorSeverity.MustFix });
                        }

                        if (!newFile.Load(newPath))
                        {
                            subErrorSet.Add(new Error(DataCompilerError.InvalidBinaryData, newPath) { Severity = ErrorSeverity.MustFix });
                        }

                        if (!subErrorSet.Contains(ErrorSeverity.MustFix))
                        {
                            foreach (LangDataObject originalObj in originalFile.DataObjects)
                            {
                                if (deltaConfig.Setting != null &&
                                    deltaConfig.Setting.DisabledDatas != null &&
                                    deltaConfig.Setting.DisabledDatas.Contains(originalObj.Token))
                                {
                                    continue;
                                }
                                else if (!newFile.IsExisted(originalObj.Token))
                                {
                                    subErrorSet.Add(DataCompilerError.NecessaryDataMissing, originalObj.Token.ToString());
                                }
                            }

                            foreach (LangDataObject newObj in newFile.DataObjects)
                            {
                                if (domain == DomainItem.GeneralDomain && newObj.Token == lexiconGuid)
                                {
                                    continue;
                                }
                                else if (!originalFile.IsExisted(newObj.Token) ||
                                    !newObj.Equals(originalFile.GetDataObject(newObj.Token)))
                                {
                                    updateFile.AddDataObject(newObj);
                                }
                            }

                            if (updateFile.DataObjects.Count == 0)
                            {
                                subErrorSet.Add(DataCompilerError.ZeroModuleData, domain);
                            }
                            else
                            {
                                updateFile.FileProperties = newFile.FileProperties;
                                updateFile.Save(updatePath);
                                MSDelta.CreateDelta(originalPath, updatePath, updatePath + ".delta");
                            }
                        }
                    }
                }
                else
                {
                    File.Copy(newPath, updatePath, true);
                    MSDelta.CreateDeltaBasedOnEmptyFile(updatePath, updatePath + ".delta");
                }

                ReportCompilingErrorSet(subErrorSet, config.LogFilePath, string.Format("Create {0} delta", domain));
                errorSet.Merge(subErrorSet);
            }
        }
    }
}