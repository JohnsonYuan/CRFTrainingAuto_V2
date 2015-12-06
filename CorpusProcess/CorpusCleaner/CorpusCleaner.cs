//----------------------------------------------------------------------------
// <copyright file="CorpusCleaner.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      This module implements Cleaner
// </summary>
//----------------------------------------------------------------------------

namespace CorpusCleaner
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Duplicate line manager.
    /// </summary>
    internal class DuplicateLineManager
    {
        #region Const field

        private const int MaxLineBufferNumber = 10000;

        #endregion

        #region Fileds

        private static MD5 _md5 = new MD5CryptoServiceProvider();
        private Encoding _encoding;
        private Dictionary<string, string> _linesMd5Map = new Dictionary<string, string>();
        private List<string> _duplicateMd5List = new List<string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateLineManager" /> class. Constructor.
        /// </summary>
        /// <param name="encoding">Content encoding.</param>
        public DuplicateLineManager(Encoding encoding)
        {
            _encoding = encoding;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Drop duplicate lines.
        /// </summary>
        /// <param name="sourceFilePath">Source file to be processed.</param>
        /// <param name="sourceFileEncoding">Source fiel encoding.</param>
        /// <param name="targetFilePath">Target file path.</param>
        /// <param name="duplicateLogFilePath">Duplicate log file path.</param>
        /// <param name="targetFileEncoding">Target file encoding.</param>
        public void DropDuplicateLines(string sourceFilePath, Encoding sourceFileEncoding,
            string targetFilePath, string duplicateLogFilePath, Encoding targetFileEncoding)
        {
            Helper.EnsureFolderExistForFile(targetFilePath);
            Helper.EnsureFolderExistForFile(duplicateLogFilePath);
            using (StreamWriter targetWriter = new StreamWriter(targetFilePath, false, targetFileEncoding))
            using (StreamWriter duplicateWriter = new StreamWriter(duplicateLogFilePath, false, targetFileEncoding))
            {
                foreach (string line in Helper.FileLines(sourceFilePath, sourceFileEncoding))
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    if (!IsDuplicate(line))
                    {
                        targetWriter.WriteLine(line);
                    }
                    else
                    {
                        duplicateWriter.WriteLine(line);
                    }
                }
            }

            CorpusCleaner.DeleteEmptyFile(targetFilePath, targetFileEncoding);
            CorpusCleaner.DeleteEmptyFile(duplicateLogFilePath, targetFileEncoding);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Get string md5 string.
        /// </summary>
        /// <param name="line">Line to calculate md5.</param>
        /// <param name="encoding">Encoding of the line.</param>
        /// <returns>Md5 string.</returns>
        private static string GetStringMd5(string line, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(line);
            byte[] result = _md5.ComputeHash(bytes);
            StringBuilder sb = new StringBuilder();

            foreach (byte b in result)
            {
                sb.AppendFormat("{0:X2}", b);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Whether the line is duplicate.
        /// </summary>
        /// <param name="line">Line to be checked.</param>
        /// <returns>True: the line is duplicate.
        /// False: the line is not duplicate.</returns>
        private bool IsDuplicate(string line)
        {
            string md5 = GetStringMd5(line, _encoding);
            bool duplicate = false;

            if (!_linesMd5Map.ContainsKey(md5))
            {
                duplicate = false;
                if (_linesMd5Map.Count >= MaxLineBufferNumber)
                {
                    _linesMd5Map.Remove(_duplicateMd5List[0]);
                    _duplicateMd5List.RemoveAt(0);
                }

                _linesMd5Map.Add(md5, null);
                _duplicateMd5List.Add(md5);
            }
            else
            {
                duplicate = true;
            }

            return duplicate;
        }

        #endregion
    }

    /// <summary>
    /// CorpusCleaner class.
    /// </summary>
    internal class CorpusCleaner
    {
        #region Public static class methods.

        /// <summary>
        /// Delete empty files, and empty directory.
        /// </summary>
        /// <param name="dir">Directory to be detected.</param>
        public static void DeleteEmptyFolder(string dir)
        {
            if (!Directory.Exists(dir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), dir);
            }

            foreach (string subDir in Directory.GetDirectories(dir))
            {
                DeleteEmptyFolder(subDir);
            }

            if (Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
            {
                Directory.Delete(dir, false);
            }

            return;
        }

        /// <summary>
        /// Delete file if the file is empty.
        /// </summary>
        /// <param name="filePath">File to be checked.</param>
        /// <param name="encoding">File encoding.</param>
        public static void DeleteEmptyFile(string filePath, Encoding encoding)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            bool empty = true;
            using (StreamReader sr = new StreamReader(filePath, encoding))
            {
                string line = sr.ReadLine();
                if (line != null)
                {
                    empty = false;
                }
            }

            if (empty)
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Apply HTML decode.
        /// </summary>
        /// <param name="sourceFilePath">Source file path.</param>
        /// <param name="sourceFileEncoding">Source file encoding.</param>
        /// <param name="targetFilePath">Target file path.</param>
        /// <param name="targetFileEncoding">Target file encoding.</param>
        public static void HtmlDecode(string sourceFilePath, Encoding sourceFileEncoding,
            string targetFilePath, Encoding targetFileEncoding)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), sourceFilePath);
            }

            Helper.EnsureFolderExistForFile(targetFilePath);
            string fileName = Path.GetFileName(sourceFilePath);

            using (StreamWriter sw = new StreamWriter(targetFilePath, false, targetFileEncoding))
            {
                foreach (string line in Helper.FileLines(sourceFilePath, sourceFileEncoding))
                {
                    sw.WriteLine(HttpUtility.HtmlDecode(line));
                }
            }

            CorpusCleaner.DeleteEmptyFile(targetFilePath, targetFileEncoding);
        }

        /// <summary>
        /// Resize corpus in source corpus to the same size.
        /// </summary>
        /// <param name="sourceDir">Source corpus dir.</param>
        /// <param name="sourceFileEncoding">Source corpus encoding.</param>
        /// <param name="targetFileDir">Target corpus dir.</param>
        /// <param name="targetFileEncoding">Target file encoding.</param>
        /// <param name="targetFileSize">Target corpus file size.</param>
        /// <param name="targetFileNameFormat">Target file name format.</param>
        /// <returns>Resized corpus file path list.</returns>
        public static List<string> ResizeFiles(
            string sourceDir, Encoding sourceFileEncoding,
            string targetFileDir, Encoding targetFileEncoding,
            long targetFileSize, string targetFileNameFormat)
        {
            Helper.EnsureFolderExist(targetFileDir);
            List<string> sourceCorpusFilesPath = new List<string>();
            foreach (string corpusFilePath in Directory.GetFiles(sourceDir))
            {
                sourceCorpusFilesPath.Add(corpusFilePath);
            }

            return ResizeFiles(sourceCorpusFilesPath, sourceFileEncoding,
                targetFileDir, targetFileEncoding,
                targetFileSize, targetFileNameFormat);
        }

        /// <summary>
        /// Resize corpus in source corpus to the same size.
        /// </summary>
        /// <param name="sourceFilesPath">Soruce corpus file path list.</param>
        /// <param name="sourceFileEncoding">Source corpus encoding.</param>
        /// <param name="targetDir">Target corpus directory.</param>
        /// <param name="targetFileEncoding">Target file encoding.</param>
        /// <param name="targetFileSize">Target corpus file size.</param>
        /// <param name="targetFileNameFormat">Target file name format.</param>
        /// <returns>Resized corpus file path list.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Reviewed. Suppression is OK here.")]
        public static List<string> ResizeFiles(
            List<string> sourceFilesPath, Encoding sourceFileEncoding,
            string targetDir, Encoding targetFileEncoding,
            long targetFileSize, string targetFileNameFormat)
        {
            if (sourceFilesPath == null)
            {
                throw new ArgumentNullException("sourceFilesPath");
            }

            Helper.EnsureFolderExist(targetDir);

            List<string> targetCorpusFilesPath = new List<string>();
            int targetCorpusIndex = 0;
            StreamWriter sw = null;

            foreach (string corpusFilePath in sourceFilesPath)
            {
                foreach (string line in Helper.FileLines(corpusFilePath, sourceFileEncoding))
                {
                    if (sw == null)
                    {
                        targetCorpusIndex++;
                        string targetCorpusFilePath = Path.Combine(targetDir,
                            Helper.NeutralFormat(targetFileNameFormat, targetCorpusIndex));
                        sw = new StreamWriter(targetCorpusFilePath, false, targetFileEncoding);
                    }

                    sw.WriteLine(line);
                    sw.Flush();

                    if (sw.BaseStream.Length > targetFileSize)
                    {
                        sw.Dispose();
                        sw = null;
                    }
                }
            }

            if (sw != null)
            {
                sw.Dispose();
                sw = null;
            }

            return targetCorpusFilesPath;
        }

        /// <summary>
        /// Convert file to unicode.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="sourceCorpusEncoding">Source corpus encoding.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="targetCorpusEncoding">Target file encoding.</param>
        public static void ConvertEncoding(
            string sourceCorpusFilePath, Encoding sourceCorpusEncoding,
            string targetCorpusFilePath, Encoding targetCorpusEncoding)
        {
            Helper.EnsureFolderExistForFile(targetCorpusFilePath);

            using (StreamWriter sw = new StreamWriter(targetCorpusFilePath, false, targetCorpusEncoding))
            {
                foreach (string line in Helper.FileLines(sourceCorpusFilePath, sourceCorpusEncoding))
                {
                    sw.WriteLine(line);
                }
            }

            DeleteEmptyFile(targetCorpusFilePath, targetCorpusEncoding);
        }

        /// <summary>
        /// The method will merge the two lines which only use "\r\n" as line separator into one line.
        /// Then separate the lines using "lineEndingPunctuations"
        /// TODO: need further discussion whether use this feature, for example: title, U.S.A.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="sourceCorpusEncoding">Source corpus encoding.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="targetCorpusEncoding">Target corpus encoding.</param>
        /// <param name="lineEndingPunctuations">Line ending punctuations.</param>
        public static void MergeLines(
            string sourceCorpusFilePath, Encoding sourceCorpusEncoding,
            string targetCorpusFilePath, Encoding targetCorpusEncoding,
            List<string> lineEndingPunctuations)
        {
            Helper.EnsureFolderExistForFile(targetCorpusFilePath);

            using (StreamWriter sw = new StreamWriter(targetCorpusFilePath, false, targetCorpusEncoding))
            {
                foreach (string sourceCorpusLine in Helper.FileLines(sourceCorpusFilePath, sourceCorpusEncoding))
                {
                    // trim the unncessary blank character at the tail
                    string trimedLine = sourceCorpusLine.Trim();
                    sw.Write(trimedLine);

                    bool lineEnding = false;

                    if (trimedLine.Length > 2 && trimedLine[trimedLine.Length - 1] == '.' &&  // if the last character is dot, consider merging two lines.
                        ((char.IsUpper(trimedLine[trimedLine.Length - 2]) && trimedLine[trimedLine.Length - 3] == ' ') || // the last space-separated token is a single uppercase character followed by dot
                        (trimedLine.Length > 3 && trimedLine.Substring(trimedLine.Length - 4) == " Mr.") ||
                        (trimedLine.Length > 4 && trimedLine.Substring(trimedLine.Length - 5)  == " Mrs.") ||
                        (trimedLine.Length > 3 && trimedLine.Substring(trimedLine.Length - 4)  == " Ms.") ||
                        trimedLine.Substring(trimedLine.LastIndexOf(' ') + 1, trimedLine.Length - trimedLine.LastIndexOf(' ') - 2).Contains(".")))
                    {
                        sw.Write(" ");
                        continue;
                    }

                    foreach (string lineEndingPunctuation in lineEndingPunctuations)
                    {
                        if (trimedLine.Length - lineEndingPunctuation.Length >= 0 &&
                            string.CompareOrdinal(lineEndingPunctuation, 0,
                            trimedLine, trimedLine.Length - lineEndingPunctuation.Length,
                            lineEndingPunctuation.Length) == 0)
                        {
                            sw.WriteLine();
                            lineEnding = true;
                            break;
                        }
                    }

                    if (!lineEnding)
                    {
                        sw.Write(" ");
                    }
                }
            }

            DeleteEmptyFile(targetCorpusFilePath, targetCorpusEncoding);
        }

        /// <summary>
        /// Drop the sentences contains invalid chars.
        /// </summary>
        /// <param name="soruceCorpusFilePath">Source corpus file path.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="targetCorpusEncoding">Target corpus encoding.</param>
        /// <param name="invalidSentencesFilePath">Invalid sentence file path.</param>
        /// <param name="corpusCharRangesInclude">Valid char include value range.</param>
        /// <param name="corpusCharRangesExclude">Valid char exclude value range.</param>
        /// <returns>Invalid char statistic.</returns>
        public static Dictionary<char, long> DropInvalidChars(
            string soruceCorpusFilePath, 
            string targetCorpusFilePath, Encoding targetCorpusEncoding,
            string invalidSentencesFilePath,
            UnicodeCharRanges corpusCharRangesInclude,
            UnicodeCharRanges corpusCharRangesExclude)
        {
            Helper.EnsureFolderExistForFile(targetCorpusFilePath);
            Helper.EnsureFolderExistForFile(invalidSentencesFilePath);
            Dictionary<char, long> invalidCharDictionary = new Dictionary<char, long>();

            using (StreamWriter targetCorpusWriter = new StreamWriter(targetCorpusFilePath, false, targetCorpusEncoding))
            using (StreamWriter invalidCorpusWriter = new StreamWriter(invalidSentencesFilePath, false, targetCorpusEncoding))
            {
                foreach (string line in Helper.FileLines(soruceCorpusFilePath))
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    bool containInvalidChar = false;

                    byte[] bytes = Encoding.Unicode.GetBytes(line);

                    Trace.Assert(bytes.Length == line.Length * 2);

                    for (int i = 0; i < line.Length; i++)
                    {
                        char currentChar = line[i];
                        short encodingValue = BitConverter.ToInt16(bytes, i * 2);
                        if (!IsValidChar(encodingValue, corpusCharRangesInclude, corpusCharRangesExclude))
                        {
                            containInvalidChar = true;
                            if (!invalidCharDictionary.ContainsKey(currentChar))
                            {
                                invalidCharDictionary.Add(currentChar, 1);
                            }
                            else
                            {
                                invalidCharDictionary[currentChar]++;
                            }
                        }
                    }

                    if (!containInvalidChar)
                    {
                        targetCorpusWriter.WriteLine(line);
                    }
                    else
                    {
                        invalidCorpusWriter.WriteLine(line);
                    }
                }
            }

            DeleteEmptyFile(targetCorpusFilePath, targetCorpusEncoding);
            DeleteEmptyFile(invalidSentencesFilePath, targetCorpusEncoding);

            return invalidCharDictionary;
        }

        /// <summary>
        /// Filter words per sentence on file.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Soruce corpus file path.</param>
        /// <param name="sourceCorpusEncoding">Source corpus file encoding.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="targetCorpusEncoding">Target corpus encoding.</param>
        /// <param name="deletedSentencesCorpusPath">Deleted sentence file path.</param>
        /// <param name="maxCharNumPerLine">Min word per sentence.</param>
        public static void FilterLineLength(
            string sourceCorpusFilePath, Encoding sourceCorpusEncoding,
            string targetCorpusFilePath, Encoding targetCorpusEncoding,
            string deletedSentencesCorpusPath, int maxCharNumPerLine)
        {
            Helper.EnsureFolderExistForFile(targetCorpusFilePath);
            Helper.EnsureFolderExistForFile(deletedSentencesCorpusPath);

            using (StreamWriter targetCorpusWriter = new StreamWriter(targetCorpusFilePath, false, targetCorpusEncoding))
            using (StreamWriter deleteSentenceCorpusWriter = new StreamWriter(deletedSentencesCorpusPath, false, targetCorpusEncoding))
            {
                foreach (string line in Helper.FileLines(sourceCorpusFilePath, sourceCorpusEncoding))
                {
                    if (string.IsNullOrEmpty(line) || string.IsNullOrEmpty(line.Trim()))
                    {
                        continue;
                    }

                    string trimmedLine = line.Trim();
                    bool validLine = trimmedLine.Length <= maxCharNumPerLine;
                    if (validLine)
                    {
                        validLine = false;
                        foreach (char c in trimmedLine)
                        {
                            // Delete the line if all characters are punctuation.
                            if (!char.IsPunctuation(c) && !char.IsSymbol(c) && !char.IsWhiteSpace(c))
                            {
                                validLine = true;
                                break;
                            }
                        }
                    }

                    if (validLine)
                    {
                        targetCorpusWriter.WriteLine(trimmedLine);
                    }
                    else
                    {
                        deleteSentenceCorpusWriter.WriteLine(trimmedLine);
                    }
                }
            }

            DeleteEmptyFile(targetCorpusFilePath, targetCorpusEncoding);
            DeleteEmptyFile(deletedSentencesCorpusPath, targetCorpusEncoding);
        }

        /// <summary>
        /// Apply regex rules on file.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Soruce corpus file path.</param>
        /// <param name="sourceCorpusEncoding">Source corpus encoding.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="deleteSentenceFilePath">Deleted sentences file path.</param>
        /// <param name="deleteWordFilePath">Deleted words file path.</param>
        /// <param name="targetCorpusEncoding">Target corpus encoding.</param>
        /// <param name="rules">Regex rules to apply.</param>
        public static void ApplyRegexRulesOnFile(
            string sourceCorpusFilePath, Encoding sourceCorpusEncoding,
            string targetCorpusFilePath,
            string deleteSentenceFilePath, string deleteWordFilePath,
            Encoding targetCorpusEncoding,
            List<RegexRule> rules)
        {
            Helper.EnsureFolderExistForFile(targetCorpusFilePath);
            Helper.EnsureFolderExistForFile(deleteSentenceFilePath);
            Helper.EnsureFolderExistForFile(deleteWordFilePath);

            using (StreamWriter targetCorpusWriter = new StreamWriter(targetCorpusFilePath, false, targetCorpusEncoding))
            using (StreamWriter deleteSentenceCorpusWriter = new StreamWriter(deleteSentenceFilePath, false, targetCorpusEncoding))
            using (StreamWriter deleteWordCorpusWriter = new StreamWriter(deleteWordFilePath, false, targetCorpusEncoding))
            {
                foreach (string line in Helper.FileLines(sourceCorpusFilePath, sourceCorpusEncoding))
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string result = line;
                    foreach (RegexRule rule in rules)
                    {
                        if (rule.RegexType == RegexRule.RegexRuleType.Delete && rule.DeleteLine)
                        {
                            Match match = Regex.Match(result, rule.Pattern);
                            if (match.Success)
                            {
                                deleteSentenceCorpusWriter.WriteLine(result);
                                result = string.Empty;
                                break;
                            }
                        }
                        else if (rule.RegexType == RegexRule.RegexRuleType.Delete && !rule.DeleteLine)
                        {
                            string deletedWordList = GetDeletedWords(result, rule.Pattern);
                            if (!string.IsNullOrEmpty(deletedWordList))
                            {
                                deleteWordCorpusWriter.WriteLine(deletedWordList);
                                result = Regex.Replace(result, rule.Pattern, string.Empty);
                            }
                        }
                        else if (rule.RegexType == RegexRule.RegexRuleType.Replace)
                        {
                            result = Regex.Replace(result, rule.Pattern, rule.Replacement);
                        }
                    }

                    if (!string.IsNullOrEmpty(result))
                    {
                        targetCorpusWriter.WriteLine(result);
                    }
                }
            }

            DeleteEmptyFile(targetCorpusFilePath, targetCorpusEncoding);
            DeleteEmptyFile(deleteSentenceFilePath, targetCorpusEncoding);
            DeleteEmptyFile(deleteWordFilePath, targetCorpusEncoding);
        }

        #endregion

        #region Private static methods.

        /// <summary>
        /// Get the deleted word collection when apply replace the pattern to empty string.
        /// </summary>
        /// <param name="content">Content to detect.</param>
        /// <param name="pattern">Delete pattern.</param>
        /// <returns>Deleted word list string.</returns>
        private static string GetDeletedWords(string content, string pattern)
        {
            List<string> result = new List<string>();

            Match match = Regex.Match(content, pattern);
            while (match.Success)
            {
                result.Add(content.Substring(match.Index, match.Length));
                content = content.Substring(match.Index + match.Length);
                match = Regex.Match(content, pattern);
            }

            string deletedWordListString = string.Empty;
            if (result.Count > 0)
            {
                deletedWordListString = string.Join(" ", result.ToArray());
            }

            return deletedWordListString;
        }

        /// <summary>
        /// Check char whether in valid char range.
        /// If both corpusCharRangesInclude and corpusCharRangesExclude are emtpy:
        ///     Any chars is valid;
        /// If only corpusCharRangesInclude range is empty:
        ///     The char should not be included in corpusCharRangesExclude;
        /// If only corpusCharRangesExclude range is empty:
        ///     The char must be included in corpusCharRangesInclude;
        /// If both corpusCharRangesInclude and corpusCharRangesExclude are not emtpy:
        ///     The char should be included in corpusCharRangesInclude but not in corpusCharRangesExclude;.
        /// </summary>
        /// <param name="encodingValue">Encoding value of the char.</param>
        /// <param name="corpusCharRangesInclude">Valid char include value range.</param>
        /// <param name="corpusCharRangesExclude">Valid char exclude value range.</param>
        /// <returns>Whether the char is in the range.</returns>
        private static bool IsValidChar(int encodingValue, UnicodeCharRanges corpusCharRangesInclude,
            UnicodeCharRanges corpusCharRangesExclude)
        {
            bool isValidChar = false;

            if (corpusCharRangesInclude.IsEmpty &&
                corpusCharRangesExclude.IsEmpty)
            {
                isValidChar = true;
            }
            else if (corpusCharRangesInclude.IsEmpty &&
                !corpusCharRangesExclude.IsEmpty)
            {
                isValidChar = !corpusCharRangesExclude.IsInRange(encodingValue);
            }
            else if (!corpusCharRangesInclude.IsEmpty &&
                corpusCharRangesExclude.IsEmpty)
            {
                isValidChar = corpusCharRangesInclude.IsInRange(encodingValue);
            }
            else if (!corpusCharRangesInclude.IsEmpty &&
                !corpusCharRangesExclude.IsEmpty)
            {
                isValidChar = corpusCharRangesInclude.IsInRange(encodingValue) &&
                    !corpusCharRangesExclude.IsInRange(encodingValue);
            }

            return isValidChar;
        }

        #endregion
    }
}
