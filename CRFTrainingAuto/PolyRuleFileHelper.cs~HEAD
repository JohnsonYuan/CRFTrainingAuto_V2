//-----------------------------------------------------------------------------------------
// <copyright file="PolyRuleFileHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <summary>
//     Load and save polyrule file.
// </summary>
//-----------------------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Frontend;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// FullRuleFile, add support for read and write comment lines.
    /// </summary>
    /// <seealso cref="RuleFile" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public class FullRuleFile : RuleFile
    {
        #region Public methods

        /// <summary>
        /// Load.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        public new void Load(string filePath)
        {
            ////#region Validate parameter

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw Helper.CreateException(typeof(FileNotFoundException), filePath);
            }

            if (!Helper.IsUnicodeFile(filePath))
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Polypony rule file [{0}] is not unicode.", filePath));
            }

            ////#endregion

            using (StreamReader sr = new StreamReader(filePath, Encoding.Unicode))
            {
                string line = null;
                string domain = DomainItem.GeneralDomain;
                RuleItem newItem = null;

                while ((line = sr.ReadLine()) != null)
                {
                    string trimedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimedLine))
                    {
                        continue;
                    }

                    if (IsDeclearKey(trimedLine))
                    {
                        this.DeclearLines.Add(trimedLine);
                        continue;
                    }

                    if (IsDomainTag(trimedLine))
                    {
                        ParseDomainKey(trimedLine, ref domain);
                        continue;
                    }

                    if (IsRuleItemEntry(trimedLine))
                    {
                        if (newItem != null)
                        {
                            this.RuleItems.Add(newItem);
                        }

                        newItem = new RuleItem();
                        newItem.EntryString = trimedLine;
                        newItem.DomainTag = domain;
                        domain = DomainItem.GeneralDomain;
                        continue;
                    }

                    if (newItem != null)
                    {
                        newItem.RuleContent.Add(trimedLine);
                    }
                }

                if (newItem != null)
                {
                    this.RuleItems.Add(newItem);
                }
            }
        }

        /// <summary>
        /// Save.
        /// </summary>
        /// <param name="filePath">FilePath.</param>
        public new void Save(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            Helper.EnsureFolderExistForFile(filePath);
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Unicode))
            {
                foreach (string declear in this.DeclearLines)
                {
                    sw.WriteLine(declear);
                }

                foreach (RuleItem ruleItem in this.RuleItems)
                {
                    sw.WriteLine();

                    string doaminTag = ruleItem.DomainTag;
                    if (doaminTag != DomainItem.GeneralDomain)
                    {
                        sw.WriteLine(string.Format("[domain={0}]", doaminTag));
                    }

                    sw.WriteLine(ruleItem.EntryString);
                    foreach (string content in ruleItem.RuleContent)
                    {
                        sw.WriteLine(content);
                    }
                }
            }
        }

        #endregion
    }
}
