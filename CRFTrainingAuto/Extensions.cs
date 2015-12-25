//-----------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//
// <summary>
//     Extension methods for string.
// </summary>
//-----------------------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    /// <summary>
    /// String extensions.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public static class StringExtensions
    {
        /// <summary>
        /// Check if current string contains single char, using generated word break file.
        /// </summary>
        /// <param name="content">Input content.</param>
        /// <param name="charName">Char name.</param>
        /// <param name="wbResult">Word break result.</param>
        /// <returns>Char index, return -1 if not found.</returns>
        public static int GetSingleCharIndexOfLine(this string content, string charName, string[] wbResult)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new System.ArgumentNullException("content");
            }

            if (wbResult == null || wbResult.Length == 0)
            {
                throw new System.ArgumentNullException("wbResult");
            }

            int index = content.IndexOf(charName, System.StringComparison.Ordinal);

            // if contains char, check if contains single char
            if (index > -1)
            {
                int foundCount = 0;
                int tempIndex = -1;

                // find the single char count
                foreach (string word in wbResult)
                {
                    tempIndex += word.Length;

                    if (word == charName)
                    {
                        index = tempIndex;
                        if (++foundCount > 1)
                        {
                            return -1;
                        }
                    }
                }

                // if foundCount == 1, then we return the index, else return -1
                if (foundCount == 1)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Check if current string contains single char, using word breaker.
        /// </summary>
        /// <example>
        /// Input: 子弹打到尽头弹了回来
        /// Output: 6 (子弹 is a word, the result will be second 弹 index).
        /// </example>
        /// <param name="content">Input content.</param>
        /// <param name="charName">Char name.</param>
        /// <param name="wordBreaker">Word breaker.</param>
        /// <param name="wbResult">Word break result.</param>
        /// <returns>Single char index, return -1 if this string doesn't contains single char.</returns>
        public static int GetSingleCharIndexOfLine(this string content, string charName, WordBreaker wordBreaker, out string[] wbResult)
        {
            wbResult = wordBreaker.BreakWords(content);

            return content.GetSingleCharIndexOfLine(charName, wbResult);
        }

        /// <summary>
        /// Check if current string contains single char, using word breaker.
        /// </summary>
        /// <example>
        /// Input: 子弹打到尽头弹了回来
        /// Output: 6 (子弹 is a word, the result will be second 弹 index).
        /// </example>
        /// <param name="content">INput content.</param>
        /// <param name="charName">Char name.</param>
        /// <param name="wordBreaker">Word breaker.</param>
        /// <returns>Single char index, return -1 if this string doesn't contains single char.</returns>
        public static int GetSingleCharIndexOfLine(this string content, string charName, WordBreaker wordBreaker)
        {
            string[] wbResult = wordBreaker.BreakWords(content);

            return content.GetSingleCharIndexOfLine(charName, wbResult);
        }

        /// <summary>
        /// Get a space separated string from word break result array.
        /// </summary>
        /// <example>
        /// Input array contains : 120 平米 城市 庭院 别墅
        /// Output : 120 平米 城市 庭院 别墅.
        /// </example>
        /// <param name="wbResult">Word break result.</param>
        /// <returns>Space separated string.</returns>
        public static string SpaceSeparate(this string[] wbResult)
        {
            if (wbResult != null && wbResult.Length > 0)
            {
                return string.Join(" ", wbResult);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Combine word break result to string.
        /// </summary>
        /// <example>
        /// Input : 120 平米 城市 庭院 别墅
        /// Output :120平米城市庭院别墅.
        /// </example>
        /// <param name="wbResult">Word break result.</param>
        /// <returns>The sentence from word break result.</returns>
        public static string ConcatToString(this string[] wbResult)
        {
            if (wbResult != null && wbResult.Length > 0)
            {
                return string.Join(string.Empty, wbResult);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get array from space spearate string.
        /// </summary>
        /// <example>
        /// Input : 120 平米 城市 庭院 别墅
        /// Output array contains :120 平米 城市 庭院 别墅.
        /// </example>
        /// <param name="content">Input string.</param>
        /// <returns>Space separated array.</returns>
        public static string[] SplitBySpace(this string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            return content.Trim().Split(new char[] { ' ' });
        }
    }
}
