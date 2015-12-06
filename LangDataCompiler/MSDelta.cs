//----------------------------------------------------------------------------
// <copyright file="MSDelta.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Language Data Compiler
// </summary>
//----------------------------------------------------------------------------

namespace LangDataCompiler
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Tts.Offline.Utility;

    public static class MSDelta
    {
        /// <summary>
        /// Create Delta.
        /// </summary>
        /// <param name="sourceFileName">Source file name.</param>
        /// <param name="targetFileName">Target file name.</param>
        /// <param name="deltaFileName">Delta file name.</param>
        /// <exception cref="MethodAccessException">Method Access Exception.</exception>
        public static void CreateDelta(string sourceFileName, string targetFileName, string deltaFileName)
        {
            DeltaInput deltaInput = new DeltaInput();
            deltaInput.Start = IntPtr.Zero;
            deltaInput.Size = UIntPtr.Zero;

            // create constant FILETIME to avoid using default changing value
            FileTime targetFileTime = new FileTime();
            targetFileTime.DwLowDateTime = uint.MinValue;
            targetFileTime.DwHighDateTime = uint.MinValue;

            const long DELTA_FILE_TYPE_SET_RAW_ONLY = 0x00000001;
            const long DELTA_FLAG_NONE = 0x00000000;
            const long DELTA_DEFAULT_FLAGS_RAW = 0x00000000;

            if (!CreateDelta(DELTA_FILE_TYPE_SET_RAW_ONLY, DELTA_FLAG_NONE, DELTA_DEFAULT_FLAGS_RAW,
                    sourceFileName, targetFileName, IntPtr.Zero, IntPtr.Zero, deltaInput, ref targetFileTime,
                    32, deltaFileName))
            {
                throw new MethodAccessException();
            }
        }

        /// <summary>
        /// Create delta based on empty file.
        /// </summary>
        /// <param name="targetFileName">Target file name.</param>
        /// <param name="deltaFileName">Delta file name.</param>
        public static void CreateDeltaBasedOnEmptyFile(string targetFileName, string deltaFileName)
        {
            string sourceFileName = Helper.GetTempFileName();
            File.Create(sourceFileName).Dispose();
            try
            {
                CreateDelta(sourceFileName, targetFileName, deltaFileName);
            }
            finally
            {
                Helper.ForcedDeleteFile(sourceFileName);
            }
        }

        /// <summary>
        /// Apply Delta.
        /// </summary>
        /// <param name="sourceFileName">Source file name.</param>
        /// <param name="deltaFileName">Delta file name.</param>
        /// <param name="targetFileName">Target file name.</param>
        public static void ApplyDelta(string sourceFileName, string deltaFileName, string targetFileName)
        {
            const long DELTA_FLAG_NONE = 0x00000000;
            if (!ApplyDelta(DELTA_FLAG_NONE, sourceFileName, deltaFileName, targetFileName))
            {
                throw new MethodAccessException();
            }
        }

        /// <summary>
        /// Create Delta.
        /// </summary>
        /// <param name="fileTypeSet">File type set.</param>
        /// <param name="setFlags">Set flags.</param>
        /// <param name="resetFlags">Reset flags.</param>
        /// <param name="sourceName">Source name.</param>
        /// <param name="targetName">Target name.</param>
        /// <param name="sourceOptionsName">Source options name.</param>
        /// <param name="targetOptionsName">Target options name.</param>
        /// <param name="globalOptions">Global options.</param>
        /// <param name="targetFileTime">Target file time.</param>
        /// <param name="hashAlgId">Hash alg id.</param>
        /// <param name="deltaName">Delta name.</param>
        /// <returns>Bool value.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Ignore."),
        DllImport("msdelta.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateDelta(long fileTypeSet, long setFlags, long resetFlags, string sourceName,
              string targetName, IntPtr sourceOptionsName, IntPtr targetOptionsName, DeltaInput globalOptions,
              ref FileTime targetFileTime, uint hashAlgId, string deltaName);

        /// <summary>
        /// Apply Delta.
        /// </summary>
        /// <param name="deltaFlags">Delta flags.</param>
        /// <param name="sourceName">Source name.</param>
        /// <param name="deltaName">Delta name.</param>
        /// <param name="targetName">Target name.</param>
        /// <returns>Bool value.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Ignore."),
        DllImport("msdelta.dll", CharSet = CharSet.Unicode)]
        private static extern bool ApplyDelta(long deltaFlags, string sourceName, string deltaName, string targetName);

        /// <summary>
        /// Delta input struct.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DeltaInput
        {
            /// <summary>
            /// Start.
            /// </summary>
            private IntPtr _start;

            /// <summary>
            /// Size.
            /// </summary>
            private UIntPtr _size;

            /// <summary>
            /// Editable.
            /// </summary>
            private bool _editable;

            #region Properties
            public IntPtr Start
            {
                get { return _start; }
                set { _start = value; }
            }

            public UIntPtr Size
            {
                get { return _size; }
                set { _size = value; }
            }

            public bool Editable
            {
                get { return _editable; }
                set { _editable = value; }
            }
            #endregion
        }

        /// <summary>
        /// FileTime struct.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct FileTime
        {
            private uint lowDateTime;
            private uint highDateTime;

            #region Properties

            /// <summary>
            /// Gets or sets DwLowDateTime.
            /// </summary>
            public uint DwLowDateTime
            {
                get { return lowDateTime; }
                set { lowDateTime = value; }
            }

            /// <summary>
            /// Gets or sets DwHighDateTime.
            /// </summary>
            public uint DwHighDateTime
            {
                get { return highDateTime; }
                set { highDateTime = value; }
            }

            #endregion
        }
    }
}