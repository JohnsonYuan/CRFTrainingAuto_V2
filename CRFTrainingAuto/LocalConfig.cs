//-----------------------------------------------------------------------------------------
// <copyright file="LocalConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <summary>
//     Configuration class
// </summary>
//-----------------------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// LocalConfig.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public class LocalConfig : XmlDataFile
    {
        #region Fields

        // target\distrib\debug\{amd64/x86}\dev\TTS\Server\bin\Offline
        private readonly string _offlineToolPathPattern = @"target\distrib\debug\{0}\dev\TTS\Server\bin\Offline";

        // private\dev\speech\tts\shenzhou\data\zh-CN\Language\Model.Rule\PolyphonyModel\ModelUsed
        private readonly string _crfModelDirPattern = @"private\dev\speech\tts\shenzhou\data\{0}\Language\Model.Rule\PolyphonyModel\ModelUsed";
        
        // private\dev\speech\tts\shenzhou\src\lochand\ZhCN\MSTTSLocZhCN.dat
        private readonly string _langDataPathPattern = @"private\dev\speech\tts\shenzhou\src\lochand\{0}\MSTTSLoc{0}.dat";
        
        // private\dev\speech\tts\shenzhou\data\zh-CN\Language\Model.Rule\Polyphony\polyrule.txt
        private readonly string _polyRuleFilePathPattern = @"private\dev\speech\tts\shenzhou\data\{0}\Language\Model.Rule\Polyphony\polyrule.txt";

        // compile config path pattern
        private readonly string _compileConfigFilePathPattern = @"private\dev\speech\tts\shenzhou\data\{0}\Release\platform\LangDataCompilerConfig.xml";
        private readonly string _compileConfigRawDataRootPathPattern = @"private\dev\speech\tts\shenzhou\data\{0}";
        private readonly string _compileConfigOutputDirPathPattern = @"private\dev\speech\tts\shenzhou";
        private readonly string _compileConfigReportFilePathPattern = @"private\dev\speech\tts\shenzhou\data\{0}\binary\report.txt";

        // singleton pattern
        private static volatile LocalConfig _instance;

        private string _charName;
        private Language _lang;
        private string _outputCRFName;
        private string _crfModelDir;
        private string _defaultWordPron;
        private int _minCaseLength;
        private int _maxCaseCount;
        private int _nCrossCaseCount;
        private int _nFolderCount;
        private Dictionary<string, string> _prons = new Dictionary<string, string>();
        private string _branchRootPath;
        private string _arch;
        private string _offlineToolPath;
        private string _langDataPath;
        private string _polyRuleFilePath;
        private string _compileConfigFilePath;
        private string _compileConfigRawDataRootPath;
        private string _compileConfigOutputDirPath;
        private string _compileConfigReportFilePath;
        private int _maxThreadCount;
        private string _trainingConfigTemplate;
        private string _featuresConfigTemplate;
        private int _showTipCount;
        private XmlSchema _schema;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LocalConfig class.
        /// </summary>
        /// <param name="configPath">Xml config file path.</param>
        public LocalConfig(string configPath)
        {
            base.Load(configPath);
            _instance = this;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets LocalConfig instance.
        /// </summary>
        public static LocalConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("Object not created");
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets corpusCleanerConfig schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (this._schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    this._schema = XmlHelper.LoadSchemaFromResource(
                        assembly,
                        "CRFTrainingAuto.CRFTrainingAutoConfig.xsd");
                }

                return this._schema;
            }
        }

        /// <summary>
        /// Gets the char to be trained.
        /// </summary>
        public string CharName
        {
            get
            {
                return this._charName;
            }
        }

        /// <summary>
        /// Gets Current Language.
        /// </summary>
        public Language Lang
        {
            get
            {
                return this._lang;
            }
        }

        /// <summary>
        /// Gets the char to be trained.
        /// </summary>
        public string OutputCRFName
        {
            get
            {
                return this._outputCRFName;
            }
        }

        /// <summary>
        /// Gets crf model folder.
        /// </summary>
        public string CRFModelDir
        {
            get
            {
                return this._crfModelDir;
            }
        }

        /// <summary>
        /// Gets default pron for word has no pronunciation.
        /// </summary>
        public string DefaultWordPron
        {
            get
            {
                return this._defaultWordPron;
            }
        }

        /// <summary>
        /// Gets filtered case min length.
        /// </summary>
        public int MinCaseLength
        {
            get
            {
                return this._minCaseLength;
            }
        }

        /// <summary>
        /// Gets max case count used to train crf model.
        /// </summary>
        public int MaxCaseCount
        {
            get
            {
                return this._maxCaseCount;
            }
        }

        /// <summary>
        /// Gets case count using N Cross.
        /// </summary>
        public int NCrossCaseCount
        {
            get
            {
                return this._nCrossCaseCount;
            }
        }

        /// <summary>
        /// Gets folder count using N Cross.
        /// </summary>
        public int NFolderCount
        {
            get
            {
                return this._nFolderCount;
            }
        }

        /// <summary>
        /// Gets word pronunciations contains pinyin and native phone.
        /// </summary>
        public Dictionary<string, string> Prons
        {
            get
            {
                return this._prons;
            }
        }

        /// <summary>
        /// Gets branch root path.
        /// </summary>
        public string BranchRootPath
        {
            get
            {
                return this._branchRootPath;
            }
        }

        /// <summary>
        /// Gets architecture, amd64 or x86.
        /// </summary>
        public string Arch
        {
            get
            {
                return this._arch;
            }
        }

        /// <summary>
        /// Gets branch offline path.
        /// </summary>
        public string OfflineToolPath
        {
            get
            {
                return this._offlineToolPath;
            }
        }

        /// <summary>
        /// Gets language data path.
        /// </summary>
        public string LangDataPath
        {
            get
            {
                return this._langDataPath;
            }
        }

        /// <summary>
        /// Gets polyrule.txt file path.
        /// </summary>
        public string PolyRuleFilePath
        {
            get
            {
                return this._polyRuleFilePath;
            }
        }

        /// <summary>
        /// Gets compile config file path.
        /// </summary>
        public string CompileConfigFilePath
        {
            get
            {
                return this._compileConfigFilePath;
            }
        }

        /// <summary>
        /// Gets compile raw data root path.
        /// </summary>
        public string CompileConfigRawDataRootPath
        {
            get
            {
                return this._compileConfigRawDataRootPath;
            }
        }

        /// <summary>
        /// Gets compile output folder.
        /// </summary>
        public string CompileConfigOutputDirPath
        {
            get
            {
                return this._compileConfigOutputDirPath;
            }
        }

        /// <summary>
        /// Gets compile report file path.
        /// </summary>
        public string CompileConfigReportPath
        {
            get
            {
                return this._compileConfigReportFilePath;
            }
        }

        /// <summary>
        /// Gets max thread count when filtering char from corpus.
        /// </summary>
        public int MaxThreadCount
        {
            get
            {
                return this._maxThreadCount;
            }
        }

        /// <summary>
        /// Gets training CRF model config template.
        /// </summary>
        public string TrainingConfigTemplate
        {
            get
            {
                return this._trainingConfigTemplate;
            }
        }

        /// <summary>
        /// Gets training CRF model feature config template.
        /// </summary>
        public string FeaturesConfigTemplate
        {
            get
            {
                return this._featuresConfigTemplate;
            }
        }

        /// <summary>
        /// Gets or sets show progress count when filtering char.
        /// </summary>
        public int ShowTipCount
        {
            get
            {
                return this._showTipCount;
            }

            set
            {
                this._showTipCount = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">XmlDocument.</param>
        /// <param name="nsmgr">Xml namespace manager.</param>
        /// <param name="contentController">Content controller.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            XmlNode node;

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/@name", nsmgr);
            if (node != null)
            {
                this._charName = node.Value;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:Language", nsmgr);
            if (node != null)
            {
                this._lang = Localor.StringToLanguage(node.InnerText);
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:OutputCRFName", nsmgr);
            if (node != null)
            {
                this._outputCRFName = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:DefaultWordPron", nsmgr);
            if (node != null)
            {
                this._defaultWordPron = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:MinCaseLength", nsmgr);
            if (node != null)
            {
                try
                {
                    this._minCaseLength = int.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("MinCaseLength is not a number!");
                }
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:MaxCaseCount", nsmgr);
            if (node != null)
            {
                try
                {
                    this._maxCaseCount = int.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("MaxCaseCount is not a number!");
                }
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:NCrossCaseCount", nsmgr);
            if (node != null)
            {
                try
                {
                    this._nCrossCaseCount = int.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("NCrossCaseCount is not a number!");
                }
            }

            if (this._maxCaseCount <= this._nCrossCaseCount)
            {
                throw new Exception("MaxCaseCount must greaterr than NCrossCaseCount!");
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:NFolderCount", nsmgr);
            if (node != null)
            {
                try
                {
                    this._nFolderCount = int.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("NFolderCount is not a number!");
                }
            }

            var prons = xmlDoc.SelectNodes("//tts:Prons/tts:Pron", nsmgr);
            if (prons != null && prons.Count > 0)
            {
                foreach (XmlNode item in prons)
                {
                    try
                    {
                        string pinyin = item.Attributes["pinyin"].Value;
                        if (!string.IsNullOrEmpty(pinyin) &&
                            !this._prons.ContainsKey(pinyin))
                        {
                            this._prons.Add(pinyin, item.InnerText);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException(ex.Message);
                    }
                }
            }

            node = xmlDoc.SelectSingleNode("//tts:Paths/tts:BranchRootPath", nsmgr);
            if (node != null)
            {
                this._branchRootPath = node.InnerText;

                if (!Directory.Exists(this._branchRootPath))
                {
                    throw Helper.CreateException(typeof(DirectoryNotFoundException), this._branchRootPath);
                }

                string langStringWithHyphen = Localor.LanguageToString(this._lang);

                this._crfModelDir = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._crfModelDirPattern, langStringWithHyphen));
                Helper.ThrowIfDirectoryNotExist(this._crfModelDir);

                this._langDataPath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._langDataPathPattern, this._lang));
                Helper.ThrowIfFileNotExist(this._langDataPath);

                this._polyRuleFilePath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._polyRuleFilePathPattern, langStringWithHyphen));
                Helper.ThrowIfFileNotExist(this._polyRuleFilePath);

                this._compileConfigFilePath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._compileConfigFilePathPattern, langStringWithHyphen));
                Helper.ThrowIfFileNotExist(this._compileConfigFilePath);

                this._compileConfigRawDataRootPath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._compileConfigRawDataRootPathPattern, langStringWithHyphen));
                Helper.ThrowIfDirectoryNotExist(this._compileConfigRawDataRootPath);

                this._compileConfigOutputDirPath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._compileConfigOutputDirPathPattern, langStringWithHyphen));
                Helper.ThrowIfDirectoryNotExist(this._compileConfigOutputDirPath);

                this._compileConfigReportFilePath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._compileConfigReportFilePathPattern, langStringWithHyphen));
                Helper.TestWritable(this._compileConfigReportFilePath);
            }

            node = xmlDoc.SelectSingleNode("//tts:Paths/tts:Arch", nsmgr);
            if (node != null)
            {
                this._arch = node.InnerText;

                this._offlineToolPath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this._offlineToolPathPattern, this._arch));
                Helper.ThrowIfDirectoryNotExist(this._offlineToolPath);
            }

            node = xmlDoc.SelectSingleNode("//tts:MaxThreadCount", nsmgr);
            if (node != null)
            {
                try
                {
                    this._maxThreadCount = int.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("MaxThreadCount is not a number!");
                }

                if (this._maxThreadCount <= 0)
                {
                    this._maxThreadCount = 1;
                }
            }

            node = node = xmlDoc.SelectSingleNode("//tts:TrainingConfigTemplate/tts:Training", nsmgr);
            if (node != null)
            {
                this._trainingConfigTemplate = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingConfigTemplate/tts:Features", nsmgr);
            if (node != null)
            {
                this._featuresConfigTemplate = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:ShowTipCount", nsmgr);
            if (node != null)
            {
                try
                {
                    this._showTipCount = int.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("ShowTipCount is not a number!");
                }

                if (this._showTipCount < 0)
                {
                    this._showTipCount = 0;
                }
            }
        }

        #endregion
    }
}
