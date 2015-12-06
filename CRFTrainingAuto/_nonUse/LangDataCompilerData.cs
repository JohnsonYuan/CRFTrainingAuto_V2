namespace CRFTrainingAuto
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    public class LangDataCompiler
    {
        #region Fields

        private readonly string commandName = "langdatacompiler.exe";
        private readonly string zippedFinalLexiconName = "FinalLexicon.zip";
        private readonly string dataFolder = @"private\dev\speech\tts\shenzhou\data";
        public static readonly string OutputFinalLexiconPath = @"binary\SetLexicon\FinalLexicon.xml";

        private string configFile;
        private CompileType compileTypes = CompileType.Full;
        private Process compileProcess;
        private StringBuilder compileMessage = new StringBuilder();
        private bool compileSucceed;

        private string _branchRootPath;
        private string _language;
        private string _offlineToolPath;

        public LangDataCompiler(string branchRootPath, string language, string offlineToolPath)
        {
            _branchRootPath = branchRootPath;
            _language = language;
            _offlineToolPath = offlineToolPath;
        }

        #endregion Fields

        #region Properties

        public string ConfigFile
        {
            get { return configFile; }
            set { configFile = value; }
        }

        public string CommandPath
        {
            get
            {
                return Path.Combine(_branchRootPath, _offlineToolPath, commandName);
            }
        }

        public string RawDataRootPath
        {
            get
            {
                return Path.Combine(_branchRootPath, dataFolder, _language);
            }
        }

        public string BinRootPath
        {
            get
            {
                return Path.Combine(_branchRootPath, dataFolder, _language);
            }
        }

        public CompileType CompileTypes
        {
            get
            {
                return compileTypes;
            }

            set
            {
                compileTypes = value;
            }
        }

        public Process CompileProcess
        {
            get { return compileProcess; }
        }

        public bool CompileSucceed
        {
            get
            {
                return compileSucceed;
            }

            set
            {
                compileSucceed = value;
            }
        }

        #endregion Properties

        #region Public Methods

        public string GetArguments(string outputDir, CompileType compileType = CompileType.Full)
        {
            ConfigFile = Path.Combine(RawDataRootPath, @"Release", compileType == CompileType.Full ? "platform" : "winmo", "LangDataCompilerConfig.xml");

            string reportPath = Path.Combine(outputDir, "report.txt");
            
            return string.Format("-config {0} -rawdatarootpath {1} -binrootpath {2} -outputDir {3} -report {4}"
                , ConfigFile, RawDataRootPath, BinRootPath, outputDir, reportPath);
        }

        public string GetCommandString(CompileType compileType, string outputDir, string reportPath = null)
        {
            ConfigFile = Path.Combine(RawDataRootPath, @"Release", compileType == CompileType.Full ? "platform" : "winmo", "LangDataCompilerConfig.xml");
            
            if (String.IsNullOrEmpty(reportPath))
            {
                reportPath = Path.Combine(RawDataRootPath, "binary", "report.txt");
            }

            return string.Format("{0} \n-config {1} \n-rawdatarootpath {2} \n-binrootpath {3} \n-outputDir {4} \n-report {5}"
                    , CommandPath, ConfigFile, RawDataRootPath, BinRootPath, outputDir, reportPath);
        }

        /// <summary>
        /// compile data.
        /// </summary>
        /// <returns>whether compiling succeed.</returns>
        public bool Compile(string language, string outputDir)
        {
            // validate
            if (CompileTypes == CompileType.None)
            {
                Console.WriteLine("no compile type specify");
                return false;
            }

            // compile
            if ((CompileTypes & CompileType.Full) == CompileType.Full)
            {
                Console.WriteLine("Compile full data - Compiling");

                CompileData(outputDir, language, CompileType.Full);

                if (CompileSucceed == true)
                {
                    // dat and ini from SD location to offline
                    Console.WriteLine("Compile full data - copy dat and ini files to Offline\\LocHandler, Offline\\UnitTest\\Lochandler and bin\\LocHandler");
                }
                else
                {
                    Console.WriteLine("Build failed! Stop proceeding.");
                    return false;
                }
            }
            return true;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Compile language data
        /// </summary>
        /// <param name="language">it should be zhCN not zh-CN</param>
        private void CompileData(string outputDir, string language, CompileType compileType = CompileType.Full)
        {
            Console.WriteLine("Start compiling language data ...");
            
            CompileSucceed = true;
            language = language.Replace("-", "");

            // clear files
            string[] datFiles = Directory.GetFiles(outputDir
                , string.Format("*{0}*.dat", language)
                , SearchOption.TopDirectoryOnly);
            foreach (string file in datFiles)
            {
                File.Delete(file);
            }

            compileProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CommandPath;
            startInfo.Arguments = GetArguments(outputDir, compileType);
            startInfo.WorkingDirectory = null;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            //startInfo.RedirectStandardOutput = true;
            //startInfo.RedirectStandardError = true;
            compileProcess.StartInfo = startInfo;
            //compileProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorOutputHandler);
            //compileProcess.OutputDataReceived += new DataReceivedEventHandler(StandardOutputHandler);

            compileProcess.Start();
            //compileProcess.BeginOutputReadLine();
            //compileProcess.BeginErrorReadLine();
            compileProcess.WaitForExit();
            compileProcess.Close();

            // check whether general dat files are generated
            if (!File.Exists(Path.Combine(outputDir, string.Format("MSTTSLoc{0}.dat", language))))
            {
                CompileSucceed = false;
            }
        }
        #endregion Private Methods
    }

    [Flags]
    public enum CompileType
    {
        None = 0x0,
        Full = 0x1,
        Pruned = 0x2,
        Both = Full | Pruned
    }
}
