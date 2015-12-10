namespace CRFTrainingAuto
{
    using Microsoft.Tts.Offline.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class SdCommand
    {
        /// <summary>
        /// SD.exe tool path
        /// </summary>
        /// <returns></returns>
        public static string SdToolPath
        {
            get
            {
                string sdToolPath = Path.Combine(LocalConfig.Instance.BranchRootPath, @"tools\coretools\sd.exe");

                return sdToolPath;
            }
        }

        /// <summary>
        /// Call sd.exe add file
        /// </summary>
        /// <param name="SdToolPath">sd.exe path</param>
        /// <param name="filePath">file path</param>
        /// <param name="message">result</param>
        public static void SdAddFile(string filePath, out string message)
        {
            string sdMsg = string.Empty;

            try
            {
                Int32 sdExitCode = CommandLine.RunCommandWithOutputAndError(SdToolPath,
                                                string.Format("add {0}", filePath),
                                                Path.GetDirectoryName(SdToolPath),
                                                ref sdMsg);

                if (sdExitCode == 0)
                {
                    message = string.Format("Add file: {0}", filePath);
                }
                else
                {
                    message = string.Format("Failed to add file: {0}.\r\n{1}", filePath, sdMsg);
                }
            }
            catch (Exception e)
            {
                message = string.Format("{0}. Failed to add file: {1}", e.Message, filePath);
            }
        }

        /// <summary>
        /// Call sd.exe edit file
        /// </summary>
        /// <param name="SdToolPath">sd.exe path</param>
        /// <param name="filePath">file path</param>
        /// <param name="message">result</param>
        public static void SdCheckoutFile(string filePath, out string message)
        {
            string sdMsg = string.Empty;

            try
            {
                Int32 sdExitCode = CommandLine.RunCommandWithOutputAndError(SdToolPath,
                                                string.Format("edit {0}", filePath),
                                                Path.GetDirectoryName(SdToolPath),
                                                ref sdMsg);

                if (sdExitCode == 0)
                {
                    message = string.Format("Checked out file: {0}", filePath);
                }
                else
                {
                    message = string.Format("Failed to check out file: {0}.\r\n{1}", filePath, sdMsg);
                }
            }
            catch (Exception e)
            {
                message = string.Format("{0}. Failed to check out file: {1}", e.Message, filePath);
            }
        }

        /// <summary>
        /// Call sd.exe revert unchanged file
        /// </summary>
        /// <param name="SdToolPath">sd.exe path</param>
        /// <param name="filePath">file path</param>
        /// <param name="message">result</param>

        public static void SdRevertUnchangedFile(string filePath, out string message)
        {
            string sdMsg = string.Empty;

            try
            {
                Int32 sdExitCode = CommandLine.RunCommandWithOutputAndError(SdToolPath, string.Format("revert -a {0}", filePath), null, ref sdMsg);

                if (sdExitCode == 0 && !string.IsNullOrEmpty(sdMsg))
                {
                    message = string.Format("--Reverted unchanged file: {0}", filePath);
                }
                else
                {
                    message = string.Empty;
                }
            }
            catch (Exception e)
            {
                message = string.Format("--{0}. Failed to revert unchanged file: {1}", e.Message, filePath);
            }
        }
    }
}
