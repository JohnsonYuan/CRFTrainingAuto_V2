//----------------------------------------------------------------------------
// <copyright file="CRFHelper.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      CRF helper class
// </summary>
//----------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Sentence and word break result struct.
    /// </summary>
    public struct SentenceAndWBResult
    {
        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the word break result.
        /// </summary>
        public string[] WBResult { get; set; }
    }

    /// <summary>
    /// CRFModelMapping, used to represent CRFLocalizedMapping.txt line content.
    /// </summary>
    public struct CRFModelMapping
    {
        /// <summary>
        /// The character name.
        /// </summary>
        public string CharName;

        /// <summary>
        /// The CRF model file name.
        /// </summary>
        public string CrfModelName;

        /// <summary>
        /// The status, "Being_used" or "Unused".
        /// </summary>
        public string Status;

        /// <summary>
        /// Initializes a new instance of the <see cref="CRFModelMapping"/> struct.
        /// </summary>
        /// <param name="line">The line content.</param>
        public CRFModelMapping(string line)
        {
            Helper.ThrowIfNull(line);

            string[] splitResult = line.Trim().Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (splitResult.Length != 4)
            {
                throw new FormatException(Helper.NeutralFormat("Mapping line content \"{0}\" has the wrong format!", line));
            }

            CharName = splitResult[0];
            CrfModelName = splitResult[2];
            Status = splitResult[3];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CRFModelMapping"/> struct.
        /// </summary>
        /// <param name="charName">Name of the character.</param>
        /// <param name="crfModelName">Name of the CRF model.</param>
        /// <param name="status">The status.</param>
        public CRFModelMapping(string charName, string crfModelName, string status)
        {
            CharName = charName;
            CrfModelName = crfModelName;
            Status = status;
        }

        /// <summary>
        /// Performs an explicit conversion from string to <see cref="CRFModelMapping"/>.
        /// </summary>
        /// <param name="line">The input line.</param>
        /// <returns>
        /// CRFModelMapping struct.
        /// </returns>
        public static explicit operator CRFModelMapping(string line)
        {
            return new CRFModelMapping(line);
        }

        /// <summary>
        /// CRFLocalizedMapping.txt line content.
        /// </summary>
        /// <returns>
        /// CRFLocalizedMapping.txt line content, like 系 -> xi.crf Unused.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        public override string ToString()
        {
            return Helper.NeutralFormat("{0}\t->\t{1}\t{2}", CharName, CrfModelName, Status);
        }
    }

    /// <summary>
    /// CRF helper class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public class CrfHelper
    {
        #region Fields

        private static int _processedFileCount = 0;

        private object _locker = new object();
        #endregion

        #region Methods

        /// <summary>
        /// Prepare train test set
        /// Generate txt file contains single training char from input folder
        /// First generate each file corresponding file to temp folder
        /// and then merge them, random select maxCount data.
        /// </summary>
        /// <param name="inputDir">Corpus folder.</param>
        /// <param name="outputDir">Output folder.</param>
        /// <param name="wbDir">Corpus Word break result folder.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
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

            Helper.PrintSuccessMessage("Start filtering");

            for (int i = 0; i < tasks.Length; i++)
            {
                string[] filesToProcess = inFilePaths.Where((input, index) => (index % LocalConfig.Instance.MaxThreadCount == i)).ToArray();

                // start each task nad show process info when this task complete
                tasks[i] = Task.Factory.StartNew(() =>
                    {
                        SelectTargetChar(filesToProcess, tempFolder, wbDir);
                    })
                    .ContinueWith((ancient) =>
                    {
                        Console.WriteLine(Helper.NeutralFormat("Processed {0} files, total {1} files", _processedFileCount, inFilePaths.Length));
                    });
            }

            Task.WaitAll(tasks);

            // clear counter
            _processedFileCount = 0;

            MergeAndRandom(tempFolder, outputDir);
        }

        /// <summary>
        /// Select target char from files
        /// if supply word break result file, then we don't need to use wordbreaker, it's more faster
        /// input file might like:羊城晚报记者林本剑本文来源：金羊网-羊城晚报
        /// word break file might like:羊城 晚报 记者 林 本 剑 本文 来源 ： 金 羊 网 - 羊城 晚报.
        /// </summary>
        /// <param name="fileProcessed">Corpus files to be processed.</param>
        /// <param name="outputDir">Temp output folder.</param>
        /// <param name="wbDir">Word break result folder.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        public void SelectTargetChar(string[] fileProcessed, string outputDir, string wbDir = null)
        {
            bool useWbResult = false;
            if (!string.IsNullOrEmpty(wbDir) &&
                Directory.Exists(wbDir))
            {
                useWbResult = true;
            }

            WordBreaker wordBreaker = null;

            foreach (string filePath in fileProcessed)
            {
                string fileName = Path.GetFileName(filePath);

                // if the filtered file already exist, skip this file
                string tempFilePath = Path.Combine(outputDir, fileName);

                if (File.Exists(tempFilePath))
                {
                    lock (_locker)
                    {
                        ++_processedFileCount;
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
                        ////#region filter data

                        // remove the empty space, if not, it will get the wrong index when using WordBraker to get the char index
                        // don't need to remove empty part, we can use word break result to restore the string, it will not contain any space
                        string sentence = fileReader.ReadLine().Trim();

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
                                Console.WriteLine(Helper.NeutralFormat("Search {0} in {1}", foundCount, fileName));
                            }
                        }

                        ////#endregion
                    }

                    // save each file to Temp folder, cause the total file is so large, then merge
                    File.WriteAllLines(tempFilePath, results);

                    lock (_locker)
                    {
                        ++_processedFileCount;
                    }

                    Console.WriteLine(Helper.NeutralFormat("Found {0} results in file {1}.", foundCount, fileName));
                }
                finally
                {
                    if (wordBreaker != null)
                    {
                        wordBreaker.Dispose();
                    }

                    if (fileReader != null)
                    {
                        fileReader.Dispose();
                    }

                    if (wbReader != null)
                    {
                        wbReader.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Generate maxCount(default 1500) random data from inFilePath
        /// e.g. generate file corpus.1500.txt .
        /// </summary>
        /// <param name="inFilePath">Input file path.</param>
        /// <param name="outputDir">Output folder.</param>
        /// <returns>Generated excel file path, return null if not success.</returns>
        public string SelectRandomCorpus(string inFilePath, string outputDir)
        {
            var inputs = Util.GetSenAndWbFromCorpus(inFilePath);

            // check whether can random select   
            if (inputs.Count < LocalConfig.Instance.MaxCaseCount)
            {
                return null;
            }

            // use the hashset to fast iteration and save the case and word break result to List results
            HashSet<string> tempResults = new HashSet<string>();
            List<string> results = new List<string>();

            Random rand = new Random();

            // results should contains LocalConfig.Instance.MaxCaseCount lines
            while (tempResults.Count < LocalConfig.Instance.MaxCaseCount)
            {
                int index = rand.Next(0, inputs.Count);

                var selectTarget = inputs[index];
                string curSentence = selectTarget.Content;

                if (!tempResults.Contains(curSentence))
                {
                    tempResults.Add(curSentence);

                    results.Add(curSentence);
                    results.Add(selectTarget.WBResult.SpaceSeparate());

                    // remove this item from input also
                    inputs.Remove(selectTarget);
                }
            }

            // save to txt file, it's easyier to view cases
            string randomTxtFilePath = Path.Combine(outputDir, Helper.NeutralFormat(Util.CorpusTxtFileNamePattern, LocalConfig.Instance.MaxCaseCount));
            File.WriteAllLines(randomTxtFilePath, results);

            // generate the excel file
            string outputExcelFilePath = Path.Combine(outputDir, Helper.NeutralFormat(Util.CorpusExcelFileNamePattern, LocalConfig.Instance.MaxCaseCount));
            try
            {
                ExcelGenerator.GenExcelFromTxtFile(randomTxtFilePath, outputExcelFilePath);
            }
            catch (Exception)
            {
                return null;
            }

            return outputExcelFilePath;
        }

        /// <summary>
        /// Generate NCrossData from excel
        /// First divide excelFile using N cross folder
        /// each folder contains 900 cases training.xls and 100 cases testing.xls
        /// and then generate training and test script from corresponding excel files.
        /// </summary>
        /// <param name="excelFile">Excel file path.</param>
        /// <param name="outputDir">Output folder.</param>
        public void GenNCrossData(string excelFile, string outputDir)
        {
            // divide corpus to training and test part, use 
            string trainingExcelFilePath;
            string testExcelFilePath;

            ExcelGenerator.DivideExcelCorpus(excelFile, outputDir, out trainingExcelFilePath, out testExcelFilePath);

            Helper.ThrowIfNull(trainingExcelFilePath);
            Helper.ThrowIfNull(testExcelFilePath);

            string generatedDataFile = null;
            bool isNeedCleanTempDir = UpdateMappingAndPolyruleFiles(LocalConfig.Instance.CRFModelDir, LocalConfig.Instance.PolyRuleFilePath, out generatedDataFile);

            ////#region put the 1000 training, and 500 test case to FinalResultFolder

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
            CompileAndTestInFolder(finalFolder, generatedDataFile);

            ////#endregion

            //// #region save the n cross test data to NCrossFolder

            // use features.config file from finalFolder, all ncross steps can use same features.config file
            string featureConfigFile = Path.Combine(finalFolder, Util.FeatureConfigFileName);

            string nCrossFolder = Path.Combine(outputDir, Util.NCrossFolderName);

            // divide training excel file corpus to 10 separate testing and training part
            Helper.PrintSuccessMessage("Start N Cross excel " + trainingExcelFilePath);
            ExcelGenerator.GenNCrossExcel(trainingExcelFilePath, nCrossFolder);
            Helper.PrintSuccessMessage("End N Cross excel " + trainingExcelFilePath);

            string[] testlogPaths = new string[LocalConfig.Instance.NFolderCount];

            for (int i = 1; i <= LocalConfig.Instance.NFolderCount; i++)
            {
                string destDir = Path.Combine(nCrossFolder, i.ToString());
                testlogPaths[i - 1] = Path.Combine(destDir, Util.TestlogFileName);

                // compile and run test in each folder
                try
                {
                    CompileAndTestInFolder(destDir, generatedDataFile, featureConfigFile);
                }
                catch (Exception ex)
                {
                    Helper.PrintColorMessageToOutput(ConsoleColor.Red, ex.Message);
                    return;
                }
            }

            ////#endregion

            // generate report based NCross test log
            string testReportPath = Path.Combine(outputDir, Util.TestReportFileName);
            GenNCrossTestReport(testlogPaths, testReportPath);
            Helper.PrintSuccessMessage("Generate test report " + testReportPath);

            // clean the temp
            if (isNeedCleanTempDir)
            {
                Helper.ForcedDeleteDir(Path.GetDirectoryName(generatedDataFile));
            }
        }

        /// <summary>
        /// Verify the excel file's pronunciation by compile and test all cases in excel.
        /// </summary>
        /// <param name="excelFile">Excel file path.</param>
        /// <param name="outputDir">Output folder.</param>
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

            Helper.PrintSuccessMessage(Helper.NeutralFormat("Start verify excel {0}, result will be saved to {1}.", excelFile, verifyResultFolder));

            ScriptGenerator.GenScript(excelFile, GenerateAction.TrainingScript, trainingFolder, Util.TrainingFileName);
            ScriptGenerator.GenScript(excelFile, GenerateAction.TestCase, verifyResultFolder, Util.TestCaseFileName);

            string generatedDataFile = null;
            bool isNeedCleanTempDir = UpdateMappingAndPolyruleFiles(LocalConfig.Instance.CRFModelDir, LocalConfig.Instance.PolyRuleFilePath, out generatedDataFile);

            CompileAndTestInFolder(verifyResultFolder, generatedDataFile, null, true);

            // clean the temp
            if (isNeedCleanTempDir)
            {
                Helper.ForcedDeleteDir(Path.GetDirectoryName(generatedDataFile));
            }
        }

        /// <summary>
        /// Merge files and random select.
        /// </summary>
        /// <param name="inputDir">Input folder.</param>
        /// <param name="outputDir">Output folder.</param>
        public void MergeAndRandom(string inputDir, string outputDir)
        {
            Helper.PrintSuccessMessage("Start merge files in " + inputDir + " !");

            // e.g. corpusAllFilePath = "corpus.all.txt"
            string corpusAllFilePath = Path.Combine(outputDir, Util.CorpusTxtAllFileName);

            // merge all files in temp folder
            int mergedFileCount = Util.MergeFiles(Path.Combine(inputDir, Util.CorpusTxtFileSearchPattern), corpusAllFilePath);

            if (mergedFileCount == 0)
            {
                Helper.PrintSuccessMessage("No data generated, ternimated!");
                return;
            }

            Helper.PrintSuccessMessage(Helper.NeutralFormat("All cases saved to {0}.", corpusAllFilePath));

            Helper.PrintSuccessMessage(Helper.NeutralFormat("Start random select {0} cases from {1}", LocalConfig.Instance.MaxCaseCount, corpusAllFilePath));

            string outputExcelFilePath = SelectRandomCorpus(corpusAllFilePath, outputDir);
            if (!string.IsNullOrEmpty(outputExcelFilePath))
            {
                Helper.PrintSuccessMessage(Helper.NeutralFormat("Random select {0} cases, saved to {1}", LocalConfig.Instance.MaxCaseCount, outputExcelFilePath));
            }
            else
            {
                Helper.PrintSuccessMessage(Helper.NeutralFormat("{0} doesn't contains {1}  data, can't generate random data.", corpusAllFilePath, LocalConfig.Instance.MaxCaseCount));
            }
        }

        /// <summary>
        /// Compile and run test in folder.
        /// </summary>
        /// <param name="destDir">Destination folder.</param>
        /// <param name="srcDataFile">If provided, the bin replace changes will based on this file, otherwise, based on LocalConfig.Instance.LangDataPath.</param>
        /// <param name="featuresConfigPath">If not provide features.config file path, we generate a new features.config in current folder.</param>
        /// <param name="genExcelReport">If true, generate the excel report based on test result.</param>
        public void CompileAndTestInFolder(string destDir, string srcDataFile = null, string featuresConfigPath = null, bool genExcelReport = false)
        {
            // generate training.config and feature.config for crf training
            GenCRFTrainingConfig(destDir, featuresConfigPath);

            PrepareTestEnvironment();

            // check if the training file exist
            string trainingFolder = Path.Combine(destDir, Util.TrainingFolderName);
            if (!Directory.Exists(trainingFolder)
                || Directory.GetFiles(trainingFolder, Util.XmlFileSearchExtension).Count() <= 0)
            {
                Helper.PrintColorMessageToOutput(ConsoleColor.Red, Helper.NeutralFormat("{0} doesn't exist or doesn't contain training scripts.", trainingFolder));
                return;
            }

            Helper.PrintSuccessMessage("Training crf model is in " + destDir);
            string message = string.Empty;

            if (TrainingCRFModel(Path.Combine(destDir, Util.TrainingConfigFileName),
                Path.Combine(destDir, "traininglog", "log.xml"),
                ref message))
            {
                Helper.PrintSuccessMessage(message);
            }
            else
            {
                throw new Exception(message);
            }

            Helper.PrintSuccessMessage("Compiling language data " + destDir);
            string generatedCrf = Path.Combine(destDir, LocalConfig.Instance.OutputCRFName);
            string generatedDataFile;

            srcDataFile = srcDataFile ?? LocalConfig.Instance.LangDataPath;

            // compile language data file
            if (ReplaceBinCrfInDat(generatedCrf, LocalConfig.Instance.CRFModelDir, srcDataFile, destDir, out generatedDataFile))
            {
                Helper.PrintSuccessMessage("Successful compile " + generatedDataFile);
            }
            else
            {
                Helper.PrintColorMessageToOutput(ConsoleColor.Red, "Compile failed in" + destDir);
                return;
            }

            string testcaseFile = Path.Combine(destDir, Util.TestCaseFileName);
            if (File.Exists(testcaseFile))
            {
                Helper.PrintSuccessMessage("Running test " + testcaseFile);

                string testLogFile = Path.Combine(destDir, Util.TestlogFileName);
                if (TestCRFModel(generatedDataFile,
                    testcaseFile,
                    testLogFile,
                    ref message))
                {
                    Helper.PrintSuccessMessage(message);

                    // test with the original dat file
                    if (TestCRFModel(LocalConfig.Instance.LangDataPath,
                        testcaseFile,
                        Path.Combine(destDir, Util.TestlogBeforeFileName),
                        ref message))
                    {
                        Helper.PrintSuccessMessage(message);
                    }

                    // generate excel report
                    if (genExcelReport)
                    {
                        Helper.PrintSuccessMessage("Genereating excel test result");
                        ExcelGenerator.GenExcelTestReport(testLogFile);
                    }
                }
                else
                {
                    Helper.PrintColorMessageToOutput(ConsoleColor.Red, message);
                    return;
                }
            }
        }

        /// <summary>
        /// Update CRFMapping file and polyrule.txt file, and return true compiled dat file if polyrule.txt exist.
        /// </summary>
        /// <param name="crfModelDir">Crf model used folder.</param>
        /// <param name="polyRuleFile">Polyrule.txt file path.</param>
        /// <param name="generatedDatFile">Generated new dat file with polyrule.txt file change.</param>
        /// <returns>True if polyrule.txt file changed and new data file generated.</returns>
        public bool UpdateMappingAndPolyruleFiles(string crfModelDir, string polyRuleFile, out string generatedDatFile)
        {
            // Update CRFLocalizedMapping.txt file and check CRF model folder
            UpdateCRFMappingFile(LocalConfig.Instance.CRFModelDir);

            // Update polyrule.txt and compile
            if (UpdatePolyruleFileAndCompile(polyRuleFile, out generatedDatFile))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update crf mapping file and make sure crf ModelDir contains only necessary crf models.
        /// </summary>
        /// <param name="crfModelDir">Crf model used folder.</param>
        public void UpdateCRFMappingFile(string crfModelDir)
        {
            string message;
            string crfMappingFilePath = Path.Combine(new DirectoryInfo(crfModelDir).Parent.FullName, Util.CRFMappingFileName);

            // update the mapping file and return the crf model folder should contained crf iles
            string[] crfFileNames = CompilerHelper.UpdateCRFModelMappingFile(crfMappingFilePath, Path.GetFileName(crfMappingFilePath));

            SdCommand.SdRevertUnchangedFile(crfMappingFilePath, out message);

            // make sure ModelUsed folder contains only crf files in CRFLocalizedMapping.txt file, if not, the compiled dat will wrong
            foreach (string crfPath in Directory.GetFiles(crfModelDir, Util.CRFFileSearchExtension))
            {
                string fileName = Path.GetFileName(crfPath);

                if (!crfFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                {
                    Helper.ForcedDeleteFile(crfPath);
                }
            }
        }

        /// <summary>
        /// Update polyrule.txt file and compile.
        /// </summary>
        /// <param name="polyRuleFile">Polyrule.txt file path.</param>
        /// <param name="generatedDatFile">Generated data file path.</param>
        /// <returns>True if polyrule.txt file changes and compile new data file success.</returns>
        public bool UpdatePolyruleFileAndCompile(string polyRuleFile, out string generatedDatFile)
        {
            // compile polyrule.txt if update polyrule.txt file
            if (CompilerHelper.UpdatePolyRuleFile(polyRuleFile, LocalConfig.Instance.CharName))
            {
                Console.WriteLine("Compiling dat file now.");

                if (!CompilerHelper.CompileGeneralPolyRule(polyRuleFile, out generatedDatFile))
                {
                    throw new Exception("Compile dat file failed!");
                }

                Helper.PrintColorMessageToOutput(ConsoleColor.Green, "Compiled dat file " + generatedDatFile + ".");
                return File.Exists(generatedDatFile);
            }

            generatedDatFile = null;
            return false;
        }

        /// <summary>
        /// Generate training script to training folder and recompile and rerun the test and generate report.
        /// </summary>
        /// <param name="bugFixingFilePath">Bug fixing file (tab separate each line)
        /// 我还差你五元钱。cha4
        /// 我们离父母的希望还差很远。cha4.
        /// </param>
        /// <param name="outputDir">Parent folder that contains TrainingScript folder.</param>
        public void AppendTrainingScriptAndReRunTest(string bugFixingFilePath, string outputDir)
        {
            PrepareTestEnvironment();

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

                    item.Id = Helper.NeutralFormat("{0:D10}", startId);

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

            if (existingXmlDoc == null)
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
            string generatedDataFile = null;
            bool isNeedCleanTempDir = UpdateMappingAndPolyruleFiles(LocalConfig.Instance.CRFModelDir, LocalConfig.Instance.PolyRuleFilePath, out generatedDataFile);

            CompileAndTestInFolder(outputDir, generatedDataFile);

            // test the bugfixing item using the newly compiled dat file
            string bugFixingTestFile = Path.Combine(outputDir, Util.BugFixingTestFileName);

            ScriptGenerator.GenRuntimeTestcase(senAndProns, bugFixingTestFile);

            string generatedDatFile = Path.Combine(outputDir, Path.GetFileName(LocalConfig.Instance.LangDataPath));

            if (File.Exists(generatedDatFile))
            {
                string message = string.Empty;

                // test with the original dat file
                if (TestCRFModel(generatedDatFile,
                    bugFixingTestFile,
                    Path.Combine(outputDir, Util.BugFixingTestLogFileName),
                    ref message))
                {
                    Helper.PrintSuccessMessage(message);
                }
            }

            // clean the temp
            if (isNeedCleanTempDir)
            {
                Helper.ForcedDeleteDir(Path.GetDirectoryName(generatedDataFile));
            }
        }

        /// <summary>
        /// Generate report from NCross testResults.
        /// </summary>
        /// <example>
        /// Frontmeasure test report is like below
        /// POLYPHONE: 弹
        /// INPUT: (P1)
        /// 我曾经三次上战场，我上去是要带着光荣弹，最后一颗子弹留给自己的，这绝对的牺牲，这点是西方军队比不了的。
        /// EXPECTED: 
        /// d a_h nn_l / 
        /// RESULT: 
        /// t a_l nn_h / 
        /// Test result of component: Pronunciation
        /// Test language: ZhCN
        /// Total Speak                = 100
        /// Total Pass                 = 92
        /// Total Fail                 = 8
        /// Total Error          = 0
        /// Match Ratio          = 92.00.
        /// </example>
        /// <param name="testResultFiles">Test result files.</param>
        /// <param name="outputFilePath">Output file path.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
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
                                radios[i] = double.Parse(match.Groups[1].Value);
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
                reportResults.Add(Helper.NeutralFormat("Set{0}\t{1}\t{2}\t{3:00.00}\t{4:00.00}", i, trainCount, testCount, radios[i - 1], diff));
            }

            reportResults.Add(string.Empty);
            reportResults.Add(Helper.NeutralFormat("Average radio: {0:00.00}", aveRadio));

            File.WriteAllLines(outputFilePath, reportResults);
        }

        /// <summary>
        /// Using ProsodyModelTrainer.exe to train crf.
        /// </summary>
        /// <param name="configPath">Training config file path.</param>
        /// <param name="logPath">Training xml file log.</param>
        /// <param name="message">Result message.</param>
        /// <returns>Success or not.</returns>
        public bool TrainingCRFModel(string configPath, string logPath, ref string message)
        {
            string sdMsg = string.Empty;

            try
            {
                int sdExitCode = CommandLine.RunCommandWithOutputAndError(Util.ProsodyModelTrainerPath,
                        Helper.NeutralFormat("-config {0} -log {1}", configPath, logPath), null, ref sdMsg);

                if (sdExitCode == 0 && !string.IsNullOrEmpty(sdMsg))
                {
                    message = Helper.NeutralFormat("Successfully training CRF Model: {0}", logPath);

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
                    message = Helper.NeutralFormat("Failed training CRF Model : {0}", sdMsg);
                    return false;
                }

                if (sdExitCode != 0)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                message = Helper.NeutralFormat("--{0}. Failed to training : {1}", e.Message, logPath);
            }

            return false;
        }

        /// <summary>
        /// Generate corresponding training.config and feature.config outputDir.
        /// </summary>
        /// <param name="outputDir">Output folder.</param>
        /// <param name="featuresConfigPath">If not provide features.cofnig file path, we generate a new features.config in current folder.</param>
        public void GenCRFTrainingConfig(string outputDir, string featuresConfigPath)
        {
            XmlDocument doc = new XmlDocument();

            string linguisticFeatureListFile;

            // if features.config file exist, we just use it
            if (!string.IsNullOrEmpty(featuresConfigPath) &&
                File.Exists(featuresConfigPath))
            {
                linguisticFeatureListFile = featuresConfigPath;
            }
            else
            {
                linguisticFeatureListFile = Path.Combine(outputDir, Util.FeatureConfigFileName);

                // create a new features.config file
                doc.LoadXml(LocalConfig.Instance.FeaturesConfigTemplate);
                doc.Save(linguisticFeatureListFile);
            }

            // generate training.config
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
                        node.InnerXml = linguisticFeatureListFile;
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
        }

        /// <summary>
        /// Replace binary crf model in general domain dat.
        /// </summary>
        /// <param name="crfFilePath">Trained crf file.</param>
        /// <param name="crfModelDir">Crf model folder.</param>
        /// <param name="srcDataFile">Original dat file path.</param>
        /// <param name="outputDir">Data file output folder.</param>
        /// <param name="generatedFilePath">Generated dat file path.</param>
        /// <returns>Compile success or not.</returns>
        public bool ReplaceBinCrfInDat(string crfFilePath, string crfModelDir, string srcDataFile, string outputDir, out string generatedFilePath)
        {
            string message;

            // copy the original dat file to outputDir
            generatedFilePath = Path.Combine(outputDir, Path.GetFileName(srcDataFile));

            // delete the existing data file
            Helper.ForcedDeleteFile(generatedFilePath);

            // delete the backup data file, LanguageDataHelper.ReplaceBinaryFile will genereate again
            string backFilePath = generatedFilePath + ".bak";
            Helper.ForcedDeleteFile(backFilePath);

            // copy dat file to current output folder
            File.Copy(srcDataFile, generatedFilePath, true);

            ////#region copy trained crf file to crfModel folder to compile temp bin file

            string destCrfFilePath = Path.Combine(crfModelDir, Path.GetFileName(crfFilePath));
            FileInfo fi = new FileInfo(destCrfFilePath);

            bool isDestCrfExist = fi.Exists;

            // if file exist, we need to sd edit the file
            if (isDestCrfExist && fi.IsReadOnly)
            {
                SdCommand.SdCheckoutFile(destCrfFilePath, out message);
                Helper.PrintSuccessMessage(message);
            }

            File.Copy(crfFilePath, Path.Combine(crfModelDir, Path.GetFileName(crfFilePath)), true);

            // sd add file if not exist
            if (!isDestCrfExist)
            {
                SdCommand.SdAddFile(destCrfFilePath, out message);
                Helper.PrintSuccessMessage(message);
            }

            ////#endregion

            ////#region Compile crf model to data file

            string tempBinFile;

            // compile crf model
            if (!CompilerHelper.CompileCRF(crfModelDir, LocalConfig.Instance.Lang, out tempBinFile))
            {
                throw new Exception("Compile crf file failed for " + crfFilePath);
            }

            Microsoft.Tts.Offline.Compiler.LanguageData.LanguageDataHelper.ReplaceBinaryFile(
                generatedFilePath,
                tempBinFile,
                Microsoft.Tts.Offline.Compiler.LanguageData.ModuleDataName.PolyphonyModel);

            // delete the temp file
            File.Delete(tempBinFile);

            ////#endregion

            return File.Exists(generatedFilePath);
        }

        /// <summary>
        /// Use FrontendMeasure to test testcaseFile and results saved to outputPath
        /// FrontendMeasure.exe -mode runtest -log "[path]\log.txt" -x "[path]\test.xml".
        /// </summary>
        /// <param name="srcDatFile">Source dat flie path.</param>
        /// <param name="testcaseFile">Test case file path.</param>
        /// <param name="logPath">Test result file path.</param>
        /// <param name="message">Output message.</param>
        /// <returns>Test success or not.</returns>
        public bool TestCRFModel(string srcDatFile, string testcaseFile, string logPath, ref string message)
        {
            string sdMsg = string.Empty;

            try
            {
                // copy generaetd data file to offline\LocaleHandler folder
                string datDestPath = Path.Combine(LocalConfig.Instance.OfflineToolPath, "LocaleHandler", Path.GetFileName(srcDatFile));

                // make sure the destination dat file can be overwrite
                FileInfo fi = new FileInfo(datDestPath);

                if (fi.Exists && fi.IsReadOnly)
                {
                    fi.IsReadOnly = false;
                }

                File.Copy(srcDatFile, datDestPath, true);

                Console.WriteLine(Helper.NeutralFormat("copy {0} to {1}", srcDatFile, datDestPath));

                Console.WriteLine(Helper.NeutralFormat("FrontendMeasure.exe -mode runtest -x {0} -log {1}", testcaseFile, logPath));

                // delete the existing log file
                if (File.Exists(logPath))
                {
                    File.Delete(logPath);
                }

                int sdExitCode = CommandLine.RunCommandWithOutputAndError(Util.FrontendMeasurePath,
                        Helper.NeutralFormat("-mode runtest -x {0} -log {1}", testcaseFile, logPath), null, ref sdMsg);

                if (sdExitCode == 0 && !string.IsNullOrEmpty(sdMsg))
                {
                    message = Helper.NeutralFormat("Successfully run test: {0}", logPath);
                    return true;
                }
                else
                {
                    message = Helper.NeutralFormat("Failed run test : {0}", sdMsg);
                    return false;
                }
            }
            catch (Exception e)
            {
                message = Helper.NeutralFormat("--{0}. Failed to training : {1}", e.Message, logPath);
            }

            return false;
        }

        /// <summary>
        /// Genereate word break result for each file.
        /// </summary>
        /// <param name="wildcard">Input file path, like "D:\corpus\*.txt".</param>
        /// <param name="outputDir">Output folder.</param>
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

            Helper.PrintSuccessMessage("Start word breaking");

            for (int i = 0; i < tasks.Length; i++)
            {
                string[] filesToProcess = inFilePaths.Where((input, index) => (index % tasks.Length == i)).ToArray();

                // start each task nad show process info when this task complete
                tasks[i] = Task.Factory.StartNew(() =>
                        {
                            WordBreakFiles(filesToProcess, outputDir);
                        })
                        .ContinueWith((ancient) =>
                        {
                            Console.WriteLine(Helper.NeutralFormat("Processed {0} files, total {1} files", _processedFileCount, inFilePaths.Length));
                        });
            }

            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Prepares the required dlls for FrontendMeasure.exe.
        /// </summary>
        public void PrepareTestEnvironment()
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
        }

        /// <summary>
        /// Word break each file.
        /// </summary>
        /// <param name="fileProcessed">Files to be processed.</param>
        /// <param name="outputDir">Output folder.</param>
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
                            ++_processedFileCount;
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
                            string sentence = reader.ReadLine().Trim();

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
                                Console.WriteLine(Helper.NeutralFormat("Searching {0} in {1}", counter, fileName));
                            }
                        }
                    }

                    lock (_locker)
                    {
                        ++_processedFileCount;
                    }
                }
            }
        }

        #endregion
    }
}
