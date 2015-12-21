//----------------------------------------------------------------------------
// <copyright file="ExcelHelper.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Excel helper
// </summary>
//----------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;
    using Excel = Microsoft.Office.Interop.Excel;

    /// <summary>
    /// Excel generator.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public static class ExcelGenerator
    {
        /// <summary>
        /// Each Dictionary item contains the case and the pron get from the execel file.
        /// </summary>
        /// <param name="usedRange">Excel used range.</param>
        /// <param name="useNavtivePhone">If true, the return value's pron is native pron, else use pinyin.</param>
        /// <returns>Dictionary contains case and native pronunciation.</returns>
        public static Dictionary<SentenceAndWBResult, string> GetCaseAndPronsFromExcel(Excel.Range usedRange, bool useNavtivePhone = true)
        {
            Dictionary<SentenceAndWBResult, string> caseAndProns = new Dictionary<SentenceAndWBResult, string>();

            WordBreaker wordBreaker = null;
            try
            {
                for (int rowIndex = 2; rowIndex <= usedRange.Rows.Count; rowIndex++)
                {
                    SentenceAndWBResult tempReseult = new SentenceAndWBResult();

                    // first column for case, second column for corrct pron
                    string caseVal = Convert.ToString((usedRange.Cells[rowIndex, Util.ExcelCaseColIndex] as Excel.Range).Value2);

                    string wbResult = Convert.ToString((usedRange.Cells[rowIndex, Util.ExcelWbColIndex] as Excel.Range).Value2);

                    // if excel doesn't contains the word break result, create a new wrod breaker
                    if (!string.IsNullOrEmpty(wbResult))
                    {
                        tempReseult.WBResult = wbResult.SplitBySpace();
                    }
                    else
                    {
                        if (wordBreaker == null)
                        {
                            wordBreaker = new WordBreaker(LocalConfig.Instance);
                        }

                        tempReseult.WBResult = wordBreaker.BreakWords(caseVal);
                    }

                    tempReseult.Content = tempReseult.WBResult.ConcatToString();

                    string pinYinPron = Convert.ToString((usedRange.Cells[rowIndex, Util.ExcelCorrectPronColIndex] as Excel.Range).Value2);
                    if (string.IsNullOrWhiteSpace(pinYinPron))
                    {
                        throw new Exception(Helper.NeutralFormat("Excel in line {0} doesn't provide the pron.", rowIndex));
                    }

                    pinYinPron = pinYinPron.Trim();

                    if (string.IsNullOrEmpty(caseVal) ||
                        string.IsNullOrEmpty(pinYinPron))
                    {
                        throw new Exception(Helper.NeutralFormat("Excel file in row {0} has the empty case or pron!", rowIndex));
                    }

                    if (!caseAndProns.ContainsKey(tempReseult))
                    {
                        // check the pron is in config file
                        if (LocalConfig.Instance.Prons.ContainsKey(pinYinPron) &&
                            !string.IsNullOrEmpty(LocalConfig.Instance.Prons[pinYinPron]))
                        {
                            // if don't use native phone, we add the pinyin pron to result
                            if (!useNavtivePhone)
                            {
                                caseAndProns.Add(tempReseult, pinYinPron);
                            }
                            else
                            {
                                caseAndProns.Add(tempReseult, LocalConfig.Instance.Prons[pinYinPron]);
                            }
                        }
                        else
                        {
                            string errorMsg = Helper.NeutralFormat("Excel file in row {0} has the wrong pron \"{1}\"! It should like ", rowIndex, pinYinPron);
                            foreach (string val in LocalConfig.Instance.Prons.Keys)
                            {
                                errorMsg += val + " ";
                            }

                            errorMsg += ".";
                            throw new Exception(errorMsg);
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

            return caseAndProns;
        }

        /// <summary>
        /// Generate Excel From caseAndProns.
        /// </summary>
        /// <param name="caseAndProns">Case and pronunciation.</param>
        /// <param name="outputFilePath">Output file path.</param>
        public static void GenExcelFromCaseAndProns(IEnumerable<KeyValuePair<SentenceAndWBResult, string>> caseAndProns, string outputFilePath)
        {
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            // initialize the Excel application Object
            Excel.Application xlApp = new Excel.Application();

            // check whether Excel is installed
            if (xlApp == null)
            {
                Console.WriteLine("Excel is not properly installed!!");
                return;
            }

            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;
            xlWorkBook = xlApp.Workbooks.Add(misValue);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            try
            {
                xlWorkSheet.Name = LocalConfig.Instance.CharName;
                xlWorkSheet.Cells[1, Util.ExcelCaseColIndex] = Util.ExcelCaseColTitle;
                xlWorkSheet.Cells[1, Util.ExcelCorrectPronColIndex] = Util.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, Util.ExcelCommentColIndex] = Util.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, Util.ExcelWbColIndex] = Util.ExcelWbColTitle;

                int rowIndex = 2;

                foreach (KeyValuePair<SentenceAndWBResult, string> item in caseAndProns)
                {
                    xlWorkSheet.Cells[rowIndex, Util.ExcelCaseColIndex] = item.Key.Content;
                    xlWorkSheet.Cells[rowIndex, Util.ExcelCorrectPronColIndex] = item.Value;

                    Excel.Range xlRange = (Excel.Range)xlWorkSheet.Cells[rowIndex, 1];
                    int startIndex = item.Key.Content.GetSingleCharIndexOfLine(LocalConfig.Instance.CharName, item.Key.WBResult);
                    if (startIndex > -1)
                    {
                        xlRange.Characters[startIndex + 1, 1].Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                    }

                    xlWorkSheet.Cells[rowIndex, Util.ExcelWbColIndex] = item.Key.WBResult.SpaceSeparate();

                    ++rowIndex;
                }

                // hide the wb result column
                Excel.Range range = (Excel.Range)xlWorkSheet.Columns[Util.ExcelWbColIndex, Type.Missing];
                range.EntireColumn.Hidden = true;

                xlWorkSheet.Columns.AutoFit();

                // delte the existing excel file
                if (File.Exists(outputFilePath))
                {
                    File.Delete(outputFilePath);
                }

                xlWorkBook.SaveAs(outputFilePath,
                    Excel.XlFileFormat.xlWorkbookNormal,
                    misValue, misValue, misValue, misValue,
                    Excel.XlSaveAsAccessMode.xlExclusive,
                    misValue, misValue, misValue, misValue, misValue);
            }
            finally
            {
                xlWorkBook.Close(true, misValue, misValue);
                xlApp.Quit();

                ReleaseExcelObject(xlWorkSheet);
                ReleaseExcelObject(xlWorkBook);
                ReleaseExcelObject(xlApp);
            }
        }

        /// <summary>
        /// Verify excel sheet, make sure it contains at least 2 rows, 2 columns
        /// first row it the title line, 2 columns one for case, one for pronunciation.
        /// </summary>
        /// <param name="xlSheet">Excel sheet.</param>
        /// <returns>Success or not.</returns>
        public static bool VerifyExcelSheet(Excel.Worksheet xlSheet)
        {
            Excel.Range range = xlSheet.UsedRange;

            // at least 2 rows, 2 columns
            if (range.Rows.Count <= 1 || range.Columns.Count <= 1)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Properly clean up Excel interop objects.
        /// </summary>
        /// <param name="obj">Excel object.</param>
        public static void ReleaseExcelObject(object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                obj = null;
            }
            catch
            {
                obj = null;
            }
            finally
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Divide Excel file to training and test part.
        /// </summary>
        /// <param name="excelFilePath">Excel file path.</param>
        /// <param name="outputDir">Output folder.</param>
        /// <param name="trainingExcelFilePath">Training excel file path.</param>
        /// <param name="testExcelFilePath">Test excel file path.</param>
        public static void DivideExcelCorpus(string excelFilePath, string outputDir, out string trainingExcelFilePath, out string testExcelFilePath)
        {
            trainingExcelFilePath = testExcelFilePath = null;

            // initialize the Excel application Object
            Excel.Application xlApp = new Excel.Application();

            // check whether Excel is installed in your system.
            if (xlApp == null)
            {
                Console.WriteLine("Excel is not properly installed!!");
                return;
            }

            Excel.Workbook xlWorkBook = xlApp.Workbooks.Open(excelFilePath, 0, false, 5, string.Empty, string.Empty, false, Excel.XlPlatform.xlWindows, string.Empty,
                        true, false, 0, true, false, false);
            object misValue = System.Reflection.Missing.Value;

            Excel.Worksheet xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            Dictionary<SentenceAndWBResult, string> caseAndProns = GetCaseAndPronsFromExcel(xlWorkSheet.UsedRange, false);

            // generate training and test script
            if (caseAndProns != null && caseAndProns.Count() > LocalConfig.Instance.NCrossCaseCount)
            {
                // generate excel for training, e.g. corpusCountFilePath = "corpus.1000.xls"
                trainingExcelFilePath = Path.Combine(outputDir, Helper.NeutralFormat(Util.CorpusExcelFileNamePattern, LocalConfig.Instance.NCrossCaseCount));
                var trainingCaseAndProns = caseAndProns.Where((input, index) => (index >= 0 && index < LocalConfig.Instance.NCrossCaseCount));

                Helper.PrintSuccessMessage(Helper.NeutralFormat("Split {0} case from {1}, saved to {2}.", LocalConfig.Instance.NCrossCaseCount, excelFilePath, trainingExcelFilePath));
                GenExcelFromCaseAndProns(trainingCaseAndProns, trainingExcelFilePath);

                // generate excel for test cases, e.g. corpusCountFilePath = "corpus.500.xls"
                int testCount = LocalConfig.Instance.MaxCaseCount - LocalConfig.Instance.NCrossCaseCount;
                testExcelFilePath = Path.Combine(outputDir, Helper.NeutralFormat(Util.CorpusExcelFileNamePattern, testCount));
                var testCaseAndProns = caseAndProns.Where((input, index) => (index >= LocalConfig.Instance.NCrossCaseCount));

                Helper.PrintSuccessMessage(Helper.NeutralFormat("Split {0} case from {1}, saved to {2}.", testCount, excelFilePath, testExcelFilePath));
                GenExcelFromCaseAndProns(testCaseAndProns, testExcelFilePath);
            }
            else
            {
                Helper.PrintColorMessageToOutput(ConsoleColor.Red, Helper.NeutralFormat("The excel file doesn't content min {0} cases for training.", LocalConfig.Instance.NCrossCaseCount));
            }

            if (xlWorkBook != null)
            {
                xlWorkBook.Close(true, misValue, misValue);
            }

            if (xlApp != null)
            {
                xlApp.Quit();
            }

            ReleaseExcelObject(xlWorkSheet);
            ReleaseExcelObject(xlWorkBook);
            ReleaseExcelObject(xlApp);
        }

        /// <summary>
        /// Genereate excel test report from frontmeasure test result
        /// test report is like this:
        /// POLYPHONE: 背
        /// INPUT: (P1)
        /// 与其让少部分阿姨们脱离家政公司，辞掉工作，穿上围裙背上工具包立马跳上O2O的大船，我们宁愿张开怀抱，迎接所有有活的，没活的，想赚钱的，不想赚钱的，还有不少去了58又走了的阿姨。
        /// EXPECTED: 
        /// b eh_h i_h /
        /// RESULT: 
        /// b eh_h i_l /.
        /// </summary>
        /// <param name="testResultFile">Test result file path.</param>
        public static void GenExcelTestReport(string testResultFile)
        {
            Helper.ThrowIfFileNotExist(testResultFile);

            // we need INPUT: (P1), EXPECTED, RESULT as result
            List<string> inputLines = new List<string>();
            List<string> expectedLines = new List<string>();
            List<string> resultLines = new List<string>();

            // find the wrod do the test
            using (StreamReader reader = new StreamReader(testResultFile))
            {
                while (reader.Peek() > -1)
                {
                    string currentLine = reader.ReadLine().Trim();

                    if (reader.Peek() > -1)
                    {
                        switch (currentLine)
                        {
                            case "INPUT: (P1)":
                                inputLines.Add(reader.ReadLine().Trim());

                                break;
                            case "EXPECTED:":
                                expectedLines.Add(reader.ReadLine().TrimEnd(new char[] { '/', ' ' }));

                                break;
                            case "RESULT:":
                                resultLines.Add(reader.ReadLine().TrimEnd(new char[] { '/', ' ' }));

                                break;
                        }
                    }
                }
            }

            // make sure items count same
            if ((inputLines.Count == expectedLines.Count) &&
                (expectedLines.Count == resultLines.Count))
            {
                // initialize the Excel application Object
                Excel.Application xlApp = new Excel.Application();

                // check whether Excel is installed
                if (xlApp == null)
                {
                    Console.WriteLine("Excel is not properly installed!!");
                    return;
                }

                Excel.Workbook xlWorkBook;
                Excel.Worksheet xlWorkSheet;
                object misValue = System.Reflection.Missing.Value;
                xlWorkBook = xlApp.Workbooks.Add(misValue);
                xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

                WordBreaker wordBreaker = new WordBreaker(LocalConfig.Instance);

                try
                {
                    xlWorkSheet.Name = LocalConfig.Instance.CharName;
                    xlWorkSheet.Cells[1, 1] = "input";
                    xlWorkSheet.Cells[1, 2] = "expected";
                    xlWorkSheet.Cells[1, 3] = "result";

                    int rowIndex = 2;

                    for (int i = 0; i < inputLines.Count; i++)
                    {
                        xlWorkSheet.Cells[rowIndex, 1] = inputLines[i];

                        // highlight the target cahr
                        Excel.Range xlRange = (Excel.Range)xlWorkSheet.Cells[rowIndex, 1];
                        int startIndex = inputLines[i].GetSingleCharIndexOfLine(LocalConfig.Instance.CharName, wordBreaker);
                        
                        if (startIndex > -1)
                        {
                            xlRange.Characters[startIndex + 1, 1].Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                        }

                        xlWorkSheet.Cells[rowIndex, 2] = expectedLines[i];
                        xlWorkSheet.Cells[rowIndex, 3] = resultLines[i];

                        ++rowIndex;
                    }

                    xlWorkSheet.Columns.AutoFit();

                    string outputFilePath = Path.Combine(Path.GetDirectoryName(testResultFile), Util.VerifyResultExcelFileName);

                    // delte the existing excel file
                    if (File.Exists(outputFilePath))
                    {
                        File.Delete(outputFilePath);
                    }

                    xlWorkBook.SaveAs(outputFilePath,
                        Excel.XlFileFormat.xlWorkbookNormal,
                        misValue, misValue, misValue, misValue,
                        Excel.XlSaveAsAccessMode.xlExclusive,
                        misValue, misValue, misValue, misValue, misValue);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    wordBreaker.Dispose();

                    xlWorkBook.Close(true, misValue, misValue);
                    xlApp.Quit();

                    ReleaseExcelObject(xlWorkSheet);
                    ReleaseExcelObject(xlWorkBook);
                    ReleaseExcelObject(xlApp);
                }
            }
            else
            {
                throw new FormatException(testResultFile + " contains wrong data format.");
            }
        }

        /// <summary>
        /// Divide 1000 corpus to 10 separate testing and training part.
        /// </summary>
        /// <param name="excelFile">Excel file path.</param>
        /// <param name="outputDir">Output folder.</param>
        public static void GenNCrossExcel(string excelFile, string outputDir)
        {
            // initialize the Excel application Object
            Excel.Application xlApp = new Excel.Application();

            // check whether Excel is installed in your system.
            if (xlApp == null)
            {
                Console.WriteLine("Excel is not properly installed!");
                return;
            }

            Excel.Workbook xlWorkBook = xlApp.Workbooks.Open(excelFile, 0, false, 5, string.Empty, string.Empty, false, Excel.XlPlatform.xlWindows, string.Empty,
                        true, false, 0, true, false, false);
            object misValue = System.Reflection.Missing.Value;

            Excel.Worksheet xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            Dictionary<SentenceAndWBResult, string> caseAndProns = GetCaseAndPronsFromExcel(xlWorkSheet.UsedRange);

            const int Count = 100;

            // Generate 10 cross folder
            for (int i = 0; i < LocalConfig.Instance.NFolderCount; i++)
            {
                var testingcaseAndProns = caseAndProns.Where((kvPair, index) =>
                    index >= i * Count && index < (i + 1) * Count);
                var trainingcaseAndProns = caseAndProns.Where((kvPair, index) =>
                    (index >= 0 && index < i * Count) || (index >= (i + 1) * Count && index < 1000));

                string dirPath = Path.Combine(outputDir, (i + 1).ToString());
                string trainingFolder = Path.Combine(dirPath, Util.TrainingFolderName);

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (!Directory.Exists(trainingFolder))
                {
                    Directory.CreateDirectory(trainingFolder);
                }

                Console.WriteLine("Generating test and training script to " + dirPath);

                ScriptGenerator.GenRuntimeTestcase(testingcaseAndProns.ToDictionary(p => p.Key, p => p.Value), Path.Combine(dirPath, Util.TestCaseFileName));
                ScriptGenerator.GenTrainingScript(trainingcaseAndProns.ToDictionary(p => p.Key.Content, p => p.Value), Path.Combine(trainingFolder, Util.TrainingFileName));
            }

            if (xlWorkBook != null)
            {
                xlWorkBook.Close(true, misValue, misValue);
            }

            if (xlApp != null)
            {
                xlApp.Quit();
            }

            ReleaseExcelObject(xlWorkSheet);
            ReleaseExcelObject(xlWorkBook);
            ReleaseExcelObject(xlApp);
        }

        /// <summary>
        /// Generate excel from txt file
        /// Generated Excel file contains 3 column: "case", "correct pron" and "comment".
        /// </summary>
        /// <param name="inFilePath">Input txt file path.</param>
        /// <param name="outputFilePath">Output excel file path.</param>
        /// <param name="hasWbResult">If true word break result read from the txt file, false create new word breaker generate break result.</param>
        public static void GenExcelFromTxtFile(string inFilePath, string outputFilePath, bool hasWbResult = true)
        {
            Console.WriteLine("Generating excel from " + inFilePath);

            // initialize the Excel application Object
            Excel.Application xlApp = new Excel.Application();

            // check whether Excel is installed in your system.
            if (xlApp == null)
            {
                Console.WriteLine("Excel is not properly installed!");
                return;
            }

            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;

            // then create new Workbook and WorkSheet
            xlWorkBook = xlApp.Workbooks.Add(misValue);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            try
            {
                xlWorkSheet.Name = LocalConfig.Instance.CharName;
                xlWorkSheet.Cells[1, Util.ExcelCaseColIndex] = Util.ExcelCaseColTitle;
                xlWorkSheet.Cells[1, Util.ExcelCorrectPronColIndex] = Util.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, Util.ExcelCommentColIndex] = Util.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, Util.ExcelWbColIndex] = Util.ExcelWbColTitle;

                var allCases = Util.GetSenAndWbFromCorpus(inFilePath, hasWbResult);
                
                // Excel start index is 1, the content row start 2
                int rowIndex = 2;

                // fill the sheet
                for (int i = 0; i < allCases.Count; i++)
                {
                    xlWorkSheet.Cells[rowIndex, Util.ExcelCaseColIndex] = allCases[i].Content;

                    // highlight the training character
                    Excel.Range xlRange = (Excel.Range)xlWorkSheet.Cells[rowIndex, 1];
                    int startIndex = allCases[i].Content.GetSingleCharIndexOfLine(LocalConfig.Instance.CharName, allCases[i].WBResult);
                    xlRange.Characters[startIndex + 1, Util.ExcelCaseColIndex].Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);

                    xlWorkSheet.Cells[rowIndex, Util.ExcelWbColIndex] = allCases[i].WBResult.SpaceSeparate();

                    ++rowIndex;
                }

                xlWorkSheet.Columns.AutoFit();

                // hide the wb result column
                Excel.Range range = (Excel.Range)xlWorkSheet.Columns[Util.ExcelWbColIndex, Type.Missing];
                range.EntireColumn.Hidden = true;

                // delte the existing excel file
                if (File.Exists(outputFilePath))
                {
                    File.Delete(outputFilePath);
                }

                xlWorkBook.SaveAs(outputFilePath,
                    Excel.XlFileFormat.xlWorkbookNormal,
                    misValue, misValue, misValue, misValue,
                    Excel.XlSaveAsAccessMode.xlExclusive,
                    misValue, misValue, misValue, misValue, misValue);

                Console.WriteLine("Successful generate excel to " + outputFilePath);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                xlWorkBook.Close(true, misValue, misValue);
                xlApp.Quit();

                ReleaseExcelObject(xlWorkSheet);
                ReleaseExcelObject(xlWorkBook);
                ReleaseExcelObject(xlApp);
            }
        }
    }
}
