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
    using System.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Frontend;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Compiler helper, compile crf model and general rule.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public static class CompilerHelper
    {
        #region Methods

        /// <summary>
        /// Compile CRF model.
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
        /// Compile general polurule and binary replace in data file.
        /// </summary>
        /// <param name="polyrRuleFile">Polyrule.txt file path.</param>
        /// <param name="compiledDatFile">New compiled dat file path.</param>
        /// <returns>Success or not.</returns>
        public static bool CompileGeneralPolyRule(string polyrRuleFile, out string compiledDatFile)
        {
            Helper.ThrowIfFileNotExist(polyrRuleFile);

            string tempFolder = Helper.GetTempFolderName();
            string tempGeneralRuleFile = Path.Combine(tempFolder, Util.TempGeneralPolyRuleFileName);
            string tempBinFile = Path.Combine(tempFolder, Util.TempGeneralPolyRuleBinFileName);

            compiledDatFile = Path.Combine(tempFolder, Path.GetFileName(LocalConfig.Instance.LangDataPath));

            // delete the existing data file
            Helper.ForcedDeleteFile(compiledDatFile);

            // delete the backup data file, LanguageDataHelper.ReplaceBinaryFile will genereate again
            string backFilePath = compiledDatFile + ".bak";
            Helper.ForcedDeleteFile(backFilePath);

            // copy the original data file to temp folder
            File.Copy(LocalConfig.Instance.LangDataPath, compiledDatFile, true);

            try
            {
                // split polyrule.txt
                RuleFile ruleFile = new RuleFile();
                ruleFile.Load(polyrRuleFile);

                // select general rule file
                var generalPolyRule = ruleFile.Split().FirstOrDefault(file => file.DomainTag == Microsoft.Tts.Offline.Core.DomainItem.GeneralDomain);

                generalPolyRule.Save(tempGeneralRuleFile);
                Helper.ThrowIfFileNotExist(tempGeneralRuleFile);

                string compilingArguments = Helper.NeutralFormat("\"{0}\" \"{1}\"", tempGeneralRuleFile, tempBinFile);

                string message = string.Empty;

                // use polycomp.exe compile general polyrule.txt bin file
                int exitCode = CommandLine.RunCommandWithOutputAndError(
                    Util.RuleCompilerPath, compilingArguments, null, ref message);

                if (exitCode == 0)
                {
                    Helper.ThrowIfFileNotExist(tempBinFile);

                    Microsoft.Tts.Offline.Compiler.LanguageData.LanguageDataHelper.ReplaceBinaryFile(
                        compiledDatFile,
                        tempBinFile,
                        Microsoft.Tts.Offline.Compiler.LanguageData.ModuleDataName.PolyphoneRule);

                    return File.Exists(compiledDatFile);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                compiledDatFile = null;
                return false;
            }
            finally
            {
                if (File.Exists(tempGeneralRuleFile))
                {
                    File.Delete(tempGeneralRuleFile);
                }

                if (File.Exists(tempBinFile))
                {
                    File.Delete(tempBinFile);
                }
            }
        }

        /// <summary>
        /// Update polyrule.txt for specific char
        /// Delete "All >= 0" line if polyrule.txt file contains
        /// polyrule.txt is like below, we should remove All >= 0 : "b eh_h i_l"; to make CRF model working
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

            PolyRuleFileHelper polyRuleFile = new PolyRuleFileHelper();

            try
            {
                polyRuleFile.Load(filePath);

                List<RuleItem> ruleItems = polyRuleFile.RuleItems;

                RuleItem charItem = ruleItems.FirstOrDefault(r => r.DomainTag == Microsoft.Tts.Offline.Core.DomainItem.GeneralDomain);

                if (charItem != null)
                {
                    string lastRuleContent = charItem.RuleContent.LastOrDefault();
                    Helper.ThrowIfNull(lastRuleContent);

                    if (polyRuleFile.IsPolyRuleDefaultRule(lastRuleContent))
                    {
                        // if current item only contains All >= rule , then remove this item
                        if (charItem.RuleContent.Count == 1)
                        {
                            ruleItems.Remove(charItem);
                        }
                        else
                        {
                            charItem.RuleContent.Remove(lastRuleContent);
                        }

                        string message;
                        SdCommand.SdCheckoutFile(filePath, out message);

                        polyRuleFile.Save(filePath);

                        Helper.PrintSuccessMessage(message);
                    }
                }
            }
            catch
            {
                throw;
            }

            return false;
        }

        /// <summary>
        /// Load CRF model name mapping(model name and localized name).
        /// We always use "Being_used" attribute for the new crf model for test it.
        /// </summary>
        /// <example>
        /// The mapping txt file is like below:
        /// 差 -> cha.crf Being_used
        /// 长 -> chang.crf Being_used
        /// 当 -> dang.crf Being_used
        /// 行 -> hang.crf Being_used
        /// 系 -> xi.crf Unused.
        /// </example>
        /// <param name="filePath">Crf mapping file path.</param>
        /// <param name="crfFileName">Crf file name.</param>
        /// <returns>Crf model files array, like bei.crf, wei.crf.</returns>
        public static string[] UpdateCRFModelMappingFile(string filePath, string crfFileName)
        {
            Helper.ThrowIfFileNotExist(filePath);

            string message;
            SdCommand.SdCheckoutFile(filePath, out message);
            Helper.PrintSuccessMessage(message);

            var crfMappings = LoadCRFModelMapping(filePath);

            // line number start index is 2, the mapping file first line won't change
            int lineNumber = 2;
            bool needModify = true;
            bool charExist = false;

            foreach (CRFModelMapping mapping in crfMappings)
            {
                string currentChar = mapping.CharName;
                string currentCrfFile = mapping.CrfModelName;
                string currentStatus = mapping.Status;

                // if current line's char is same with charName para, check whether need to modify this line
                if (string.Equals(currentChar, LocalConfig.Instance.CharName))
                {
                    // if crf file name and using info are same, don't modify
                    // else edit thie line
                    if (string.Equals(currentCrfFile, crfFileName) &&
                        string.Equals(currentStatus, Util.CRFMappingFileBeingUsedValue))
                    {
                        needModify = false;
                    }
                    else
                    {
                        charExist = true;
                    }
                }

                ++lineNumber;
            }

            if (needModify)
            {
                string content = Helper.NeutralFormat("{0}\t->\t{1}\t{2}", LocalConfig.Instance.CharName, crfFileName, Util.CRFMappingFileBeingUsedValue);
                Util.EditLineInFile(filePath, lineNumber, content, !charExist);
            }

            // if the newly char not exist, update the crfMappings variable
            if (!charExist)
            {
                crfMappings = LoadCRFModelMapping(filePath);
            }

            return crfMappings.Select(m => m.CrfModelName).ToArray();
        }

        /// <summary>
        /// Loads the CRF model mapping.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>List collection of CRFModelMapping in this file.</returns>
        public static IEnumerable<CRFModelMapping> LoadCRFModelMapping(string filePath)
        {
            int lineNumber = 1;

            using (StreamReader reader = new StreamReader(filePath))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    // if the first line matches, then continue to read
                    if (lineNumber == 1 &&
                        !string.Equals(Util.CRFMappingFileFirstLineContent, line))
                    {
                        throw new FormatException(Helper.NeutralFormat("{0} mapping file has the wrong format!", filePath));
                    }
                    else if (lineNumber > 1)
                    {
                        yield return (CRFModelMapping)line;
                    }

                    ++lineNumber;
                }
            }
        }

        #endregion
    }
}
