//----------------------------------------------------------------------------
// <copyright file="ScriptGenerator.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Generate training or test script
// </summary>
//----------------------------------------------------------------------------

namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.Tts.Offline;
    using Excel = Microsoft.Office.Interop.Excel;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Generate action.
    /// </summary>
    public enum GenerateAction
    {
        TestCase,
        TrainingScript
    }

    public class ScriptGenerator
    {
        /// <summary>
        /// Generate training script or test case.
        /// </summary>
        /// <param name="excelFilePath">excel file path.</param>
        /// <param name="action">generate training or test script.</param>
        /// <param name="outputDir">output folder.</param>
        /// <param name="outputFileName">if not supply, TrainingFileName(training.xml) for training script, TestCaseFileName(testing.xml) for test case.</param>
        /// <param name="startIndex">training script start index, it could be an path or a number.</param>
        public static void GenScript(string excelFilePath, GenerateAction action, string outputDir, string outputFileName = null, string startIndex = "")
        {
            // initialize the Excel application Object
            Excel.Application xlApp = new Excel.Application();

            // check whether Excel is installed in your system.
            if (xlApp == null)
            {
                Console.WriteLine("Excel is not properly installed!!");
                return;
            }

            Excel.Workbook xlWorkBook = xlApp.Workbooks.Open(excelFilePath, 0, false, 5, string.Empty, string.Empty, false, Excel.XlPlatform.xlWindows, string.Empty, true, false, 0, true, false, false);

            object misValue = System.Reflection.Missing.Value;

            Excel.Worksheet xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
            Excel.Range range = xlWorkSheet.UsedRange;

            try
            {
                if (ExcelHelper.VerifyExcelSheet(xlWorkSheet))
                {
                    // load cases and prons
                    Dictionary<SentenceAndWBResult, string> caseAndProns = ExcelHelper.GetCaseAndPronsFromExcel(range);
                    string outputFilePath;
                    switch (action)
                    {
                        case GenerateAction.TestCase:
                            Util.ConsoleOutTextColor("Generate test cases for " + excelFilePath);
                            
                            // Generate FM Test Cases, if not supply file name, using TestCaseFileName as default name
                            outputFilePath = Path.Combine(outputDir, outputFileName ?? Util.TestCaseFileName);

                            GenRuntimeTestcase(caseAndProns, outputFilePath);
                            break;
                        case GenerateAction.TrainingScript:
                            
                            // Generate training script, if not supply file name, using TrainingFileName as default name
                            Util.ConsoleOutTextColor("Generating training script for " + excelFilePath);
                            outputFilePath = Path.Combine(outputDir, outputFileName ?? Util.TrainingFileName);

                            // currently, the 3rd para is always null, if specified, output file's start index continue with existing script file
                            GenTrainingScript(caseAndProns.ToDictionary(p => p.Key.Content, p => p.Value), outputFilePath, startIndex);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("The excel doesn't contains enough data!");
                    return;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (xlWorkBook != null)
                {
                    xlWorkBook.Close(true, misValue, misValue);
                }

                if (xlApp != null)
                {
                    xlApp.Quit();
                }

                ExcelHelper.ReleaseExcelObject(xlWorkSheet);
                ExcelHelper.ReleaseExcelObject(xlWorkBook);
                ExcelHelper.ReleaseExcelObject(xlApp);
            }
        }

        /// <summary>
        /// Generate test case file.
        /// </summary>
        /// <example>
        /// <cases lang="zh-CN" component="Pronunciation" xmlns="http://schemas.microsoft.com/tts">
        ///   <case priority="P1" category="polyphone" pron_polyword="弹" index="1">
        ///      <input>”怎么变形都能弹回原状此外，此款眼镜还采用了柔性镜腿，精选高弹性塑胶钛制造，镜腿360度旋转不折断无变形。</input>
        ///      <output>
        ///           <part>t a_l nn_h</part>
        ///      </output>
        ///   </case>
        ///   <case priority="P1" category="polyphone" pron_polyword="弹">
        ///        <input>寂静的深山里，顿时炮声隆隆，飞弹呼啸，硝烟弥漫。</input>
        ///        <output>
        ///             <part>d a_h nn_l</part>
        ///        </output>
        ///   </case>
        /// </cases>
        /// </example>
        /// <param name="caseAndPronsWithWb">dictionary contains case and pron and word break result.</param>
        /// <param name="outputFilePath">output xml file path.</param>
        public static void GenRuntimeTestcase(Dictionary<SentenceAndWBResult, string> caseAndPronsWithWb, string outputFilePath)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "     ",
                Encoding = System.Text.Encoding.Unicode,
            };

            using (XmlWriter xtw = XmlTextWriter.Create(outputFilePath, settings))
            {
                xtw.WriteStartElement("cases", Util.TestXmlNamespace);
                xtw.WriteAttributeString("lang", Localor.LanguageToString(LocalConfig.Instance.Lang));
                xtw.WriteAttributeString("component", "Pronunciation");

                string charName = LocalConfig.Instance.CharName;

                foreach (SentenceAndWBResult caseAndWb in caseAndPronsWithWb.Keys)
                {
                    xtw.WriteStartElement("case");
                    xtw.WriteAttributeString("priority", "P1");
                    xtw.WriteAttributeString("category", "polyphone");
                    xtw.WriteAttributeString("pron_polyword", LocalConfig.Instance.CharName);

                    string testCase = caseAndWb.Content;

                    // the content might have more than one target char
                    // e.g. index start from 1 <case priority='P1' category='polyphone' pron_polyword='还' index='2'>
                    int tempIndex = testCase.IndexOf(charName);

                    // make sure this case has more than one char
                    if (tempIndex > -1 && testCase.IndexOf(charName, tempIndex + 1) > -1)
                    {
                        int charIndex = testCase.GetSingleCharIndexOfLine(charName, caseAndWb.WBResult);

                        // if cannot find the single char index, skip this case
                        if (charIndex == -1)
                        {
                            continue;
                        }

                        int charCount = 1;

                        while (tempIndex != charIndex)
                        {
                            ++charCount;
                            tempIndex = testCase.IndexOf(charName, tempIndex + 1);
                        }

                        xtw.WriteAttributeString("index", charCount.ToString());
                    }

                    xtw.WriteStartElement("input");
                    xtw.WriteString(testCase);
                    xtw.WriteEndElement();

                    xtw.WriteStartElement("output");
                    xtw.WriteElementString("part", caseAndPronsWithWb[caseAndWb]);
                    xtw.WriteEndElement();

                    xtw.WriteEndElement();
                }

                xtw.WriteEndElement();
                xtw.WriteEndDocument();
            }

            // genereate a txt file with same name for clear look
            File.WriteAllLines(Util.ChangeFileExtension(outputFilePath, Util.TxtFileExtension), caseAndPronsWithWb.Keys.Select(s => s.Content));

            Console.WriteLine("Generate test case " + outputFilePath);
        }

        /// <summary>
        /// Generate test case file.
        /// </summary>
        /// <param name="caseAndProns">dictionary contains case and pron.</param>
        /// <param name="outputFilePath">output xml file path.</param>
        public static void GenRuntimeTestcase(Dictionary<string, string> caseAndProns, string outputFilePath)
        {
            Dictionary<SentenceAndWBResult, string> caseAndPronsWithWb = new Dictionary<SentenceAndWBResult, string>();

            SentenceAndWBResult tempResult;

            using (WordBreaker wordBreaker = new WordBreaker(LocalConfig.Instance))
            {
                foreach (var item in caseAndProns)
                {
                    tempResult = new SentenceAndWBResult 
                    {
                        Content = item.Key,
                        WBResult = wordBreaker.BreakWords(item.Key)
                    };

                    caseAndPronsWithWb.Add(tempResult, item.Value);
                }
            }

            GenRuntimeTestcase(caseAndPronsWithWb, outputFilePath);
        }

        /// <summary>
        /// Generate training scirpt, if specify the existing script, output file's start index continue with existing script file.
        /// </summary>
        /// <example>
        /// <script language="zh-CN" xmlns="http://schemas.microsoft.com/tts">
        ///   <si id="0000000861">
        ///     <text>莹莹水润的Q弹啫喱质地，脂玉一般清润透亮的色泽，极为清淡的植物清香，很”美味“的样子。</text>
        ///     <sent>
        ///       <text>莹 莹 水 润 的 Q 弹 啫 喱 质地 , 脂 玉 一般 清润 透亮 的 色泽 , 极为 清淡 的 植物 清香 , 很 " 美味 " 的 样子 。</text>
        ///       <words>
        ///         <w v="莹" p="yi el_l ng_h" type="normal" />
        ///         ......
        ///       </words>
        ///     </sent>
        ///   </si>
        ///   <si id="0000000862">
        ///   ......
        ///   </si>
        /// </script>
        /// </example>
        /// <param name="caseAndProns">dictionary contains case and pron.</param>
        /// <param name="outputFilePath">output xml file path.</param>
        /// <param name="startIndexOrFilePath">if it is number, then the start index plus 1, if it as an script file path, the start index will be the last item in the script plus 1.</param>
        public static void GenTrainingScript(Dictionary<string, string> caseAndProns, string outputFilePath, string startIndexOrFilePath = "")
        {
            int startId = 1;

            // if existing script file exist, the generate item's id attribute should be continuted, else start with 1
            if (!string.IsNullOrEmpty(startIndexOrFilePath))
            {
                try
                {
                    if (File.Exists(startIndexOrFilePath))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(startIndexOrFilePath);
                        XmlNodeList list = doc.DocumentElement.ChildNodes;
                        if (list != null && list.Count > 0)
                        {
                            startId = Convert.ToInt32(list.Item(list.Count - 1).Attributes["id"].Value) + 1;
                        }
                    }
                    else
                    {
                        startId = Convert.ToInt32(startIndexOrFilePath) + 1;
                    }
                }
                catch
                {
                    // if convert failed, the start index should be 1
                    startId = 1;
                }
            }

            XmlScriptFile result = new XmlScriptFile(LocalConfig.Instance.Lang);

            foreach (var caseAndPron in caseAndProns)
            {
                string testCase = caseAndPron.Key;

                ScriptItem item = GenerateScriptItem(testCase);

                ScriptWord charWord = item.AllWords.FirstOrDefault(p => p.Grapheme.Equals(LocalConfig.Instance.CharName, StringComparison.InvariantCultureIgnoreCase));

                if (charWord != null)
                {
                    charWord.Pronunciation = caseAndPron.Value;

                    item.Id = Microsoft.Tts.Offline.Utility.Helper.NeutralFormat("{0:D10}", startId);

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

                    result.Items.Add(item);
                    ++startId;
                }
            }

            result.Save(outputFilePath, System.Text.Encoding.Unicode);

            // genereate a txt file with same name for clear look
            File.WriteAllLines(
                               Util.ChangeFileExtension(outputFilePath, Util.TxtFileExtension),
                               caseAndProns.Keys);

            Console.WriteLine("Generate training script " + outputFilePath);
        }

        /// <summary>
        /// Generate script item from raw text(only generate to word level).
        /// </summary>
        /// <param name="text">Plain text.</param>
        /// <returns>ScriptItem.</returns>
        public static ScriptItem GenerateScriptItem(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            ScriptItem item = new ScriptItem();
            item.Text = text;

            using (WordBreaker wordBreaker = new WordBreaker(LocalConfig.Instance))
            {
                foreach (SP.TtsUtterance utt in wordBreaker.EspUtterances(text))
                {
                    using (utt)
                    {
                        if (utt.Words.Count == 0)
                        {
                            continue;
                        }

                        ScriptSentence sentence = new ScriptSentence();
                        foreach (SP.TtsWord word in utt.Words)
                        {
                            if (!string.IsNullOrEmpty(word.WordText))
                            {
                                ScriptWord scriptWord = new ScriptWord();
                                scriptWord.Grapheme = word.WordText;

                                if (!string.IsNullOrEmpty(word.Pronunciation))
                                {
                                    scriptWord.Pronunciation = word.Pronunciation.ToLowerInvariant();
                                }

                                scriptWord.WordType = WordType.Normal;

                                sentence.Words.Add(scriptWord);
                            }
                        }

                        sentence.Text = sentence.BuildTextFromWords();
                        item.Sentences.Add(sentence);
                    }
                }
            }

            return item;
        }
    }
}
