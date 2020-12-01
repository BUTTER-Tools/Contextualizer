using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using PluginContracts;
using OutputHelperLib;
using System.Text.RegularExpressions;
using System.Linq;
using ContextObj;

namespace Contextualizer
{
    public class Contextualizer : Plugin
    {


        public string[] InputType { get; } = { "Tokens" };
        public string OutputType { get; } = "OutputArray";

        public Dictionary<int, string> OutputHeaderData { get; set; } = new Dictionary<int, string>() { {0, "DictionaryWord"},
                                                                                                        {1, "MatchNumber" },
                                                                                                        {2, "ContextLeft"},
                                                                                                        {3, "Match"},
                                                                                                        {4, "ContextRight"} };
        public bool InheritHeader { get; } = false;

        private string WordList { get; set; } = string.Join(Environment.NewLine, new string[] { "good", "great*", "bad", "awful*" });
        private int WordWindowLeft { get; set; } = 3;
        private int WordWindowRight { get; set; } = 3;
        private bool CaseSensitive { get; set; } = false;

        private Regex NewlineClean = new Regex(@"[\r\n]+", RegexOptions.Compiled);
        private List<string> WordsToHash;
        private string[] OriginalWordList;
        private int MaxWords;
        private HashSet<string> HashedWords;
        private Dictionary<int, List<string>> WildcardMap;
        private Dictionary<string, Regex> WordsWithWildCards;


        #region Plugin Details and Info

        public string PluginName { get; } = "Contextualize Words";
        public string PluginType { get; } = "Preprocessing";
        public string PluginVersion { get; } = "1.0.6";
        public string PluginAuthor { get; } = "Ryan L. Boyd (ryan@ryanboyd.io)";
        public string PluginDescription { get; } = "This plugin allows you to extract \"key words in context\" (KWIC), the most common format for concordance lines. In this plugin's settings, " +
                                                    "you can specify a list of words and phrases for which you would like to see their context " +
                                                    "within your texts. You can also specify the size of the context that you are interested in. " +
                                                    "For example, if you want to see the 3 words surrounding the word \"happy\" " +
                                                    "in your text, this plugin will help you accomplish this. This plugin can also capture multi-word " +
                                                    "phrases (\"the happy\") and process wildcards (\"the happ*\"; this will capture " +
                                                    "phrases like \"the happiest\" and \"the happier\").";
        public bool TopLevel { get; } = false;
        public string PluginTutorial { get; } = "Coming Soon";

        public Icon GetPluginIcon
        {
            get
            {
                return Properties.Resources.icon;
            }
        }

        #endregion



        public void ChangeSettings()
        {


            using (var form = new SettingsForm_Contextualizer(WordList, WordWindowLeft, WordWindowRight, CaseSensitive))
            {


                form.Icon = Properties.Resources.icon;
                form.Text = PluginName;


                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    WordList = form.WordList;
                    WordWindowLeft = form.WordWindowLeft;
                    WordWindowRight = form.WordWindowRight;
                    CaseSensitive = form.CaseSensitive;
                }
            }



        }





        public Payload RunPlugin(Payload Input)
        {



            Payload pData = new Payload();
            pData.FileID = Input.FileID;

            bool trackSegmentID = false;
            if (Input.SegmentID.Count > 0)
            {
                trackSegmentID = true;
            }
            else
            {
                pData.SegmentID = Input.SegmentID;
            }



            for (int counter = 0; counter < Input.StringArrayList.Count; counter++)
            {

                uint NumberOfMatches = 0;
                string DictionaryEntry = "";

                string[] Words = Input.StringArrayList[counter];

                if (!CaseSensitive) Words = Words.Select(s => s.ToLowerInvariant()).ToArray();

                int TotalStringLength = Words.Length;



                for (int i = 0; i < TotalStringLength; i++)
                {


                    bool IsWordMatched = false;
                    int NumWordsInMatchedString = 0;
                    string WordToMatch = "";
                    string[] SubArray = new string[] { };

                    for (int NumWords = MaxWords; NumWords > 0; NumWords--)
                    {

                        //here, we go in and construct n-grams up to the length of the 
                        //largest word phrase in the user-supplied list

                        if (i + NumWords > TotalStringLength) continue;


                        SubArray = new string[NumWords];
                        Array.Copy(Words, i, SubArray, 0, NumWords);




                        WordToMatch = string.Join(" ", SubArray);


                        //this is where all of your magic is going to happen
                        if (HashedWords.Contains(WordToMatch))
                        {
                            IsWordMatched = true;
                            DictionaryEntry = WordToMatch;
                            NumWordsInMatchedString = NumWords;
                        }
                        else if (WildcardMap.ContainsKey(NumWords))
                        {
                            for (int j = 0; j < WildcardMap[NumWords].Count; j++)
                            {
                                if (WordsWithWildCards[WildcardMap[NumWords][j]].IsMatch(WordToMatch))
                                {
                                    IsWordMatched = true;
                                    DictionaryEntry = WildcardMap[NumWords][j];
                                    NumWordsInMatchedString = NumWords;
                                    break;
                                }
                            }

                        }

                        //if we found the word / phrase, we'll break out of this internal n-gram loop
                        if (IsWordMatched) break;


                    }


                    if (IsWordMatched)
                    {
                        NumberOfMatches += 1;

                        //create a new array that will contain the word window
                        //string[] WordsInWindow = new string[1 + (WordWindowSize * 2)];
                        int SkipPositionLeft = i - WordWindowLeft;
                        int SkipPositionRight = i + 1 + (NumWordsInMatchedString - 1);

                        int TakeLeft = WordWindowLeft;
                        int TakeRight = WordWindowRight;


                        if (SkipPositionLeft < 0)
                        {

                            TakeLeft = i;
                            SkipPositionLeft = 0;

                        }

                        if (SkipPositionRight + TakeRight >= TotalStringLength)
                        {
                            TakeRight = (SkipPositionRight + TakeRight) - TotalStringLength;
                        }



                        string[] ContextLeft = Words.Skip(SkipPositionLeft).Take(TakeLeft).ToArray();
                        string[] ContextRight = Words.Skip(SkipPositionRight).Take(TakeRight).ToArray();


                        pData.StringArrayList.Add(new string[] {
                                                                //NumberOfMatches.ToString(),
                                                                DictionaryEntry,
                                                                NumberOfMatches.ToString(),
                                                                String.Join(" ", ContextLeft),
                                                                WordToMatch,
                                                                String.Join(" ", ContextRight)
                                                               });

                        //this is for if we're going to use the contextualizer helper
                        pData.ObjectList.Add(new ContextObj.ContextObj(ContextLeft, ContextRight, SubArray));

                        pData.SegmentNumber.Add(Input.SegmentNumber[counter]);
                        if (trackSegmentID)
                        {
                            pData.SegmentID.Add(Input.SegmentID[counter]);
                        }

                        //this moves things forward by the correct number of words
                        //helps prevent us from double-capturing words that are part of a captured bigram
                        i += NumWordsInMatchedString - 1;

                    }


                }



            }
            

            return (pData);



        }




