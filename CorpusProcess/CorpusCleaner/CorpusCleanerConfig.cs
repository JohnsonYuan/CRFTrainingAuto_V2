//----------------------------------------------------------------------------
// <copyright file="CorpusCleanerConfig.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      This module implements corpus cleaner
// </summary>
//----------------------------------------------------------------------------

namespace CorpusCleaner
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    #region Class used for process corpus regex

    /// <summary>
    /// Corpus process regex rule.
    /// </summary>
    public class RegexRule
    {
        #region Fields

        private RegexRuleType _type;
        private string _pattern;
        private string _replacement;
        private bool _deleteLine;
        private bool _beforeMerge;

        #endregion

        #region Enum

        /// <summary>
        /// Regex rule type.
        /// </summary>
        public enum RegexRuleType
        {
            /// <summary>
            /// Delete regex rule.
            /// </summary>
            Delete,

            /// <summary>
            /// Replace regex rule.
            /// </summary>
            Replace
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether True:  Delete the line if the line contains the word to be deleted.
        /// False: Only delete the word math the delete regex.
        /// </summary>
        public bool DeleteLine
        {
            get { return _deleteLine; }
            set { _deleteLine = value; }
        }

        /// <summary>
        /// Gets or sets Regex rule type.
        /// </summary>
        public RegexRuleType RegexType
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets Regex pattern.
        /// </summary>
        public string Pattern
        {
            get
            {
                return _pattern;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                if (!Helper.CheckRegex(value))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Invalid regex rule pattern : [{0}]", value));
                }

                _pattern = value;
            }
        }

        /// <summary>
        /// Gets or sets Regex replacement.
        /// </summary>
        public string Replacement
        {
            get
            {
                return _replacement;
            }

            set
            {
                // This value can be empty for deleting the matched lines.
                if (string.IsNullOrEmpty(value))
                {
                    _replacement = value;
                }

                _replacement = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Process before merge lines or not.
        /// </summary>
        public bool BeforeMerge
        {
            get { return _beforeMerge; }
            set { _beforeMerge = value; }
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Source corpus config.
    /// </summary>
    public class SourceCorpusConfig
    {
        #region Fields

        private string _type;
        private UnicodeCharRanges _corpusCharRangesInclude = new UnicodeCharRanges();
        private UnicodeCharRanges _corpusCharRangesExclude = new UnicodeCharRanges();
        private bool _enableMergeLines = false;
        private List<string> _lineEndingPunctuations = new List<string>();
        private List<RegexRule> _beforeLineMergeRegexRules = new List<RegexRule>();
        private List<RegexRule> _afterLineMergeRegexRules = new List<RegexRule>();
        private Encoding _encoding = Encoding.Unicode;
        private List<string> _searchPatterns = new List<string>();
        private string _midtermDir;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether enable merge lines which don't ending with line ending puncuation.
        /// </summary>
        public bool EnableMergeLines
        {
            get { return _enableMergeLines; }

            set { _enableMergeLines = value; }
        }

        /// <summary>
        /// Gets line ending punctuation char list.
        /// </summary>
        public List<string> LineEndingPunctuations
        {
            get { return _lineEndingPunctuations; }
        }

        /// <summary>
        /// Gets regex rule list.
        /// </summary>
        public List<RegexRule> BeforeLineMergeRegexRules
        {
            get { return _beforeLineMergeRegexRules; }
        }

        /// <summary>
        /// Gets regex rule list.
        /// </summary>
        public List<RegexRule> AfterLineMergeRegexRules
        {
            get { return _afterLineMergeRegexRules; }
        }

        /// <summary>
        /// Gets or sets Source corpus type.
        /// </summary>
        public string CorpusType
        {
            get
            {
                return _type;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _type = value;
            }
        }

        /// <summary>
        /// Gets source corpus search patterns.
        /// </summary>
        public List<string> SearchPatterns
        {
            get
            {
                return _searchPatterns;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Whether remove duplicate lines .
        /// </summary>
        public bool RemoveDuplicateLine { get; set; }

        /// <summary>
        /// Gets or sets Source corpus encoding.
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _encoding = value;
            }
        }

        /// <summary>
        /// Gets corpus char include ranges.
        /// </summary>
        public UnicodeCharRanges CorpusCharRangesInclude
        {
            get { return _corpusCharRangesInclude; }
        }

        /// <summary>
        /// Gets corpus char exclude ranges.
        /// </summary>
        public UnicodeCharRanges CorpusCharRangesExclude
        {
            get { return _corpusCharRangesExclude; }
        }

        #endregion

        #region Midterm dirs properties

        /// <summary>
        /// Gets or sets Midterm dir.
        /// </summary>
        public string MidtermDir
        {
            get
            {
                if (string.IsNullOrEmpty(_midtermDir))
                {
                    throw new InvalidOperationException("_midtermDir is null");
                }

                return _midtermDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _midtermDir = value;
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains files with the same size.
        /// </summary>
        public string MidtermDirSameFileSize
        {
            get
            {
                return Path.Combine(MidtermDir, @"1.HasMergedToSameFileSize\" + _type);
            }
        }

        /// <summary>
        /// Gets Mideterm corpus directory contains unicode files
        /// invalid chars.
        /// </summary>
        public string MidtermDirUnicode
        {
            get
            {
                return Path.Combine(MidtermDir, @"2.HasConverttedToUnicode\" + _type);
            }
        }

        /// <summary>
        /// Gets mideterm corpus directory contains files have done html decode.
        /// </summary>
        public string MidtermDirHtmlDecode
        {
            get
            {
                return Path.Combine(MidtermDir, @"3.HasDoneHtmlDecode\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains files contain invalid lines before merge lines.
        /// </summary>
        public string MidtermDirDeleteRegexSentencesBeforeMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"4.DeletedRegexSentencesBeforeMerge\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains contents deleted by Delete RegexRule before merge lines,
        /// the content match the delete pattern and set deleteLine=false.
        /// </summary>
        public string MidtermDirDeletedRegexWordsBeforeMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"4.DeletedRegexWordsBeforeMerge\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains files which haved been applied
        /// regex rules before merge lines.
        /// </summary>
        public string MidtermDirApplyRegexRulesBeforeMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"4.HasAppliedRegexRulesBeforeMerge\" + _type);
            }
        }

        /// <summary>
        /// Gets merged lines midterm corpus directory.
        /// </summary>
        public string MidtermDirMergeLines
        {
            get
            {
                return Path.Combine(MidtermDir, @"5.HasMergedLines\" + _type);
            }
        }

        /// <summary>
        /// Gets mideterm corpus directory contains files have dropped duplicated lines before merge lines.
        /// </summary>
        public string MidtermDirDropDuplicateLinesAfterMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"6.HasDroppedDuplicateLines\" + _type);
            }
        }

        /// <summary>
        /// Gets mideterm corpus directory contains files contain duplicate lines after merge lines.
        /// </summary>
        public string MidtermDirDuplicateLinesAfterMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"6.DeletedDuplicateLines\" + _type);
            }
        }

        /// <summary>
        /// Gets mideterm corpus directory contains files don't contain
        /// invalid chars.
        /// </summary>
        public string MidtermDirDropInvalidChars
        {
            get
            {
                return Path.Combine(MidtermDir, @"7.HasDroppedInvalidChars\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains files contain invalid sentences.
        /// </summary>
        public string MidtermDirInvalidCharSentences
        {
            get
            {
                return Path.Combine(MidtermDir, @"7.DeletedInvalidCharSentences\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains files contain invalid sentences after merge lines.
        /// </summary>
        public string MidtermDirDeleteRegexSentencesAfterMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"8.DeletedRegexSentencesAfterMerge\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains contents deleted by Delete RegexRule after merge lines,
        /// the content match the delete pattern and set deleteLine=false.
        /// </summary>
        public string MidtermDirDeletedRegexWordsAfterMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"8.DeletedRegexWordsAfterMerge\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains files which haved been applied
        /// regex rules after merge lines.
        /// </summary>
        public string MidtermDirApplyRegexRulesAfterMerge
        {
            get
            {
                return Path.Combine(MidtermDir, @"8.HasAppliedRegexRulesAfterMerge\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains files which haved been filterred by word min/max count.
        /// </summary>
        public string MidtermDirFilterLineLength
        {
            get
            {
                return Path.Combine(MidtermDir, @"9.HasFilterredLineLength\" + _type);
            }
        }

        /// <summary>
        /// Gets midterm corpus directory contains sentences haved been filterred by word min/max count.
        /// </summary>
        public string MidtermDirDeletedFilterLineLength
        {
            get
            {
                return Path.Combine(MidtermDir, @"9.DeletedFilterLineLength\" + _type);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create XmlNode for SourceCorpusConfig.
        /// </summary>
        /// <param name="dom">XmlDocument to which to create XmlNode.</param>
        /// <param name="nameSpace">Name space of the XmlElement.</param>
        /// <returns>Created SourceCorpusConfig XmlElement.</returns>
        public XmlElement CreateXmlElement(XmlDocument dom, string nameSpace)
        {
            // Raw corpus
            XmlElement corpusEle = dom.CreateElement("CorpusFile", nameSpace);
            corpusEle.SetAttribute("type", _type);
            corpusEle.SetAttribute("codePage", Encoding.CodePage.ToString(CultureInfo.InvariantCulture));
            corpusEle.SetAttribute("searchPatterns", string.Join("|", _searchPatterns.ToArray()));
            corpusEle.SetAttribute("removeDuplicateLine", RemoveDuplicateLine.ToString().ToLower(CultureInfo.InvariantCulture));

            XmlElement charRangeEle = dom.CreateElement("CharRange", nameSpace);
            corpusEle.AppendChild(charRangeEle);

            XmlElement charRangeIncludeEle = dom.CreateElement("Include", nameSpace);
            charRangeEle.AppendChild(charRangeIncludeEle);

            foreach (UnicodeCharRange range in _corpusCharRangesInclude.CharRangeList)
            {
                XmlElement rangeEle = dom.CreateElement("Range", nameSpace);
                rangeEle.SetAttribute("from", range.BeginUnicode.Expression);
                rangeEle.SetAttribute("to", range.EndUnicode.Expression);
                charRangeIncludeEle.AppendChild(rangeEle);
            }

            foreach (UnicodeCharWrap charWrap in _corpusCharRangesInclude.CharList)
            {
                XmlElement charsEle = dom.CreateElement("Chars", nameSpace);
                charsEle.SetAttribute("symbol", charWrap.Expression);
                charRangeIncludeEle.AppendChild(charsEle);
            }

            XmlElement charRangeExcludeEle = dom.CreateElement("Exclude", nameSpace);
            charRangeEle.AppendChild(charRangeExcludeEle);

            foreach (UnicodeCharRange range in _corpusCharRangesExclude.CharRangeList)
            {
                XmlElement rangeEle = dom.CreateElement("Range", nameSpace);
                rangeEle.SetAttribute("from", range.BeginUnicode.Expression);
                rangeEle.SetAttribute("to", range.EndUnicode.Expression);
                charRangeExcludeEle.AppendChild(rangeEle);
            }

            foreach (UnicodeCharWrap charWrap in _corpusCharRangesExclude.CharList)
            {
                XmlElement charsEle = dom.CreateElement("Chars", nameSpace);
                charsEle.SetAttribute("symbol", charWrap.Expression);
                charRangeExcludeEle.AppendChild(charsEle);
            }

            XmlElement lineEndingPunctuationEle = dom.CreateElement("LineEndingPunctuation",
                nameSpace);
            lineEndingPunctuationEle.SetAttribute("merge", _enableMergeLines.ToString().ToLower(CultureInfo.InvariantCulture));
            corpusEle.AppendChild(lineEndingPunctuationEle);

            foreach (string lineEndingPunctuation in _lineEndingPunctuations)
            {
                XmlElement charEle = dom.CreateElement("Punctuation", nameSpace);
                charEle.SetAttribute("symbol", lineEndingPunctuation);
                lineEndingPunctuationEle.AppendChild(charEle);
            }

            XmlElement regexRulesEle = dom.CreateElement("RegexRules", nameSpace);
            corpusEle.AppendChild(regexRulesEle);

            foreach (RegexRule rule in _beforeLineMergeRegexRules)
            {
                if (rule.RegexType == RegexRule.RegexRuleType.Delete)
                {
                    XmlElement deleteRuleEle = dom.CreateElement("Delete", nameSpace);
                    deleteRuleEle.SetAttribute("pattern", rule.Pattern);
                    deleteRuleEle.SetAttribute("deleteLine", rule.DeleteLine.ToString().ToLower(CultureInfo.InvariantCulture));
                    deleteRuleEle.SetAttribute("beforeMerge", "true");
                    regexRulesEle.AppendChild(deleteRuleEle);
                }
                else if (rule.RegexType == RegexRule.RegexRuleType.Replace)
                {
                    XmlElement replaceRuleEle = dom.CreateElement("Replace", nameSpace);
                    replaceRuleEle.SetAttribute("pattern", rule.Pattern);
                    replaceRuleEle.SetAttribute("replacement", rule.Replacement);
                    replaceRuleEle.SetAttribute("beforeMerge", "true");
                    regexRulesEle.AppendChild(replaceRuleEle);
                }
            }

            foreach (RegexRule rule in _afterLineMergeRegexRules)
            {
                if (rule.RegexType == RegexRule.RegexRuleType.Delete)
                {
                    XmlElement deleteRuleEle = dom.CreateElement("Delete", nameSpace);
                    deleteRuleEle.SetAttribute("pattern", rule.Pattern);
                    deleteRuleEle.SetAttribute("deleteLine", rule.DeleteLine.ToString().ToLower(CultureInfo.InvariantCulture));
                    deleteRuleEle.SetAttribute("beforeMerge", "false");
                    regexRulesEle.AppendChild(deleteRuleEle);
                }
                else if (rule.RegexType == RegexRule.RegexRuleType.Replace)
                {
                    XmlElement replaceRuleEle = dom.CreateElement("Replace", nameSpace);
                    replaceRuleEle.SetAttribute("pattern", rule.Pattern);
                    replaceRuleEle.SetAttribute("replacement", rule.Replacement);
                    replaceRuleEle.SetAttribute("beforeMerge", "false");
                    regexRulesEle.AppendChild(replaceRuleEle);
                }
            }

            return corpusEle;
        }

        /// <summary>
        /// Parse corpus raw corpus config.
        /// </summary>
        /// <param name="node">Xml node.</param>
        /// <param name="nsmgr">Xml namespace manager.</param>
        public void Parse(XmlNode node, XmlNamespaceManager nsmgr)
        {
            _type = node.Attributes["type"].InnerText;
            _searchPatterns.AddRange(node.Attributes["searchPatterns"].InnerText.Split(
                new char[]
                {
                    '|'
                },
                StringSplitOptions.RemoveEmptyEntries));
            RemoveDuplicateLine = bool.Parse(node.Attributes["removeDuplicateLine"].InnerText);

            _encoding = Encoding.Unicode;

            if (node.Attributes["codePage"] != null)
            {
                int codepage = int.Parse(node.Attributes["codePage"].InnerText, CultureInfo.InvariantCulture);
                try
                {
                    _encoding = System.Text.Encoding.GetEncoding(codepage);
                }
                catch (ArgumentException ae)
                {
                    string message = Helper.NeutralFormat(
                        "Invalid source corpus encoding codepage [{0}].",
                        node.InnerText);
                    throw new InvalidDataException(message, ae);
                }
            }

            XmlNodeList nodeList = node.SelectNodes(@"tts:CharRange/tts:Include/tts:Range", nsmgr);
            if (nodeList != null)
            {
                foreach (XmlNode rangeNode in nodeList)
                {
                    _corpusCharRangesInclude.AddRange(rangeNode.Attributes["from"].InnerText,
                        rangeNode.Attributes["to"].InnerText);
                }
            }

            nodeList = node.SelectNodes(@"tts:CharRange/tts:Include/tts:Chars", nsmgr);
            if (nodeList != null)
            {
                foreach (XmlNode charsNode in nodeList)
                {
                    _corpusCharRangesInclude.AddChars(charsNode.Attributes["symbol"].InnerText);
                }
            }

            nodeList = node.SelectNodes(@"tts:CharRange/tts:Exclude/tts:Range", nsmgr);
            if (nodeList != null)
            {
                foreach (XmlNode rangeNode in nodeList)
                {
                    _corpusCharRangesExclude.AddRange(rangeNode.Attributes["from"].InnerText,
                        rangeNode.Attributes["to"].InnerText);
                }
            }

            nodeList = node.SelectNodes(@"tts:CharRange/tts:Exclude/tts:Chars", nsmgr);
            if (nodeList != null)
            {
                foreach (XmlNode charsNode in nodeList)
                {
                    _corpusCharRangesExclude.AddChars(charsNode.Attributes["symbol"].InnerText);
                }
            }

            XmlNode mergeNode = node.SelectSingleNode(@"tts:LineEndingPunctuation/@merge", nsmgr);
            _enableMergeLines = bool.Parse(mergeNode.InnerText);

            nodeList = node.SelectNodes(@"tts:LineEndingPunctuation/tts:Punctuation", nsmgr);
            if (nodeList != null)
            {
                foreach (XmlNode xmlNode in nodeList)
                {
                    string lineEndingPunctuation = xmlNode.Attributes["symbol"].InnerText;
                    if (string.IsNullOrEmpty(lineEndingPunctuation))
                    {
                        throw new InvalidDataException("Line ending punctuation should not be empty");
                    }

                    if (_lineEndingPunctuations.IndexOf(lineEndingPunctuation) >= 0)
                    {
                        throw new InvalidDataException(Helper.NeutralFormat("Duplicate line ending " +
                            "punctuation detected : [{0}]", lineEndingPunctuation));
                    }

                    _lineEndingPunctuations.Add(lineEndingPunctuation);
                }
            }

            nodeList = node.SelectSingleNode(@"tts:RegexRules", nsmgr).ChildNodes;
            foreach (XmlNode xmlNode in nodeList)
            {
                if (xmlNode.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                RegexRule rule = new RegexRule();
                if (xmlNode.Name.Equals("Replace"))
                {
                    rule.RegexType = RegexRule.RegexRuleType.Replace;
                    if (string.IsNullOrEmpty(xmlNode.Attributes["pattern"].InnerText))
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "RegexRule's attribute pattern should not be empty in " +
                            "corpus [type={0}].", _type));
                    }

                    rule.Pattern = xmlNode.Attributes["pattern"].InnerText;
                    rule.Replacement = xmlNode.Attributes["replacement"].InnerText;
                }
                else if (xmlNode.Name.Equals("Delete"))
                {
                    rule.RegexType = RegexRule.RegexRuleType.Delete;
                    rule.Pattern = xmlNode.Attributes["pattern"].InnerText;
                    rule.DeleteLine = bool.Parse(xmlNode.Attributes["deleteLine"].InnerText);
                }
                else
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Invalid regex rule type [{0}], only [Replace and Delete] are allowed..",
                        xmlNode.Name));
                }

                if (xmlNode.Attributes["beforeMerge"] != null && 
                    bool.Parse(xmlNode.Attributes["beforeMerge"].InnerText))
                {
                    rule.BeforeMerge = true;
                    _beforeLineMergeRegexRules.Add(rule);
                }
                else
                {
                    rule.BeforeMerge = false;
                    _afterLineMergeRegexRules.Add(rule);
                }
            }
        }

        #endregion
    }

    #region Class used for processing exclude files

    /// <summary>
    /// File filter class.
    /// </summary>
    public class FileFilter
    {
        #region Fields

        private string _dir;
        private string _searchPattern;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FileFilter" /> class. Constructor.
        /// </summary>
        /// <param name="dir">Search root dir.</param>
        /// <param name="searchPattern">Search pattern.</param>
        public FileFilter(string dir, string searchPattern)
        {
            _dir = dir;
            _searchPattern = searchPattern;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets Search root dir.
        /// </summary>
        public string Dir
        {
            get { return _dir; }
            set { _dir = value; }
        }

        /// <summary>
        /// Gets or sets Search pattern.
        /// </summary>
        public string SearchPattern
        {
            get { return _searchPattern; }
            set { _searchPattern = value; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Get all files to be processed.
        /// </summary>
        /// <param name="rootDir">Search root dir.</param>
        /// <param name="searchPattern">Search pattern.</param>
        /// <param name="excludeFileFilters">Exclude file filters.</param>
        /// <returns>Searched file path list.</returns>
        public static List<string> GetFilesPath(string rootDir, string searchPattern,
            List<FileFilter> excludeFileFilters)
        {
            List<string> filesPath = new List<string>();
            filesPath.AddRange(Directory.GetFiles(rootDir, searchPattern, SearchOption.AllDirectories));

            foreach (FileFilter excludeFileFilter in excludeFileFilters)
            {
                string excludeFileDir = rootDir;

                // If equal "." don't need add ".", if add it the path looks like:
                //      D:\SourceCorpus\.\readme.txt, different from
                //      D:\SourceCorpus\readme.txt.
                if (!excludeFileFilter.Dir.Equals("."))
                {
                    excludeFileDir = Path.Combine(rootDir, excludeFileFilter.Dir);
                }

                foreach (string excludeFilePath in Directory.GetFiles(
                    excludeFileDir, 
                    excludeFileFilter.SearchPattern, SearchOption.AllDirectories))
                {
                    filesPath.Remove(excludeFilePath);
                }
            }

            return filesPath;
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Corpus cleaner config class.
    /// </summary>
    public class CorpusCleanerConfig
    {
        #region Fields

        private const string TtsSchemaUri = @"http://schemas.microsoft.com/tts/toolsuite";
        private const string TargetCorpusFileSizeRegex = "[0-9]{1,4}[kKmM]";
        private const int DefaultMaxCharNumPerLine = 5120;

        private static XmlSchema _schema;

        private string _targetCorpusFileSizeString;
        private string _targetCorpusDir;
        private int _maxCharNumPerLine = DefaultMaxCharNumPerLine;

        private string _sourceCorpusDir;
        private List<FileFilter> _excludeFileFilters = new List<FileFilter>();

        private List<SourceCorpusConfig> _sourceCorpusConfigs = new List<SourceCorpusConfig>();

        #endregion

        #region Static properties

        /// <summary>
        /// Gets corpusCleanerConfig schema.
        /// </summary>
        public static XmlSchema ConfigSchema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = XmlHelper.LoadSchemaFromResource(assembly,
                        "CorpusCleaner.CorpusCleanerConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets target corpus encoding.
        /// </summary>
        public static Encoding TargetCorpusEncoding
        {
            get { return Encoding.Unicode; }
        }

        #endregion

        #region Instance properties

        /// <summary>
        /// Gets or sets Max char number per line.
        /// </summary>
        public int MaxCharNumPerLine
        {
            get
            {
                return _maxCharNumPerLine;
            }

            set
            {
                if (value <= 0)
                {
                    throw new InvalidDataException("_maxCharNumPerLine should be positive");
                }

                _maxCharNumPerLine = value;
            }
        }

        /// <summary>
        /// Gets midterm dir.
        /// </summary>
        public string MidtermDir
        {
            get
            {
                return Path.Combine(TargetCorpusDir, "Log");
            }
        }

        /// <summary>
        /// Gets log file path.
        /// </summary>
        public string LogFilePath
        {
            get
            {
                return Path.Combine(MidtermDir, "CorpusCleaner.log");
            }
        }

        /// <summary>
        /// Gets exclude file filters.
        /// </summary>
        public List<FileFilter> ExcludeFileFilters
        {
            get { return _excludeFileFilters; }
        }

        /// <summary>
        /// Gets source corpus configs.
        /// </summary>
        public List<SourceCorpusConfig> SourceCorpusConfigs
        {
            get { return _sourceCorpusConfigs; }
        }

        /// <summary>
        /// Gets or sets target corpus file size string.
        /// Must ending with one of : M, m, K, k.
        /// </summary>
        public string TargetCorpusFileSizeString
        {
            get
            {
                return _targetCorpusFileSizeString;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                value = value.Trim();
                if (!Regex.Match(value, TargetCorpusFileSizeRegex).Success)
                {
                    throw new InvalidDataException(Helper.NeutralFormat("File size should math " +
                        "the format [{0}]", TargetCorpusFileSizeRegex));
                }

                _targetCorpusFileSizeString = value;
            }
        }

        /// <summary>
        /// Gets target corpus file size.
        /// </summary>
        public long TargetCorpusFileSize
        {
            get
            {
                if (string.IsNullOrEmpty(_targetCorpusFileSizeString))
                {
                    throw new NullObjectFieldException("_targetCorpusFileSizeString is null");
                }

                return CalculateFileSize();
            }
        }

        /// <summary>
        /// Gets or sets Source corpus directory.
        /// </summary>
        public string SourceCorpusDir
        {
            get
            {
                return _sourceCorpusDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _sourceCorpusDir = value;
            }
        }

        /// <summary>
        /// Gets or sets Target corpus directory.
        /// </summary>
        public string TargetCorpusDir
        {
            get
            {
                return _targetCorpusDir;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _targetCorpusDir = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Save corpus cleaner config file.
        /// </summary>
        /// <param name="path">CorpusCleaner config file path.</param>
        public void Save(string path)
        {
            XmlDocument dom = new XmlDocument();
            dom.NameTable.Add(ConfigSchema.TargetNamespace);

            XmlDeclaration declaration = dom.CreateXmlDeclaration("1.0", "utf-8", null);
            dom.AppendChild(declaration);

            // Root CorpusCleaner element
            XmlElement rootEle = dom.CreateElement("CorpusCleaner", ConfigSchema.TargetNamespace);
            dom.AppendChild(rootEle);

            // Add TargetCorpus node.
            XmlElement targetCorpusEle = dom.CreateElement("CleanCorpus", ConfigSchema.TargetNamespace);
            rootEle.AppendChild(targetCorpusEle);
            targetCorpusEle.SetAttribute("dir", TargetCorpusDir);
            targetCorpusEle.SetAttribute("fileSize", TargetCorpusFileSizeString);
            targetCorpusEle.SetAttribute("maxCharNumPerLine", MaxCharNumPerLine.ToString(CultureInfo.InvariantCulture));

            // Add SourceCorpus node
            XmlElement sourceCorpusEle = dom.CreateElement("RawCorpus", ConfigSchema.TargetNamespace);
            rootEle.AppendChild(sourceCorpusEle);
            sourceCorpusEle.SetAttribute("dir", _sourceCorpusDir);
            foreach (FileFilter fileFilter in _excludeFileFilters)
            {
                XmlElement excludeFilesEle = dom.CreateElement("ExcludeFiles", ConfigSchema.TargetNamespace);
                excludeFilesEle.SetAttribute("dir", fileFilter.Dir);
                excludeFilesEle.SetAttribute("searchPattern", fileFilter.SearchPattern);
                sourceCorpusEle.AppendChild(excludeFilesEle);
            }

            foreach (SourceCorpusConfig config in _sourceCorpusConfigs)
            {
                XmlElement corpusEle = config.CreateXmlElement(dom, ConfigSchema.TargetNamespace);
                sourceCorpusEle.AppendChild(corpusEle);
            }

            dom.Save(path);

            // Performance compatibility format checking
            XmlHelper.Validate(path, ConfigSchema);
        }

        /// <summary>
        /// Load corpus cleaner config file.
        /// </summary>
        /// <param name="path">CorpusCleaner config file path.</param>
        public void Load(string path)
        {
            // Check the configuration file first
            try
            {
                XmlHelper.Validate(path, ConfigSchema);
            }
            catch (InvalidDataException ide)
            {
                string message = Helper.NeutralFormat(
                    "The configuration file [{0}] error is found.",
                    path);
                throw new InvalidDataException(message, ide);
            }

            XmlDocument dom = new XmlDocument();
            dom.Load(path);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(dom.NameTable);
            nsmgr.AddNamespace("tts", TtsSchemaUri);

            XmlNode node = dom.DocumentElement.SelectSingleNode(@"//tts:CleanCorpus/@dir", nsmgr);
            _targetCorpusDir = node.InnerText;
            if (!Helper.IsValidPath(_targetCorpusDir))
            {
                throw new InvalidDataException(Helper.NeutralFormat("Invalid CleanCorpus dir path [{0}]",
                    _targetCorpusDir));
            }

            Helper.EnsureFolderExist(_targetCorpusDir);

            node = dom.DocumentElement.SelectSingleNode(@"//tts:CleanCorpus/@fileSize", nsmgr);
            TargetCorpusFileSizeString = node.InnerText;

            MaxCharNumPerLine = DefaultMaxCharNumPerLine;
            node = dom.DocumentElement.SelectSingleNode(@"//tts:CleanCorpus/@maxCharNumPerLine", nsmgr);
            if (node != null)
            {
                MaxCharNumPerLine = int.Parse(node.InnerText, CultureInfo.InvariantCulture);
            }

            node = dom.DocumentElement.SelectSingleNode(@"//tts:RawCorpus/@dir", nsmgr);
            _sourceCorpusDir = node.InnerText;
            if (!Directory.Exists(_sourceCorpusDir))
            {
                throw Helper.CreateException(typeof(DirectoryNotFoundException), _sourceCorpusDir);
            }

            XmlNodeList nodeList = dom.DocumentElement.SelectNodes(@"//tts:RawCorpus/tts:ExcludeFiles", nsmgr);
            foreach (XmlNode excludeFilesNode in nodeList)
            {
                _excludeFileFilters.Add(new FileFilter(excludeFilesNode.Attributes["dir"].InnerText,
                    excludeFilesNode.Attributes["searchPattern"].InnerText));
            }

            nodeList = dom.DocumentElement.SelectNodes(@"//tts:RawCorpus/tts:CorpusFile", nsmgr);
            foreach (XmlNode corpusNode in nodeList)
            {
                SourceCorpusConfig sourceCorpusConfig = new SourceCorpusConfig();
                sourceCorpusConfig.Parse(corpusNode, nsmgr);
                sourceCorpusConfig.MidtermDir = MidtermDir;
                _sourceCorpusConfigs.Add(sourceCorpusConfig);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Get the file size.
        /// </summary>
        /// <returns>File size.</returns>
        private long CalculateFileSize()
        {
            long fileSize = 0;
            string sizeValueString = _targetCorpusFileSizeString.Substring(0,
                _targetCorpusFileSizeString.Length - 1);

            if (_targetCorpusFileSizeString.ToLower(CultureInfo.InvariantCulture).EndsWith(
                "k", StringComparison.Ordinal))
            {
                fileSize = long.Parse(sizeValueString, CultureInfo.InvariantCulture) * 1024;
            }
            else if (_targetCorpusFileSizeString.ToLower(CultureInfo.InvariantCulture).EndsWith(
                "m", StringComparison.Ordinal))
            {
                fileSize = long.Parse(sizeValueString, CultureInfo.InvariantCulture) * 1024 * 1024;
            }
            else
            {
                throw new InvalidDataException(Helper.NeutralFormat("File size should math " +
                    "the format [{0}]", TargetCorpusFileSizeRegex));
            }

            return fileSize;
        }

        #endregion
    }
}
