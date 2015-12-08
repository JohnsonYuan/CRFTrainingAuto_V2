namespace CRFTrainingAuto
{
    using Microsoft.Tts.Offline.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Excel = Microsoft.Office.Interop.Excel;

    public static class ExcelHelper
    {
        /// <summary>
        /// Each Dictionary item contains the case and the pron
        /// </summary>
        /// <param name="usedRange">Excel used range</param>
        /// <param name="useNavtivePhone">if true, the return value's pron is native pron, else use pinyin</param>
        /// <returns>Dictionary contains case and native pron</returns>
        public static Dictionary<SentenceAndWbResult, string> GetCaseAndPronsFromExcel(Excel.Range usedRange, bool useNavtivePhone = true)
        {
            Dictionary<SentenceAndWbResult, string> caseAndProns = new Dictionary<SentenceAndWbResult, string>();

            for (int rCnt = 2; rCnt <= usedRange.Rows.Count; rCnt++)
            {
                SentenceAndWbResult tempReseult = new SentenceAndWbResult();

                // first column for case, second column for corrct pron
                string caseVal = Convert.ToString((usedRange.Cells[rCnt, GlobalVar.ExcelCaseColIndex] as Excel.Range).Value2);
                tempReseult.Content = caseVal.Replace(" ", "").Replace("\t", "");
                string wbResult = Convert.ToString((usedRange.Cells[rCnt, GlobalVar.ExcelWbColIndex] as Excel.Range).Value2);
                tempReseult.WbResult = wbResult.SplitBySpace();
                
                string pinYinPron = Convert.ToString((usedRange.Cells[rCnt, GlobalVar.ExcelCorrectPronColIndex] as Excel.Range).Value2);
                if (string.IsNullOrWhiteSpace(pinYinPron))
                {
                    throw new Exception(string.Format("Excel in line {0} doesn't provide the pron.", rCnt));
                }

                pinYinPron = pinYinPron.Trim();

                if (string.IsNullOrEmpty(caseVal) ||
                    string.IsNullOrEmpty(pinYinPron))
                {
                    throw new Exception(string.Format("Excel file in row {0} has the empty case or pron!", rCnt));
                }

                if (!caseAndProns.ContainsKey(tempReseult))
                {
                    // check the pron is in config file
                    if ((GlobalVar.Config.Prons.ContainsKey(pinYinPron) &&
                        !string.IsNullOrEmpty(GlobalVar.Config.Prons[pinYinPron])))
                    {
                        // if don't use native phone, we add the pinyin pron to result
                        if (!useNavtivePhone)
                        {
                            caseAndProns.Add(tempReseult, pinYinPron);
                        }
                        else
                        {
                            caseAndProns.Add(tempReseult, GlobalVar.Config.Prons[pinYinPron]);
                        }
                    }
                    else
                    {
                        string errorMsg = string.Format("Excel file in row {0} has the wrong pron \"{1}\"! It should like ", rCnt, pinYinPron);
                        foreach (string val in GlobalVar.Config.Prons.Keys)
                        {
                            errorMsg += val + " ";
                        }
                        errorMsg += ".";
                        throw new Exception(errorMsg);
                    }
                }
            }
            return caseAndProns;
        }

        /// <summary>
        /// Generate Excel From caseAndProns
        /// </summary>
        /// </summary>
        /// <param name="caseAndProns">case and pron</param>
        /// <param name="outputFilePath">output file path</param>
        public static void GenExcelFromCaseAndProns(IEnumerable<KeyValuePair<SentenceAndWbResult, string>> caseAndProns, string outputFilePath)
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
                xlWorkSheet.Name = GlobalVar.Config.CharName;
                xlWorkSheet.Cells[1, GlobalVar.ExcelCaseColIndex] = GlobalVar.ExcelCaseColTitle;
                xlWorkSheet.Cells[1, GlobalVar.ExcelCorrectPronColIndex] = GlobalVar.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, GlobalVar.ExcelCommentColIndex] = GlobalVar.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, GlobalVar.ExcelWbColIndex] = GlobalVar.ExcelWbColTitle;


                int rowIndex = 2;

                foreach (KeyValuePair<SentenceAndWbResult, string> item in caseAndProns)
                {
                    xlWorkSheet.Cells[rowIndex, GlobalVar.ExcelCaseColIndex] = item.Key.Content;
                    xlWorkSheet.Cells[rowIndex, GlobalVar.ExcelCorrectPronColIndex] = item.Value;

                    Excel.Range xlRange = (Excel.Range)xlWorkSheet.Cells[rowIndex, 1];
                    int startIndex = item.Key.Content.GetSingleCharIndexOfLine(GlobalVar.Config.CharName, item.Key.WbResult);
                    if (startIndex > -1)
                    {
                        xlRange.Characters[startIndex + 1, 1].Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                    }

                    xlWorkSheet.Cells[rowIndex, GlobalVar.ExcelWbColIndex] = item.Key.WbResult.SpaceSeparate();

                    ++rowIndex;
                }

                // hide the wb result column
                Excel.Range range = (Excel.Range)xlWorkSheet.Columns[GlobalVar.ExcelWbColIndex, Type.Missing];
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
        /// first row it the title line, 2 columns one for case, one for pron
        /// </summary>
        /// <param name="xlSheet">excel sheet</param>
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
        /// Properly clean up Excel interop objects
        /// </summary>
        /// <param name="obj">excel object</param>
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
        /// Divide Excel file to training and test part
        /// </summary>
        /// <param name="excelFilePath">excel file path</param>
        /// <param name="outputDir">output folder</param>
        /// <param name="trainingExcelFilePath">training excel file path</param>
        /// <param name="testExcelFilePath">test excel file path</param>
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

            Excel.Workbook xlWorkBook = xlApp.Workbooks.Open(excelFilePath, 0, false, 5, "", "", false, Excel.XlPlatform.xlWindows, "",
                        true, false, 0, true, false, false);
            object misValue = System.Reflection.Missing.Value;

            Excel.Worksheet xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            Dictionary<SentenceAndWbResult, string> caseAndProns = GetCaseAndPronsFromExcel(xlWorkSheet.UsedRange, false);

            // generate training and test script
            if (caseAndProns != null && caseAndProns.Count() > GlobalVar.Config.NCrossCaseCount)
            {
                // generate excel for training, e.g. corpusCountFilePath = "corpus.1000.xls"
                trainingExcelFilePath = Path.Combine(outputDir, string.Format(GlobalVar.CorpusExcelFileNamePattern, GlobalVar.Config.NCrossCaseCount));
                var trainingCaseAndProns = caseAndProns.Where((input, index) => (index >= 0 && index < GlobalVar.Config.NCrossCaseCount));

                Util.ConsoleOutTextColor(string.Format("Split {0} case from {1}, saved to {2}.", GlobalVar.Config.NCrossCaseCount, excelFilePath, trainingExcelFilePath));
                GenExcelFromCaseAndProns(trainingCaseAndProns, trainingExcelFilePath);

                // generate excel for test cases, e.g. corpusCountFilePath = "corpus.500.xls"
                int testCount = GlobalVar.Config.MaxCaseCount - GlobalVar.Config.NCrossCaseCount;
                testExcelFilePath = Path.Combine(outputDir, string.Format(GlobalVar.CorpusExcelFileNamePattern, testCount));
                var testCaseAndProns = caseAndProns.Where((input, index) => (index >= GlobalVar.Config.NCrossCaseCount));

                Util.ConsoleOutTextColor(string.Format("Split {0} case from {1}, saved to {2}.", testCount, excelFilePath, testExcelFilePath));
                GenExcelFromCaseAndProns(testCaseAndProns, testExcelFilePath);
            }
            else
            {
                Util.ConsoleOutTextColor(string.Format("The excel file doesn't content min {0} cases for training.", GlobalVar.Config.NCrossCaseCount), ConsoleColor.Red);
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
        /// 
        /// test report is like this:
        /// POLYPHONE: 背
        /// INPUT: (P1)
        /// 与其让少部分阿姨们脱离家政公司，辞掉工作，穿上围裙背上工具包立马跳上O2O的大船，我们宁愿张开怀抱，迎接所有有活的，没活的，想赚钱的，不想赚钱的，还有不少去了58又走了的阿姨。
        /// EXPECTED: 
        /// b eh_h i_h /
        /// RESULT: 
        /// b eh_h i_l /
        /// 
        /// </summary>
        /// <param name="testResultFile">test result file path</param>
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

                    switch (currentLine)
                    {
                        case "INPUT: (P1)":
                            if (reader.Peek() > -1)
                            {
                                inputLines.Add(reader.ReadLine().Trim());
                            }
                            break;
                        case "EXPECTED:":
                            if (reader.Peek() > -1)
                            {
                                expectedLines.Add(reader.ReadLine().TrimEnd(new char[] { '/', ' '}));
                            }
                            break;
                        case "RESULT:":
                            if (reader.Peek() > -1)
                            {
                                resultLines.Add(reader.ReadLine().TrimEnd(new char[] { '/', ' ' }));
                            }
                            break;
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

                try
                {
                    xlWorkSheet.Name = GlobalVar.Config.CharName;
                    xlWorkSheet.Cells[1, 1] = "input";
                    xlWorkSheet.Cells[1, 2] = "expected";
                    xlWorkSheet.Cells[1, 3] = "result";

                    int rowIndex = 2;

                    for (int i = 0; i < inputLines.Count; i++)
                    {
                        xlWorkSheet.Cells[rowIndex, 1] = inputLines[i];

                        // highlight the target cahr
                        Excel.Range xlRange = (Excel.Range)xlWorkSheet.Cells[rowIndex, 1];
                        int startIndex = inputLines[i].GetSingleCharIndexOfLine(GlobalVar.Config.CharName, GlobalVar.WordBreaker);
                        if (startIndex > -1)
                        {
                            xlRange.Characters[startIndex + 1, 1].Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                        }

                        xlWorkSheet.Cells[rowIndex, 2] = expectedLines[i];
                        xlWorkSheet.Cells[rowIndex, 3] = resultLines[i];

                        ++rowIndex;
                    }

                    xlWorkSheet.Columns.AutoFit();

                    string outputFilePath = Path.Combine(Path.GetDirectoryName(testResultFile), GlobalVar.VerifyResultExcelFileName);

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
        /// Divide 1000 corpus to 10 separate testing and training part
        /// </summary>
        /// <param name="excelFile">excel file path</param>
        /// <param name="outputDir">output folder</param>
        public static void GenNCrossExcel(string excelFile, string outputDir)
        {
            // initialize the Excel application Object
            Excel.Application xlApp = new Excel.Application();

            // check whether Excel is installed in your system.
            if (xlApp == null)
            {
                Console.WriteLine("Excel is not properly installed!!");
                return;
            }

            Excel.Workbook xlWorkBook = xlApp.Workbooks.Open(excelFile, 0, false, 5, "", "", false, Excel.XlPlatform.xlWindows, "",
                        true, false, 0, true, false, false);
            object misValue = System.Reflection.Missing.Value;

            Excel.Worksheet xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            Dictionary<SentenceAndWbResult, string> caseAndProns = GetCaseAndPronsFromExcel(xlWorkSheet.UsedRange);

            const int count = 100;

            // Generate 10 cross folder
            for (int i = 0; i < GlobalVar.Config.NFolderCount; i++)
            {
                var testingcaseAndProns = caseAndProns.Where((kvPair, index) =>
                    index >= i * count && index < (i + 1) * count);
                var trainingcaseAndProns = caseAndProns.Where((kvPair, index) =>
                    (index >= 0 && index < i * count) || (index >= (i + 1) * count && index < 1000));

                string dirPath = Path.Combine(outputDir, (i + 1).ToString());
                string trainingFolder = Path.Combine(dirPath, GlobalVar.TrainingFolderName);

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (!Directory.Exists(trainingFolder))
                {
                    Directory.CreateDirectory(trainingFolder);
                }

                Console.WriteLine("Generating test and training script to " + dirPath);

                ScriptGenerator.GenRuntimeTestcase(testingcaseAndProns.ToDictionary(p => p.Key, p => p.Value), Path.Combine(dirPath, GlobalVar.TestCaseFileName));
                ScriptGenerator.GenTrainingScript(trainingcaseAndProns.ToDictionary(p => p.Key, p => p.Value), Path.Combine(trainingFolder, GlobalVar.TrainingFileName));
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
        /// Generated Excel file contains 3 column: "case", "correct pron" and "comment"
        /// </summary>
        /// <param name="inFilePath">txt file path</param>
        /// <param name="outputDir">output file path</param>
        /// <param name="isNeedWb">if true, need use wrod breaker genereate the word break result, false the input file contains the word break result</param>
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
                xlWorkSheet.Name = GlobalVar.Config.CharName;
                xlWorkSheet.Cells[1, GlobalVar.ExcelCaseColIndex] = GlobalVar.ExcelCaseColTitle;
                xlWorkSheet.Cells[1, GlobalVar.ExcelCorrectPronColIndex] = GlobalVar.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, GlobalVar.ExcelCommentColIndex] = GlobalVar.ExcelCorrectPronColTitle;
                xlWorkSheet.Cells[1, GlobalVar.ExcelWbColIndex] = GlobalVar.ExcelWbColTitle;

                var allCases = Util.GetSenAndWbFromCorpus(inFilePath, hasWbResult);
                
                // Excel start index is 1, the content row start 2
                int rowIndex = 2;

                // fill the sheet
                for (int i = 0; i < allCases.Count; i++)
                {
                    xlWorkSheet.Cells[rowIndex, GlobalVar.ExcelCaseColIndex] = allCases[i].Content;

                    // highlight the training character
                    Excel.Range xlRange = (Excel.Range)xlWorkSheet.Cells[rowIndex, 1];
                    int startIndex = allCases[i].Content.GetSingleCharIndexOfLine(GlobalVar.Config.CharName, allCases[i].WbResult);
                    xlRange.Characters[startIndex + 1, GlobalVar.ExcelCaseColIndex].Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);

                    xlWorkSheet.Cells[rowIndex, GlobalVar.ExcelWbColIndex] = allCases[i].WbResult.SpaceSeparate();

                    ++rowIndex;
                }

                xlWorkSheet.Columns.AutoFit();

                // hide the wb result column
                Excel.Range range = (Excel.Range)xlWorkSheet.Columns[GlobalVar.ExcelWbColIndex, Type.Missing];
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
