namespace CRFTrainingAuto
{
    using System;
    using System.Configuration;
    using System.IO;
    public class _ConfigHelper
    {
        private Configuration _config = null;

        /// <summary>
        /// provide a absolute or relative config file and load the config
        /// </summary>
        /// <param name="configPath">config file path, absolute or relative path</param>
        public ConfigHelper(string confiePath)
        {
            confiePath = Util.MakeAbsolutePath(confiePath);

            if (!File.Exists(confiePath))
            {
                throw new Exception(
                  "The configuration file does not exist.");
            }

            // Map the new configuration file.
            ExeConfigurationFileMap configFileMap =
                new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = confiePath;

            // Get the application configuration file.
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
            _config = config;
        }
        
        public string GetSettingValue(string key)
        {
            return _config.AppSettings.Settings[key].Value;
        }

        /// <summary>
        /// The char to be trained
        /// </summary>
        public string CharName
        {
            get
            {
                return GetSettingValue("CharName");
            }
        }

        /// <summary>
        /// The char to be trained
        /// </summary>
        public string OutputCRFName
        {
            get
            {
                return GetSettingValue("OutputCRFName");
            }
        }

        /// <summary>
        /// Current Language
        /// </summary>
        public string Language
        {
            get
            {
                return GetSettingValue("Language");
            }
        }

        public string LanguageDataPath
        {
            get
            {
                return Path.Combine(BranchRootPath, 
                        @"private\dev\speech\tts\shenzhou\src\lochand", 
                        Language.Replace("-", ""), 
                        "MSTTSLoc" + Language.Replace("-", "") + ".dat");
            }
        }

        public string ExistTrainingScriptFolder
        {
            get
            {
                return Path.Combine(BranchRootPath, 
                    GetSettingValue("ExistTrainingScriptFolderFormat"));
            }
        }

        public string BranchRootPath
        {
            get
            {
                return GetSettingValue("BranchRootPath");
            }
        }
        public string OfflineToolPath
        {
            get
            {
                return GetSettingValue("OfflineToolPath");
            }
        }

        public string VoicePath
        {
            get
            {
                return Path.Combine(BranchRootPath,
                    GetSettingValue("VoicePath"));
            }
        }
        public string LexiconPath
        {
            get
            {
                return Path.Combine(BranchRootPath,
                    GetSettingValue("LexiconPath"));
            }
        }
        public string OutputDir
        {
            get
            {
                return GetSettingValue("OutputDir");
            }
        }
        public string CorpusDir
        {
            get
            {
                return GetSettingValue("CorpusDir");
            }
        }
    }
}
