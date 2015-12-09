namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Contains global variables for project.
    /// </summary>
    public class GlobalVar
    {
        private static WordBreaker _wordBreaker;
        internal static LocalConfig Config;

        /// <summary>
        /// GlobalVar class
        /// </summary>
        /// <param name="config">local config</param>
        public GlobalVar(LocalConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Release resource
        /// </summary>
        public void ReleaseBreaker()
        {
            if(_wordBreaker != null)
            {
                _wordBreaker.Dispose();
            }
        }

        #region properties

        /// <summary>
        /// Get the word breaker
        /// </summary>
        public static WordBreaker WordBreaker
        {
            get
            {
                if (_wordBreaker == null)
                {
                    _wordBreaker = WordBreaker.GenWordBreaker(GlobalVar.Config);
                }
                return _wordBreaker;
            }
        }

        #endregion

        #region fields

        // Excel first column for case, second column for corrct pron
        internal const int ExcelCaseColIndex = 1;
        internal const string ExcelCaseColTitle = "case";
        internal const int ExcelCorrectPronColIndex = 2;
        internal const string ExcelCorrectPronColTitle = "correct pron";
        internal const int ExcelCommentColIndex = 3;
        internal const string ExcelCommentColTitle = "comment";
        internal const int ExcelWbColIndex = 4;
        internal const string ExcelWbColTitle = "wb result";

        // Use 1000 for N cross folder test
        // Temp folder store filtered corpus data
        internal const string TempFolderName = "temp";
        internal const string ExcelFileExtension = ".xls";
        internal const string TxtFileExtension = ".txt";

        internal const string CorpusTxtFileSearchPattern = "*.txt";
        internal const string XmlFileSearchExtension = "*.xml";
        internal const string CorpusTxtFileNamePattern = "corpus.{0}.txt";
        internal const string CorpusTxtAllFileName = "corpus.all.txt";
        internal const string CorpusExcelFileNamePattern = "corpus.{0}.xls";

        internal const string TrainingFolderName = "trainingScript";
        internal const string NCrossFolderName = "NCross";
        internal const string VerifyResultFolderName = "VerifyResult";
        internal const string FinalResultFolderName = "FinalResult";

        internal const string TrainingExcelFileName = "training.xls";
        internal const string TestingExcelFileName = "testing.xls";
        internal const string VerifyResultExcelFileName = "verifyResult.xls";

        internal const string TrainingConfigFileName = "training.config";
        internal const string TrainingConfigNamespace = "http://schemas.microsoft.com/tts/toolsuite";
        internal const string FeatureConfigFileName = "features.config";

        internal const int BugFixingXmlStartIndex = 1000000000;
        internal const string BugFixingFileName = "bugfixing.xml";
        internal const string ScriptFileName = "script.xml";
        internal const string TrainingFileName = "training.xml";
        internal const string TestFileName = "testing.xml";
        internal const string TestXmlNamespace = "http://schemas.microsoft.com/tts";
        internal const string TestCaseFileName = "Pron_Polyphony.xml";
        internal const string TestlogFileName = "testlog.txt";
        internal const string TestlogBeforeFileName = "testlog.before.txt";
        internal const string TestReportFileName = "NCrossTestReport.txt";

        #endregion
    }
}
