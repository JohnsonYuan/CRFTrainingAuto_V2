//----------------------------------------------------------------------------
// <copyright file="AppConfig.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements AppConfigManager class
// </summary>
//----------------------------------------------------------------------------

namespace LangDataCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// AppConfig.cs.
    /// </summary>
    public class AppConfig
    {
        #region Fields

        private const string AppConfigEnableValidationModuleName = "EnableValidationModule";

        private static AppConfig _instance = new AppConfig();

        private Dictionary<string, bool> _validationControlDict =
            new Dictionary<string, bool>();

        #endregion

        #region Construction

        private AppConfig()
        {
            NameValueCollection nameValueCollection = (NameValueCollection)
                ConfigurationManager.GetSection(AppConfigEnableValidationModuleName);

            if (nameValueCollection != null)
            {
                foreach (string name in nameValueCollection.AllKeys)
                {
                    _validationControlDict.Add(name.ToLowerInvariant(),
                        bool.Parse(nameValueCollection[name]));
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        public static AppConfig Instance
        {
            get
            {
                return _instance;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Judge whether enable validate the module.
        /// </summary>
        /// <param name="moduleName">Module name to be checked.</param>
        /// <returns>Whether enable validate the module.</returns>
        public bool IsEnableValidModule(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentNullException("moduleName");
            }

            bool enableValidModule = true;
            moduleName = moduleName.ToLowerInvariant();
            if (_validationControlDict.ContainsKey(moduleName))
            {
                enableValidModule = _validationControlDict[moduleName];
            }

            return enableValidModule;
        }

        #endregion
    }
}