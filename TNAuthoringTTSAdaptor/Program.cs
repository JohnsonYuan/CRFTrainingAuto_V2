namespace TNAuthoringTTSAdaptor
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler;
    using Microsoft.Tts.Offline.Utility;

    static class Program
    {
        private static int Main(string[] args)
        {
            return ConsoleApp<Arguments>.Run(args, Process);
        }

        private static int Process(Arguments args)
        {
            int ret = ExitCode.NoError;
            ErrorSet errorSet = new ErrorSet();

            LocalArguments localArgs = LocalArguments.ConvertToLocalArguments(args);

            BuildData(localArgs, errorSet);

            printLog(localArgs.LogFilePath, errorSet);

            return ret;
        }

        #region Compile TNML
        /// <summary>
        /// Tnml rule compiler
        /// </summary>
        /// <param name="localArgs">local arguments</param>
        /// <param name="errorSet">error set</param>
        private static void CompileTNML(LocalArguments localArgs, ErrorSet errorSet)
        {
            Debug.Assert(localArgs != null && errorSet != null);
            CheckFileExists(localArgs.TnmlFilePath, "Tnml", errorSet, DataCompilerError.RawDataNotFound);
            CheckFileExists(localArgs.TnmlCompiler, "TNML Compiler", errorSet, DataCompilerError.ToolNotFound);

            String compilingArguments = Helper.NeutralFormat(
                "-lcid {0} -tnml \"{1}\" -tnbin \"{2}\" -mode TTS -norulename FALSE",
                (uint)localArgs.Lang, localArgs.TnmlFilePath, localArgs.BinaryTNRule);

            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                String message = string.Empty;
                Int32 exitCode = CommandLine.RunCommandWithOutputAndError(
                    localArgs.TnmlCompiler, compilingArguments, LocalArguments.WorkingDirectory, ref message);

                if (!string.IsNullOrEmpty(message))
                {
                    if (exitCode == 0)
                    {
                        errorSet.Add(DataCompilerError.CompilingLogWithDataName, "Compile TN rule", message);
                    }
                    else
                    {
                        errorSet.Add(DataCompilerError.CompilingLogWithError, "Compile TN rule", message);
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Build data with raw full data and latest tnml file.
        /// </summary>
        /// <param name="localArgs">local arguments</param>
        /// <param name="sorted">sort langugage data</param>
        private static void BuildData(LocalArguments localArgs, ErrorSet errorSet)
        {
            Debug.Assert(localArgs != null && errorSet != null);

            CompileTNML(localArgs, errorSet);

            CheckFileExists(localArgs.LangDataFilePath, "Language dat", errorSet, DataCompilerError.RawDataNotFound);

            // if file is read-only, chmod to read-write.
            DisableFileReadOnly(localArgs.LangDataFilePath);
            CheckFileExists(localArgs.BinaryTNRule, "TN Rule binary", errorSet, DataCompilerError.RawDataNotFound);
            
            if (!errorSet.Contains(ErrorSeverity.MustFix))
            {
                Microsoft.Tts.Offline.Compiler.LanguageData.LanguageDataHelper.ReplaceBinaryFile(
                    localArgs.LangDataFilePath,
                    localArgs.BinaryTNRule,
                    Microsoft.Tts.Offline.Compiler.LanguageData.ModuleDataName.TnRule);
            }
        }
        
        /// <summary>
        /// Check whether the file  existed.
        /// </summary>
        /// <param name="path">file path</param>
        /// <param name="fileName">file name</param>
        /// <param name="errorSet">error set</param>
        private static void CheckFileExists(String path, String fileName, ErrorSet errorSet, DataCompilerError error)
        {
            if (errorSet == null)
            {
                throw new ArgumentNullException("errorSet");
            }

            if (String.IsNullOrEmpty(path))
            {
                errorSet.Add(DataCompilerError.PathNotInitialized, fileName, path);
            }
            else if (!File.Exists(path))
            {
                errorSet.Add(error, fileName, path);
            }
        }

        /// <summary>
        /// Print the log into file
        /// </summary>
        /// <param name="logFilePath">path of log file</param>
        /// <param name="title">title</param>
        /// <param name="errorSet">error set</param>
        private static void printLog(String logFilePath, ErrorSet errorSet)
        {
            if (!String.IsNullOrEmpty(logFilePath))
            {
                using (TextWriter writer = new StreamWriter(logFilePath, true, Encoding.Unicode))
                {
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
        /// Check file read-only attribute.
        /// if it's read-only, modify to read-write.
        /// </summary>
        /// <param name="filePath">File path.</param>
        private static void DisableFileReadOnly(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (fi.IsReadOnly)
            {
                fi.IsReadOnly = false;
            }
        }
    }
}