        private static string wildCardReplacement = "REGEXWILDCARDREPLACE";

        public void Initialize()
        {
            
            //the very first thing that we want to do is set up our function word lists
            WildcardMap = new Dictionary<int, List<string>>();
            WordsWithWildCards = new Dictionary<string, Regex>();
            WordsToHash = new List<string>();

            if (CaseSensitive)
            {
                OriginalWordList = NewlineClean.Split(WordList);
            }
            else
            {
                OriginalWordList = NewlineClean.Split(WordList.ToLower());
            }

            OriginalWordList = OriginalWordList.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            MaxWords = 1;

            foreach (string Word in OriginalWordList)
            {
                string WordToParse = Word.Trim();

                int numwords = WordToParse.Split().Length;

                if (numwords > MaxWords) MaxWords = numwords;

                //what we do if the wildcard word
                if (WordToParse.Contains('*') && !WordsWithWildCards.ContainsKey(WordToParse))
                {
                    //just make sure that we have a key in the dictionary for the length of the wildcard word phrase
                    if (!WildcardMap.ContainsKey(numwords)) WildcardMap.Add(numwords, new List<string>());
                    WildcardMap[numwords].Add(WordToParse);

                    string regexString = WordToParse.Replace("*", wildCardReplacement);
                    regexString = Regex.Escape(regexString);
                    regexString = "^" + regexString.Replace(wildCardReplacement, ".*?") + "$";


                    WordsWithWildCards.Add(WordToParse, new Regex(regexString, RegexOptions.Compiled));
                }
                else
                {
                    WordsToHash.Add(WordToParse);
                }

            }

            //remove duplicates
            //WordWildcardList = WordWildcardList.Distinct().ToList();
            WordsToHash = WordsToHash.Distinct().ToList();

            HashedWords = new HashSet<string>(WordsToHash);
            //WordsWithWildCards = WordWildcardList.ToArray();

            WordsToHash = null;
            //WordWildcardList = null;

            //WildCardWordListLength = WordsWithWildCards.Length;

        }


        public bool InspectSettings()
        {
            return true;
        }

        public Payload FinishUp(Payload Input)
        {
            return (Input);
        }




        #region Import/Export Settings
        public void ImportSettings(Dictionary<string, string> SettingsDict)
        {
            WordList = SettingsDict["WordList"];
            WordWindowLeft = int.Parse(SettingsDict["WordWindowLeft"]);
            WordWindowRight = int.Parse(SettingsDict["WordWindowRight"]);
            CaseSensitive = Boolean.Parse(SettingsDict["CaseSensitive"]);
        }

        public Dictionary<string, string> ExportSettings(bool suppressWarnings)
        {
            Dictionary<string, string> SettingsDict = new Dictionary<string, string>();
            SettingsDict.Add("WordList", WordList);
            SettingsDict.Add("WordWindowLeft", WordWindowLeft.ToString());
            SettingsDict.Add("WordWindowRight", WordWindowRight.ToString());
            SettingsDict.Add("CaseSensitive", CaseSensitive.ToString());
            return (SettingsDict);
        }
        #endregion




    }
}
