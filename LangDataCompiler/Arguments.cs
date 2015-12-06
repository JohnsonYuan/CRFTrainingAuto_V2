//----------------------------------------------------------------------------
// <copyright file="Arguments.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Command line arguments for LangDataCompiler
// </summary>
//----------------------------------------------------------------------------

namespace LangDataCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Command line arguments.
    /// </summary>
    [Comment("Language Data Compiler tool compiles frontend.dat.")]

    public class Arguments : ILogSink
    {
        /// <summary>
        /// Arguments.
        /// </summary>
        #region Fields used by command line parser
        [Argument("mode", Description = "Specifies the execute mode: Normal, CreateDelta. The default mode is Normal.",
           Optional = false, UsagePlaceholder = "executeMode", RequiredModes = "Normal, CreateDelta")]
        private string _mode = string.Empty;

        [Argument("config", Description = "Specifies the location of the configuration file.", 
           Optional = false, UsagePlaceholder = "configFile", RequiredModes = "Normal, CreateDelta")]
        private string _configFilePath = string.Empty;

        [Argument("binRootPath", Description = "Specifies the root path of binary data dir.",
           Optional = true, UsagePlaceholder = "binRootPath", OptionalModes = "Normal, CreateDelta")]
        private string _binRootDirPath = string.Empty;

        [Argument("rawDataRootPath", Description = "Specifies the root path of raw data dir.",
           Optional = true, UsagePlaceholder = "rawDataRootPath", OptionalModes = "Normal, CreateDelta")]
        private string _rawDataDirPath = string.Empty;

        [Argument("customerDataRootPath", Description = "Specifies the root path of customer data dir.",
           Optional = true, UsagePlaceholder = "customerDataRootPath", OptionalModes = "Normal, CreateDelta")]
        private string _customerDataRootDirPath = string.Empty;

        [Argument("toolDir", Description = "Specifies the path of directory for denpendent tools.",
           Optional = true, UsagePlaceholder = "toolDir", OptionalModes = "Normal, CreateDelta")]
        private string _toolDirPath = string.Empty;

        [Argument("outputDir", Description = "Specifies the path of directory of several final engine data.",
            Optional = true, UsagePlaceholder = "outputDir", OptionalModes = "Normal, CreateDelta")]
        private string _outputDirPath = string.Empty;

        [Argument("report", Description = "Specifies the path of report file.",
           Optional = true, UsagePlaceholder = "reportFile", OptionalModes = "Normal, CreateDelta")]
        private string _logFilePath = string.Empty;

        [Argument("originalDataDir", Description = "Specifies the path of original data dir.",
           Optional = true, UsagePlaceholder = "orignialDataDir", RequiredModes = "CreateDelta")]
        private string _originalDataDir = string.Empty;

        [Argument("outputDeltaDir", Description = "Specifies the path of directory of final delta data.",
           Optional = true, UsagePlaceholder = "outputDeltaDir", RequiredModes = "CreateDelta")]
        private string _outputDeltaDir = string.Empty;
        #endregion

        #region Properties

        /// <summary>
        /// Gets mode of the application.
        /// </summary>
        public string Mode
        {
            get { return _mode; }
        }

        /// <summary>
        /// Gets location of the configuration file.
        /// </summary>
        public string ConfigFilePath
        {
            get { return _configFilePath; }
        }

        /// <summary>
        /// Gets or sets path of Bin Data Directory.
        /// </summary>
        public string BinRootDirPath
        {
            get { return _binRootDirPath; }
            set { _binRootDirPath = value; }
        }

        /// <summary>
        /// Gets or sets path of Raw Data Directory.
        /// </summary>
        public string RawDataDirPath
        {
            get { return _rawDataDirPath; }
            set { _rawDataDirPath = value; }
        }

        /// <summary>
        /// Gets or sets path of Customer Data Directory.
        /// </summary>
        public string CustomerDataRootDirPath
        {
            get { return _customerDataRootDirPath; }
            set { _customerDataRootDirPath = value; }
        }

        /// <summary>
        /// Gets or sets folder of final engine data.
        /// </summary>
        public string OutputDirPath
        {
            get { return _outputDirPath; }
            set { _outputDirPath = value; }
        }

        /// <summary>
        /// Gets or sets path of log file.
        /// </summary>
        public string LogFilePath
        {
            get { return _logFilePath; }
            set { _logFilePath = value; }
        }

        /// <summary>
        /// Gets or sets path of directory for tools.
        /// </summary>
        public string ToolDirPath
        {
            get { return _toolDirPath; }
            set { _toolDirPath = value; }
        }

        /// <summary>
        /// Gets or sets path of directory of original data.
        /// </summary>
        public string OriginalDataDir
        {
            get { return _originalDataDir; }
            set { _originalDataDir = value; }
        }

        /// <summary>
        /// Gets or sets path of directory of output delta.
        /// </summary>
        public string OutputDeltaDir
        {
            get { return _outputDeltaDir; }
            set { _outputDeltaDir = value; }
        }
        #endregion
    }
}