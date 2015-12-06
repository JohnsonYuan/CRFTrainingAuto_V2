//----------------------------------------------------------------------------
// <copyright file="Program.cs" company="MICROSOFT">
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
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// CorpusCleaner arguments.
    /// </summary>
    [Comment("Corpus cleaner tool converts original corpus into well formatted cleaned corpus.",
        "Copyright (C) Microsoft Corporation. All rights reserved.")]
    internal class Arguments
    {
        #region Fields

        [Argument("config", Description = "Specifies the location of the data cleaning configuration file",
            Optional = false, UsagePlaceholder = "configFile")]
        private string _configFilePath = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets location of the config file path.
        /// </summary>
        public string ConfigFilePath
        {
            get { return _configFilePath; }
        }

        #endregion
    }

    /// <summary>
    /// Corpus cleaner program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main of CorpusCleaner.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>0:Succeeded; -1:Catch exception.</returns>
        public static int Main(string[] args)
        {
            return ConsoleApp<Arguments>.Run(args, Process);
        }

        /// <summary>
        /// Main process function of CorpusCleaner.
        /// </summary>
        /// <param name="arguments">Command line arguments.</param>
        /// <returns>If it finished successfully, then return successful code.</returns>
        private static int Process(Arguments arguments)
        {
            CorpusCleanerConfig config = new CorpusCleanerConfig();
            config.Load(arguments.ConfigFilePath);
            DateTime start = DateTime.Now;
            CleanCorpus(config);
            TimeSpan duration = DateTime.Now.Subtract(start);
            AppendAllTextLine(config.LogFilePath, Helper.NeutralFormat("Total cost : {0}", duration.ToString()), true);

            return ExitCode.NoError;
        }

        #region Private static methods

        /// <summary>
        /// Append all text in a new line to a file.
        /// </summary>
        /// <param name="filePath">File path to be appended.</param>
        /// <param name="content">Content to be appended.</param>
        /// <param name="printScreen">Whether print the log to screen.</param>
        private static void AppendAllTextLine(string filePath, string content, bool printScreen)
        {
            Helper.EnsureFolderExistForFile(filePath);
            File.AppendAllText(filePath, content + Environment.NewLine, Encoding.Unicode);
            if (printScreen)
            {
                Console.Error.WriteLine(content);
            }
        }

        /// <summary>
        /// Append all text to the file.
        /// </summary>
        /// <param name="filePath">File path to be appneded.</param>
        /// <param name="content">Content to be appened.</param>
        private static void AppendAllTextLine(string filePath, string content)
        {
            AppendAllTextLine(filePath, content, false);
        }

        /// <summary>
        /// Process all corpus.
        /// </summary>
        /// <param name="config">CorpusCleanerConfig.</param>
        private static void CleanCorpus(CorpusCleanerConfig config)
        {
            List<string> droppedFiles = new List<string>();
            List<string> targetFiles = new List<string>();

            foreach (SourceCorpusConfig sourceCorpusConfig in config.SourceCorpusConfigs)
            {
                CleanOneTypeCorpus(sourceCorpusConfig, config, targetFiles, droppedFiles);
            }

            // Combine final target corpus.
            CorpusCleaner.ResizeFiles(droppedFiles, CorpusCleanerConfig.TargetCorpusEncoding,
                config.TargetCorpusDir, CorpusCleanerConfig.TargetCorpusEncoding,
                config.TargetCorpusFileSize, "FilteredOutSentences_{0}.txt");

            // Combine error sentences.
            CorpusCleaner.ResizeFiles(targetFiles, CorpusCleanerConfig.TargetCorpusEncoding,
                config.TargetCorpusDir, CorpusCleanerConfig.TargetCorpusEncoding,
                config.TargetCorpusFileSize, "Corpus_{0}.txt");

            CorpusCleaner.DeleteEmptyFolder(config.MidtermDir);
        }

        /// <summary>
        /// Whole process to clean corpus.
        /// </summary>
        /// <param name="sourceCorpusConfig">Config of source raw corpus.</param>
        /// <param name="config">Corpus cleaner config.</param>
        /// <param name="targetFiles">Target files name.</param>
        /// <param name="droppedFiles">Dropped files name.</param>
        private static void CleanOneTypeCorpus(SourceCorpusConfig sourceCorpusConfig,
            CorpusCleanerConfig config, List<string> targetFiles, List<string> droppedFiles)
        {
            Helper.EnsureFolderExist(config.TargetCorpusDir);

            List<string> processFiles = new List<string>();
            foreach (string searchPattern in sourceCorpusConfig.SearchPatterns)
            {
                processFiles.AddRange(FileFilter.GetFilesPath(config.SourceCorpusDir,
                    searchPattern, config.ExcludeFileFilters));
            }

            string targetCorpusDir = sourceCorpusConfig.MidtermDirSameFileSize;

            AppendAllTextLine(config.LogFilePath, Helper.NeutralFormat("Resize corpus to [{0}], from [{1}] to [{2}].", config.TargetCorpusFileSizeString, config.SourceCorpusDir, sourceCorpusConfig.MidtermDirSameFileSize), true);

            StringBuilder sb = new StringBuilder();
            foreach (string file in processFiles)
            {
                sb.AppendLine(file);
            }

            AppendAllTextLine(config.LogFilePath, Helper.NeutralFormat("Source file list(FileNumber={0}):{1}{2}", processFiles.Count, Environment.NewLine, sb.ToString()));

            // Resize corpus and convert to Unicode.
            CorpusCleaner.ResizeFiles(processFiles, sourceCorpusConfig.Encoding, targetCorpusDir, CorpusCleanerConfig.TargetCorpusEncoding, config.TargetCorpusFileSize, "Corpus_{0}.txt");

            DuplicateLineManager duplicateLineBeforeMergeManager =
                new DuplicateLineManager(CorpusCleanerConfig.TargetCorpusEncoding);

            DuplicateLineManager duplicateLineAfterMergeManager =
                new DuplicateLineManager(CorpusCleanerConfig.TargetCorpusEncoding);

            // Combine all lines, and split lines using line ending puncuations.
            foreach (string filePath in Directory.GetFiles(targetCorpusDir))
            {
                string fileName = Path.GetFileName(filePath);
                string currentCorpusFilePath = filePath;
                string targetCorpusFilePath = currentCorpusFilePath;

                // HTML decode
                currentCorpusFilePath = targetCorpusFilePath;
                targetCorpusFilePath = Path.Combine(sourceCorpusConfig.MidtermDirHtmlDecode,
                    fileName);
                HtmlDecode(currentCorpusFilePath, targetCorpusFilePath, config.LogFilePath);

                // Process regex rules before merge lines.
                currentCorpusFilePath = targetCorpusFilePath;
                targetCorpusFilePath = Path.Combine(
                    sourceCorpusConfig.MidtermDirApplyRegexRulesBeforeMerge, fileName);
                string beforeMergeInvalidSentencesFilePath = Path.Combine(sourceCorpusConfig.MidtermDirDeleteRegexSentencesBeforeMerge,
                    fileName);
                string beforeMergeInvalidWordsFilePath = Path.Combine(sourceCorpusConfig.MidtermDirDeletedRegexWordsBeforeMerge,
                    fileName);
                ApplyRegexRulesOnFile(currentCorpusFilePath, targetCorpusFilePath,
                    beforeMergeInvalidSentencesFilePath, beforeMergeInvalidWordsFilePath,
                    config.LogFilePath, sourceCorpusConfig.BeforeLineMergeRegexRules);

                if (File.Exists(beforeMergeInvalidSentencesFilePath))
                {
                    droppedFiles.Add(beforeMergeInvalidSentencesFilePath);
                }

                if (File.Exists(beforeMergeInvalidWordsFilePath))
                {
                    droppedFiles.Add(beforeMergeInvalidWordsFilePath);
                }

                // Merge two lines if the first line doesn't ending with line ending puncuation.
                if (sourceCorpusConfig.EnableMergeLines)
                {
                    currentCorpusFilePath = targetCorpusFilePath;
                    targetCorpusFilePath = Path.Combine(sourceCorpusConfig.MidtermDirMergeLines,
                        fileName);
                    MergeLines(currentCorpusFilePath, targetCorpusFilePath,
                        sourceCorpusConfig.LineEndingPunctuations,
                        config.LogFilePath);
                }

                // Deal with duplicate lines after merge two lines without line ending puncuation.
                currentCorpusFilePath = targetCorpusFilePath;
                targetCorpusFilePath = Path.Combine(sourceCorpusConfig.MidtermDirDropDuplicateLinesAfterMerge,
                    fileName);
                if (sourceCorpusConfig.RemoveDuplicateLine)
                {
                    DropDuplicateLines(duplicateLineAfterMergeManager,
                        currentCorpusFilePath, targetCorpusFilePath,
                        Path.Combine(sourceCorpusConfig.MidtermDirDuplicateLinesAfterMerge, fileName),
                        config.LogFilePath);
                }
                else
                {
                    Helper.CopyFileIfSourceExist(currentCorpusFilePath, targetCorpusFilePath, true);
                }

                // Drop invalid chars.
                currentCorpusFilePath = targetCorpusFilePath;
                targetCorpusFilePath = Path.Combine(sourceCorpusConfig.MidtermDirDropInvalidChars,
                    fileName);
                beforeMergeInvalidSentencesFilePath = Path.Combine(
                    sourceCorpusConfig.MidtermDirInvalidCharSentences,
                    fileName);

                DropInvalidChars(currentCorpusFilePath, targetCorpusFilePath,
                    beforeMergeInvalidSentencesFilePath,
                    config.LogFilePath,
                    sourceCorpusConfig.CorpusCharRangesInclude, sourceCorpusConfig.CorpusCharRangesExclude);
                if (File.Exists(beforeMergeInvalidSentencesFilePath))
                {
                    droppedFiles.Add(beforeMergeInvalidSentencesFilePath);
                }

                // Process regex rules before merge lines.
                currentCorpusFilePath = targetCorpusFilePath;
                targetCorpusFilePath = Path.Combine(
                    sourceCorpusConfig.MidtermDirApplyRegexRulesAfterMerge, fileName);
                string afterMergeInvalidSentencesFilePath = Path.Combine(sourceCorpusConfig.MidtermDirDeleteRegexSentencesAfterMerge,
                    fileName);
                string afterMergeInvalidWordsFilePath = Path.Combine(sourceCorpusConfig.MidtermDirDeletedRegexWordsAfterMerge,
                    fileName);
                ApplyRegexRulesOnFile(currentCorpusFilePath, targetCorpusFilePath,
                    afterMergeInvalidSentencesFilePath, afterMergeInvalidWordsFilePath,
                    config.LogFilePath, sourceCorpusConfig.AfterLineMergeRegexRules);

                if (File.Exists(afterMergeInvalidSentencesFilePath))
                {
                    droppedFiles.Add(afterMergeInvalidSentencesFilePath);
                }

                if (File.Exists(afterMergeInvalidWordsFilePath))
                {
                    droppedFiles.Add(afterMergeInvalidWordsFilePath);
                }

                // Filter sentence with min/max words.
                currentCorpusFilePath = targetCorpusFilePath;
                targetCorpusFilePath = Path.Combine(
                    sourceCorpusConfig.MidtermDirFilterLineLength, fileName);
                beforeMergeInvalidSentencesFilePath = Path.Combine(sourceCorpusConfig.MidtermDirDeletedFilterLineLength,
                    fileName);
                FilterLineLength(currentCorpusFilePath, targetCorpusFilePath,
                    beforeMergeInvalidSentencesFilePath, config.LogFilePath, config.MaxCharNumPerLine);

                if (File.Exists(beforeMergeInvalidSentencesFilePath))
                {
                    droppedFiles.Add(beforeMergeInvalidSentencesFilePath);
                }

                if (File.Exists(targetCorpusFilePath))
                {
                    targetFiles.Add(targetCorpusFilePath);
                }
            }
        }

        /// <summary>
        /// Drop duplicate lines.
        /// </summary>
        /// <param name="duplicateLineManager">Duplicate line process manager.</param>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="duplicateLineFilePath">Duplicate lines corpus file path.</param>
        /// <param name="logFilePath">Log file path.</param>
        private static void DropDuplicateLines(DuplicateLineManager duplicateLineManager,
            string sourceCorpusFilePath, string targetCorpusFilePath,
            string duplicateLineFilePath, string logFilePath)
        {
            if (File.Exists(sourceCorpusFilePath))
            {
                duplicateLineManager.DropDuplicateLines(sourceCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding, targetCorpusFilePath, duplicateLineFilePath, CorpusCleanerConfig.TargetCorpusEncoding);
                AppendAllTextLine(logFilePath, Helper.NeutralFormat("Drop duplicate lines from [{0}] to [{1}].", sourceCorpusFilePath, targetCorpusFilePath), true);
            }
        }

        /// <summary>
        /// Apply Html decode on corpus.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="logFilePath">Log file path.</param>
        private static void HtmlDecode(string sourceCorpusFilePath, string targetCorpusFilePath, string logFilePath)
        {
            if (File.Exists(sourceCorpusFilePath))
            {
                CorpusCleaner.HtmlDecode(sourceCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding, targetCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding);
                AppendAllTextLine(logFilePath, Helper.NeutralFormat("Apply HTML decode from [{0}] to [{1}].", sourceCorpusFilePath, targetCorpusFilePath), true);
            }
        }

        /// <summary>
        /// Convert file to unicode file.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="sourceCorpusEncoding">Source corpus encoding.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="logFilePath">Log file path.</param>
        private static void ConvertEncoding(string sourceCorpusFilePath, Encoding sourceCorpusEncoding, string targetCorpusFilePath, string logFilePath)
        {
            Helper.EnsureFolderExistForFile(targetCorpusFilePath);

            AppendAllTextLine(logFilePath, Helper.NeutralFormat("Convert encoding from [{0}({1})] to [{2}({3})], source corpus file [{4}], target corpus file [{5}].", sourceCorpusEncoding.CodePage, sourceCorpusEncoding.EncodingName, Encoding.Unicode.CodePage, Encoding.Unicode.EncodingName, sourceCorpusFilePath, targetCorpusFilePath), true);

            CorpusCleaner.ConvertEncoding(sourceCorpusFilePath, sourceCorpusEncoding, targetCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding);
        }

        /// <summary>
        /// Merge two lines if the first line doesn't ending with line ending puncuation.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="lineEndingPunctuation">Line ending puncuation list.</param>
        /// <param name="logFilePath">Log file path.</param>
        private static void MergeLines(string sourceCorpusFilePath, string targetCorpusFilePath, List<string> lineEndingPunctuation, string logFilePath)
        {
            if (File.Exists(sourceCorpusFilePath))
            {
                AppendAllTextLine(logFilePath, Helper.NeutralFormat("Meger lines without line ending puncuation, source corpus file [{0}], target corpus file [{1}].", sourceCorpusFilePath, targetCorpusFilePath), true);

                CorpusCleaner.MergeLines(sourceCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding, targetCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding, lineEndingPunctuation);
            }
        }

        /// <summary>
        /// Apply regex rules on file.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="deleteSentenceFilePath">Deleted sentences file path.</param>
        /// <param name="deleteWordsFilePath">Deleted words file path.</param>
        /// <param name="logFilePath">Log file path.</param>
        /// <param name="rules">Regex rule path.</param>
        private static void ApplyRegexRulesOnFile(
            string sourceCorpusFilePath, string targetCorpusFilePath,
            string deleteSentenceFilePath, string deleteWordsFilePath,
            string logFilePath,
            List<RegexRule> rules)
        {
            if (File.Exists(sourceCorpusFilePath))
            {
                AppendAllTextLine(logFilePath, Helper.NeutralFormat("Apply regex rules on corpus, source corpus file [{0}], target corpus file [{1}].", sourceCorpusFilePath, targetCorpusFilePath), true);

                CorpusCleaner.ApplyRegexRulesOnFile(
                    sourceCorpusFilePath,
                    CorpusCleanerConfig.TargetCorpusEncoding, targetCorpusFilePath,
                    deleteSentenceFilePath, deleteWordsFilePath,
                    CorpusCleanerConfig.TargetCorpusEncoding, rules);
            }
        }

        /// <summary>
        /// Filter words per sentence on file.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="invalidSentencesCorpusPath">Deleted sentences file path.</param>
        /// <param name="logFilePath">Log file path.</param>
        /// <param name="maxCharNumPerLine">Min words per sentence.</param>
        private static void FilterLineLength(string sourceCorpusFilePath, string targetCorpusFilePath, string invalidSentencesCorpusPath, string logFilePath, int maxCharNumPerLine)
        {
            if (File.Exists(sourceCorpusFilePath))
            {
                AppendAllTextLine(logFilePath, Helper.NeutralFormat("Filter line length [maxCharNumPerLine={0}] on corpus.", maxCharNumPerLine), true);

                CorpusCleaner.FilterLineLength(sourceCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding, targetCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding, invalidSentencesCorpusPath, maxCharNumPerLine);
            }
        }

        /// <summary>
        /// Drop lines contain invalid chars.
        /// </summary>
        /// <param name="sourceCorpusFilePath">Source corpus file path.</param>
        /// <param name="targetCorpusFilePath">Target corpus file path.</param>
        /// <param name="invalidSentencesFilePath">File contains lines with invalid char.</param>
        /// <param name="logFilePath">Log file path.</param>
        /// <param name="includeRange">Include ranges.</param>
        /// <param name="excludeRange">Exclude ranges.</param>
        private static void DropInvalidChars(string sourceCorpusFilePath, string targetCorpusFilePath, string invalidSentencesFilePath, string logFilePath, UnicodeCharRanges includeRange, UnicodeCharRanges excludeRange)
        {
            if (File.Exists(sourceCorpusFilePath))
            {
                AppendAllTextLine(logFilePath, Helper.NeutralFormat("Drop lines with invalid chars, source corpus file [{0}], target corpus file [{1}], " + "invalid sentences file [{2}].", sourceCorpusFilePath, targetCorpusFilePath, invalidSentencesFilePath), true);

                Dictionary<char, long> invalidCharStatistic = CorpusCleaner.DropInvalidChars(sourceCorpusFilePath, targetCorpusFilePath, CorpusCleanerConfig.TargetCorpusEncoding, invalidSentencesFilePath, includeRange, excludeRange);

                // Log all invalid char statistic in log file.
                StringBuilder sb = new StringBuilder();
                sb.Append(Helper.NeutralFormat("Invalid char statistics in corpus [{0}] :{1}\t", sourceCorpusFilePath, Environment.NewLine));
                foreach (char c in invalidCharStatistic.Keys)
                {
                    sb.AppendFormat("[{0},{1}]", c, invalidCharStatistic[c]);
                }

                AppendAllTextLine(logFilePath, sb.ToString());
            }
        }

        #endregion
    }
}
