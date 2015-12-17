//----------------------------------------------------------------------------
// <copyright file="Breaker.cs" company="MICROSOFT">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      Word breaker and sentence breaker.
// </summary>
//----------------------------------------------------------------------------
namespace CRFTrainingAuto
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Microsoft.Tts.Offline;
    using Microsoft.Tts.Offline.Utility;
    using SP = Microsoft.Tts.ServiceProvider;

    /// <summary>
    /// Word breaker.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public class WordBreaker : IDisposable
    {
        #region Fields

        private SP.TtsEngine _engine;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WordBreaker"/> class.
        /// </summary>
        /// <param name="config">Local config.</param>
        public WordBreaker(LocalConfig config) : this(config.Lang, config.LangDataPath)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WordBreaker"/> class.
        /// </summary>
        /// <param name="lang">Language.</param>
        /// <param name="langDataPath">Language data path.</param>
        public WordBreaker(Language lang, string langDataPath)
        {
            Helper.ThrowIfNull(langDataPath);

            _engine = new SP.TtsEngine((SP.Language)lang, null, null, langDataPath);
            _engine.PipelineMode = SP.ModulePipelineMode.PM_TEXT_ANALYSIS;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the TTS engine.
        /// </summary>
        public SP.TtsEngine TtsEngine
        {
            get
            {
                return _engine;
            }
        }

        #endregion

        #region Public Method

        /// <summary>
        /// Get TtsUtterance.
        /// </summary>
        /// <param name="content">Content to be spoken.</param>
        /// <param name="sayas">Sayas used by ESP.</param>
        /// <returns>Utterance enum.</returns>
        public IEnumerable<SP.TtsUtterance> EspUtterances(string content, string sayas = null)
        {
            Helper.ThrowIfNull(_engine);
            Helper.ThrowIfNull(content);

            if (string.IsNullOrEmpty(sayas))
            {
                _engine.SetSpeakText(content);
            }
            else
            {
                _engine.SetSpeakText(content, sayas);
            }

            // reset text processor
            _engine.TextProcessor.Reset();

            while (true)
            {
                SP.TtsUtterance utterance = new SP.TtsUtterance();

                try
                {
                    if (!_engine.TextProcessor.Process(utterance))
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                yield return utterance;
            }
        }

        /// <summary>
        /// Get word break result.
        /// </summary>
        /// <example>
        /// Input:  父亲在银行界人脉广
        /// Output: 父亲 在 银行 界 人脉 广.
        /// </example>
        /// <param name="sentence">Input sentence.</param>
        /// <returns>Word break result.</returns>
        public string[] BreakWords(string sentence)
        {
            Helper.ThrowIfNull(sentence);

            Collection<string> words = new Collection<string>();

            foreach (Microsoft.Tts.ServiceProvider.TtsUtterance utterance in EspUtterances(sentence))
            {
                using (utterance)
                {
                    int num = -1;
                    foreach (SP.TtsWord word in utterance.Words)
                    {
                        if (word.TextOffset != num)
                        {
                            string item = sentence.Substring((int)word.TextOffset, (int)word.TextLength);
                            words.Add(item);
                            num = (int)word.TextOffset;
                        }
                    }
                }
            }

            return words.ToArray();
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            // Finalizer calls Dispose(false)
            Dispose(true);
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">True or false.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _engine.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// Sentence breaker.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
    public class SentenceBreaker
    {
        // the messy part are full-width ! and ?
        private static readonly string[] SentenceSpliters = { "。", "!", "！", ";", "?", "？", "......", "..." };

        /// <summary>
        /// Sentence Separate files in folder.
        /// </summary>
        /// <example>
        /// Input:
        /// 07073游戏网->8090《魔兽部落》刀塔战场杀四方->在8090魔兽部落中，战争并非是一场儿戏，而是真真切切的强者对决!在刀塔战场上，这是堡垒的争夺赛，更是一场尊严的较量!而此时，策略就显得尤为重要，队员们如何分配战力?在同一战场又怎样配合?在这即将来临的风暴前，只有努力提升自身实力，才能幸存下来! 8090魔兽部落：http://msbl.8090yxs.com/?xw-msbl 8090魔兽部落刀塔战场玩法每天20:00开启，持续25分钟，进入副本后随即分配到两个敌对阵营，最先达到5000阵营积分或结束时阵营积分高的阵营将获得胜利。在20:10和20:15，战场内刷新BOSS，击杀BOSS有概率掉落武器碎片、再生宝石、强化石等。 战况瞬息万变，击杀敌方卫兵可增加1点阵营积分，卫兵每30S刷新一波。击杀对方阵营玩家可增加2点阵营积分，同时记录击杀排行，副本结束后根据击杀排行榜给予个人奖励。获胜方阵营可获得：荣誉1000点+经验50W+金币15W。失败方阵营可获得：荣誉500点+经验30W+金币10W。 击杀排行也是有丰厚奖励的!第一名：荣誉4000点+勋章碎片*2+中级强化石*5+强化石*10。第二名：荣誉2000点+勋章碎片*1+中级强化石*3+强化石*5。第三名：荣誉1500点+勋章碎片*1+中级强化石*2+强化石*3。第四—十名：荣誉1000点+中级强化石*1+强化石*2。第11—999名：荣誉1000点+强化石*1。与个人PK不同,个人战主要体现为玩家的个人能力，而这项战斗主要表现为玩家与玩家之间的配合! 8090魔兽部落采用经典游戏模式MMORPG，融合当下最新技术，完美展现即时战斗特点，技能炫酷，内容丰富，各种特色玩法，等待玩家的发现，期待玩家的探索。3个职业个人生涯贯穿着整个游戏故事。绚丽的冒险故事，打开玩家风尘已久的记忆之门，重新去探索那一段经典的岁月，欢迎体验神秘之旅，尽在8090魔兽部落。 37《轩辕剑之天之痕》此次有幸请到素有古典美女之称的刘诗诗作代言,游戏中的拓跋玉儿…
        /// Output:
        /// 07073游戏网->8090《魔兽部落》刀塔战场杀四方->在8090魔兽部落中，战争并非是一场儿戏，而是真真切切的强者对决!
        /// 在刀塔战场上，这是堡垒的争夺赛，更是一场尊严的较量!
        /// 而此时，策略就显得尤为重要，队员们如何分配战力?在同一战场又怎样配合?在这即将来临的风暴前，只有努力提升自身实力，才能幸存下来!
        /// 8090魔兽部落：http://msbl.8090yxs.com/?xw-msbl 8090魔兽部落刀塔战场玩法每天20:00开启，持续25分钟，进入副本后随即分配到两个敌对阵营，最先达到5000阵营积分或结束时阵营积分高的阵营将获得胜利。
        /// 在20:10和20:15，战场内刷新BOSS，击杀BOSS有概率掉落武器碎片、再生宝石、强化石等。
        /// 战况瞬息万变，击杀敌方卫兵可增加1点阵营积分，卫兵每30S刷新一波。.
        /// </example>
        /// <param name="inputDir">Input Folder.</param>
        /// <param name="outputDir">Output Folder.</param>
        /// <param name="searchPattern">Search pattern, default *.txt.</param>
        public static void DoSentenceSeparate(string inputDir, string outputDir, string searchPattern = "*.txt")
        {
            foreach (string file in Directory.GetFiles(inputDir, searchPattern))
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    while (reader.Peek() > -1)
                    {
                        string paragraph = reader.ReadLine();
                        if (string.IsNullOrEmpty(paragraph))
                        {
                            continue;
                        }

                        List<string> outputs = new List<string>();
                        int startIndex = 0;

                        while (startIndex < paragraph.Length)
                        {
                            int endIndex = FindEndIndex(paragraph, startIndex, SentenceSpliters);
                            if (endIndex != -1)
                            {
                                outputs.Add(paragraph.Substring(startIndex, endIndex - startIndex));
                                startIndex = endIndex;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (startIndex < paragraph.Length)
                        {
                            outputs.Add(paragraph.Substring(startIndex, paragraph.Length - startIndex));
                        }

                        File.WriteAllLines(Path.Combine(outputDir, Path.GetFileName(file)), outputs);
                    }
                }
            }
        }

        /// <summary>
        /// Find the first spliter index.
        /// </summary>
        /// <param name="paragraph">Paragraph content.</param>
        /// <param name="startIndex">Find start index.</param>
        /// <param name="splitters">Sentence splitters.</param>
        /// <returns>Min index of splitter.</returns>
        private static int FindEndIndex(string paragraph, int startIndex, string[] splitters)
        {
            int splitterLength = 0;
            int minEndIndex = paragraph.Length;
            foreach (var splitter in splitters)
            {
                int endIndex = paragraph.IndexOf(splitter, startIndex);

                if (endIndex != -1 && endIndex < minEndIndex)
                {
                    minEndIndex = endIndex;
                    splitterLength = splitter.Length;
                }
            }

            if (minEndIndex == paragraph.Length)
            {
                return -1;
            }
            else
            {
                return minEndIndex + splitterLength;
            }
        }
    }
}
