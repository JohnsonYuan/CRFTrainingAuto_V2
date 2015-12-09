namespace CRFTrainingAuto
{
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
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
        private static XmlSchema _schema;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LocalConfig class
        /// </summary>
        /// <param name="configPath">xml config file path</param>
        private LocalConfig(string configFilePath)
        {
            base.Load(configFilePath);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets corpusCleanerConfig schema.
        /// </summary>
        public override XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    _schema = Microsoft.Tts.Offline.Utility.XmlHelper.LoadSchemaFromResource(assembly,
                        "CRFTrainingAuto.CRFTrainingAutoConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// The char to be trained
        /// </summary>
        public string CharName
        {
            get
            {
                return _charName;
            }
        }

        /// <summary>
        /// Using info in CRF mapping file, "Bing Used", or "Unused"
        /// </summary>
        public string UsingInfo
        {
            get
            {
                return _usingInfo;
            }
        }

        /// <summary>
        /// Current Language
        /// </summary>
        public Language Lang
        {
            get
            {
                return _lang;
            }
        }

        /// <summary>
        /// The char to be trained
        /// </summary>
        public string OutputCRFName
        {
            get
            {
                return _outputCRFName;
            }
        }

        /// <summary>
        /// Used crf model folder
        /// </summary>
        public string CRFModelDir
        {
            get
            {
                return _crfModelDir;
            }
        }

        /// <summary>
        /// Default pron for word has no pron
        /// </summary>
        public string DefaultWordPron
        {
            get
            {
                return _defaultWordPron;
            }
        }

        /// <summary>
        /// Filtered case min length
        /// </summary>
        public int MinCaseLength
        {
            get
            {
                return _minCaseLength;
            }
        }

        /// <summary>
        /// Max case count used to train crf model
        /// </summary>
        public int MaxCaseCount
        {
            get
            {
                return _maxCaseCount;
            }
        }

        /// <summary>
        /// Case count using N Cross
        /// </summary>
        public int NCrossCaseCount
        {
            get
            {
                return _nCrossCaseCount;
            }
        }

        /// <summary>
        /// Folder count using N Cross
        /// </summary>
        public int NFolderCount
        {
            get
            {
                return _nFolderCount;
            }
        }

        /// <summary>
        /// Word prons contains pinyin and native phone
        /// </summary>
        public Dictionary<string, string> Prons
        {
            get
            {
                return _prons;
            }
        }

        /// <summary>
        /// Brach root path
        /// </summary>
        public string BranchRootPath
        {
            get
            {
                return _branchRootPath;
            }
        }

        /// <summary>
        /// Architecure, Amd64 or x86
        /// </summary>
        public string Arch
        {
            get
            {
                return _arch;
            }
        }

        /// <summary>
        /// Branch offline path
        /// </summary>
        public string OfflineToolPath
        {
            get
            {
                return _offlineToolPath;
            }
        }

        /// <summary>
        /// Language data path
        /// </summary>
        public string LangDataPath
        {
            get
            {
                return _langDataPath;
            }
        }

        /// <summary>
        /// polyrule.txt file path
        /// </summary>
        public string PolyRuleFilePath
        {
            get
            {
                return _polyRuleFilePath;
            }
        }

        /// <summary>
        /// Max thread count when filtering char from corpus
        /// </summary>
        public int MaxThreadCount
        {
            get
            {
                return _maxThreadCount;
            }
        }

        /// <summary>
        /// Training CRF model config template
        /// </summary>
        public string TrainingConfigTemplate
        {
            get
            {
                return _trainingConfigTemplate;
            }
        }

        /// <summary>
        /// Training CRF model feature config template
        /// </summary>
        public string FeaturesConfigTemplate
        {
            get
            {
                return _featuresConfigTemplate;
            }
        }

        /// <summary>
        /// Used to show pogress when filtering char
        /// </summary>
        public int ShowTipCount
        {
            get
            {
                return _showTipCount;
            }
            set
            {
                _showTipCount = value;
            }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">XmlDoc.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controler.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            XmlNode node;

            #region init fields
            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/@name", nsmgr);
            if (node != null)
            {
                _charName = node.Value;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:Language", nsmgr);
            if (node != null)
            {
                _lang = Localor.StringToLanguage(node.InnerText);
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:OutputCRFName", nsmgr);
            if (node != null)
            {
                _outputCRFName = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:Enabled", nsmgr);
            if (node != null)
            {
                switch (node.InnerText)
                {
                    case "0":
                        _usingInfo = "Unused";
                        break;
                    case "1":
                        _usingInfo = "Being_used";
                        break;
                    default:
                        throw new Exception("Enabled value can only be 1 or 0!");
                }
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:DefaultWordPron", nsmgr);
            if (node != null)
            {
                _defaultWordPron = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:MinCaseLength", nsmgr);
            if (node != null)
            {
                try
                {
                    _minCaseLength = Int32.Parse(node.InnerText);
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
                    _maxCaseCount = Int32.Parse(node.InnerText);
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
                    _nCrossCaseCount = Int32.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("NCrossCaseCount is not a number!");
                }
            }

            if (_maxCaseCount <= _nCrossCaseCount)
            {
                throw new Exception("MaxCaseCount must greaterr than NCrossCaseCount!");
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingChar/tts:NFolderCount", nsmgr);
            if (node != null)
            {
                try
                {
                    _nFolderCount = Int32.Parse(node.InnerText);
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
                            !_prons.ContainsKey(pinyin))
                        {
                            _prons.Add(pinyin, item.InnerText);
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
                _branchRootPath = node.InnerText;
                if (!Directory.Exists(_branchRootPath))
                {
                    throw Helper.CreateException(typeof(DirectoryNotFoundException), _branchRootPath);
                }
            }

            node = xmlDoc.SelectSingleNode("//tts:Paths/tts:Arch", nsmgr);
            if (node != null)
            {
                _arch = node.InnerText;

                _offlineToolPath = Path.Combine(_branchRootPath, string.Format(OfflineToolPathPattern, _arch));
                Helper.ThrowIfDirectoryNotExist(_offlineToolPath);

                _crfModelDir = Path.Combine(_branchRootPath, string.Format(CRFModelDirPattern, Localor.LanguageToString(_lang)));
                Helper.ThrowIfDirectoryNotExist(_crfModelDir);

                _langDataPath = Path.Combine(_branchRootPath, string.Format(LangDataPathPattern, Localor.LanguageToString(_lang).Replace("-", "")));
                Helper.ThrowIfFileNotExist(_langDataPath);
                
                _polyRuleFilePath = Path.Combine(_branchRootPath, string.Format(PolyRuleFilePathPattern, Localor.LanguageToString(_lang).Replace("-", "")));
                Helper.ThrowIfFileNotExist(_polyRuleFilePath);
            }

            node = xmlDoc.SelectSingleNode("//tts:MaxThreadCount", nsmgr);
            if (node != null)
            {
                try
                {
                    _maxThreadCount = Int32.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("MaxThreadCount is not a number!");
                }

                if (_maxThreadCount <= 0)
                {
                    _maxThreadCount = 1;
                }
            }

            node = node = xmlDoc.SelectSingleNode("//tts:TrainingConfigTemplate/tts:Training", nsmgr);
            if (node != null)
            {
                _trainingConfigTemplate = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:TrainingConfigTemplate/tts:Features", nsmgr);
            if (node != null)
            {
                _featuresConfigTemplate = node.InnerText;
            }

            node = xmlDoc.SelectSingleNode("//tts:ShowTipCount", nsmgr);
            if (node != null)
            {
                try
                {
                    _showTipCount = Int32.Parse(node.InnerText);
                }
                catch
                {
                    throw new FormatException("ShowTipCount is not a number!");
                }

                if (_showTipCount < 0)
                {
                    _showTipCount = 0;
                }
            }
            #endregion
        }

        /// <summary>
        /// Init _instance when _instance is null, support multithread Singleton
        /// </summary>
        /// <param name="configFilePath">xml config file path</param>
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
        /// LocalConfig instance
        /// </summary>
        public static LocalConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new Exception("Object not created");
                }

                return _instance;
            }
        }

        #endregion
    }
}
