using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CRFTrainingAuto.Tools
{
    class Class1
    {        /// <summary>
        /// Step 1: Generate txt file contains single training char from inFolder
        /// First generate each file corresponding file to temp folder
        /// and then merge them, random select maxCount data
        /// </summary>
        /// <param name="inputDir">corpus folder</param>
        /// <param name="outputDir">output folder</param>
        /// <param name="maxCount">max random selcted count from corpus</param>
        public void SelectTargetCharCorpus(string inputDir, string outputDir)
        {
            if (!Directory.Exists(inputDir))
            {
                throw new Exception(inputDir + " not exists!");
            }

            int totalFound = 0;
            int fileIndex = 0;

            // get all corpus file paths, CorpusFileSearchPattern currently is "*.txt"
            string[] inFilePaths = Directory.GetFiles(inputDir, CorpusFileSearchPattern);
            // tempFolder used to save the temp result, because the whole corpus might be very large
            string tempFolder = Path.Combine(outputDir, TempFolderName);

            Util.CreateDirIfNotExist(outputDir);
            Util.CreateDirIfNotExist(tempFolder);

            foreach (string filePath in inFilePaths)
            {
                Console.WriteLine(string.Format("Finding in file {0}, {1} of {2} files", filePath, ++fileIndex, inFilePaths.Length));

                HashSet<string> results = new HashSet<string>();
                string[] inputs = File.ReadAllLines(filePath);

                #region filter data
                int count = 0;

                for (int i = 0; i < inputs.Length; i++)
                {
                    // remove the empty space, if not, it will get the wrong index when using WordBraker to get the char index
                    string curSentence = inputs[i].Trim().Replace(" ", "").Replace("\t", "");

                    // if results donesn't contains the curSentence and curSentence contains only one single char
                    // then add curSentence to results
                    if (!results.Contains(curSentence) &&
                        curSentence.GetSingleCharIndexOfLine(_localConfig.CharName, _espHelper) > -1)
                    {
                        results.Add(curSentence);
                        ++count;
                    }

                    // show searching progress, i is start with 0, so ShowTipCount should minus 1
                    // if ShowTipCount = 5000, when i = 4999, 5000 cases searched
                    if ((i + 1) >= _localConfig.ShowTipCount &&
                        (i + 1) % _localConfig.ShowTipCount == 0)
                    {
                        Console.WriteLine("Searching " + (i + 1) + " of " + inputs.Length);
                    }
                }
                #endregion

                // save each file to temp folder with same file name, in next step merge them
                string tempFilepath = Path.Combine(tempFolder, Path.GetFileName(filePath));
                File.WriteAllLines(tempFilepath, results);
                results.Clear();

                Console.WriteLine(string.Format("Found {0} results in file {1}, {2} of {3} files", count, filePath, fileIndex, inFilePaths.Length));
                totalFound += count;
            }

            if (totalFound == 0)
            {
                Console.WriteLine("Could not find required case!");
                return;
            }

            // e.g. corpusAllFilePath = "corpus.txt"
            string corpusAllFilePath = Path.Combine(outputDir, CorpusFileNamePattern.Replace("{0}", ""));
            // merge all files in temp folder
            Util.MergeFiles(Path.Combine(tempFolder, CorpusFileSearchPattern), corpusAllFilePath);
            Console.WriteLine(string.Format("Saved to {0}, total found {1}.", corpusAllFilePath, totalFound));

            // random select maxCount corpus
            SelectRandomCorpus(corpusAllFilePath, outputDir);
            Console.WriteLine("Random " + corpusAllFilePath + " file complete");
        }
    }
}
