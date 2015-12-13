//----------------------------------------------------------------------------
// <copyright file="SdCommand.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Execute sd.exe edit, sd.exe add and sd.exe revert command
// </summary>
//----------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.IO;
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Sd Command class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public static class SdCommand
    {
        #region Properties

        /// <summary>
        /// Gets SD.exe tool path.
        /// </summary>
        public static string SdToolPath
        {
            get
            {
                string sdToolPath = Path.Combine(LocalConfig.Instance.BranchRootPath, @"tools\coretools\sd.exe");

                return sdToolPath;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Call sd.exe add file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="message">Result.</param>
        public static void SdAddFile(string filePath, out string message)
        {
            string sdMsg = string.Empty;

            try
            {
                int sdExitCode = CommandLine.RunCommandWithOutputAndError(
                                                SdToolPath,
                                                Helper.NeutralFormat("add {0}", filePath),
                                                Path.GetDirectoryName(SdToolPath),
                                                ref sdMsg);

                if (sdExitCode == 0)
                {
                    message = Helper.NeutralFormat("Add file: {0}", filePath);
                }
                else
                {
                    message = Helper.NeutralFormat("Failed to add file: {0}.\r\n{1}", filePath, sdMsg);
                }
            }
            catch (Exception e)
            {
                message = Helper.NeutralFormat("{0}. Failed to add file: {1}", e.Message, filePath);
            }
        }

        /// <summary>
        /// Call sd.exe edit file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="message">Result.</param>
        public static void SdCheckoutFile(string filePath, out string message)
        {
            string sdMsg = string.Empty;

            try
            {
                int sdExitCode = CommandLine.RunCommandWithOutputAndError(
                                                SdToolPath,
                                                Helper.NeutralFormat("edit {0}", filePath),
                                                Path.GetDirectoryName(SdToolPath),
                                                ref sdMsg);

                if (sdExitCode == 0)
                {
                    message = Helper.NeutralFormat("Checked out file: {0}", filePath);
                }
                else
                {
                    message = Helper.NeutralFormat("Failed to check out file: {0}.\r\n{1}", filePath, sdMsg);
                }
            }
            catch (Exception e)
            {
                message = Helper.NeutralFormat("{0}. Failed to check out file: {1}", e.Message, filePath);
            }
        }

        /// <summary>
        /// Call sd.exe revert unchanged file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="message">Result.</param>
        public static void SdRevertUnchangedFile(string filePath, out string message)
        {
            string sdMsg = string.Empty;

            try
            {
                int sdExitCode = CommandLine.RunCommandWithOutputAndError(SdToolPath, Helper.NeutralFormat("revert -a {0}", filePath), null, ref sdMsg);

                if (sdExitCode == 0 && !string.IsNullOrEmpty(sdMsg))
                {
                    message = Helper.NeutralFormat("--Reverted unchanged file: {0}", filePath);
                }
                else
                {
                    message = string.Empty;
                }
            }
            catch (Exception e)
            {
                message = Helper.NeutralFormat("--{0}. Failed to revert unchanged file: {1}", e.Message, filePath);
            }
        }

        #endregion
    }
}
