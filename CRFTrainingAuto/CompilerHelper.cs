//-----------------------------------------------------------------------------------------
// <copyright file="CompilerHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <summary>
//     Compiler helper, compile crf model and general rule.
// </summary>
//-----------------------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Compiler helper, compile crf model and general rule.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public static class CompilerHelper
    {
        #region Fields

        // polyrule.txt [domain=message]
        private static readonly Regex DomainLineRegex = new Regex(@"^[ \t]*(\[domain=)([a-zA-Z]+)*(\])$", RegexOptions.Compiled);

        // polyrule.txt all >= 0 line
        private static readonly Regex PolyRuleDefaultRuleStartRegex = new Regex("^[ \t]*All[ \t]*>=[ \t]*0[ \t]*:(.+)$", RegexOptions.Compiled);

        // use this regex to find rule is used for which word
        private static readonly Regex PolyRuleKeyLineRegex = new Regex("^[ \t]*CurW[ \t]*=[ \t]*\"(.+)\"[ \t]*;[ \t]*$", RegexOptions.Compiled);

        #endregion

        #region Methods

        /// <summary>
        /// Compile dat file.
        /// </summary>
        /// <param name="message">Compile result message.</param>
        /// <param name="compiledDatFile">Compiled dat file path.</param>
        /// <returns>Success or not.</returns>
        public static bool CompileAll(out string message, out string compiledDatFile)
        {
            compiledDatFile = LocalConfig.Instance.LangDataPath;
            try
            {
                string sdMsg = string.Empty;

                string configPath = LocalConfig.Instance.CompileConfigFilePath;
                string rawDataRootPath = LocalConfig.Instance.CompileConfigRawDataRootPath;
                string binRootPath = rawDataRootPath;
                string outputDir = LocalConfig.Instance.CompileConfigOutputDirPath;
                string reportPath = LocalConfig.Instance.CompileConfigReportPath;

                int sdExitCode = CommandLine.RunCommandWithOutputAndError(
                                                Util.LangDataCompilerPath,
                                                Helper.NeutralFormat("- config {0} -rawdatarootpath {1} -binrootpath {2} -outputDir {3} -report {4}", configPath, rawDataRootPath, binRootPath, outputDir, reportPath),
                                                Directory.GetCurrentDirectory(),
                                                ref sdMsg);

                if (sdExitCode == 0)
                {
                    message = Helper.NeutralFormat("Compile succeed.");

                    compiledDatFile = Path.Combine(outputDir, Helper.NeutralFormat(Util.DataFileNamePattern, LocalConfig.Instance.Lang));

                    return File.Exists(compiledDatFile);
                }
                else
                {
                    message = Helper.NeutralFormat("Failed to compile! {0}", sdMsg);
                    return false;
                }
            }
            catch (Exception e)
            {
                message = Helper.NeutralFormat("{0}. Failed to compile", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Compile CRF model.
        /// </summary>
        /// <param name="crfModelDir">Crf model folder.</param>
        /// <param name="lang">Language.</param>
        /// <param name="crfBinFile">Crf bin file path.</param>
        /// <returns>Success or not.</returns>
        public static bool CompileCRF(string crfModelDir, Language lang, out string crfBinFile)
        {
            MemoryStream outputStream = new MemoryStream();
            FileStream fs = null;
            try
            {
                Collection<string> addedFileNames = new Collection<string>();
                var errorSet = Microsoft.Tts.Offline.Compiler.CrfModelCompiler.Compile(crfModelDir, outputStream, addedFileNames, lang);

                if (errorSet != null && errorSet.Count > 0)
                {
                    foreach (var error in errorSet.Errors)
                    {
                        Helper.PrintSuccessMessage(error.ToString());
                    }

                    crfBinFile = null;
                    return false;
                }

                crfBinFile = Helper.GetTempFileName();

                fs = new FileStream(crfBinFile, FileMode.OpenOrCreate);

                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    bw.Write(outputStream.ToArray());
                }

                return File.Exists(crfBinFile);
            }
            catch
            {
                crfBinFile = null;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }

                if (outputStream != null)
                {
                    outputStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Compile general polurule and binary replace in data file.
        /// </summary>
        /// <param name="polyrRuleFile">Polyrule.txt file path.</param>
        /// <param name="compiledDatFile">New compiled dat file path.</param>
        /// <returns>Success or not.</returns>
        public static bool CompileGeneralPolyRule(string polyrRuleFile, out string compiledDatFile)
        {
            Helper.ThrowIfFileNotExist(polyrRuleFile);

            string tempFolder = Helper.GetTempFolderName();
            string tempGeneralRuleFile = Path.Combine(tempFolder, Util.TempGeneralPolyRuleFileName);
            string tempBinFile = Path.Combine(tempFolder, Util.TempGeneralPolyRuleBinFileName);

            compiledDatFile = Path.Combine(tempFolder, Path.GetFileName(LocalConfig.Instance.LangDataPath));

            // delete the existing data file
            Helper.ForcedDeleteFile(compiledDatFile);

            // delete the backup data file, LanguageDataHelper.ReplaceBinaryFile will genereate again
            string backFilePath = compiledDatFile + ".bak";
            Helper.ForcedDeleteFile(backFilePath);

            // copy the original data file to temp folder
            File.Copy(LocalConfig.Instance.LangDataPath, compiledDatFile, true);

            try
            {
                // split polyrule.txt
                Microsoft.Tts.Offline.Frontend.RuleFile ruleFile = new Microsoft.Tts.Offline.Frontend.RuleFile();
                ruleFile.Load(polyrRuleFile);

                // select general rule file
                var generalPolyRule = ruleFile.Split().FirstOrDefault(file => file.DomainTag == Microsoft.Tts.Offline.Core.DomainItem.GeneralDomain);

                generalPolyRule.Save(tempGeneralRuleFile);
                Helper.ThrowIfFileNotExist(tempGeneralRuleFile);

                string compilingArguments = Helper.NeutralFormat("\"{0}\" \"{1}\"", tempGeneralRuleFile, tempBinFile);

                string message = string.Empty;

                // use polycomp.exe compile general polyrule.txt bin file
                int exitCode = CommandLine.RunCommandWithOutputAndError(
                    Util.RuleCompilerPath, compilingArguments, null, ref message);

                if (exitCode == 0)
                {
                    Helper.ThrowIfFileNotExist(tempBinFile);

                    Microsoft.Tts.Offline.Compiler.LanguageData.LanguageDataHelper.ReplaceBinaryFile(
                        compiledDatFile,
                        tempBinFile,
                        Microsoft.Tts.Offline.Compiler.LanguageData.ModuleDataName.PolyphoneRule);

                    return File.Exists(compiledDatFile);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                compiledDatFile = null;
                return false;
            }
            finally
            {
                if (File.Exists(tempGeneralRuleFile))
                {
                    File.Delete(tempGeneralRuleFile);
                }

                if (File.Exists(tempBinFile))
                {
                    File.Delete(tempBinFile);
                }
            }
        }

        /// <summary>
        /// Update polyrule.txt for specific char
        /// Delete "All >= 0" line if polyrule.txt file contains
        /// polyrule.txt is like below, we should remove All >= 0 : "b eh_h i_l"; to make CRF model working
        /// CurW = "背";
        /// PrevW = "肩" : "b eh_h i_h";
        /// PrevW = "越" : "b eh_h i_h";
        /// All >= 0 : "b eh_h i_l";.
        /// </summary>
        /// <param name="filePath">Poly rule file path.</param>
        /// <param name="charName">Char name.</param>
        /// <returns>True if updated, false not changed.</returns>
        public static bool UpdatePolyRuleFile(string filePath, string charName)
        {
            Helper.ThrowIfFileNotExist(filePath);
            Helper.ThrowIfNull(charName);

            int lineNumber = 0;

            bool foundTargetChar = false;
            bool isNeedModify = false;
            bool currentCharHasDomainAttr = false;
            string currentChar;

            using (StreamReader reader = new StreamReader(filePath))
            {
                while (reader.Peek() > -1)
                {
                    ++lineNumber;

                    string lineContent = reader.ReadLine();

                    if (string.IsNullOrEmpty(lineContent))
                    {
                        continue;
                    }

                    if (DomainLineRegex.IsMatch(lineContent))
                    {
                        currentCharHasDomainAttr = true;
                    }
                    else if (PolyRuleKeyLineRegex.IsMatch(lineContent))
                    {
                        // if found next CurW="" line, don't need to modify the polyrule.txt
                        if (foundTargetChar)
                        {
                            isNeedModify = false;
                            break;
                        }

                        currentChar = PolyRuleKeyLineRegex.Match(lineContent).Groups[1].Value;

                        // if current rule is for training char and this is an general rule
                        if (string.Equals(currentChar, charName, StringComparison.Ordinal) &&
                            !currentCharHasDomainAttr)
                        {
                            foundTargetChar = true;
                        }

                        // assign to false, iterate next char
                        currentCharHasDomainAttr = false;
                    }
                    else if (foundTargetChar)
                    {
                        // update poly rule file the target char's general rule contains All >= 0
                        if (PolyRuleDefaultRuleStartRegex.IsMatch(lineContent))
                        {
                            isNeedModify = true;
                            break;
                        }
                    }
                }
            }

            if (isNeedModify)
            {
                string message;

                SdCommand.SdCheckoutFile(filePath, out message);
                Helper.PrintSuccessMessage(message);

                // remove All >= 0 line in poly rule file
                Util.EditLineInFile(filePath, lineNumber, null);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Load CRF model name mapping(model name and localized name).
        /// We always use "Being_used" attribute for the new crf model for test it.
        /// </summary>
        /// <example>
        /// The mapping txt file is like below:
        /// 差 -> cha.crf Being_used
        /// 长 -> chang.crf Being_used
        /// 当 -> dang.crf Being_used
        /// 行 -> hang.crf Being_used
        /// 系 -> xi.crf Unused.
        /// </example>
        /// <param name="filePath">Crf mapping File Path.</param>
        /// <param name="crfFileName">Crf file name.</param>
        /// <returns>Crf model files array, like bei.crf, wei.crf.</returns>
        // TODO:
        /*
        The implementation is too verbose, try this:
struct CRFModelMapping
{
       string char;
       string crfModelName;
       string status;

       CRFModelMapping(string line)
       {
                 line.split()
                 char = [0];
                 cfrModelName = [2];
                 status = [3];
       }

       override ToString()
       {
                return string.Format("{0}->{1}{2}");
       }
}
        */
        public static string[] UpdateCRFModelMappingFile(string filePath, string crfFileName)
        {
            Helper.ThrowIfFileNotExist(filePath);

            string message;
            SdCommand.SdCheckoutFile(filePath, out message);
            Helper.PrintSuccessMessage(message);

            List<string> crfFileNames = new List<string>();

            // line number start index is 1, next line will be read is 2
            int lineNumber = 1;
            bool needModify = true;
            bool charExist = false;

            using (StreamReader reader = new StreamReader(filePath))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    // if the first line matches, then continue to read
                    if (lineNumber == 1 &&
                        !string.Equals(Util.CRFMappingFileFirstLineContent, line))
                    {
                        throw new FormatException(Helper.NeutralFormat("{0} mapping file has the wrong format!", filePath));
                    }
                    else if (lineNumber > 1)
                    {
                        string[] mapping = line.Trim().Split('\t');

                        if (mapping.Length != 4)
                        {
                            throw new FormatException(Helper.NeutralFormat("{0} mapping file has the wrong format!", filePath));
                        }

                        string currentChar = mapping[0];
                        string currentCRFFile = mapping[2];
                        string currentUsingInfo = mapping[3];

                        // if current line's char is same with charName para, check whether need to modify this line
                        if (string.Equals(currentChar, LocalConfig.Instance.CharName))
                        {
                            // if crf file name and using info are same, don't modify
                            // else edit thie line
                            if (string.Equals(currentCRFFile, crfFileName) &&
                                string.Equals(currentUsingInfo, Util.CRFMappingFileBeingUsedValue))
                            {
                                needModify = false;
                            }
                            else
                            {
                                charExist = true;
                            }
                        }

                        crfFileNames.Add(currentCRFFile);
                    }
                    ++lineNumber;
                }
            }

            if (needModify)
            {
                string content = Helper.NeutralFormat("{0}\t->\t{1}\t{2}", LocalConfig.Instance.CharName, crfFileName, Util.CRFMappingFileBeingUsedValue);
                Util.EditLineInFile(filePath, lineNumber, content, !charExist);
            }

            return crfFileNames.ToArray();
        }

        #endregion
    }
}
