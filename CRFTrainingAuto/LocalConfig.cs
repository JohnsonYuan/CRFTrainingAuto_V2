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

    public class LocalConfig : XmlDataFile
    {
        #region Static fields

        private static volatile LocalConfig _instance;
        private static object _locker = new object();

        #endregion

        #region Fields

        // target\distrib\debug\{amd64/x86}\dev\TTS\Server\bin\Offline
        private string OfflineToolPathPattern = @"target\distrib\debug\{0}\dev\TTS\Server\bin\Offline";
        
        // private\dev\speech\tts\shenzhou\data\zh-CN\Language\Model.Rule\PolyphonyModel\ModelUsed
        private string CRFModelDirPattern = @"private\dev\speech\tts\shenzhou\data\{0}\Language\Model.Rule\PolyphonyModel\ModelUsed";
        
        // private\dev\speech\tts\shenzhou\src\lochand\ZhCN\MSTTSLocZhCN.dat
        private string LangDataPathPattern = @"private\dev\speech\tts\shenzhou\src\lochand\{0}\MSTTSLoc{0}.dat";
        
        // private\dev\speech\tts\shenzhou\data\zh-CN\Language\Model.Rule\Polyphony\polyrule.txt
        private string PolyRuleFilePathPattern = @"private\dev\speech\tts\shenzhou\data\{0}\Language\Model.Rule\Polyphony\polyrule.txt";

        private string _charName;
        private Language _lang;
        private string _outputCRFName;
        private string _usingInfo;
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
        /// <param name="configPath">xml config file path.</param>
        private LocalConfig(string configPath)
        {
            base.Load(configPath);
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
        /// Gets Using info in CRF mapping file, "Bing Used", or "Unused".
        /// </summary>
        public string UsingInfo
        {
            get
            {
                return this._usingInfo;
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
        /// Gets default pron for word has no pron.
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
        /// Gets word prons contains pinyin and native phone.
        /// </summary>
        public Dictionary<string, string> Prons
        {
            get
            {
                return this._prons;
            }
        }

        /// <summary>
        /// Gets brach root path.
        /// </summary>
        public string BranchRootPath
        {
            get
            {
                return this._branchRootPath;
            }
        }

        /// <summary>
        /// Gets architecure, Amd64 or x86.
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
        /// Gets or sets show pogress count when filtering char.
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
        /// Init _instance when _instance is null, support multithread Singleton.
        /// </summary>
        /// <param name="configFilePath">xml config file path.</param>
        public static void Create(string configFilePath)
        {
            if (_instance == null)
            {
                lock (_locker)
                {
                    if (_instance == null)
                    {
                        _instance = new LocalConfig(configFilePath);
                    }
                }
            }
            else
            {
                throw new Exception("Object already created");
            }
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">XmlDoc.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controler.</param>
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

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:Enabled", nsmgr);
            if (node != null)
            {
                switch (node.InnerText)
                {
                    case "0":
                        this._usingInfo = "Unused";
                        break;
                    case "1":
                        this._usingInfo = "Being_used";
                        break;
                    default:
                        throw new Exception("Enabled value can only be 1 or 0!");
                }
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
            }

            node = xmlDoc.SelectSingleNode("//tts:Paths/tts:Arch", nsmgr);
            if (node != null)
            {
                this._arch = node.InnerText;

                this._offlineToolPath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this.OfflineToolPathPattern, this._arch));
                Helper.ThrowIfDirectoryNotExist(this._offlineToolPath);

                this._crfModelDir = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this.CRFModelDirPattern, Localor.LanguageToString(this._lang)));
                Helper.ThrowIfDirectoryNotExist(this._crfModelDir);

                this._langDataPath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this.LangDataPathPattern, Localor.LanguageToString(this._lang).Replace("-", string.Empty)));
                Helper.ThrowIfFileNotExist(this._langDataPath);

                this._polyRuleFilePath = Path.Combine(this._branchRootPath, Helper.NeutralFormat(this.PolyRuleFilePathPattern, Localor.LanguageToString(this._lang)));
                Helper.ThrowIfFileNotExist(this._polyRuleFilePath);
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
