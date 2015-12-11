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
    /// Command line arguments.
    /// </summary>
    [Comment("CRF Training Automation tool let you train crf process more easyier.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "Reviewed.")]
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

        #region Enums

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
            ///  Generate excel report from frontmeasure.exe test result.
            /// </summary>
            GenXlsTestReport,

            /// <summary>
            ///  Generate NCrossData from excelr.
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
            /// Add bug fixing items to the new crf model and retrain the data.
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
        /// Input path could be a file path or a folder path depends on mode.
        /// </summary>
        public string InputPath
        {
            get { return _inputPath; }
        }

        /// <summary>
        /// Onput path could be a file path or a folder path depends on mode.
        /// </summary>
        public string OutputPath
        {
            get { return _outputPath; }
        }

        /// <summary>
        /// Word break result file path.
        /// </summary>
        public string WbPath
        {
            get { return _wordBreakPath; }
        }
        /// <summary>
        /// Config file path.
        /// </summary>
        public string ConfigPath
        {
            get { return _configPath; }
        }
        
        /// <summary>
        /// Split unit, GB, MB, KB, Byte.
        /// </summary>
        public string SplitUnit
        {
            get { return _splitUnit; }
        }
        
        /// <summary>
        /// Split file size.
        /// </summary>
        public int SplitSize
        {
            get { return _splitSize; }
        }


        /// <summary>
        /// Script start index, if it is number, then the start index plus 1, if it as an script file path, the start index will be the last item in the script plus 1.
        /// </summary>
        public string StartIndexOrFilePath
        {
            get { return _startIndexOrFilePath; }
        }

        /// <summary>
        /// whether need wrod break when genereate excel file.
        /// </summary>
        public int IsNeedWb
        {
            get { return _isNeedWb; }
        }

        #endregion

        /// <summary>
        /// Validate the arguments.
        /// </summary>
        /// <returns>error messages.</returns>
        public IEnumerable<string> Validate()
        {
            string msg = null;

            switch (Mode)
            {
                // make sure input and out folder all exist
                case ExecuteMode.FilterChar:
                case ExecuteMode.SS:
                    if (!IsDirectoryExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }

                    // check word break result folder if provided
                    if (!string.IsNullOrEmpty(_wordBreakPath) &&
                        !IsDirectoryExist(_wordBreakPath, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                case ExecuteMode.Compile:
                    if (!IsDirectoryExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                case ExecuteMode.WB:
                    if (!IsDirectoryExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                // the input should be txt file, output is excel file
                case ExecuteMode.GenXls:
                    if (!IsMatchFileExtension(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsMatchFileExtension(_outputPath, ExcelFileExtension, ref msg, false))
                    {
                        yield return msg;
                    }
                    break;
                // make sure input is excel file and output folder exist
                case ExecuteMode.GenVerify:
                case ExecuteMode.NCRF:
                    if (!IsMatchFileExtension(_inputPath, ExcelFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                case ExecuteMode.GenXlsTestReport:
                    if (!IsMatchFileExtension(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                // make sure input is excel file, output is xml file
                case ExecuteMode.GenTrain:
                case ExecuteMode.GenTest:
                    if (!IsMatchFileExtension(_inputPath, ExcelFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsMatchFileExtension(_outputPath, XmlFileExtension, ref msg, false))
                    {
                        yield return msg;
                    }
                    break;
                case ExecuteMode.BugFixing:
                    // use txt file for bugfixing, the format is like this
                    // 我还差你五元钱。	cha4
                    // 我们离父母的希望还差很远。	cha4
                    if (!IsMatchFileExtension(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                case ExecuteMode.Rand:
                    if (!IsMatchFileExtension(_inputPath, TxtFileExtension, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                case ExecuteMode.Split:
                    if (!IsFileExist(_inputPath, ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryExist(_outputPath, ref msg))
                    {
                        yield return msg;
                    }
                    break;
                case ExecuteMode.Merge:
                    // make sure the directory exist
                    if (!IsDirectoryExist(Path.GetDirectoryName(_inputPath), ref msg))
                    {
                        yield return msg;
                    }

                    if (!IsDirectoryExist(Path.GetDirectoryName(_outputPath), ref msg))
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
        /// <param name="filePath">filePath.</param>
        /// <param name="msg">error message.</param>
        /// <returns>true or false.</returns>
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
        /// Error message if directory not exist.
        /// </summary>
        /// <param name="dirPath">directory.</param>
        /// <param name="msg">error message.</param>
        /// <returns>true or false.</returns>
        private bool IsDirectoryExist(string dirPath, ref string msg)
        {
            if (string.IsNullOrEmpty(dirPath))
            {
                msg = "The directory is empty!";
                return false;
            }

            if (!Directory.Exists(dirPath))
            {
                msg = Helper.NeutralFormat("{0} is not a directory or it doesn't exist!", dirPath);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Error message for if file not match the required extension.
        /// </summary>
        /// <param name="filePath">file path.</param>
        /// <param name="extension">required extension.</param>
        /// <param name="msg">error message.</param>
        /// <returns>true or false.</returns>
        private bool IsMatchFileExtension(string filePath, string extension, ref string msg, bool checkExist = true)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                msg = "The file path is empty!";
                return false;
            }

            if (checkExist && !File.Exists(filePath))
            {
                msg = Helper.NeutralFormat("{0} doesn't exist!", filePath);
                return false;
            }
            else if (!string.Equals(Path.GetExtension(filePath), extension))
            {
                msg = Helper.NeutralFormat("{0} is not a {1} file!", filePath, extension.Substring(extension.IndexOf(".") + 1));
                return false;
            }
            return true;
        }
    }
}
