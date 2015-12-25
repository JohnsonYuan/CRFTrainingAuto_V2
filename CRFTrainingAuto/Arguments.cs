//----------------------------------------------------------------------------
// <copyright file="Arguments.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Command line arguments
// </summary>
//----------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Execute mode.
    /// </summary>
    public enum ExecuteMode
    {
        /// <summary>
        ///  Generate txt file contains single training char from folder.
        /// </summary>
        FilterChar,

        /// <summary>
        /// Compile and run test.
        /// </summary>
        Compile,

        /// <summary>
        ///  Generate excel report verify excel.
        /// </summary>
        GenVerify,

        /// <summary>
        ///  Generate excel report from front measure test result.
        /// </summary>
        GenXlsTestReport,

        /// <summary>
        ///  Generate NCrossData from excel.
        /// </summary>
        NCRF,

        /// <summary>
        /// Generate excel from txt file, it's often called by internal functions.
        /// </summary>
        GenXls,

        /// <summary>
        /// Generate training script.
        /// </summary>
        GenTrain,

        /// <summary>
        /// Generate test case script.
        /// </summary>
        GenTest,

        /// <summary>
        /// Add bug fixing items to the new $$crf$$ model and retrain the data.
        /// </summary>
        BugFixing,

        /// <summary>
        /// Word break.
        /// </summary>
        WB,

        /// <summary>
        /// Sentence separate.
        /// </summary>
        SS,

        /// <summary>
        /// Split file.
        /// </summary>
        Split,

        /// <summary>
        /// Merge files to one file.
        /// </summary>
        Merge,

        /// <summary>
        /// Random select cases.
        /// </summary>
        Rand,
    }

    /// <summary>
    /// Command line arguments.
    /// </summary>
    public class Arguments
    {
        #region constant fields

        private const string ExcelFileExtension = ".xls";
        private const string TxtFileExtension = ".txt";
        private const string XmlFileExtension = ".xml";

        #endregion

        #region Fields used by command line parser

        [Argument("mode", Description = "Specifies the execute mode: FilterChar, CompileAndTest, NCRF, GenVerify, GenXlsTestReport, GenXls, GenTrain, GenTest, BugFixing, WB, SS, Split, Merge",
            Optional = false, UsagePlaceholder = "executeMode", RequiredModes = "FilterChar, NCRF, GenVerify, GenXlsTestReport, Compile, GenXls, GenTrain, GenTest, BugFixing, WB, SS, Split, Merge, Rand")]
        private string _mode = string.Empty;

        [Argument("config", Description = "config file path", Optional = false, UsagePlaceholder = "configPath", RequiredModes = "FilterChar, NCRF, GenVerify, GenXlsTestReport, Compile, GenXls, GenTrain, GenTest, BugFixing, WB, SS, Merge, Rand")]
        private string _configPath = string.Empty;

        [Argument("i", Description = "input path",
            Optional = true, UsagePlaceholder = "inputPath", RequiredModes = "FilterChar, NCRF, GenVerify, Compile, GenXlsTestReport, GenXls, GenTrain, GenTest, BugFixing, WB, SS, Split, Merge, Rand")]
        private string _inputPath = string.Empty;

        [Argument("o", Description = "output path",
            Optional = true, UsagePlaceholder = "outputPath", RequiredModes = "FilterChar, NCRF, GenVerify, GenXls, GenTrain, GenTest, BugFixing, WB, SS, Split, Merge, Rand")]
        private string _outputPath = string.Empty;

        [Argument("wbFolder", Description = "word break result folder",
            Optional = true, UsagePlaceholder = "wbPath", OptionalModes = "FilterChar, WB")]
        private string _wordBreakPath = string.Empty;

        [Argument("u", Description = "split unit(GB, MB, KB, Byte)",
            Optional = false, UsagePlaceholder = "splitunit", RequiredModes = "Split")]
        private string _splitUnit = string.Empty;

        [Argument("size", Description = "split size",
            Optional = false, UsagePlaceholder = "splitsize", RequiredModes = "Split")]
        private int _splitSize = 0;

        [Argument("startIndex", Description = "script index for generating script, default 0, if not supply, will start with 1",
            Optional = true, UsagePlaceholder = "startIndex", OptionalModes = "GenTrain")]
        private string _startIndexOrFilePath = "0";

        [Argument("needWb", Description = "if the txt file doesn't contain the word breaker result, this shoud be 1, default 0",
            Optional = true, UsagePlaceholder = "needWb", OptionalModes = "GenXls")]
        private int _isNeedWb = 0;

        #endregion

        #region Properties used by command line parser

        /// <summary>
        /// Gets program execute mode.
        /// </summary>
        public ExecuteMode Mode
        {
            get { return (ExecuteMode)Enum.Parse(typeof(ExecuteMode), _mode, true); }
        }

        /// <summary>
        /// Gets input path could be a file path or a folder path depends on mode.
        /// </summary>
        public string InputPath
        {
            get { return _inputPath; }
        }

        /// <summary>
        /// Gets output path could be a file path or a folder path depends on mode.
        /// </summary>
        public string OutputPath
        {
            get { return _outputPath; }
        }

        /// <summary>
        /// Gets word break result file path.
        /// </summary>
        public string WbPath
        {
            get { return _wordBreakPath; }
        }

        /// <summary>
        /// Gets config file path.
        /// </summary>
        public string ConfigPath
        {
            get { return _configPath; }
        }

        /// <summary>
        /// Gets split unit, GB, MB, KB or Byte.
        /// </summary>
        public string SplitUnit
        {
            get { return _splitUnit; }
        }

        /// <summary>
        /// Gets split file size.
        /// </summary>
        public int SplitSize
        {
            get { return _splitSize; }
        }

        /// <summary>
        /// Gets script start index, if it is number, then the start index plus 1, if it as an script file path, the start index will be the last item in the script plus 1.
        /// </summary>
        public string StartIndexOrFilePath
        {
            get { return _startIndexOrFilePath; }
        }

        /// <summary>
        /// Gets whether need word break when generate excel file.
        /// </summary>
        public int IsNeedWb
        {
            get { return _isNeedWb; }
        }

        #endregion

        /// <summary>
        /// Validate the arguments.
        /// </summary>
        /// <returns>Error messages.</returns>
        public IEnumerable<string> Validate()
        {
            string msg = null;

            switch (Mode)
            {
                // make sure input and out folder all exist
                case ExecuteMode.FilterChar:
                case ExecuteMode.SS:
                    if (!IsDirectoryOrFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryOrFileExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }

                    // check word break result folder if provided
                    if (!string.IsNullOrEmpty(_wordBreakPath) &&
                        !IsDirectoryOrFileExist(_wordBreakPath, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.Compile:
                    if (!IsDirectoryOrFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.WB:
                    if (!IsDirectoryOrFileExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.GenXls:

                    if (!IsDirectoryOrFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    // the input should be txt file, output is excel file
                    if (!IsFileExtensionValid(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsFileExtensionValid(_outputPath, ExcelFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.GenVerify:
                case ExecuteMode.NCRF:

                    if (!IsDirectoryOrFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    // make sure input is excel file and output folder exist
                    if (!IsFileExtensionValid(_inputPath, ExcelFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryOrFileExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.GenXlsTestReport:

                    if (!IsFileExtensionValid(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.GenTrain:
                case ExecuteMode.GenTest:

                    if (!IsDirectoryOrFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    // make sure input is excel file, output is xml file
                    if (!IsFileExtensionValid(_inputPath, ExcelFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsFileExtensionValid(_outputPath, XmlFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.BugFixing:

                    if (!IsDirectoryOrFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    // use txt file for bugfixing, the format is like this
                    // 我还差你五元钱。->cha4
                    // 我们离父母的希望还差很远。->cha4
                    if (!IsFileExtensionValid(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.Rand:

                    if (!IsDirectoryOrFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsFileExtensionValid(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryOrFileExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.Split:
                    if (!IsFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryOrFileExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }

                    break;
                case ExecuteMode.Merge:
                    // make sure the directory exist
                    if (!IsDirectoryOrFileExist(Path.GetDirectoryName(_inputPath), ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryOrFileExist(Path.GetDirectoryName(_outputPath), ref msg))
                    {
                        yield return msg;
                    }

                    break;
            }

            // check config file if specified
            if (!string.IsNullOrEmpty(_configPath))
            {
                _configPath = Util.GetAbsolutePath(_configPath);
                if (!File.Exists(_configPath))
                {
                    yield return Helper.NeutralFormat("{0} not exist!", _configPath);
                }
            }
        }

        /// <summary>
        /// Error message if file not exist.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="msg">Error message.</param>
        /// <returns>True or false.</returns>
        private bool IsFileExist(string filePath, ref string msg)
        {
            if (!File.Exists(filePath))
            {
                msg = Helper.NeutralFormat("{0} doesn't exist!", filePath);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check directory or file whether exist.
        /// </summary>
        /// <param name="path">Folder or file path.</param>
        /// <param name="msg">Error message.</param>
        /// <returns>True or false.</returns>
        private bool IsDirectoryOrFileExist(string path, ref string msg)
        {
            if (string.IsNullOrEmpty(path))
            {
                msg = "The path is empty!";
                return false;
            }

            if (!Directory.Exists(path)
            && !File.Exists(path))
            {
                msg = Helper.NeutralFormat("{0} doesn't exist!", path);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Error message for if file not match the required extension.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="extension">Required extension.</param>
        /// <param name="msg">Error message.</param>
        /// <returns>True or false.</returns>
        private bool IsFileExtensionValid(string filePath, string extension, ref string msg)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                msg = "The file path is empty!";
                return false;
            }

            if (!string.Equals(Path.GetExtension(filePath), extension))
            {
                msg = Helper.NeutralFormat("{0} is not a {1} file!", filePath, extension.Substring(extension.IndexOf(".") + 1));

                return false;
            }

            return true;
        }
    }
}
