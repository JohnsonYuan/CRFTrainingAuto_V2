//----------------------------------------------------------------------------
// <copyright file="CreateDeltaConfig.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Original Language Data File for LangDataCompiler
// </summary>
//----------------------------------------------------------------------------
namespace LangDataCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Common;
    using Microsoft.Tts.Offline.Compiler.LanguageData;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Utility;
    using Microsoft.Tts.ServiceProvider.LangData;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Original data directory.
    /// </summary>
    public class CreateDeltaConfig
    {
        #region Field Members

        /// <summary>
        /// Frontend setting.
        /// </summary>
        private FrontendSetting _setting;

        /// <summary>
        /// Original dir.
        /// </summary>
        private string _originalDir;

        /// <summary>
        /// Output delta dir.
        /// </summary>
        private string _outputDeltaDir;

        /// <summary>
        /// General lexicon path.
        /// </summary>
        private string _generalLexiconPath;

        /// <summary>
        /// Key: domain, Value: path.
        /// </summary>
        private IDictionary<string, string> _originalDataPaths = new Dictionary<string, string>();
        #endregion

        #region Constructor Members

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDeltaConfig" /> class.
        /// </summary>
        /// <param name="originalDir">Original dir.</param>
        /// <param name="outputDeltaDir">Output delta dir.</param>
        /// <param name="language">Language.</param>
        public CreateDeltaConfig(string originalDir, string outputDeltaDir, Language language)
        {
            _originalDir = originalDir;
            _outputDeltaDir = outputDeltaDir;
            Helper.CheckFolderNotEmpty(_originalDir);
            string general = "MSTTSLoc" + language.ToString();
            string iniPath = Helper.GetFullPath(_originalDir, general + ".ini");
            string generalDatPath = Helper.GetFullPath(_originalDir, general + ".dat");
            Helper.CheckFileExists(generalDatPath);
            _generalLexiconPath = Helper.GetFullPath(_originalDir, "lexicon.general.xml");
            Helper.CheckFileExists(_generalLexiconPath);
            _originalDataPaths.Add(DomainItem.GeneralDomain, generalDatPath);
            if (File.Exists(iniPath))
            {
                _setting = new FrontendSetting(iniPath);
                foreach (KeyValuePair<string, string> pair in _setting.DomainMembers)
                {
                    string domainPath = Helper.GetFullPath(_originalDir, pair.Value);
                    Helper.CheckFileExists(domainPath);
                    _originalDataPaths.Add(pair.Key.ToLowerInvariant(), domainPath);
                }
            }
            else
            {
                var backup = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Warning: There is no {0} file in original directory. If you continue, all domain DATs in the directory will be ignored.", general + ".ini");
                Console.Write("Warning: Do you want to continue? [Y/N]");
                var key = Console.ReadKey().KeyChar;
                if (key != 'Y' && key != 'y')
                {
                    Environment.Exit(ExitCode.NoError);
                }

                Console.WriteLine();
            }
        }

        #endregion

        #region Property Members

        /// <summary>
        /// Gets Frontend Setting.
        /// </summary>
        public FrontendSetting Setting
        {
            get { return _setting; }
        }

        /// <summary>
        /// Gets General lexicon path.
        /// </summary>
        public string GeneralLexiconPath
        {
            get { return _generalLexiconPath; }
        }

        /// <summary>
        /// Gets Output delta dir.
        /// </summary>
        public string OutputDeltaDir
        {
            get { return _outputDeltaDir; }
        }

        /// <summary>
        /// Gets Original Data paths.
        /// Key: domain, value: dat path.
        /// </summary>
        public IDictionary<string, string> OriginalDataPaths
        {
            get { return _originalDataPaths; }
        }
        #endregion
    }
}
