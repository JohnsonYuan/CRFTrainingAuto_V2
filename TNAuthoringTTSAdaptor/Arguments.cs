//----------------------------------------------------------------------------
// <copyright file="Arguments.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Command line arguments for TNAuthoringTTSAdaptor
// </summary>
//----------------------------------------------------------------------------

namespace TNAuthoringTTSAdaptor
{
    using System;
    using System.IO;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Command line arguments
    /// </summary>
    [Comment("TTS adaptor to support to run TTS test case in TN Authoring tool")]
    public class Arguments : ILogSink
    {
        #region Fields used by command line parser

        [Argument("TNML", Description = "Specifies the location of TNML file.",
            Optional = false, UsagePlaceholder ="TnmlFilePath")]
        private string _tnmlFilePath = string.Empty;

        [Argument("lang", Description = "Specifies the language name of tns.",
            Optional = false, UsagePlaceholder = "Language")]
        private string _language = string.Empty;

        [Argument("log", Description = "Specifies the path of log file",
           Optional = false, UsagePlaceholder = "LogFile")]
        private string _logFilePath = string.Empty;
            
        #endregion

        #region Properties
        /// <summary>
        /// Location of the TNML file.
        /// </summary>
        public string TnmlFilePath
        {
            get { return _tnmlFilePath; }
            set { _tnmlFilePath = value; }
        }

        /// <summary>
        /// Language name.
        /// </summary>
        public string Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Location of log file
        /// </summary>
        public string LogFilePath
        {
            get { return _logFilePath; }
            set { _logFilePath = value; }
        }
        #endregion
    }

    /// <summary>
    /// Local arguments, which is converted from args in Main method.
    /// </summary>
    internal class LocalArguments
    {
        #region Fileds
        /// <summary>
        /// Working directory
        /// </summary>
        public static readonly string WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

        private Language _lang;

        private string _tnmlFilePath;

        private string _logFilePath;

        private string _langDataFilePath;

        private readonly string _tnmlCompiler;

        private readonly string _binaryTnRule;
        #endregion

        #region properties
        /// <summary>
        /// Location of TNML file
        /// </summary>
        public string TnmlFilePath
        {
            get { return _tnmlFilePath; }
            set { _tnmlFilePath = value; }
        }

        /// <summary>
        /// Location of log file
        /// </summary>
        public string LogFilePath
        {
            get { return _logFilePath; }
            set { _logFilePath = value; }
        }

        /// <summary>
        /// Location of language data file
        /// </summary>
        public string LangDataFilePath
        {
            get { return _langDataFilePath; }
            set { _langDataFilePath = value; }
        }

        /// <summary>
        /// Language
        /// </summary>
        public Language Lang
        {
            get { return _lang; }
            set { _lang = value; }
        }

        /// <summary>
        /// TNML compiler
        /// </summary>
        public string TnmlCompiler
        {
            get { return _tnmlCompiler; }
        }

        /// <summary>
        /// Binary TN Rule
        /// </summary>
        public string BinaryTNRule
        {
            get { return _binaryTnRule; }
        }
        #endregion

        private LocalArguments() 
        {
            _tnmlCompiler = Path.Combine(WorkingDirectory, "CompTNML.exe");
            _binaryTnRule = Path.Combine(WorkingDirectory,  "tnRule.bin");
        }

        /// <summary>
        /// Convert Arguments to LocalArguments
        /// </summary>
        /// <param name="args">arguments</param>
        /// <returns>local arguments</returns>
        public static LocalArguments ConvertToLocalArguments(Arguments args)
        {
            #region validate whether to absolute path
            if (!Path.IsPathRooted(args.TnmlFilePath))
            {
                throw new Exception(string.Format("Please use full path: {0}.", args.TnmlFilePath));
            }
            if (!Path.IsPathRooted(args.LogFilePath))
            {
                throw new Exception(string.Format("Please use full path: {0}.", args.LogFilePath));
            }
            #endregion

            LocalArguments localArgs = new LocalArguments();

            localArgs.Lang = Localor.StringToLanguage(args.Language);
            localArgs.TnmlFilePath = args.TnmlFilePath;
            localArgs.LogFilePath = args.LogFilePath;
            
            string languageDataDir = Path.Combine(new DirectoryInfo(WorkingDirectory).Parent.FullName, "LangData");
            if (!Directory.Exists(languageDataDir))
            {
                throw new DirectoryNotFoundException(string.Format("The folder \"{0}\" doesn't exist.", languageDataDir));
            }

            //get language data file path
            localArgs.LangDataFilePath = Path.Combine(languageDataDir, string.Format("MSTTSLoc{0}.dat", localArgs.Lang));

            return localArgs;
        }
    }
}
