namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Excel = Microsoft.Office.Interop.Excel;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Collections.ObjectModel;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Sentence and word break result struct
    /// </summary>
    public struct SentenceAndWbResult
    {
        public string Content { get; set; }
        public string[] WbResult { get; set; }
    }

    /// <summary>
    /// CRF Helper
    /// </summary>
    public class CRFHelper
    {
        #region Fields

        private object _locker = new object();
        private static int ProcessedFileCount = 0;

        #endregion

        #region Methods

        /// <summary>
        /// Step 1: Generate txt file contains single training char from inFolder
        /// First generate each file corresponding file to temp folder
        /// and then merge them, random select maxCount data
        /// </summary>
        /// <param name="inputDir">corpus folder</param>
        /// <param name="outputDir">output folder</param>
        /// <param name="wbDir">corpus word break result folder</param>
        public void PrepareTrainTestSet(string inputDir, string outputDir, string wbDir = null)
        {
            if (!Directory.Exists(inputDir))
            {
                throw new Exception(inputDir + " not exists!");
            }

            string[] inFilePaths = Directory.GetFiles(inputDir, Util.CorpusTxtFileSearchPattern);
            string tempFolder = Path.Combine(outputDir, Util.TempFolderName);

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            Task[] tasks;
            if (inFilePaths.Length <= LocalConfig.Instance.MaxThreadCount)
            {
                tasks = new Task[inFilePaths.Length];
            }
            else
            {
                tasks = new Task[LocalConfig.Instance.MaxThreadCount];
            }

            Util.ConsoleOutTextColor("Start filtering");

            for (int i = 0; i < tasks.Length; i++)
            {
                string[] filesToProcess = inFilePaths.Where((input, index) => (index % LocalConfig.Instance.MaxThreadCount == i)).ToArray();

                // start each task nad show process info when this task complete
                tasks[i] = Task.Factory.StartNew(() =>
                        {
                            SelectTargetChar(filesToProcess, tempFolder, wbDir);
                        }
                    ).ContinueWith((ancient) =>
                    {
                        Console.WriteLine(string.Format("Processed {0} files, total {1} files", ProcessedFileCount, inFilePaths.Length));
                    });
            }

            Task.WaitAll(tasks);

            // clear counter
            ProcessedFileCount = 0;

            MergeAndRandom(tempFolder, outputDir);
        }

        /// <summary>
        /// Select target char from files
        /// if supply word break result file, then we don't need to use wordbreaker, it's more faster
        /// input file might like:羊城晚报记者林本剑本文来源：金羊网-羊城晚报
        /// word break file might like:羊城 晚报 记者 林 本 剑 本文 来源 ： 金 羊 网 - 羊城 晚报
        /// </summary>
        /// <param name="fileProcessed">files corpus</param>
        /// <param name="outputFolder">temp folder</param>
        /// <param name="wbDir">word break result folder</param>
        private void SelectTargetChar(string[] fileProcessed, string outputDir, string wbDir = null)
        {
            bool useWbResult = false;
            if (!string.IsNullOrEmpty(wbDir) &&
                Directory.Exists(wbDir))
            {
                useWbResult = true;
            }

            WordBreaker wordBreaker = null;

            if (!useWbResult)
            {
                wordBreaker = new WordBreaker(LocalConfig.Instance);
            }

            foreach (string filePath in fileProcessed)
            {
                string fileName = Path.GetFileName(filePath);

                // if the filtered file already exist, skip this file
                string tempFilePath = Path.Combine(outputDir, fileName);

                if (File.Exists(tempFilePath))
                {
                    lock (_locker)
                    {
                        ++ProcessedFileCount;
                    }

                    Console.WriteLine("File " + fileName + " exist, skipped!");
                    continue;
                }

                HashSet<string> results = new HashSet<string>();

                int foundCount = 0;

                StreamReader fileReader = null;
                // reader for word break result file
                StreamReader wbReader = null;

                try
                {
                    fileReader = new StreamReader(filePath);

                    string wbFilePath = Path.Combine(wbDir, fileName);
                    if (useWbResult &&
                        File.Exists(wbFilePath))
                    {
                        wbReader = new StreamReader(wbFilePath);
                    }
                    else
                    {
                        useWbResult = false;
                    }

                    while (fileReader.Peek() > -1)
                    {
                        #region filter data
                        // remove the empty space, if not, it will get the wrong index when using WordBraker to get the char index
                        string sentence = fileReader.ReadLine().Trim().Replace(" ", "").Replace("\t", "");

                        bool isSentenceMatch = false;
                        string[] wbResult = null;

                        // check the case's length
                        if (string.IsNullOrEmpty(sentence) ||
                            sentence.Length < LocalConfig.Instance.MinCaseLength)
                        {
                            continue;
                        }

                        // if results donesn't contains the curSentence and curSentence contains only one single char
                        // then add curSentence to results
                        if (useWbResult &&
                            wbReader.Peek() > -1)
                        {
                            wbResult = wbReader.ReadLine().SplitBySpace();

                            if (!results.Contains(sentence) &&
                                   sentence.GetSingleCharIndexOfLine(LocalConfig.Instance.CharName, wbResult) > -1)
                            {
                                isSentenceMatch = true;
                            }
                        }
                        else
                        {
                            if (wordBreaker == null)
                            {
                                wordBreaker = new WordBreaker(LocalConfig.Instance);
                            }

                            if (!results.Contains(sentence) &&
                                sentence.GetSingleCharIndexOfLine(LocalConfig.Instance.CharName, wordBreaker, out wbResult) > -1)
                            {
                                isSentenceMatch = true;
                            }
                        }

                        if (isSentenceMatch)
                        {
                            results.Add(sentence);
                            // add the word break result
                            results.Add(wbResult.SpaceSeparate());

                            ++foundCount;
                            // show searching progress, i is start with 0, so ShowTipCount should minus 1
                            // if ShowTipCount = 5000, when i = 4999, 5000 cases searched
                            if (LocalConfig.Instance.ShowTipCount > 0 &&
                                foundCount >= LocalConfig.Instance.ShowTipCount &&
                                foundCount % LocalConfig.Instance.ShowTipCount == 0)
                            {
                                Console.WriteLine(string.Format("Search {0} in {1}", foundCount, fileName));
                            }
                        }

                        #endregion
                    }

                    // save each file to Temp folder, cause the total file is so large, then merge
                    File.WriteAllLines(tempFilePath, results);

                    lock (_locker)
                    {
                        ++ProcessedFileCount;
                    }

                    Console.WriteLine(string.Format("Found {0} results in file {1}.", foundCount, fileName));
                }
                finally
                {
                    if (fileReader != null)
                    {
                        fileReader.Dispose();
                    }
                    if (wbReader != null)
                    {
                        wbReader.Dispose();
                    }

                    if(wordBreaker != null)
                    {
                        wordBreaker.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Generate maxCount(default 1500) random data from inFilePath
        /// e.g. generate file corpus.1500.txt</summary>
        /// <param name="inFilePath">intput file path</param>
        /// <param name="outputDir">output folder</param>
        /// <returns>generated excel file path, return null if not success</returns>
        public string SelectRandomCorpus(string inFilePath, string outputDir)
        {
            var inputs = Util.GetSenAndWbFromCorpus(inFilePath);

            // check whether can random select
            if (inputs.Count < LocalConfig.Instance.MaxCaseCount)
            {
                return null;
            }

            HashSet<string> results = new HashSet<string>();
            Random rand = new Random();

            // results should contains LocalConfig.Instance.MaxCaseCount * 2 lines
            while (results.Count < LocalConfig.Instance.MaxCaseCount * 2)
            {
                int index = rand.Next(0, inputs.Count);

                var selectTarget = inputs[index];
                string curSentence = selectTarget.Content;

                if (!results.Contains(curSentence))
                {
                    results.Add(curSentence);

                    // remove this item from input also
                    inputs.Remove(selectTarget);
                }
            }

            // save to txt file, it's easyier to view cases
            string randomTxtFilePath = Path.Combine(outputDir, string.Format(Util.CorpusTxtFileNamePattern, LocalConfig.Instance.MaxCaseCount));
            File.WriteAllLines(randomTxtFilePath, results);

            // generate the excel file
            string outputExcelFilePath = Path.Combine(outputDir, string.Format(Util.CorpusExcelFileNamePattern, LocalConfig.Instance.MaxCaseCount));
            try
            {
                ExcelHelper.GenExcelFromTxtFile(randomTxtFilePath, outputExcelFilePath);
            }
            catch (Exception)
            {
                return null;
            }

            return outputExcelFilePath;
        }

        ///<summary>
        /// Step 2: Generate NCrossData from excel
        /// First divide excelFile using N cross folder
        /// each folder contains 900 cases training.xls and 100 cases testing.xls
        /// and then generate training and test script from corresponding excel files
        /// </summary>
        /// <param name="excelFile">excel file path</param>
        /// <param name="outputDir">output folder</outputDir>
        public void GenNCrossData(string excelFile, string outputDir)
        {
            // divide corpus to training and test part, use 
            string trainingExcelFilePath;
            string testExcelFilePath;

            ExcelHelper.DivideExcelCorpus(excelFile, outputDir, out trainingExcelFilePath, out testExcelFilePath);

            Helper.ThrowIfNull(trainingExcelFilePath);
            Helper.ThrowIfNull(testExcelFilePath);

            #region put the 1000 training, and 500 test case to FinalResultFolder

            string finalFolder = Path.Combine(outputDir, Util.FinalResultFolderName);
            if (!Directory.Exists(finalFolder))
            {
                Directory.CreateDirectory(finalFolder);
            }

            string finalTrainingFolder = Path.Combine(finalFolder, Util.TrainingFolderName);
            if (!Directory.Exists(finalTrainingFolder))
            {
                Directory.CreateDirectory(finalTrainingFolder);
            }

            // generate scirpt.xml, script.xml = training.xml + testing.xml
            ScriptGenerator.GenScript(excelFile, GenerateAction.TrainingScript, finalFolder, Util.ScriptFileName);
            // generate training.xml
            ScriptGenerator.GenScript(trainingExcelFilePath, GenerateAction.TrainingScript, finalTrainingFolder, Util.TrainingFileName);
            // generate testing.xml
            ScriptGenerator.GenScript(testExcelFilePath, GenerateAction.TrainingScript, finalFolder, Util.TestFileName);

            // generate test cases to Pron_Polyphony.xml, used to put it to testcase folder
            ScriptGenerator.GenScript(testExcelFilePath, GenerateAction.TestCase, finalFolder, Util.TestCaseFileName);

            // comple and run test
            CompileAndTestInFolder(finalFolder);
            #endregion

            // save the n cross test data to NCrossFolder
            string nCrossFolder = Path.Combine(outputDir, Util.NCrossFolderName);
            // divide training excel file corpus to 10 separate testing and training part
            Util.ConsoleOutTextColor("Start N Cross excel " + trainingExcelFilePath);
            ExcelHelper.GenNCrossExcel(trainingExcelFilePath, nCrossFolder);
            Util.ConsoleOutTextColor("End N Cross excel " + trainingExcelFilePath);

            string[] testlogPaths = new string[LocalConfig.Instance.NFolderCount];

            for (int i = 1; i <= LocalConfig.Instance.NFolderCount; i++)
            {
                string destDir = Path.Combine(nCrossFolder, i.ToString());
                testlogPaths[i - 1] = Path.Combine(destDir, Util.TestlogFileName);

                // compile and run test in each folder
                try
                {
                    CompileAndTestInFolder(destDir);
                }
                catch (Exception ex)
                {
                    Util.ConsoleOutTextColor(ex.Message, ConsoleColor.Red);
                    return;
                }
            }

            // generate report based NCross test log
            string testReportPath = Path.Combine(outputDir, Util.TestReportFileName);
            GenNCrossTestReport(testlogPaths, testReportPath);
            Util.ConsoleOutTextColor("Generate test report " + testReportPath);
        }

        /// <summary>
        /// Verify the excel file's pron by compile and test all cases in excel
        /// </summary>
        /// <param name="excelFile">excel file path</param>
        /// <param name="outputDir">output folder</param>
        public void GenVerifyResult(string excelFile, string outputDir)
        {
            // put the result to VerifyResultFolder
            string verifyResultFolder = Path.Combine(outputDir, Util.VerifyResultFolderName);

            // Verify the excel file's pron by compile and test all cases in excel
            if (!Directory.Exists(verifyResultFolder))
            {
                Directory.CreateDirectory(verifyResultFolder);
            }

            // bacause training script should put in one folder, so we put it in TrainingFolder
            string trainingFolder = Path.Combine(verifyResultFolder, Util.TrainingFolderName);

            if (!Directory.Exists(trainingFolder))
            {
                Directory.CreateDirectory(trainingFolder);
            }

            Util.ConsoleOutTextColor(string.Format("Start verify excel {0}, result will be saved to {1}.", excelFile, verifyResultFolder));

            ScriptGenerator.GenScript(excelFile, GenerateAction.TrainingScript, trainingFolder, Util.TrainingFileName);
            ScriptGenerator.GenScript(excelFile, GenerateAction.TestCase, verifyResultFolder, Util.TestCaseFileName);
            CompileAndTestInFolder(verifyResultFolder, true);
        }

        /// <summary>
        /// Merge files and random select
        /// </summary>
        /// <param name="inputDir">input folder</param>
        /// <param name="outputDir">output folder</param>
        public void MergeAndRandom(string inputDir, string outputDir)
        {
            Util.ConsoleOutTextColor("Start merge files in " + inputDir + " !");

            // e.g. corpusAllFilePath = "corpus.all.txt"
            string corpusAllFilePath = Path.Combine(outputDir, Util.CorpusTxtAllFileName);
            // merge all files in temp folder
            int mergedFileCount = Util.MergeFiles(Path.Combine(inputDir, Util.CorpusTxtFileSearchPattern), corpusAllFilePath);

            if (mergedFileCount == 0)
            {
                Util.ConsoleOutTextColor("No data generated, ternimated!");
                return;
            }

            Util.ConsoleOutTextColor(string.Format("All cases saved to {0}.", corpusAllFilePath));

            Util.ConsoleOutTextColor(string.Format("Start random select {0} cases from {1}", LocalConfig.Instance.MaxCaseCount, corpusAllFilePath));

            string outputExcelFilePath = SelectRandomCorpus(corpusAllFilePath, outputDir);
            if (!string.IsNullOrEmpty(outputExcelFilePath))
            {
                Util.ConsoleOutTextColor(string.Format("Random select {0} cases, saved to {1}", LocalConfig.Instance.MaxCaseCount, outputExcelFilePath));
            }
            else
            {
                Util.ConsoleOutTextColor(string.Format("{0} doesn't contains {1}  data, can't generate random data.", corpusAllFilePath, LocalConfig.Instance.MaxCaseCount));
            }
        }

        /// <summary>
        /// Compile and run test in folder
        /// </summary>
        /// <param name="destDir">folder</param>
        /// <param name="genExcelReport">if true, genereate the excel report based on test result</param>
        public void CompileAndTestInFolder(string destDir, bool genExcelReport = false)
        {
            // generate ing.config and feature.config for crf training
            GenCRFTrainingConfig(destDir);

            // Check if the training file exist
            string trainingFolder = Path.Combine(destDir, Util.TrainingFolderName);
            if (!Directory.Exists(trainingFolder)
                || Directory.GetFiles(trainingFolder, Util.XmlFileSearchExtension).Count() <= 0)
            {
                Util.ConsoleOutTextColor(string.Format("{0} doesn't exist or doesn't contains training scripts.", trainingFolder), ConsoleColor.Red);
                return;
            }

            Util.ConsoleOutTextColor("Training crf model in " + destDir);
            string message = "";

            if (TrainingCRFModel(Path.Combine(destDir, Util.TrainingConfigFileName),
                Path.Combine(destDir, "traininglog", "log.xml"),
                ref message))
            {
                Util.ConsoleOutTextColor(message);
            }
            else
            {
                throw new Exception(message);
            }

            Util.ConsoleOutTextColor("Compiling language data " + destDir);
            string generatedCrf = Path.Combine(destDir, LocalConfig.Instance.OutputCRFName);

            string generatedDataFile;
            // compile lang  data file
            if (CompileLangData(generatedCrf, LocalConfig.Instance.CRFModelDir, destDir, out generatedDataFile))
            {
                Util.ConsoleOutTextColor("Successful compile " + generatedDataFile);
            }
            else
            {
                Util.ConsoleOutTextColor("Compile failed in" + destDir, ConsoleColor.Red);
                return;
            }

            string testcaseFile = Path.Combine(destDir, Util.TestCaseFileName);
            if (File.Exists(testcaseFile))
            {
                Util.ConsoleOutTextColor("Running test " + testcaseFile);

                string testLogFile = Path.Combine(destDir, Util.TestlogFileName);
                if (TestCRFModel(generatedDataFile,
                    testcaseFile,
                    testLogFile,
                    ref message))
                {
                    Util.ConsoleOutTextColor(message);

                    // test with the original dat file
                    if (TestCRFModel(LocalConfig.Instance.LangDataPath,
                        testcaseFile,
                        Path.Combine(destDir, Util.TestlogBeforeFileName),
                        ref message))
                    {
                        Util.ConsoleOutTextColor(message);
                    }

                    // genereate excel report
                    if (genExcelReport)
                    {
                        Util.ConsoleOutTextColor("Genereating excel test result");
                        ExcelHelper.GenExcelTestReport(testLogFile);
                    }
                }
                else
                {
                    Util.ConsoleOutTextColor(message, ConsoleColor.Red);
                    return;
                }
            }
        }

        /// <summary>
        /// Generate training script to training folder and recompile and rerun the test and generate report
        /// </summary>
        /// <param name="bugFixingFilePath">bug fixing file (tab separate each line)
        /// 我还差你五元钱。	cha4
        /// 我们离父母的希望还差很远。	cha4
        /// </param>
        /// <param name="outputDir">Parent folder that contains TrainingScript folder</param>
        public void AppendTrainingScriptAndReRunTest(string bugFixingFilePath, string outputDir)
        {
            string trainingFolder = Path.Combine(outputDir, Util.TrainingFolderName);

            Helper.ThrowIfDirectoryNotExist(trainingFolder);

            string saveFilePath = Path.Combine(trainingFolder, Util.BugFixingFileName);

            int startId = Util.BugFixingXmlStartIndex + 1;

            XmlDocument existingXmlDoc = null;

            if (File.Exists(saveFilePath))
            {
                existingXmlDoc = new XmlDocument();
                existingXmlDoc.Load(saveFilePath);

                XmlNodeList childs = existingXmlDoc.DocumentElement.ChildNodes;

                if (childs != null && childs.Count > 0)
                {
                    startId = Convert.ToInt32(childs.Item(childs.Count - 1).Attributes["id"].Value) + 1;
                }
            }

            XmlScriptFile results = new XmlScriptFile(LocalConfig.Instance.Lang);
            // append the cases
            var senAndProns = Util.GetSenAndPronFromBugFixingFile(bugFixingFilePath);

            foreach (var senAndPron in senAndProns)
            {
                ScriptItem item = ScriptGenerator.GenerateScriptItem(senAndPron.Key);

                ScriptWord charWord = item.AllWords.FirstOrDefault(p => p.Grapheme.Equals(LocalConfig.Instance.CharName, StringComparison.InvariantCultureIgnoreCase));

                if (charWord != null)
                {
                    charWord.Pronunciation = senAndPron.Value;

                    item.Id = string.Format("{0:D10}", startId);

                    // make sure each word contains pron, if not, use the default pron
                    foreach (ScriptWord word in item.AllWords)
                    {
                        // force to provide pronunciation when training, it's necessary for training crf model
                        if (string.IsNullOrEmpty(word.Pronunciation))
                        {
                            word.Pronunciation = LocalConfig.Instance.DefaultWordPron;
                            word.WordType = WordType.Normal;
                        }
                    }

                    results.Items.Add(item);
                    ++startId;
                }
            }

            if(existingXmlDoc == null)
            {
                results.Save(saveFilePath, System.Text.Encoding.Unicode);
            }
            else
            {
                // if exist bug fixing file, save the new items to a temp path, delete it when merge with existing file
                string tempFile = Path.GetTempFileName();

                results.Save(tempFile, System.Text.Encoding.Unicode);

                // append the temp file to existing file
                XmlDocument tempDoc = new XmlDocument();
                tempDoc.Load(tempFile);

                foreach (XmlNode child in tempDoc.DocumentElement.ChildNodes)
                {
                    XmlNode newChild = existingXmlDoc.ImportNode(child, true);
                    existingXmlDoc.DocumentElement.AppendChild(newChild);
                }

                existingXmlDoc.Save(saveFilePath);

                File.Delete(tempFile);
            }
            
            Console.WriteLine("Generate bug fixing file " + saveFilePath);

            // recompile and run test
            CompileAndTestInFolder(outputDir);

            // test the bugfixing item using the newly compiled dat file
            string bugFixingTestFile = Path.Combine(outputDir, Util.BugFixingTestFileName);

            ScriptGenerator.GenRuntimeTestcase(senAndProns, bugFixingTestFile);
            
            string generatedDatFile = Path.Combine(outputDir, Path.GetFileName(LocalConfig.Instance.LangDataPath));

            if(File.Exists(generatedDatFile))
            {
                string message = "";

                // test with the original dat file
                if (TestCRFModel(generatedDatFile,
                    bugFixingTestFile,
                    Path.Combine(outputDir, Util.BugFixingTestLogFileName),
                    ref message))
                {
                    Util.ConsoleOutTextColor(message);
                }
            }
        }

        /// <summary>
        /// Generate report from all testResults
        /// </summary>
        /// <example>
        /// frontmeasure test report is like below, Match Ratio          = 92.00 it the redio result line
        /// 
        /// POLYPHONE: 弹
        /// INPUT: (P1)
        /// 我曾经三次上战场，我上去是要带着光荣弹，最后一颗子弹留给自己的，这绝对的牺牲，这点是西方军队比不了的。
        /// EXPECTED: 
        /// d a_h nn_l / 
        /// RESULT: 
        /// t a_l nn_h / 
        /// 
        /// Test result of component: Pronunciation
        /// Test language: ZhCN
        /// Total Speak                = 100
        /// Total Pass                 = 92
        /// Total Fail                 = 8
        /// Total Error          = 0
        /// Match Ratio          = 92.00
        /// </example>
        /// <param name="testResultFiles">test result files</param>
        /// <param name="outputFilePath">output file path</param>
        public void GenNCrossTestReport(string[] testResultFiles, string outputFilePath)
        {
            if (testResultFiles == null ||
                testResultFiles.Length <= 0)
            {
                return;
            }

            List<string> reportResults = new List<string>();
            reportResults.Add("Item\tTraining\tTesting\tAccuracy\tDiff");
            double[] radios = new double[testResultFiles.Length];

            for (int i = 0; i < testResultFiles.Length; i++)
            {
                using (StreamReader reader = new StreamReader(testResultFiles[i]))
                {
                    while (reader.Peek() > -1)
                    {
                        string line = reader.ReadLine();
                        // find the the radio result line
                        Match match = Regex.Match(line, @"(?:Match Ratio *= )(\d+\.\d+)");
                        if (match.Success)
                        {
                            try
                            {
                                radios[i] = Double.Parse(match.Groups[1].Value);
                            }
                            catch
                            {
                                throw new Exception("Report format is not correct!");
                            }
                            break;
                        }
                    }
                }
            }

            // comput the average radio
            double aveRadio = radios.Average();

            // generate report content
            for (int i = 1; i <= radios.Length; i++)
            {
                double diff = radios[i - 1] - aveRadio;
                int trainCount = Convert.ToInt32(LocalConfig.Instance.NCrossCaseCount * 0.9);
                int testCount = LocalConfig.Instance.NCrossCaseCount - trainCount;
                reportResults.Add(string.Format("Set{0}\t{1}\t{2}\t{3:00.00}\t{4:00.00}", i, trainCount, testCount, radios[i - 1], diff));
            }

            reportResults.Add("");
            reportResults.Add(string.Format("Average radio: {0:00.00}", aveRadio));

            File.WriteAllLines(outputFilePath, reportResults);
        }

        /// <summary>
        /// Using ProsodyModelTrainer.exe to train crf
        /// </summary>
        /// <param name="configPath">training config file path</param>
        /// <param name="logPath">training xml file log</param>
        /// <param name="message">result message</param>
        /// <returns>success or not</returns>
        public bool TrainingCRFModel(string configPath, string logPath, ref string message)
        {
            string sdMsg = string.Empty;

            try
            {
                Int32 sdExitCode = CommandLine.RunCommandWithOutputAndError(Util.ProsodyModelTrainerPath,
                        string.Format("-config {0} -log {1}", configPath, logPath), null, ref sdMsg);

                if (sdExitCode == 0 && !string.IsNullOrEmpty(sdMsg))
                {
                    message = string.Format("Successfully training CRF Model: {0}", logPath);

                    // renaming the trained file, because the trained file name is like 2052.TD
                    XmlDocument doc = new XmlDocument();
                    doc.Load(configPath);
                    // currently the namespace is http://schemas.microsoft.com/tts/toolsuite
                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("ns", Util.TrainingConfigNamespace);
                    XmlNode node = doc.SelectSingleNode("//ns:input[@name='$env.OutputDir']", nsmgr);

                    if (node != null && Directory.Exists(node.InnerText))
                    {
                        string outputDir = node.InnerText;

                        // rename the file *.td to *.crf
                        string tempTDfile = Directory.GetFiles(outputDir, "*.TD").FirstOrDefault();
                        if (tempTDfile != null)
                        {
                            File.Copy(tempTDfile, Path.Combine(outputDir, LocalConfig.Instance.OutputCRFName), true);
                            Console.WriteLine("generate file: " + Path.Combine(outputDir, LocalConfig.Instance.OutputCRFName));
                            return true;
                        }
                    }
                }
                else
                {
                    message = string.Format("Failed training CRF Model : {0}", sdMsg);
                    return false;
                }

                if (sdExitCode != 0)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                message = string.Format("--{0}. Failed to training : {1}", e.Message, logPath);
            }
            return false;
        }

        /// <summary>
        /// Generate corresponding training.config and feature.config outputDir
        /// </summary>
        /// <param name="configTemplateDir">dir contains training.config and feature.config template</param>
        public void GenCRFTrainingConfig(string outputDir)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(LocalConfig.Instance.TrainingConfigTemplate);

            foreach (XmlNode node in doc.DocumentElement.GetElementsByTagName("include").Item(0))
            {
                string attribute = node.Attributes["name"].Value;
                switch (attribute)
                {
                    case "$feature.TargetWord":
                        node.InnerXml = LocalConfig.Instance.CharName;
                        break;
                    case "$env.Language":
                        node.InnerXml = Localor.LanguageToString(LocalConfig.Instance.Lang);
                        break;
                    case "$env.LexiconSchemaFile":
                    case "$env.PhoneSetFile":
                        node.InnerXml = node.InnerXml.Replace("#branch_root#", LocalConfig.Instance.BranchRootPath).Replace("#lang#", Localor.LanguageToString(LocalConfig.Instance.Lang));
                        break;
                    case "$env.LinguisticFeatureListFile":
                        node.InnerXml = Path.Combine(outputDir, Util.FeatureConfigFileName);
                        break;
                    case "$env.OutputDir":
                        node.InnerXml = outputDir;
                        break;
                    case "$env.Script":
                        node.InnerXml = Path.Combine(outputDir, Util.TrainingFolderName);
                        break;
                    default:
                        break;
                }
            }

            doc.Save(Path.Combine(outputDir, Util.TrainingConfigFileName));

            // copy the feature.config
            doc.LoadXml(LocalConfig.Instance.FeaturesConfigTemplate);
            doc.Save(Path.Combine(outputDir, Util.FeatureConfigFileName));
        }

        /// <summary>
        /// Compile data file
        /// </summary>
        /// <param name="crfFilePath">trained crf file</param>
        /// <param name="crfModelDir">crf model folder</param>
        /// <param name="outputDir">data file output folder</param>
        /// <param name="generatedFilePath">generated dat file path</param>
        public bool CompileLangData(string crfFilePath, string crfModelDir, string outputDir, out string generatedFilePath)
        {
            string message;
            // use the langDataPath
            string srcDataFilePath = LocalConfig.Instance.LangDataPath;

            // copy the original dat file to outputDir
            generatedFilePath = Path.Combine(outputDir, Path.GetFileName(srcDataFilePath));

            // delete the existing data file
            FileInfo fi = new FileInfo(generatedFilePath);
            if (fi.Exists && fi.IsReadOnly)
            {
                fi.IsReadOnly = false;
                fi.Delete();
            }

            // delete the backup data file, LanguageDataHelper.ReplaceBinaryFile will genereate again
            string backFilePath = generatedFilePath + ".bak";
            fi = new FileInfo(backFilePath);
            if (fi.Exists && fi.IsReadOnly)
            {
                fi.IsReadOnly = false;
                fi.Delete();
            }

            // copy dat file to current output folder
            File.Copy(srcDataFilePath, generatedFilePath, true);

            #region copy trained crf file to crfModel folder to compile temp bin file

            string destCrfFilePath = Path.Combine(crfModelDir, Path.GetFileName(crfFilePath));
            fi = new FileInfo(destCrfFilePath);

            bool isDestCrfExist = fi.Exists;

            // if file exist, we need to sd edit the file
            if (isDestCrfExist && fi.IsReadOnly)
            {
                SdCommand.SdCheckoutFile(destCrfFilePath, out message);
                Util.ConsoleOutTextColor(message);
            }

            File.Copy(crfFilePath, Path.Combine(crfModelDir, Path.GetFileName(crfFilePath)), true);

            // sd add file if not exist
            if (!isDestCrfExist)
            {
                SdCommand.SdAddFile(destCrfFilePath, out message);
                Util.ConsoleOutTextColor(message);
            }

            #endregion

            #region Update polyrule.txt file

            UpdatePolyRuleFile(LocalConfig.Instance.PolyRuleFilePath, LocalConfig.Instance.CharName);

            #endregion

            #region Update CRFLocalizedMapping.txt file

            string crfMappingFilePath = Path.Combine(new DirectoryInfo(crfModelDir).Parent.FullName, "CRFLocalizedMapping.txt");

            Helper.ThrowIfFileNotExist(crfMappingFilePath);

            SdCommand.SdCheckoutFile(crfMappingFilePath, out message);
            Console.WriteLine(message);

            // edit the mapping file
            string[] crfFileNames = UpdateCRFModelMappingFile(crfMappingFilePath, Path.GetFileName(crfFilePath), LocalConfig.Instance.UsingInfo);

            SdCommand.SdRevertUnchangedFile(crfMappingFilePath, out message);

            // make sure ModelUsed contains only crf files in CRFLocalizedMapping.txt file, if not, the compiled dat will wrong
            foreach (string crfPath in Directory.GetFiles(crfModelDir, Util.CRFFileSearchExtension))
            {
                string fileName = Path.GetFileName(crfPath);

                if(!crfFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                {
                    // delete the crf file not declared in mapping file
                    File.Delete(crfPath);
                }
            }

            #endregion

            #region Compile

            string tempCRFBinFile;

            if (!CompileCRF(crfModelDir, LocalConfig.Instance.Lang, out tempCRFBinFile))
            {
                throw new Exception("Compile crf file failed for " + crfFilePath);
            }

            Microsoft.Tts.Offline.Compiler.LanguageData.LanguageDataHelper.ReplaceBinaryFile(
                generatedFilePath,
                tempCRFBinFile,
                Microsoft.Tts.Offline.Compiler.LanguageData.ModuleDataName.PolyphonyModel);

            // delete the temp file
            File.Delete(tempCRFBinFile);

            #endregion

            return File.Exists(generatedFilePath);
        }

        /// <summary>
        /// CRF compiler
        /// </summary>
        /// <param name="crfModelDir">crf model folder</param>
        /// <param name="lang">language</param>
        /// <param name="crfBinFile">crf bin file path</param>
        /// <returns>success or not</returns>
        private static bool CompileCRF(string crfModelDir, Language lang, out string crfBinFile)
        {
            // TODO
            // E:\IPESpeechCore_Dev\private\dev\speech\tts\shenzhou\tools\Offline\src\Framework\Microsoft.Tts.Offline\Compiler\LangDataCompiler.cs 
            // E:\IPESpeechCore_Dev\private\dev\speech\tts\shenzhou\tools\Offline\src\Framework\Microsoft.Tts.Offline\Frontend\PolyphonyRuleFile.cs 


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
                        Util.ConsoleOutTextColor(error.ToString());
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
        /// Update polyrule.txt for specific char
        /// Delete "All >= 0" line if polyrule.txt file contains
        /// 
        /// polyrule.txt is like below, we should remove All >= 0 : "b eh_h i_l"; to make CRF model working
        /// [domain=address]
        /// CurW = "背";
        /// PrevW = "肩" : "b eh_h i_h";
        /// PrevW = "越" : "b eh_h i_h";
        /// All >= 0 : "b eh_h i_l";
        /// </summary>
        /// <param name="filePath">poly rule file path</param>
        /// <param name="charName">char name</param>
        public void UpdatePolyRuleFile(string filePath, string charName)
        {
            Helper.ThrowIfFileNotExist(filePath);
            Helper.ThrowIfNull(charName);

            int lineNumber = 1;
            bool needModify = true;

            bool isGeneralRule = true;
            string currentChar = "";
            string prevChar = "";

            // use this regex to find rule is used for which word
            Regex rxChar = new Regex("CurW = \"(.+)\";", RegexOptions.Compiled);

            // TODO: update function
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (reader.Peek() > -1)
                {
                    string lineContent = reader.ReadLine().Trim();

                    if(lineContent.StartsWith("[domain"))
                    {
                        isGeneralRule = false;
                    }
                    else if (rxChar.IsMatch(lineContent))
                    {
                        currentChar = rxChar.Match(lineContent).Groups[1].Value;

                        if(string.Equals(currentChar, charName))
                        {

                        }
                    }
                }
            }
            //{
            //    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
            //    {
            //            string[] mapping = line.Trim().Split('\t');

            //            if (mapping.Length != 4)
            //            {
            //                throw new FormatException(string.Format("{0} mapping file has the wrong format!", filePath));
            //            }

            //            string currentChar = mapping[0];
            //            string currentCRFFile = mapping[2];
            //            string currentUsingInfo = mapping[3];

            //            // if current line's char is same with charName para, check whether need to modify this line
            //            if (string.Equals(currentChar, LocalConfig.Instance.CharName))
            //            {
            //                // if crf file name and using info are same, don't modify
            //                // else edit thie line
            //                if (string.Equals(currentCRFFile, crfFileName) &&
            //                    string.Equals(currentUsingInfo, usingInfo))
            //                {
            //                    needModify = false;
            //                    break;
            //                }
            //                else
            //                {
            //                    charExist = true;
            //                    break;
            //                }
            //            }

            //        ++lineNumber;
            //    }
            //}

            //if (needModify)
            //{
            //    string content = string.Format("{0}\t->\t{1}\t{2}", LocalConfig.Instance.CharName, crfFileName, usingInfo);
            //    Util.EditLineInFile(filePath, lineNumber, content, !charExist);
            //}
        }

        /// <summary>
        /// Load CRF model name mapping(model name and localized name).
        /// </summary>
        /// <example>
        /// The mapping txt file is like this:
        /// 
        /// Map between polyphony model:
        /// 差	->	cha.crf	Being_used
        /// 长	->	chang.crf	Being_used
        /// 当	->	dang.crf	Being_used
        /// 行	->	hang.crf	Being_used
        /// 系	->	xi.crf	Unused
        /// </example>
        /// <param name="filePath">crf mapping File Path.</param>
        /// <param name="crfFileName">crf file name</param>
        /// <param name="usingInfo">check the char whether to be used, in mapping file "Being_used" or "Unused"</param>
        /// <returns>crf model files array, like bei.crf, wei.crf</returns>
        public string[] UpdateCRFModelMappingFile(string filePath, string crfFileName, string usingInfo)
        {
            if (!string.Equals(usingInfo, "Being_used") && !string.Equals(usingInfo, "Unused"))
            {
                throw new ArgumentException("usingInfo can only be \"Being_used\" or \"Unused\"!");
            }

            List<string> crfFileNames = new List<string>();

            // start flag of crf model mapping data
            const string MappingFlag = "Map between polyphony model:";

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
                        !string.Equals(MappingFlag, line))
                    {
                        throw new FormatException(string.Format("{0} mapping file has the wrong format!", filePath));
                    }
                    else if (lineNumber > 1)
                    {
                        string[] mapping = line.Trim().Split('\t');

                        if (mapping.Length != 4)
                        {
                            throw new FormatException(string.Format("{0} mapping file has the wrong format!", filePath));
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
                                string.Equals(currentUsingInfo, usingInfo))
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
                string content = string.Format("{0}\t->\t{1}\t{2}", LocalConfig.Instance.CharName, crfFileName, usingInfo);
                Util.EditLineInFile(filePath, lineNumber, content, !charExist);
            }

            return crfFileNames.ToArray();
        }

        /// <summary>
        /// Use FrontendMeasure to test testcaseFile and results saved to outputPath
        /// FrontendMeasure.exe -mode runtest -log "[path]\log.txt" -x "[path]\test.xml"
        /// </summary>
        /// <param name="logPath">fm result file</param>
        public bool TestCRFModel(string srcDatFile, string testcaseFile, string logPath, ref string message)
        {
            string sdMsg = string.Empty;

            try
            {
                // copy the required 4 dlls to FrontendMeasure.exe folder
                // Microsoft.Tts.Offline.dll, System.Speech.dll from Offline
                // HostCommon.dll, TestEngine_UTest.dll test\TTS\bin\Avatar
                string frontendMeasureDir = Path.GetDirectoryName(Util.FrontendMeasurePath);
                string[] requiredDllPaths =
                {
                    Path.Combine(LocalConfig.Instance.OfflineToolPath, "Microsoft.Tts.Offline.dll"),
                    Path.Combine(LocalConfig.Instance.OfflineToolPath, "System.Speech.dll"),
                    Path.Combine(frontendMeasureDir, "Avatar", "HostCommon.dll"),
                    Path.Combine(frontendMeasureDir, "Avatar", "TestEngine_UTest.dll")
                };

                foreach (string dllPath in requiredDllPaths)
                {
                    string dllName = Path.GetFileName(dllPath);
                    if (!File.Exists(Path.Combine(frontendMeasureDir, dllName)))
                    {
                        File.Copy(dllPath, Path.Combine(frontendMeasureDir, dllName));
                    }
                }

                // copy generaetd data file to offline\LocaleHandler folder
                string datDestPath = Path.Combine(LocalConfig.Instance.OfflineToolPath, "LocaleHandler", Path.GetFileName(srcDatFile));

                // make sure the destination dat file can be overwrite
                FileInfo fi = new FileInfo(datDestPath);

                if (fi.Exists && fi.IsReadOnly)
                {
                    fi.IsReadOnly = false;
                }

                File.Copy(srcDatFile, datDestPath, true);

                Console.WriteLine("copy " + srcDatFile + " to " + datDestPath);

                Console.WriteLine(string.Format("FrontendMeasure.exe -mode runtest -x {0} -log {1}", testcaseFile, logPath));

                // delete the existing log file
                if(File.Exists(logPath))
                {
                    File.Delete(logPath);
                }

                Int32 sdExitCode = CommandLine.RunCommandWithOutputAndError(Util.FrontendMeasurePath,
                        string.Format("-mode runtest -x {0} -log {1}", testcaseFile, logPath), null, ref sdMsg);

                if (sdExitCode == 0 && !string.IsNullOrEmpty(sdMsg))
                {
                    message = string.Format("Successfully run test: {0}", logPath);
                    return true;
                }
                else
                {
                    message = string.Format("Failed run test : {0}", sdMsg);
                    return false;
                }
            }
            catch (Exception e)
            {
                message = string.Format("--{0}. Failed to training : {1}", e.Message, logPath);
            }

            return false;
        }

        /// <summary>
        /// Genereate word break result for each file
        /// </summary>
        /// <param name="wildcard">input file path, like "D:\corpus\*.txt"</param>
        /// <param name="outputDir">output folder</param>
        public void DoWordBreak(string wildcard, string outputDir)
        {
            string[] inFilePaths = Util.GetAllFiles(wildcard);
            if (inFilePaths.Length == 0)
            {
                return;
            }

            Task[] tasks;
            if (inFilePaths.Length <= LocalConfig.Instance.MaxThreadCount)
            {
                tasks = new Task[inFilePaths.Length];
            }
            else
            {
                tasks = new Task[LocalConfig.Instance.MaxThreadCount];
            }

            Util.ConsoleOutTextColor("Start word breaking");

            for (int i = 0; i < tasks.Length; i++)
            {
                string[] filesToProcess = inFilePaths.Where((input, index) => (index % tasks.Length == i)).ToArray();

                // start each task nad show process info when this task complete
                tasks[i] = Task.Factory.StartNew(() =>
                        {
                            WordBreakFiles(filesToProcess, outputDir);
                        }
                    ).ContinueWith((ancient) =>
                        {
                            Console.WriteLine(string.Format("Processed {0} files, total {1} files", ProcessedFileCount, inFilePaths.Length));
                        }
                    );
            }

            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Word break each file
        /// </summary>
        /// <param name="fileProcessed">files to be processed</param>
        /// <param name="outputDir">output folder</param>
        private void WordBreakFiles(string[] fileProcessed, string outputDir)
        {
            using (WordBreaker wordBreaker = new WordBreaker(LocalConfig.Instance))
            {
                foreach (string filePath in fileProcessed)
                {
                    string fileName = Path.GetFileName(filePath);

                    // if the filtered file already exist, skip this file
                    string outputFilePath = Path.Combine(outputDir, fileName);
                    if (File.Exists(outputFilePath))
                    {
                        lock (_locker)
                        {
                            ++ProcessedFileCount;
                        }

                        Console.WriteLine("File " + fileName + " exist, skipped!");
                        continue;
                    }

                    Console.WriteLine("Breaking file " + fileName);

                    int counter = 0;
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        while (reader.Peek() > -1)
                        {
                            string sentence = reader.ReadLine().Replace(" ", "").Replace("\t", "");

                            if (string.IsNullOrEmpty(sentence))
                            {
                                continue;
                            }

                            try
                            {
                                File.AppendAllText(outputFilePath, wordBreaker.BreakWords(sentence).SpaceSeparate());
                                File.AppendAllText(outputFilePath, Environment.NewLine);
                                ++counter;
                            }
                            catch
                            {
                                continue;
                            }

                            if (counter >= LocalConfig.Instance.ShowTipCount &&
                                counter % LocalConfig.Instance.ShowTipCount == 0)
                            {
                                Console.WriteLine(string.Format("Searching {0} in {1}", counter, fileName));
                            }
                        }
                    }

                    lock (_locker)
                    {
                        ++ProcessedFileCount;
                    }
                }
            }
        }

        #endregion
    }
}
