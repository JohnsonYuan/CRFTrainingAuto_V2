namespace CRFTrainingAuto
{
    #region Extension Methods
    public static class StringExtensions
    {
        /// <summary>
        /// Check if current string contains single char, using generated word break file
        /// </summary>
        /// <param name="content">input</param>
        /// <param name="charName">char name</param>
        /// <param name="wbResult">word break result</param>
        /// <returns></returns>
        public static int GetSingleCharIndexOfLine(this string content, string charName, string[] wbResult)
        {
            int index = content.IndexOf(charName);

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

                // if foundCount == 1, then we return the index, else return -1
                if (foundCount == 1)
                {
                    return index;
                }
            }
            return -1;
        }

        /// <summary>
        /// Check if current string contains single char, using word breaker
        /// </summary>
        /// <example>
        /// input: 子弹打到尽头弹了回来
        /// output: 6 (子弹 is a word, the result will be second 弹 index)
        /// </example>
        /// <param name="content">input</param>
        /// <param name="charName">char name</param>
        /// <param name="wordBreaker">wordBreaker</param>
        /// <param name="wbResult">word break result</paramTraining crf model>
        /// <returns>return the single char index, return -1 if this string doesn't contains single char</returns>
        public static int GetSingleCharIndexOfLine(this string content, string charName, WordBreaker wordBreaker, out string[] wbResult)
        {
            wbResult = wordBreaker.BreakWords(content);

            return content.GetSingleCharIndexOfLine(charName, wbResult);
        }

        /// <summary>
        /// Check if current string contains single char, using word breaker
        /// </summary>
        /// <example>
        /// input: 子弹打到尽头弹了回来
        /// output: 6 (子弹 is a word, the result will be second 弹 index)
        /// </example>
        /// <param name="content">input</param>
        /// <param name="charName">char name</param>
        /// <param name="wordBreaker">wordBreaker</param>
        /// <param name="wbResult">word break result</paramTraining crf model>
        /// <returns>return the single char index, return -1 if this string doesn't contains single char</returns>
        public static int GetSingleCharIndexOfLine(this string content, string charName, WordBreaker wordBreaker)
        {
            string[] wbResult = wordBreaker.BreakWords(content);

            return content.GetSingleCharIndexOfLine(charName, wbResult);
        }

        /// <summary>
        /// Get a space separated result from array
        /// </summary>
        /// <param name="contents"></param>
        /// <returns>string</returns>
        public static string SpaceSeparate(this string[] wbResult)
        {
            return string.Join(" ", contents);
        }

        /// <summary>
        /// Combile word break result to string
        /// </summary>
        /// <example>
        /// input: 120 平米 城市 庭院 别墅
        /// output:120平米城市庭院别墅
        /// </example>
        /// <param name="contents"></param>
        /// <returns>string</returns>
        public static string CombileToString(this string[] wbResult)
        {
            return string.Join("", wbResult);
        }

        /// <summary>
        /// Get a space separated result from array
        /// </summary>
        /// <param name="contents"></param>
        /// <returns>string</returns>
        public static string[] SplitBySpace(this string content)
        {
            return content.Split(new char[] { ' ' });
        }
    }

    #endregion
}
