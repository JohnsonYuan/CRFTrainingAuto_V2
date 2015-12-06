namespace CRFTrainingAuto
{
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Training char info
    /// </summary>
    public struct TrainingChar
    {
        public string CharName;
        // crf trained file name
        public string OutputCRFName;
        // Using info in CRF mapping file, "Bing Used", or "Unused"
        public string UsageInfo;
        // char's pron
        public Dictionary<string, string> Prons;
    }
    public class LocalConfig
    {
        #region fields
        private string TtsSchemaUri = @"http://schemas.microsoft.com/tts/toolsuite";

        private Language _lang;
        private string _crfModelDir;
        private string _defaultWordPron;
        private int _maxCaseCount;
        private int _nCrossCaseCount;
        private int _nFolderCount;
        private string _trainingChar;
        private List<TrainingChar> _trainingChars = new List<TrainingChar>();
        private string _branchRootPath;
        private string _offlineToolPath;
        private string _voicePath;
        private string _langDataPath;
        private int _maxThreadCount;
        private string _trainingConfigTemplate;
        private string _featuresConfigTemplate;
        private int _showTipCount;
        private static XmlSchema _schema;
        #endregion

        /// <summary>
        /// provide a absolute or relative config file and load the config
        /// </summary>
        /// <param name="configPath">config file path, absolute or relative path</param>
        public LocalConfig(string configPath)
        {
            configPath = Util.GetAbsolutePath(configPath);

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException(configPath + " not exist!");
            }

            // Check the configuration file first
            try
            {
                Microsoft.Tts.Offline.Utility.XmlHelper.Validate(configPath, ConfigSchema);
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine("The config file format is not correct, error message: \r\n" + ex.InnerException.Message);
                throw;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(configPath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("tts", TtsSchemaUri);

            XmlElement root = doc.DocumentElement;
            XmlNode node;

            #region init fields
            node = root.SelectSingleNode("//tts:TrainingSettings/@trainingWord", nsmgr);
            if (node != null)
            {
                _trainingChar = node.Value;
            }

            node = root.SelectSingleNode("//tts:TrainingSettings/tts:Language", nsmgr);
            if (node != null)
            {
                _lang = Localor.StringToLanguage(node.InnerText);
            }

            node = root.SelectSingleNode("//tts:TrainingSettings/tts:DefaultWordPron", nsmgr);
            if (node != null)
            {
                _defaultWordPron = node.InnerText;
            }

            node = root.SelectSingleNode("//tts:TrainingSettings/tts:MaxCaseCount", nsmgr);
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

            node = root.SelectSingleNode("//tts:TrainingSettings/tts:NCrossCaseCount", nsmgr);
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

            node = root.SelectSingleNode("//tts:TrainingSettings/tts:NFolderCount", nsmgr);
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

            var trainingChars = root.SelectNodes("//tts:TrainingChars/tts:Char", nsmgr);
            if (trainingChars != null && trainingChars.Count > 0)
            {
                string charName;
                string outputCRFName;
                string usageInfo;
                Dictionary<string, string> prons = new Dictionary<string, string>();
                foreach (XmlNode trainingChar in trainingChars)
                {
                    charName = trainingChar.Attributes["name"].Value;
                    outputCRFName = trainingChar["OutputCRFName"].InnerText;
                    usageInfo = trainingChar["OutputCRFName"].InnerText == "0" ? "Unused" : "Being_used";

                    foreach (XmlNode pron in trainingChar["Prons"].ChildNodes)
                    {
                        string pinyin = pron.Attributes["pinyin"].Value;
                        if (!string.IsNullOrEmpty(pinyin) &&
                            !prons.ContainsKey(pinyin))
                        {
                            prons.Add(pinyin, pron.InnerText);
                        }
                    }

                    _trainingChars.Add(new TrainingChar
                    {
                        CharName = charName,
                        OutputCRFName = outputCRFName,
                        UsageInfo = usageInfo,
                        Prons = prons
                    });
                }
            }
            /*
            node = root.SelectSingleNode("//tts:TrainingSettings/tts:OutputCRFName", nsmgr);
            if (node != null)
            {
                _outputCRFName = node.InnerText;
            }

            node = root.SelectSingleNode("//tts:TrainingSettings/tts:UsageInfo", nsmgr);
            if (node != null)
            {
                switch (node.InnerText)
                {
                    case "0":
                        _usageInfo = "Unused";
                        break;
                    case "1":
                        _usageInfo = "Being_used";
                        break;
                    default:
                        throw new Exception("Enabled value can only be 1 or 0!");
                }
            }


            var prons = root.SelectNodes("//tts:Prons/tts:Pron", nsmgr);
            if(prons != null)
            {
                foreach (XmlNode item in prons)
                {
                    string pinyin = item.Attributes["pinyin"].Value;
                    if (!string.IsNullOrEmpty(pinyin) &&
                        !_prons.ContainsKey(pinyin))
                    {
                        _prons.Add(pinyin, item.InnerText);
                    }
                }
            }*/

            node = root.SelectSingleNode("//tts:Paths/tts:BranchRootPath", nsmgr);
            if (node != null)
            {
                _branchRootPath = node.InnerText;
                if (!Directory.Exists(_branchRootPath))
                {
                    throw Helper.CreateException(typeof(DirectoryNotFoundException), _branchRootPath);
                }
            }

            node = root.SelectSingleNode("//tts:Paths/tts:OfflineToolPath", nsmgr);
            if (node != null)
            {
                _offlineToolPath = Path.Combine(_branchRootPath, node.InnerText);

                if (!Directory.Exists(_offlineToolPath))
                {
                    throw Helper.CreateException(typeof(DirectoryNotFoundException), _offlineToolPath);
                }
            }

            node = root.SelectSingleNode("//tts:Paths/tts:VoicePath", nsmgr);
            if (node != null)
            {
                _voicePath = Path.Combine(_branchRootPath, node.InnerText);

                if (!File.Exists(_voicePath + ".APM"))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException), _voicePath + ".APM");
                }
            }

            node = root.SelectSingleNode("//tts:Paths/tts:LangDataPath", nsmgr);
            if (node != null)
            {
                _langDataPath = Path.Combine(_branchRootPath, node.InnerText);

                if (!File.Exists(_langDataPath))
                {
                    throw Helper.CreateException(typeof(FileNotFoundException), _langDataPath);
                }
            }

            node = root.SelectSingleNode("//tts:Paths/tts:CRFModelDir", nsmgr);
            if (node != null)
            {
                _crfModelDir = Path.Combine(_branchRootPath, node.InnerText);

                if (!Directory.Exists(_crfModelDir))
                {
                    throw Helper.CreateException(typeof(DirectoryNotFoundException), _crfModelDir);
                }
            }

            node = root.SelectSingleNode("//tts:Paths/tts:LangDataPath", nsmgr);
            if (node != null)
            {
                _langDataPath = Path.Combine(_branchRootPath, node.InnerText);

                // if not supply data path, use the default handler
                if (!File.Exists(_langDataPath))
                {
                    _langDataPath = null;
                }
            }

            node = root.SelectSingleNode("//tts:MaxThreadCount", nsmgr);
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

            node = node = root.SelectSingleNode("//tts:TrainingConfigTemplate/tts:Training", nsmgr);
            if (node != null)
            {
                _trainingConfigTemplate = node.InnerText;
            }

            node = root.SelectSingleNode("//tts:TrainingConfigTemplate/tts:Features", nsmgr);
            if (node != null)
            {
                _featuresConfigTemplate = node.InnerText;
            }

            node = root.SelectSingleNode("//tts:ShowTipCount", nsmgr);
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

        #region Properties
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
                    _schema = Microsoft.Tts.Offline.Utility.XmlHelper.LoadSchemaFromResource(assembly,
                        "CRFTrainingAuto.CRFTrainingAutoConfig.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// The char to be trained
        /// </summary>
        public TrainingChar CurrentTrainingChar
        {
            get
            {
                try
                {
                    return _trainingChars.First(p=> string.Equals(p.CharName, _trainingChar));
                }
                catch
                {
                    throw new ArgumentException("Current training char info is not provided in config section <TrainingChars>.");
                }
                
            }
        }

        /// <summary>
        /// Training char list
        /// </summary>
        public List<TrainingChar> TrainingChars
        {
            get
            {
                return _trainingChars;
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
        /// Speech voice path
        /// </summary>
        public string VoicePath
        {
            get
            {
                return _voicePath;
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
    }
}
