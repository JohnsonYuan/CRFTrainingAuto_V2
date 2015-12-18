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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Tts.Offline.Core;
    using Microsoft.Tts.Offline.Frontend;
    using Microsoft.Tts.Offline.Utility;

    // TODO: All your calsses are named as helper. Needn't put it in a new folder. Just give it a easy understand name
    /// <summary>
    /// PolyRuleFileHelper
    /// Since Microsoft.Tts.Offline.Frontend doesn't expose _ruleItems, so create a new class, then we can edit the polyrule item and save.
    /// </summary>
    /// <seealso cref="Microsoft.Tts.Offline.Frontend.RuleFile" />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public class PolyRuleFileHelper
    {
        private const string DeclearKeyRegex = @"^([a-zA-Z]+)[ \t]*#[ \t]*(string|int|(enum.*))[ \t]*;[ \t]*(//.*)?$";

        // polyrule.txt [domain=message]
        private const string DomainLineRegex = @"^[ \t]*(\[domain=)([a-zA-Z]+)*(\])$";

        // polyrule.txt all >= 0 line
        private const string PolyRuleDefaultRuleRegex = @"^[ \t]*All[ \t]*>=[ \t]*0[ \t]*:(.+)$";

        private List<RuleItem> _ruleItems = new List<RuleItem>();

        private List<string> _declearLines = new List<string>();

        /// <summary>
        /// Gets or sets the rule items.
        /// </summary>
        public List<RuleItem> RuleItems
        {
            get { return this._ruleItems; }
            set { this._ruleItems = value; }
        }

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
                        this._declearLines.Add(trimedLine);
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
                            this._ruleItems.Add(newItem);
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
                    this._ruleItems.Add(newItem);
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
                foreach (string declear in this._declearLines)
                {
                    sw.WriteLine(declear);
                }

                foreach (RuleItem ruleItem in this._ruleItems)
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

        /// <summary>
        /// Indicating whether the line is declear.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is declear.</returns>
        public bool IsDeclearKey(string line)
        {
            return Regex.Match(line, DeclearKeyRegex).Success;
        }

        /// <summary>
        /// Indicating whether the line is domain tag.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is domain tag.</returns>
        public bool IsDomainTag(string line)
        {
            return Regex.Match(line, DomainLineRegex).Success;
        }

        /// <summary>
        /// Indicating whether the line is default rule, All >= 0.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is default rule.</returns>
        public bool IsPolyRuleDefaultRule(string line)
        {
            return Regex.Match(line, PolyRuleDefaultRuleRegex).Success;
        }

        /// <summary>
        /// Whether the line is rule entry.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <returns>Whether the line is declear.</returns>
        public bool IsRuleItemEntry(string line)
        {
            return Regex.Match(line, KeyLineRegex).Success;
        }

        /// <summary>
        /// ParseDomainKey.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="keyName">KeyName.</param>
        public void ParseDomainKey(string line, ref string keyName)
        {
            Match match = Regex.Match(line, DomainLineRegex);

            if (match.Groups.Count != 4)
            {
                throw new InvalidDataException(Helper.NeutralFormat(
                    "Invalid domain line : [{0}]", line));
            }

            keyName = match.Groups[2].ToString();
        }

        #endregion
    }
}
