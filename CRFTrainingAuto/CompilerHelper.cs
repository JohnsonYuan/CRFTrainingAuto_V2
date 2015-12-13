//-----------------------------------------------------------------------------------------
// <copyright file="CompilerHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <summary>
//     Compiler helper, compile crf model and general rule.
// </summary>
//-----------------------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Compiler helper, compile crf model and general rule.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public static class CompilerHelper
    {
        #region Fields

        // polyrule.txt all >= 0 line
        private const string PolyRuleDefaultRuleStartTag = "All >= 0 :";

        // use this regex to find rule is used for which word
        private static Regex _polyRuleKeyLineRegex = new Regex("CurW = \"(.+)\";", RegexOptions.Compiled);

        #endregion

        #region Methods

        /// <summary>
        /// Compile dat file.
        /// </summary>
        /// <param name="configFilePath">Compile config file path.</param>
        /// <param name="message">Compile result message.</param>
        /// <returns>Success or not.</returns>
        public static bool CompileAll(string configFilePath, out string message)
        {
            string sdMsg = string.Empty;

            try
            {
                int sdExitCode = CommandLine.RunCommandWithOutputAndError(
                                                "langdatacompiler.exe",
                                                Helper.NeutralFormat("add {0}"),
                                                Directory.GetCurrentDirectory(),
                                                ref sdMsg);

                if (sdExitCode == 0)
                {
                    message = Helper.NeutralFormat("Compile succeed.");
                }
                else
                {
                    message = Helper.NeutralFormat("Failed to compile! {0}", sdMsg);
                    return false;
                }
            }
            catch (Exception e)
            {
                message = Helper.NeutralFormat("{0}. Failed to compile", e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// CRF compiler.
        /// </summary>
        /// <param name="crfModelDir">Crf model folder.</param>
        /// <param name="lang">Language.</param>
        /// <param name="crfBinFile">Crf bin file path.</param>
        /// <returns>Success or not.</returns>
        public static bool CompileCRF(string crfModelDir, Language lang, out string crfBinFile)
        {
            MemoryStream outputStream = new MemoryStream();
            FileStream fs = null;
            try
            {
                Collection<string> addedFileNames = new Collection<string>();
                var errorSet = Microsoft.Tts.Offline.Compiler.CrfModelCompiler.Compile(crfModelDir, outputStream, addedFileNames, lang);

                if (errorSet != null && errorSet.Count > 0)
                {
                    foreach (var error in errorSet.Errors)
                    {
                        Helper.PrintSuccessMessage(error.ToString());
                    }

                    crfBinFile = null;
                    return false;
                }

                crfBinFile = Helper.GetTempFileName();

                fs = new FileStream(crfBinFile, FileMode.OpenOrCreate);

                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    fs = null;
                    bw.Write(outputStream.ToArray());
                }

                return File.Exists(crfBinFile);
            }
            catch
            {
                crfBinFile = null;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Dispose();
                }

                if (outputStream != null)
                {
                    outputStream.Dispose();
                }
            }
        }

        /// <summary>
        /// General Rule Compiler.
        /// </summary>
        /// <param name="txtPath">Path of txt formatted general rule.</param>
        /// <param name="tempBinFile">Temp binary file path.</param>
        /// <returns>Success or not.</returns>
        public static bool CompileGeneralRule(string txtPath, out string tempBinFile)
        {
            MemoryStream outputStream = new MemoryStream();
            try
            {
                tempBinFile = Helper.GetTempFileName();
                string compilingArguments = Helper.NeutralFormat("\"{0}\" \"{1}\"", txtPath, tempBinFile);

                string message = string.Empty;

                int exitCode = CommandLine.RunCommandWithOutputAndError(
                    Util.RuleCompilerPath, compilingArguments, null, ref message);

                if (exitCode == 0)
                {
                    return File.Exists(tempBinFile);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                tempBinFile = null;
                return false;
            }
        }

        /// <summary>
        /// Update polyrule.txt for specific char
        /// Delete "All >= 0" line if polyrule.txt file contains
        /// polyrule.txt is like below, we should remove All >= 0 : "b eh_h i_l"; to make CRF model working
        /// [domain=address]
        /// CurW = "背";
        /// PrevW = "肩" : "b eh_h i_h";
        /// PrevW = "越" : "b eh_h i_h";
        /// All >= 0 : "b eh_h i_l";.
        /// </summary>
        /// <param name="filePath">Poly rule file path.</param>
        /// <param name="charName">Char name.</param>
        /// <returns>True if updated, false not changed.</returns>
        public static bool UpdatePolyRuleFile(string filePath, string charName)
        {
            Helper.ThrowIfFileNotExist(filePath);
            Helper.ThrowIfNull(charName);

            int lineNumber = 0;

            bool foundTargetChar = false;
            bool isNeedModify = false;
            bool currentCharHasDomainAttr = false;
            string currentChar;

            using (StreamReader reader = new StreamReader(filePath))
            {
                while (reader.Peek() > -1)
                {
                    ++lineNumber;

                    string lineContent = reader.ReadLine();

                    if (string.IsNullOrEmpty(lineContent))
                    {
                        continue;
                    }

                    if (lineContent.StartsWith("[domain", StringComparison.OrdinalIgnoreCase))
                    {
                        currentCharHasDomainAttr = true;
                    }
                    else if (_polyRuleKeyLineRegex.IsMatch(lineContent))
                    {
                        currentChar = _polyRuleKeyLineRegex.Match(lineContent).Groups[1].Value;

                        // if current rule is for training char and this is an general rule
                        if (string.Equals(currentChar, charName, StringComparison.Ordinal) &&
                            !currentCharHasDomainAttr)
                        {
                            foundTargetChar = true;
                        }

                        // assign to false, iterate next char
                        currentCharHasDomainAttr = false;
                    }
                    else if (foundTargetChar)
                    {
                        // update poly rule file the target char's general rule contains All >= 0
                        if (lineContent.StartsWith(PolyRuleDefaultRuleStartTag, StringComparison.OrdinalIgnoreCase))
                        {
                            isNeedModify = true;
                            break;
                        }
                    }
                }
            }

            if (isNeedModify)
            {
                string message;

                SdCommand.SdCheckoutFile(filePath, out message);
                Helper.PrintSuccessMessage(message);

                // remove All >= 0 line in poly rule file
                Util.EditLineInFile(filePath, lineNumber, null);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Load CRF model name mapping(model name and localized name).
        /// </summary>
        /// <example>
        /// The mapping txt file is like below:
        /// 差 -> cha.crf Being_used
        /// 长 -> chang.crf Being_used
        /// 当 -> dang.crf Being_used
        /// 行 -> hang.crf Being_used
        /// 系 -> xi.crf Unused.
        /// </example>
        /// <param name="filePath">Crf mapping File Path.</param>
        /// <param name="crfFileName">Crf file name.</param>
        /// <param name="usingInfo">Check the char whether to be used, in mapping file "Being_used" or "Unused".</param>
        /// <returns>Crf model files array, like bei.crf, wei.crf.</returns>
        public static string[] UpdateCRFModelMappingFile(string filePath, string crfFileName, string usingInfo)
        {
            Helper.ThrowIfFileNotExist(filePath);

            string message;
            SdCommand.SdCheckoutFile(filePath, out message);
            Helper.PrintSuccessMessage(message);

            if (!string.Equals(usingInfo, "Being_used") && !string.Equals(usingInfo, "Unused"))
            {
                throw new ArgumentException("usingInfo can only be \"Being_used\" or \"Unused\"!");
            }

            List<string> crfFileNames = new List<string>();

            // start flag of crf model mapping data
            const string MappingFlag = "Map between polyphony model:";

            // line number start index is 1, next line will be read is 2
            int lineNumber = 1;
            bool needModify = true;
            bool charExist = false;

            using (StreamReader reader = new StreamReader(filePath))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    // if the first line matches, then continue to read
                    if (lineNumber == 1 &&
                        !string.Equals(MappingFlag, line))
                    {
                        throw new FormatException(Helper.NeutralFormat("{0} mapping file has the wrong format!", filePath));
                    }
                    else if (lineNumber > 1)
                    {
                        string[] mapping = line.Trim().Split('\t');

                        if (mapping.Length != 4)
                        {
                            throw new FormatException(Helper.NeutralFormat("{0} mapping file has the wrong format!", filePath));
                        }

                        string currentChar = mapping[0];
                        string currentCRFFile = mapping[2];
                        string currentUsingInfo = mapping[3];

                        // if current line's char is same with charName para, check whether need to modify this line
                        if (string.Equals(currentChar, LocalConfig.Instance.CharName))
                        {
                            // if crf file name and using info are same, don't modify
                            // else edit thie line
                            if (string.Equals(currentCRFFile, crfFileName) &&
                                string.Equals(currentUsingInfo, usingInfo))
                            {
                                needModify = false;
                            }
                            else
                            {
                                charExist = true;
                            }
                        }

                        crfFileNames.Add(currentCRFFile);
                    }
                    ++lineNumber;
                }
            }

            if (needModify)
            {
                string content = Helper.NeutralFormat("{0}\t->\t{1}\t{2}", LocalConfig.Instance.CharName, crfFileName, usingInfo);
                Util.EditLineInFile(filePath, lineNumber, content, !charExist);
            }

            return crfFileNames.ToArray();
        }

        #endregion
    }
}
