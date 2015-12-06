//----------------------------------------------------------------------------
// <copyright file="LanguageData.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Language Data Compiler
// </summary>
//----------------------------------------------------------------------------

namespace LangDataCompiler
{
    using System;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;

    /// <summary>
    /// Class of Each Language Data.
    /// </summary>
    public class LanguageData
    {
        #region Fields

        private string _name;
        private string _domain = DomainItem.GeneralDomain;
        private string _guid;
        private string _formatGuid;
        private string _path;
        private string _innerCompilingXml;
        private bool _compile = true;
        private bool _isCustomer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the data is load from rawData or customeData element in the config file.
        /// </summary>
        public bool IsCustomer
        {
            get { return _isCustomer; }
            set { _isCustomer = value; }
        }

        /// <summary>
        /// Gets or sets GuidString of Language Data.
        /// </summary>
        public string Guid
        {
            get
            {
                return _guid;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _guid = value;
            }
        }

        /// <summary>
        /// Gets or sets GuidString of Language Format Data .
        /// </summary>
        public string FormatGuid
        {
            get
            {
                return _formatGuid;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _formatGuid = value;
            }
        }

        /// <summary>
        /// Gets or sets Language Data Name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                _name = value;
                string guid = LanguageDataHelper.GetReservedGuid(_name);
                if (!string.IsNullOrEmpty(guid))
                {
                    _guid = guid;
                }
            }
        }

        /// <summary>
        /// Gets or sets Domain name of language data.
        /// </summary>
        public string Domain
        {
            get
            {
                return _domain;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }

                _domain = value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets File name of language data.
        /// </summary>
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether compile the data.
        /// </summary>
        public bool Compile
        {
            get { return _compile; }
            set { _compile = value; }
        }

        /// <summary>
        /// Gets or sets Inner Compiling Xml.
        /// </summary>
        public string InnerCompilingXml
        {
            get { return _innerCompilingXml; }
            set { _innerCompilingXml = value; }
        }

        #endregion
    }
}