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
    public static class CompilerHelper
    {
        #region Methods

        /// <summary>
        /// CRF compiler
        /// </summary>
        /// <param name="crfModelDir">crf model folder</param>
        /// <param name="lang">language</param>
        /// <param name="crfBinFile">crf bin file path</param>
        /// <returns>success or not</returns>
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
                        Util.ConsoleOutTextColor(error.ToString());
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
        /// <param name="tempBinFile">temp binary file path</param>
        /// <returns>success or not</returns>
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
        /// 
        /// polyrule.txt is like below, we should remove All >= 0 : "b eh_h i_l"; to make CRF model working
        /// [domain=address]
        /// CurW = "背";
        /// PrevW = "肩" : "b eh_h i_h";
        /// PrevW = "越" : "b eh_h i_h";
        /// All >= 0 : "b eh_h i_l";.
        /// </summary>
        /// <param name="filePath">poly rule file path.</param>
        /// <param name="charName">char name.</param>
        /// <returns>true if updated, false not changed.</returns>
        public static bool UpdatePolyRuleFile(string filePath, string charName)
        {
            Helper.ThrowIfFileNotExist(filePath);
            Helper.ThrowIfNull(charName);

            int lineNumber = 0;

            bool foundTargetChar = false;

            bool currentCharHasDomainAttr = false;
            string currentChar = "";

            // use this regex to find rule is used for which word
            Regex rxChar = new Regex("CurW = \"(.+)\";", RegexOptions.Compiled);

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
                    else if (rxChar.IsMatch(lineContent))
                    {
                        // we don't need to iterate again if meet the next char
                        if (foundTargetChar)
                        {
                            return false;
                        }

                        currentChar = rxChar.Match(lineContent).Groups[1].Value;

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
                        if (lineContent.StartsWith("All >= 0 :", StringComparison.OrdinalIgnoreCase))
                        {
                            string message;

                            // remove All >= 0 line in poly rule file
                            SdCommand.SdCheckoutFile(filePath, out message);
                            Util.ConsoleOutTextColor(message);

                            reader.Dispose();

                            Util.EditLineInFile(filePath, lineNumber, null);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Load CRF model name mapping(model name and localized name).
        /// </summary>
        /// <example>
        /// The mapping txt file is like this:
        /// 
        /// Map between polyphony model:
        /// 差	->	cha.crf	Being_used
        /// 长	->	chang.crf	Being_used
        /// 当	->	dang.crf	Being_used
        /// 行	->	hang.crf	Being_used
        /// 系	->	xi.crf	Unused.
        /// </example>
        /// <param name="filePath">crf mapping File Path.</param>
        /// <param name="crfFileName">crf file name.</param>
        /// <param name="usingInfo">check the char whether to be used, in mapping file "Being_used" or "Unused".</param>
        /// <returns>crf model files array, like bei.crf, wei.crf.</returns>
        public static string[] UpdateCRFModelMappingFile(string filePath, string crfFileName, string usingInfo)
        {
            Helper.ThrowIfFileNotExist(filePath);

            string message;
            SdCommand.SdCheckoutFile(filePath, out message);
            Util.ConsoleOutTextColor(message);

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
