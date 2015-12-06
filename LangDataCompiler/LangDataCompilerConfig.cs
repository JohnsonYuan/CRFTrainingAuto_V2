//----------------------------------------------------------------------------
// <copyright file="LangDataCompilerConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements LangDataCompilerConfig
// </summary>
//----------------------------------------------------------------------------

namespace LangDataCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Class of LanguageDataFile.
    /// </summary>
    public class LangDataCompilerConfig : XmlDataFile
    {
        #region Fields

        private static XmlSchema _schema;

        /// <summary>
        /// Tool work site folder, from where some dependent tools could be found.
        /// </summary>
        private string _toolDir;
        private string _binRootDir;
        private string _customerRootDir;
        private string _rawRootDir;
        private bool _isServiceProviderRequired = true;

        /// <summary>
        /// Dictionary.
        /// </summary>
        private Dictionary<string, string> _outputFilePaths = new Dictionary<string, string>();
        private Collection<LanguageData> _moduleDataList = new Collection<LanguageData>();
        private Dictionary<string, string> _rawDataList = new Dictionary<string, string>();
        private string _logFilePath;

        #endregion
        
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LangDataCompilerConfig"/> class.
        /// </summary>
        public LangDataCompilerConfig()
        {
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets schema of LangDataCompiler.xml.
        /// </summary>
        public override System.Xml.Schema.XmlSchema Schema
        {
            get
            {
                if (_schema == null)
                {
                    _schema = XmlHelper.LoadSchemaFromResource("Microsoft.Tts.Offline.Config.LangDataCompiler.xsd");
                }

                return _schema;
            }
        }

        /// <summary>
        /// Gets list of language data.
        /// </summary>
        public Collection<LanguageData> LanguageDataList
        {
            get
            {
                return _moduleDataList;
            }
        }

        /// <summary>
        /// Gets output file paths.
        /// </summary>
        public Dictionary<string, string> OutputPaths
        {
            get { return _outputFilePaths; }
        }

        /// <summary>
        /// Gets or sets directory of tool.
        /// </summary>
        public string ToolDir
        {
            get { return _toolDir; }
            set { _toolDir = value; }
        }

        /// <summary>
        /// Gets or sets directory of bin data.
        /// </summary>
        public string BinRootDir
        {
            get { return _binRootDir; }
            set { _binRootDir = value; }
        }

        /// <summary>
        /// Gets or sets directory of customer data.
        /// </summary>
        public string CustomerRootDir
        {
            get { return _customerRootDir; }
            set { _customerRootDir = value; }
        }

        /// <summary>
        /// Gets or sets directory of raw data.
        /// </summary>
        public string RawRootDir
        {
            get { return _rawRootDir; }
            set { _rawRootDir = value; }
        }

        /// <summary>
        /// Gets or sets log file.
        /// </summary>
        public string LogFilePath
        {
            get { return _logFilePath; }
            set { _logFilePath = value; }
        }

        /// <summary>
        /// Gets raw data List.
        /// </summary>
        public Dictionary<string, string> RawDataList
        {
            get { return _rawDataList; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ServiceProvider is required.
        /// </summary>
        public bool IsServiceProviderRequired
        {
            get { return _isServiceProviderRequired; }
            set { _isServiceProviderRequired = value; }
        }

        #endregion

        #region method

        /// <summary>
        /// Apply data directory for those not Rooted paths.
        /// </summary>
        public void ApplyRootDataDir()
        {
            // Set the path for binary output data
            foreach (LanguageData languageData in LanguageDataList)
            {
                if (!string.IsNullOrEmpty(languageData.Path) && !Path.IsPathRooted(languageData.Path))
                {
                    if (languageData.IsCustomer)
                    {
                        languageData.Path = Path.Combine(CustomerRootDir, languageData.Path);
                    }
                    else
                    {
                        languageData.Path = Path.Combine(BinRootDir, languageData.Path);
                    }
                }
            }

            // Set the path for raw data
            Collection<string> rawDataNames = new Collection<string>();
            foreach (string rawDataName in _rawDataList.Keys)
            {
                rawDataNames.Add(rawDataName);
            }

            foreach (string rawDataName in rawDataNames)
            {
                string path = _rawDataList[rawDataName];
                if (!rawDataName.Equals(RawDataName.ForeignLtsCollection) && !string.IsNullOrEmpty(path) && !Path.IsPathRooted(path))
                {
                    _rawDataList[rawDataName] = Path.Combine(RawRootDir, path);
                }
            }
        }

        /// <summary>
        /// Validate the DataDir and ToolDir.
        /// </summary>
        public void ValidateDirPath()
        {
            if (!string.IsNullOrEmpty(_customerRootDir) && !Directory.Exists(_customerRootDir))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The directory of customer data '{0}' does not exist", _customerRootDir));
            }

            if (!string.IsNullOrEmpty(_rawRootDir) && !Directory.Exists(_rawRootDir))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The directory of raw data '{0}' does not exist", _rawRootDir));
            }

            if (string.IsNullOrEmpty(_binRootDir))
            {
                throw new InvalidDataException("Binary directory should not be empty");
            }

            Helper.EnsureFolderExist(_binRootDir);

            if (!string.IsNullOrEmpty(_toolDir) && !Directory.Exists(_toolDir))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "The directory of tool '{0}' does not exist", _toolDir));
            }
        }

        /// <summary>
        /// Get all domains.
        /// </summary>
        /// <returns>Domain List.</returns>
        public List<string> GetAllDomains()
        {
            List<string> domains = new List<string>();

            foreach (LanguageData langData in _moduleDataList)
            {
                if (!domains.Contains(langData.Domain))
                {
                    domains.Add(langData.Domain);
                }
            }

            return domains;
        }

        /// <summary>
        /// Get LanguageData dictionary, key is domain , value is LanguageData Collection.
        /// </summary>
        /// <returns>LanguageData dictionary.</returns>
        public Dictionary<string, Collection<LanguageData>> GetAllLanguageDataInDomains()
        {
            Dictionary<string, Collection<LanguageData>> datas = new Dictionary<string, Collection<LanguageData>>();

            foreach (LanguageData langData in _moduleDataList)
            {
                if (datas.ContainsKey(langData.Domain))
                {
                    datas[langData.Domain].Add(langData);
                }
                else
                {
                    Collection<LanguageData> dataCollection = new Collection<LanguageData>() { langData };
                    datas.Add(langData.Domain, dataCollection);
                }
            }

            return datas;
        }

        /// <summary>
        /// Get LanguageData in specified domain.
        /// </summary>
        /// <param name="dataName">Langdata name.</param>
        /// <param name="domain">Domain tag.</param>
        /// <returns>LanguageData.</returns>
        public LanguageData GetLangDataInDomain(string dataName, string domain)
        {
            Helper.ThrowIfNull(dataName);
            Helper.ThrowIfNull(domain);

            LanguageData data = null;
            foreach (LanguageData langData in _moduleDataList)
            {
                if (langData.Name.Equals(dataName, StringComparison.Ordinal) &&
                    langData.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase))
                {
                    data = langData;
                    break;
                }
            }

            return data;
        }

        /// <summary>
        /// Load XML file.
        /// </summary>
        /// <param name="xmlDoc">XmlDoc.</param>
        /// <param name="nsmgr">Nsmgr.</param>
        /// <param name="contentController">Content controler.</param>
        protected override void Load(XmlDocument xmlDoc, XmlNamespaceManager nsmgr, object contentController)
        {
            Language = Localor.StringToLanguage(xmlDoc.DocumentElement.Attributes["language"].InnerText);
            XmlNode node = xmlDoc.DocumentElement.SelectSingleNode(@"tts:toolDir", nsmgr);
            if (node != null)
            {
                _toolDir = node.Attributes["path"].Value;
            }

            node = xmlDoc.DocumentElement.SelectSingleNode(@"tts:isServiceProviderRequired", nsmgr);
            if (node != null)
            {
                bool.TryParse(node.Attributes["value"].Value, out _isServiceProviderRequired);
            }

            node = xmlDoc.DocumentElement.SelectSingleNode(@"tts:logFile", nsmgr);
            if (node != null)
            {
                _logFilePath = node.Attributes["path"].Value;
            }

            _moduleDataList.Clear();
            LoadModuleDataSetting(xmlDoc, nsmgr);
            LoadCustomerDataSetting(xmlDoc, nsmgr);
            LoadRawDataSetting(xmlDoc, nsmgr);
            LoadOutputFilePaths(xmlDoc, nsmgr);
        }

        /// <summary>
        /// Check whether the Guid string is valid.
        /// </summary>
        /// <param name="guidValue">Guid string.</param>
        /// <returns>True if existed, otherwise false.</returns>
        protected bool ExistGuidString(string guidValue)
        {
            bool found = false;
            foreach (LanguageData langData in _moduleDataList)
            {
                if (langData.Guid.Equals(guidValue))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Exist data.
        /// </summary>
        /// <param name="name">Data name.</param>
        /// <param name="domain">Domain name.</param>
        /// <returns>True if existed, otherwise false.</returns>
        protected bool ExistData(string name, string domain)
        {
            bool found = false;
            foreach (LanguageData langData in _moduleDataList)
            {
                if (langData.Name.Equals(name) &&
                    langData.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Load output file paths.
        /// </summary>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="nsmgr">Namespace manager.</param>
        private void LoadOutputFilePaths(XmlDocument xmlDoc, XmlNamespaceManager nsmgr)
        {
            XmlNodeList outputNodeList = xmlDoc.DocumentElement.SelectNodes(@"//tts:outputFile", nsmgr);
            foreach (XmlNode dataNode in outputNodeList)
            {
                if (dataNode.Attributes["path"] != null)
                {
                    string path = dataNode.Attributes["path"].Value;
                    string domain = null;
                    if (dataNode.Attributes["domain"] != null &&
                        !string.IsNullOrEmpty(dataNode.Attributes["domain"].Value))
                    {
                        domain = dataNode.Attributes["domain"].Value.ToLowerInvariant();
                    }
                    else
                    {
                        domain = DomainItem.GeneralDomain;
                    }

                    if (!_outputFilePaths.ContainsKey(domain))
                    {
                        _outputFilePaths.Add(domain, path);
                    }
                    else
                    {
                        throw new InvalidDataException(
                            Helper.NeutralFormat("duplicate domain name {0} for output is found", domain));
                    }
                }
                else
                {
                    throw new InvalidDataException("OutputFile is needed.");
                }
            }
        }

        /// <summary>
        /// Load Raw Data setting.
        /// </summary>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="nsmgr">Namespace manager.</param>
        private void LoadRawDataSetting(XmlDocument xmlDoc, XmlNamespaceManager nsmgr)
        {
            XmlNode rawRootDirAttribute = xmlDoc.DocumentElement.SelectSingleNode(@"//tts:rawDataSet/@rootPath", nsmgr);
            if (rawRootDirAttribute != null)
            {
                RawRootDir = rawRootDirAttribute.InnerText;
            }

            this._rawDataList.Clear();
            XmlNodeList dataNodeList = xmlDoc.DocumentElement.SelectNodes(@"//tts:rawDataSet/tts:data", nsmgr);
            foreach (XmlNode dataNode in dataNodeList)
            {
                if (dataNode.Attributes["name"] != null && dataNode.Attributes["path"] != null)
                {
                    string name = dataNode.Attributes["name"].Value;
                    string path = dataNode.Attributes["path"].Value;
                    if (!_rawDataList.ContainsKey(name))
                    {
                        _rawDataList.Add(name, path);
                    }
                    else
                    {
                        throw new InvalidDataException(
                            Helper.NeutralFormat("duplicate raw data name {0} is found", name));
                    }
                }
                else
                {
                    if (dataNode.Attributes["name"] != null)
                    {
                        throw new InvalidDataException("name for the raw data setting should be valid");
                    }

                    if (dataNode.Attributes["path"] != null)
                    {
                        throw new InvalidDataException("path for the raw data setting should be valid");
                    }
                }
            }
        }

        /// <summary>
        /// Load Module Data setting.
        /// </summary>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="nsmgr">Namespace manager.</param>
        private void LoadModuleDataSetting(XmlDocument xmlDoc, XmlNamespaceManager nsmgr)
        {
            XmlNode binRootDirAttribute = xmlDoc.DocumentElement.SelectSingleNode(@"//tts:dataSet/@rootPath", nsmgr);
            if (binRootDirAttribute != null)
            {
                BinRootDir = binRootDirAttribute.InnerText;
            }

            XmlNodeList dataNodeList = xmlDoc.DocumentElement.SelectNodes(@"//tts:dataSet/tts:data", nsmgr);
            foreach (XmlNode dataNode in dataNodeList)
            {
                LanguageData langData = new LanguageData();
                
                // Schema has ensured "name" is required
                Debug.Assert(dataNode.Attributes["name"] != null, "The value must always can't be null");
                langData.Name = dataNode.Attributes["name"].Value;

                if (dataNode.Attributes["path"] != null)
                {
                    langData.Path = dataNode.Attributes["path"].Value;
                }

                if (dataNode.Attributes["compile"] != null)
                {
                    bool compile = true;
                    if (bool.TryParse(dataNode.Attributes["compile"].Value, out compile))
                    {
                        langData.Compile = compile;
                    }
                }

                if (!langData.Compile && string.IsNullOrEmpty(langData.Path))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Path is required for non-compiling data [{0}]", langData.Name));
                }

                if (dataNode.Attributes["domain"] != null &&
                    !string.IsNullOrEmpty(dataNode.Attributes["domain"].Value))
                {
                    langData.Domain = dataNode.Attributes["domain"].Value;

                    List<string> domainSupportedData = new List<string>(new string[] 
                    { 
                        ModuleDataName.Lexicon, 
                        ModuleDataName.PolyphoneRule, 
                        ModuleDataName.SentenceDetector, 
                        ModuleDataName.AcronymDisambiguation, 
                        ModuleDataName.RNNLts,
                        ModuleDataName.CRFWordBreaker,
                        ModuleDataName.CRFSentTypeDetectorModel
                    });
                    if (!domainSupportedData.Contains(langData.Name) && langData.Domain.ToLower() != "general")
                    {
                        throw new InvalidDataException(Helper.NeutralFormat(
                            "Domain data is not supported for [{0}]", langData.Name));
                    }
                }
                else
                {
                    langData.Domain = DomainItem.GeneralDomain;
                }

                if (dataNode.Attributes["guid"] != null)
                {
                    langData.FormatGuid = dataNode.Attributes["guid"].Value;
                }

                langData.InnerCompilingXml = dataNode.InnerXml;

                if (ExistData(langData.Name, langData.Domain))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Duplicate name found for Data [{0}] in Domain [{1}]", langData.Name, langData.Domain));
                }

                langData.IsCustomer = false;
                _moduleDataList.Add(langData);
            }
        }

        /// <summary>
        /// Load Customer Data setting
        /// Example: <data guid="6CE456C1-AB57-4261-A5C5-F5395DDAA5E8" name="CustomerData" path="customer\powerLexicon.bin" />
        /// Guid is required; 
        /// Path is required; 
        /// Name is required.
        /// </summary>
        /// <param name="xmlDoc">Xml document.</param>
        /// <param name="nsmgr">Namespace manager.</param>
        private void LoadCustomerDataSetting(XmlDocument xmlDoc, XmlNamespaceManager nsmgr)
        {
            XmlNode customerRootDirAttribute = xmlDoc.DocumentElement.SelectSingleNode(@"//tts:customerDataSet/@rootPath", nsmgr);
            if (customerRootDirAttribute != null)
            {
                CustomerRootDir = customerRootDirAttribute.InnerText;
            }

            XmlNodeList dataNodeList = xmlDoc.DocumentElement.SelectNodes(@"//tts:customerDataSet/tts:data", nsmgr);
            foreach (XmlNode dataNode in dataNodeList)
            {
                LanguageData langData = new LanguageData();
                
                // Schema has ensured "name" is required
                Debug.Assert(dataNode.Attributes["name"] != null, "The value must always can't be null");
                langData.Name = dataNode.Attributes["name"].Value;
                if (!string.IsNullOrEmpty(langData.Guid))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                    "Name [{0}] of customer Data has been reserved by internal data", langData.Name));
                }

                // Schema has ensured "guid" is required
                Debug.Assert(dataNode.Attributes["guid"] != null, "The value must always can't be null");
                langData.Guid = dataNode.Attributes["guid"].Value;
                if (!string.IsNullOrEmpty(LanguageDataHelper.GetReservedDataName(langData.Guid)))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Guid {{{0}}} of customer Data [{1}] has been reserved by internal data", 
                        langData.Guid, langData.Name));
                }

                // Schema has ensured "path" is required
                Debug.Assert(dataNode.Attributes["path"] != null, "The value must always can't be null");
                langData.Path = dataNode.Attributes["path"].Value;

                if (dataNode.Attributes["domain"] != null &&
                    !string.IsNullOrEmpty(dataNode.Attributes["domain"].Value))
                {
                    langData.Domain = dataNode.Attributes["domain"].Value;
                }
                else
                {
                    langData.Domain = DomainItem.GeneralDomain;
                }

                if (ExistData(langData.Name, langData.Domain))
                {
                    throw new InvalidDataException(Helper.NeutralFormat(
                        "Duplicate name found for Data [{0}] in Domain [{1}]", langData.Name, langData.Domain));
                }

                langData.Compile = false;
                langData.IsCustomer = true;
                _moduleDataList.Add(langData);
            }
        }

        #endregion
    }
}
