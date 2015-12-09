namespace CRFTrainingAuto
{
    using Microsoft.Tts.Offline.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public static class Util
    {
        #region Properties

        /// <summary>
        /// ProsodyModelTrainer.exe path, used to train crf model
        /// </summary>
        public static string ProsodyModelTrainerPath
        {
            get
            {
                string toolPath = Path.Combine(GlobalVar.Config.OfflineToolPath, "ProsodyModelTrainer.exe");

                return toolPath;
            }
        }

        /// <summary>
        /// FrontendMeasure.exe tool path, used to run test case
        /// </summary>
        public static string FrontendMeasurePath
        {
            get
            {
                // use the test version of FrontendMeasure.exe
                string toolPath = Path.Combine(GlobalVar.Config.BranchRootPath, @"target\distrib\debug\amd64\test\TTS\bin", "FrontendMeasure.exe");

                return toolPath;
            }
        }

        #endregion

        /// <summary>
        /// Merge all files to a single file
        /// </summary>
        /// <param name="wildcard">file path contains wildcard</param>
        /// <param name="saveFilePath">output file path</param>
        /// <returns>files.Length</returns>
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
        /// Splict large files and save to
        /// </summary>
        /// <param name="splitUnit">split unit, GB, MB, KB, Byte</param>
        /// <param name="intFlag">split size</param>
        /// <param name="inFilePath">input file path</param>
        /// <param name="outputDir">output folder</param>
        /// <returns>splict success fail or not</returns>
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

                    Byte[] tempBytes;

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
                            } while (!isEndLine);
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
        /// Get all files from a file path with wildcard such as "*" and "?"
        /// </summary>
        /// <param name="wildcard">the file path with wildcard</param>
        /// <returns>the files</returns>
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
        /// if path is config/training.config, convert to D:/config/training.config
        /// </summary>
        public static string GetAbsolutePath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                return Path.Combine(Environment.CurrentDirectory, path);
            }
            return path;
        }

        /// <summary>
        /// Create directory if not exist
        /// </summary>
        /// <param name="dirPath">directory path</param>
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
        /// Modify or insert new value for some line
        /// </summary>
        /// <param name="filePath">the file to be modified</param>
        /// <param name="lineNumber">line number</param>
        /// <param name="newLineValue">new value for line</param>
        /// <param name="insert">if true, new value will be inserted, if false, original value in this line will be replaced</param>
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
                        if (!insert)
                        {
                            reader.ReadLine();
                        }


                        sb.Append(newLineValue + Environment.NewLine);
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

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(sb.ToString());
            }
        }

        /// <summary>
        /// Set the console text color
        /// </summary>
        /// <param name="content">content</param>
        /// <param name="color">color, default green</param>
        public static void ConsoleOutTextColor(string content, ConsoleColor color = ConsoleColor.Green)
        {
            ConsoleColor consolePrevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(content);
            Console.ForegroundColor = consolePrevColor;
        }

        /// <summary>
        /// Get same file name but different extension
        /// </summary>
        /// <example>
        /// change D:\filename.txt to D:\filename.xls
        /// </example>
        /// <param name="fileName">file name</param>
        /// <param name="newExtension">new extension name</param>
        /// <returns></returns>
        public static string ChangeFileExtension(string filePath, string newExtension)
        {
            return Path.Combine(Path.GetDirectoryName(filePath),
                Path.GetFileNameWithoutExtension(filePath) + newExtension);
        }


        /// <summary>
        /// Get case and wb result from corpus file
        /// </summary>
        /// <param name="inputFilePath">txt corpus file path</param>
        /// <param name="hasWbResult">whether the file has word break</param>
        /// <returns></returns>
        public static IList<SentenceAndWbResult> GetSenAndWbFromCorpus(string inputFilePath, bool hasWbResult = true)
        {
            Helper.ThrowIfFileNotExist(inputFilePath);

            List<SentenceAndWbResult> results = new List<SentenceAndWbResult>();

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
                            results.Add(new SentenceAndWbResult
                            {
                                Content = wbResult.ConcatToString(),
                                WbResult = wbResult
                            });
                        }
                        else
                        {
                            throw new Exception(inputFilePath + " format is wrong!");
                        }
                    }
                    else
                    {
                        var wbResult = GlobalVar.WordBreaker.BreakWords(caseLine);
                        results.Add(new SentenceAndWbResult
                        {
                            Content = wbResult.ConcatToString(),
                            WbResult = wbResult
                        });
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Get case and pron from bug fixing file
        /// </summary>
        /// <param name="inputFilePath">bug fixing file path
        /// file format is like below
        /// 我还差你五元钱。	ch a_h a_l
        /// 我们离父母的希望还差很远。	ch a_h a_l
        /// </param>
        /// <param name="hasWbResult"></param>
        /// <returns></returns>
        public static IDictionary<string, string> GetSenAndPronFromBugFixingFile(string inputFilePath)
        {
            IDictionary < string, string> senAndProns = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(inputFilePath))
            {
                int lineNumber = 1;
                while (reader.Peek() > -1)
                {
                    string line = reader.ReadLine();
                    if(!string.IsNullOrEmpty(line))
                    {
                        string[] caseAndPron = line.Trim().Split(new char[] { '\t' });
                        if (caseAndPron.Length != 2)
                        {
                            throw new Exception(string.Format("{0} file at line {1} has the wrong format!", inputFilePath, lineNumber));
                        }

                        string sentence = caseAndPron[0];
                        if(string.IsNullOrEmpty(sentence))
                        {
                            throw new Exception(string.Format("{0} file at line {1} has the empty sentence!", inputFilePath, lineNumber));
                        }

                        if (sentence.GetSingleCharIndexOfLine(GlobalVar.Config.CharName, GlobalVar.WordBreaker) == -1)
                        {
                            throw new Exception(string.Format("{0} file at line {1} has the wrong sentence!", inputFilePath, lineNumber));
                        }

                        sentence = sentence.Trim();

                        string pinYinPron = caseAndPron[1];

                        if ((GlobalVar.Config.Prons.ContainsKey(pinYinPron) &&
                            !string.IsNullOrEmpty(GlobalVar.Config.Prons[pinYinPron])))
                        {
                            senAndProns.Add(sentence, pinYinPron);
                        }
                        else
                        {
                            throw new Exception(string.Format("{0} file at line {1} has the wrong pronunciation!", inputFilePath, lineNumber));
                        }
                    }
                    ++lineNumber;
                }
            }

            return senAndProns;
        }
    }
}
