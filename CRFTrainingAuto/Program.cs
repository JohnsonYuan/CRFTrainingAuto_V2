//-----------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <summary>
//     This application is used to automatic crf training.
// </summary>
//-----------------------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main of CRFTrainingAuto.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>0:Succeeded; -1:Catch exception.</returns>
        public static int Main(string[] args)
        {
            return ConsoleApp<Arguments>.Run(args, Process);
        }

        /// <summary>
        /// Main process function.
        /// </summary>
        /// <param name="arguments">Processor argument.</param>
        /// <returns>If it finished successfully, then return successful code.</returns>
        private static int Process(Arguments arguments)
        {
            IEnumerable<string> errors = arguments.Validate();

            if (errors.Count() != 0)
            {
                Helper.PrintSuccessMessage("Arguments you provided has some error");

                foreach (var error in errors)
                {
                    Helper.PrintColorMessageToOutput(ConsoleColor.Red, error);
                    Console.WriteLine();
                }

                return ExitCode.InvalidArgument;
            }

            LocalConfig configInstance;

            if (!string.IsNullOrEmpty(arguments.ConfigPath))
            {
                configInstance = new LocalConfig(arguments.ConfigPath);
            }

            CrfHelper crfHelper = new CrfHelper();

            switch (arguments.Mode)
            {
                case ExecuteMode.FilterChar:
                    // InputPath, OutputPath are all folders
                    crfHelper.PrepareTrainTestSet(arguments.InputPath, arguments.OutputPath, arguments.WbPath);
                    break;
                case ExecuteMode.NCRF:
                    // InputPath is excel file path, OutputPath is folder path
                    crfHelper.GenNCrossData(arguments.InputPath, arguments.OutputPath);
                    break;
                case ExecuteMode.GenVerify:
                    // InputPath is excel file path, OutputPath is folder path
                    crfHelper.GenVerifyResult(arguments.InputPath, arguments.OutputPath);
                    break;
                case ExecuteMode.GenXlsTestReport:
                    // InputPath is txt file path
                    ExcelGenerator.GenExcelTestReport(arguments.InputPath);
                    break;
                case ExecuteMode.Compile:
                    // InputPath is folder
                    crfHelper.CompileAndTestInFolder(arguments.InputPath);
                    break;
                case ExecuteMode.GenXls:
                    // InputPath is txt file path, OutputPath is excel file path
                    // if IsNeedWb == 0, the word break result is in input file, otherwise, use word break genereate word break result
                    ExcelGenerator.GenExcelFromTxtFile(arguments.InputPath, arguments.OutputPath, arguments.IsNeedWb == 0);
                    break;
                case ExecuteMode.GenTrain:
                case ExecuteMode.GenTest:
                    // InputPath is excel file path, OutputPath is xml file path
                    GenerateAction action = arguments.Mode == ExecuteMode.GenTrain ? GenerateAction.TrainingScript : GenerateAction.TestCase;
                    string folder = Path.GetDirectoryName(arguments.OutputPath);
                    string filename = Path.GetFileName(arguments.OutputPath);
                    ScriptGenerator.GenScript(arguments.InputPath, action, folder, filename, arguments.StartIndexOrFilePath);
                    break;
                case ExecuteMode.BugFixing:
                    // InputPath is txt file path
                    crfHelper.AppendTrainingScriptAndReRunTest(arguments.InputPath, arguments.OutputPath);
                    break;
                case ExecuteMode.WB:
                    // InputPath is wildcard file path, OutputPath is folder path
                    crfHelper.DoWordBreak(arguments.InputPath, arguments.OutputPath);
                    break;
                case ExecuteMode.SS:
                    // InputPath, OutputPath are all folders
                    SentenceBreaker.DoSentenceSeparate(arguments.InputPath, arguments.OutputPath);
                    break;
                case ExecuteMode.Merge:
                    // InputPath, OutputPath are all folders
                    crfHelper.MergeAndRandom(arguments.InputPath, arguments.OutputPath);
                    break;
                case ExecuteMode.Split:
                    SplitFile(arguments.SplitUnit, arguments.SplitSize, arguments.InputPath, arguments.OutputPath);
                    break;
                default:
                    break;
            }

            return ExitCode.NoError;
        }

        /// <summary>
        /// Split file.
        /// </summary>
        /// <param name="splitUnit">Split unit.</param>
        /// <param name="splitSize">Split size.</param>
        /// <param name="inputFile">Input file path.</param>
        /// <param name="outputDir">Output folder.</param>
        private static void SplitFile(string splitUnit, int splitSize, string inputFile, string outputDir)
        {
            bool success = Util.SplitFile(splitUnit, splitSize, inputFile, outputDir);
            if (success)
            {
                Helper.PrintSuccessMessage(Helper.NeutralFormat("Split file {0} to {1}.", inputFile, outputDir));
            }
            else
            {
                Helper.PrintSuccessMessage("Split !");
            }
        }
    }
}
