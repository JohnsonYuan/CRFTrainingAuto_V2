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
                Util.ConsoleOutTextColor("Arguments you provided has some error");

                foreach (var error in errors)
                {
                    Util.ConsoleOutTextColor(error, ConsoleColor.Red);
                    Console.WriteLine();
                }

                return ExitCode.InvalidArgument;
            }

            if (!string.IsNullOrEmpty(arguments.ConfigPath))
            {
                LocalConfig.Create(arguments.ConfigPath);
            }

            CrfHelper crfHelper = new CrfHelper();

            // TODO 

            // crfHelper.UpdatePolyRuleFile(LocalConfig.Instance.PolyRuleFilePath, "背");

            string generatedFilePath = @"D:\WorkFolder\Test\MSTTSLocZhCN.dat";
            string generatedBakFilePath = @"D:\WorkFolder\Test\MSTTSLocZhCN.dat.bak";

            string[] tempFiles = {
                @"D:\Enlistments\IPESpeechCore_Dev\private\dev\speech\tts\shenzhou\data\zh-CN\binary\polyphony.address.bin",
                @"D:\Enlistments\IPESpeechCore_Dev\private\dev\speech\tts\shenzhou\data\zh-CN\binary\polyphony.bin",
                @"D:\Enlistments\IPESpeechCore_Dev\private\dev\speech\tts\shenzhou\data\zh-CN\binary\polyphony.message.bin",
                @"D:\Enlistments\IPESpeechCore_Dev\private\dev\speech\tts\shenzhou\data\zh-CN\binary\polyphony.name.bin",
            };

            foreach (var item in tempFiles)
            {
                Microsoft.Tts.Offline.Compiler.LanguageData.LanguageDataHelper.ReplaceBinaryFile(
         generatedFilePath,
         item,
         Microsoft.Tts.Offline.Compiler.LanguageData.ModuleDataName.PolyphoneRule);
            }

            return 1;



            string tempBinFile;
            // compile polyrule.txt if update polyrule.txt file
            //if (CompilerHelper.UpdatePolyRuleFile(LocalConfig.Instance.PolyRuleFilePath, "背"))
            //{
            CompilerHelper.CompileGeneralRule(LocalConfig.Instance.PolyRuleFilePath, out tempBinFile);

            Microsoft.Tts.Offline.Compiler.LanguageData.LanguageDataHelper.ReplaceBinaryFile(
                generatedFilePath,
                tempBinFile,
                Microsoft.Tts.Offline.Compiler.LanguageData.ModuleDataName.PolyphoneRule);

            // delete the temp file
            File.Copy(tempBinFile, @"D:\WorkFolder\Test\temp.bin");
            File.Delete(tempBinFile);
            //}

            return 1;

            switch (arguments.Mode)
            {
                case Arguments.ExecuteMode.FilterChar:
                    // InputPath, OutputPath are all folders
                    crfHelper.PrepareTrainTestSet(arguments.InputPath, arguments.OutputPath, arguments.WbPath);
                    break;
                case Arguments.ExecuteMode.NCRF:
                    // InputPath is excel file path, OutputPath is folder path
                    crfHelper.GenNCrossData(arguments.InputPath, arguments.OutputPath);
                    break;
                case Arguments.ExecuteMode.GenVerify:
                    // InputPath is excel file path, OutputPath is folder path
                    crfHelper.GenVerifyResult(arguments.InputPath, arguments.OutputPath);
                    break;
                case Arguments.ExecuteMode.GenXlsTestReport:
                    // InputPath is txt file path
                    ExcelHelper.GenExcelTestReport(arguments.InputPath);
                    break;
                case Arguments.ExecuteMode.Compile:
                    // InputPath is folder
                    crfHelper.CompileAndTestInFolder(arguments.InputPath);
                    break;
                case Arguments.ExecuteMode.GenXls:
                    // InputPath is txt file path, OutputPath is excel file path
                    // if IsNeedWb == 0, the word break result is in input file, otherwise, use word break genereate word break result
                    ExcelHelper.GenExcelFromTxtFile(arguments.InputPath, arguments.OutputPath, arguments.IsNeedWb == 0);
                    break;
                case Arguments.ExecuteMode.GenTrain:
                case Arguments.ExecuteMode.GenTest:
                    // InputPath is excel file path, OutputPath is xml file path
                    GenerateAction action = arguments.Mode == Arguments.ExecuteMode.GenTrain ? GenerateAction.TrainingScript : GenerateAction.TestCase;
                    string folder = Path.GetDirectoryName(arguments.OutputPath);
                    string filename = Path.GetFileName(arguments.OutputPath);
                    ScriptGenerator.GenScript(arguments.InputPath, action, folder, filename, arguments.StartIndexOrFilePath);
                    break;
                case Arguments.ExecuteMode.BugFixing:
                    // InputPath is txt file path
                    crfHelper.AppendTrainingScriptAndReRunTest(arguments.InputPath, arguments.OutputPath);
                    break;
                case Arguments.ExecuteMode.WB:
                    // InputPath is wildcard file path, OutputPath is folder path
                    crfHelper.DoWordBreak(arguments.InputPath, arguments.OutputPath);
                    break;
                case Arguments.ExecuteMode.SS:
                    // InputPath, OutputPath are all folders
                    SentenceBreaker.DoSentenceSeparate(arguments.InputPath, arguments.OutputPath);
                    break;
                case Arguments.ExecuteMode.Merge:
                    // InputPath, OutputPath are all folders
                    crfHelper.MergeAndRandom(arguments.InputPath, arguments.OutputPath);
                    break;
                case Arguments.ExecuteMode.Split:
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
        /// <param name="splitUnit">split unit.</param>
        /// <param name="splitSize">split size.</param>
        /// <param name="inputFile">input file path.</param>
        /// <param name="outputDir">output folder.</param>
        private static void SplitFile(string splitUnit, int splitSize, string inputFile, string outputDir)
        {
            bool success = Util.SplitFile(splitUnit, splitSize, inputFile, outputDir);
            if (success)
            {
                Util.ConsoleOutTextColor(Helper.NeutralFormat("Split file {0} to {1}.", inputFile, outputDir));
            }
            else
            {
                Util.ConsoleOutTextColor("Split !");
            }
        }
    }
}
