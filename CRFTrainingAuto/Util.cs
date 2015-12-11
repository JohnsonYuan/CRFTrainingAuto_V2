//----------------------------------------------------------------------------
// <copyright file="Util.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Util
// </summary>
//----------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    public static class Util
    {
        #region internal Fields

        // Excel first column for case, second column for corrct pron
        internal const int ExcelCaseColIndex = 1;
        internal const string ExcelCaseColTitle = "case";
        internal const int ExcelCorrectPronColIndex = 2;
        internal const string ExcelCorrectPronColTitle = "correct pron";
        internal const int ExcelCommentColIndex = 3;
        internal const string ExcelCommentColTitle = "comment";
        internal const int ExcelWbColIndex = 4;
        internal const string ExcelWbColTitle = "wb result";

        // Use 1000 for N cross folder test
        // Temp folder store filtered corpus data
        internal const string TempFolderName = "temp";
        internal const string ExcelFileExtension = ".xls";
        internal const string TxtFileExtension = ".txt";

        internal const string CRFFileSearchExtension = "*.crf";
        internal const string CorpusTxtFileSearchPattern = "*.txt";
        internal const string XmlFileSearchExtension = "*.xml";
        internal const string CorpusTxtFileNamePattern = "corpus.{0}.txt";
        internal const string CorpusTxtAllFileName = "corpus.all.txt";
        internal const string CorpusExcelFileNamePattern = "corpus.{0}.xls";

        internal const string TrainingFolderName = "trainingScript";
        internal const string NCrossFolderName = "NCross";
        internal const string VerifyResultFolderName = "VerifyResult";
        internal const string FinalResultFolderName = "FinalResult";

        internal const string TrainingExcelFileName = "training.xls";
        internal const string TestingExcelFileName = "testing.xls";
        internal const string VerifyResultExcelFileName = "verifyResult.xls";

        internal const string TrainingConfigFileName = "training.config";
        internal const string TrainingConfigNamespace = "http://schemas.microsoft.com/tts/toolsuite";
        internal const string FeatureConfigFileName = "features.config";

        internal const int BugFixingXmlStartIndex = 1000000000;
        internal const string BugFixingFileName = "bugfixing.xml";
        internal const string BugFixingTestFileName = "bugfixingTest.xml";
        internal const string BugFixingTestLogFileName = "bugfixingTestlog.txt";
        internal const string ScriptFileName = "script.xml";
        internal const string TrainingFileName = "training.xml";
        internal const string TestFileName = "testing.xml";
        internal const string TestXmlNamespace = "http://schemas.microsoft.com/tts";
        internal const string TestCaseFileName = "Pron_Polyphony.xml";
        internal const string TestlogFileName = "testlog.txt";
        internal const string TestlogBeforeFileName = "testlog.before.txt";
        internal const string TestReportFileName = "NCrossTestReport.txt";

        #endregion

        #region Properties

        /// <summary>
        /// Gets ProsodyModelTrainer.exe path, used to train crf model.
        /// </summary>
        public static string ProsodyModelTrainerPath
        {
            get
            {
                string toolPath = Path.Combine(LocalConfig.Instance.OfflineToolPath, "ProsodyModelTrainer.exe");

                return toolPath;
            }
        }

        /// <summary>
        /// Gets FrontendMeasure.exe tool path, used to run test case.
        /// </summary>
        public static string FrontendMeasurePath
        {
            get
            {
                // use the test version of FrontendMeasure.exe
                string toolPath = Path.Combine(LocalConfig.Instance.BranchRootPath, @"target\distrib\debug\amd64\test\TTS\bin", "FrontendMeasure.exe");

                return toolPath;
            }
        }

        /// <summary>
        /// Gets polycomp.exe path, used to compile polyrule.txt.
        /// </summary>
        public static string RuleCompilerPath
        {
            get
            {
                string toolPath = Path.Combine(LocalConfig.Instance.OfflineToolPath, "polycomp.exe");

                return toolPath;
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Merge all files to a single file.
        /// </summary>
        /// <param name="wildcard">file path contains wildcard.</param>
        /// <param name="saveFilePath">output file path.</param>
        /// <returns>files.Length.</returns>
        public static int MergeFiles(string wildcard, string saveFilePath)
        {
            string[] files = GetAllFiles(wildcard);

            // if file less than 2 files, needn't merge
            if (files == null || files.Length == 0)
            {
                return 0;
            }
            else if (files.Length == 1)
            {
                File.Copy(files[0], saveFilePath, true);
                return 1;
            }

            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }

            FileStream fsOutput = null;
            FileStream fsInput = null;
            try
            {
                fsOutput = new FileStream(saveFilePath, FileMode.Append);

                foreach (var filePath in files)
                {
                    fsInput = new FileStream(filePath, FileMode.Open);

                    if (fsInput.Length > 0)
                    {
                        byte[] inputBytes = new byte[fsInput.Length];
                        fsInput.Read(inputBytes, 0, (int)fsInput.Length);
                        fsOutput.Write(inputBytes, 0, (int)fsInput.Length);
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                fsInput.Close();
                fsOutput.Close();
            }

            return files.Length;
        }

        /// <summary>
        /// Splict large files and save to.
        /// </summary>
        /// <param name="splitUnit">split unit, GB, MB, KB, Byte.</param>
        /// <param name="intFlag">split size.</param>
        /// <param name="inFilePath">input file path.</param>
        /// <param name="outputDir">output folder.</param>
        /// <returns>splict success fail or not.</returns>
        public static bool SplitFile(string splitUnit, int intFlag, string inFilePath, string outputDir)
        {
            bool suc = false;
            if (!File.Exists(inFilePath))
            {
                throw new FileNotFoundException(inFilePath + " not exist!");
            }

            int perFileSize = 0;
            switch (splitUnit.ToUpper())
            {
                case "BYTE":
                    perFileSize = intFlag;
                    break;
                case "KB":
                    perFileSize = 1024 * intFlag;
                    break;
                case "MB":
                    perFileSize = 1024 * 1024 * intFlag;
                    break;
                case "GB":
                    perFileSize = 1024 * 1024 * 1024 * intFlag;
                    break;
                default:
                    throw new Exception("splict unit is not correct!");
            }

            FileStream splitFileStream = null;
            FileStream tempStream = null;
            try
            {
                splitFileStream = new FileStream(inFilePath, FileMode.Open);

                int fileCount = (int)(splitFileStream.Length / perFileSize);

                if (splitFileStream.Length % perFileSize != 0)
                {
                    fileCount++;
                }

                using (BinaryReader splitFileReader = new BinaryReader(splitFileStream))
                {
                    splitFileStream = null;

                    byte[] tempBytes;

                    for (int i = 1; i <= fileCount; i++)
                    {
                        string tempFileName = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(inFilePath) + "." + i.ToString().PadLeft(6, '0') + Path.GetExtension(inFilePath));
                        tempStream = new FileStream(tempFileName, FileMode.OpenOrCreate);

                        using (BinaryWriter bw = new BinaryWriter(tempStream))
                        {
                            tempStream = null;

                            tempBytes = splitFileReader.ReadBytes(perFileSize);
                            bw.Write(tempBytes);

                            // if is not end line, we need read to end of this line
                            bool isEndLine = false;
                            do
                            {
                                if (splitFileReader.PeekChar() == -1 ||
                                    splitFileReader.PeekChar() == '\n')
                                {
                                    isEndLine = true;
                                }

                                bw.Write(splitFileReader.ReadByte());
                            }
                            while (!isEndLine);
                        }
                    }

                    suc = true;
                }
            }
            catch
            {
                suc = false;
            }
            finally
            {
                if (tempStream != null)
                {
                    tempStream.Dispose();
                }

                if (splitFileStream != null)
                {
                    splitFileStream.Dispose();
                }
            }

            return suc;
        }

        /// <summary>
        /// Get all files from a file path with wildcard such as "*" and "?".
        /// </summary>
        /// <param name="wildcard">the file path with wildcard.</param>
        /// <returns>the files.</returns>
        public static string[] GetAllFiles(string wildcard)
        {
            if (Path.IsPathRooted(wildcard))
            {
                return Directory.GetFiles(Path.GetDirectoryName(wildcard), Path.GetFileName(wildcard), SearchOption.AllDirectories);
            }
            else
            {
                return Directory.GetFiles(Environment.CurrentDirectory, wildcard, SearchOption.AllDirectories);
            }
        }

        /// <summary>
        /// if path is config/training.config, convert to D:/config/training.config.
        /// </summary>
        /// <param name="path">file path.</param>
        /// <returns>absolute path.</returns>
        public static string GetAbsolutePath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                return Path.Combine(Environment.CurrentDirectory, path);
            }

            return path;
        }

        /// <summary>
        /// Create directory if not exist.
        /// </summary>
        /// <param name="dirPath">directory path.</param>
        public static void CreateDirIfNotExist(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        /// <summary>
        /// Check file read-only attribute.
        /// if it's read-only, modify to read-write.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public static void DisableFileReadOnly(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (fi.IsReadOnly)
            {
                fi.IsReadOnly = false;
            }
        }

        /// <summary>
        /// Modify or insert new value for some line.
        /// </summary>
        /// <param name="filePath">the file to be modified.</param>
        /// <param name="lineNumber">line number.</param>
        /// <param name="newLineValue">new value for line, if null, remove this line.</param>
        /// <param name="insert">if true, new value will be inserted, if false, original value in this line will be replaced.</param>
        public static void EditLineInFile(string filePath, int lineNumber, string newLineValue, bool insert = true)
        {
            StringBuilder sb = new StringBuilder();

            int curLine = 1;
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (reader.Peek() > -1)
                {
                    if (lineNumber == curLine)
                    {
                        // if don't insert, just skip this line
                        if (!insert || newLineValue == null)
                        {
                            reader.ReadLine();
                        }
                        else
                        {

                            sb.Append(newLineValue + Environment.NewLine);
                        }
                    }
                    else
                    {
                        sb.Append(reader.ReadLine() + Environment.NewLine);
                    }
                    ++curLine;
                }
            }

            // current line shoud - 1
            --curLine;

            // if lineNumber large than current file's line, append blank line, and the new line
            if (lineNumber > curLine)
            {
                for (int i = 0; i < lineNumber - curLine - 1; i++)
                {
                    sb.AppendLine();
                }

                sb.AppendLine(newLineValue);
            }

            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.Unicode))
            {
                writer.Write(sb.ToString());
            }
        }

        /// <summary>
        /// Set the console text color.
        /// </summary>
        /// <param name="content">content.</param>
        /// <param name="color">color, default green.</param>
        public static void ConsoleOutTextColor(string content, ConsoleColor color = ConsoleColor.Green)
        {
            ConsoleColor consolePrevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(content);
            Console.ForegroundColor = consolePrevColor;
        }

        /// <summary>
        /// Get same file name but different extension.
        /// </summary>
        /// <example>
        /// change D:\filename.txt to D:\filename.xls.
        /// </example>
        /// <param name="filePath">file path.</param>
        /// <param name="newExtension">new extension name.</param>
        /// <returns>new file path.</returns>
        public static string ChangeFileExtension(string filePath, string newExtension)
        {
            return Path.Combine(
                Path.GetDirectoryName(filePath),
                Path.GetFileNameWithoutExtension(filePath) + newExtension);
        }

        /// <summary>
        /// Get case and wb result from corpus file.
        /// </summary>
        /// <param name="inputFilePath">txt corpus file path.</param>
        /// <param name="hasWbResult">whether the file has word break.</param>
        /// <returns>sentence and word break result.</returns>
        public static IList<SentenceAndWBResult> GetSenAndWbFromCorpus(string inputFilePath, bool hasWbResult = true)
        {
            Helper.ThrowIfFileNotExist(inputFilePath);

            List<SentenceAndWBResult> results = new List<SentenceAndWBResult>();

            WordBreaker wordBreaker = null;

            try
            {
                using (StreamReader reader = new StreamReader(inputFilePath))
                {
                    while (reader.Peek() > -1)
                    {
                        string caseLine = reader.ReadLine().Trim();

                        if (hasWbResult)
                        {
                            if (reader.Peek() > -1)
                            {
                                // we use the wbResult to generate the case, because we have to remove empty part
                                // in the case, and it's hard to list all space possibility, like space, tab, or unicode empty char(8195)
                                var wbResult = reader.ReadLine().SplitBySpace();
                                results.Add(new SentenceAndWBResult
                                {
                                    Content = wbResult.ConcatToString(),
                                    WBResult = wbResult
                                });
                            }
                            else
                            {
                                throw new Exception(inputFilePath + " format is wrong!");
                            }
                        }
                        else
                        {
                            wordBreaker = new WordBreaker(LocalConfig.Instance);
                            var wbResult = wordBreaker.BreakWords(caseLine);

                            results.Add(new SentenceAndWBResult
                            {
                                Content = wbResult.ConcatToString(),
                                WBResult = wbResult
                            });
                        }
                    }
                }
            }
            finally
            {
                if (wordBreaker != null)
                {
                    wordBreaker.Dispose();
                }
            }

            return results;
        }

        /// <summary>
        /// Get case and pron from bug fixing file.
        /// </summary>
        /// <param name="inputFilePath">Bug fixing file path
        /// file format is like below
        /// 我还差你五元钱。	ch a_h a_l
        /// 我们离父母的希望还差很远。	ch a_h a_l.
        /// </param>
        /// <returns>Dictionary contains case and pronunciation.</returns>
        public static Dictionary<string, string> GetSenAndPronFromBugFixingFile(string inputFilePath)
        {
            Dictionary<string, string> senAndProns = new Dictionary<string, string>();

            WordBreaker wordBreaker = new WordBreaker(LocalConfig.Instance);

            try
            {
                using (StreamReader reader = new StreamReader(inputFilePath))
                {
                    int lineNumber = 1;

                    while (reader.Peek() > -1)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] caseAndPron = line.Trim().Split(new char[] { '\t' });

                            if (caseAndPron.Length != 2)
                            {
                                throw new Exception(Helper.NeutralFormat("{0} file at line {1} has the wrong format!", inputFilePath, lineNumber));
                            }

                            string sentence = caseAndPron[0];

                            if (string.IsNullOrEmpty(sentence))
                            {
                                throw new Exception(Helper.NeutralFormat("{0} file at line {1} has the empty sentence!", inputFilePath, lineNumber));
                            }

                            if (sentence.GetSingleCharIndexOfLine(LocalConfig.Instance.CharName, wordBreaker) == -1)
                            {
                                throw new Exception(Helper.NeutralFormat("{0} file at line {1} has the wrong sentence!", inputFilePath, lineNumber));
                            }

                            sentence = sentence.Trim();

                            string pinYinPron = caseAndPron[1];

                            if (LocalConfig.Instance.Prons.ContainsKey(pinYinPron) &&
                                !string.IsNullOrEmpty(LocalConfig.Instance.Prons[pinYinPron]))
                            {
                                senAndProns.Add(sentence, LocalConfig.Instance.Prons[pinYinPron]);
                            }
                            else
                            {
                                throw new Exception(Helper.NeutralFormat("{0} file at line {1} has the wrong pronunciation!", inputFilePath, lineNumber));
                            }
                        }
                        ++lineNumber;
                    }
                }
            }
            finally
            {

                wordBreaker.Dispose();
            }


            return senAndProns;
        }

        #endregion
    }
}
