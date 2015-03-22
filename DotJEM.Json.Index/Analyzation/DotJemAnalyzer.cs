using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace DotJEM.Json.Index.Analyzation
{
    public class DotJemAnalyzer : Analyzer
    {
        private ISet<string> stopSet;
        private bool replaceInvalidAcronym;
        private bool enableStopPositionIncrements;
        private Version matchVersion;

        public int MaxTokenLength { get; set; }

        /// <summary>
        /// Builds an analyzer with the default stop words (<see cref="F:Lucene.Net.Analysis.Standard.StandardAnalyzer.STOP_WORDS_SET"/>).
        /// 
        /// </summary>
        /// <param name="matchVersion">Lucene version to match see <see cref="T:Lucene.Net.Util.Version">above</see></param>
        public DotJemAnalyzer(Version matchVersion)
            : this(matchVersion, StopAnalyzer.ENGLISH_STOP_WORDS_SET)
        {
        }

        public DotJemAnalyzer(Version matchVersion, ISet<string> stopWords)
        {
            MaxTokenLength = byte.MaxValue;
            stopSet = stopWords;

            SetOverridesTokenStreamMethod<DotJemAnalyzer>();

            enableStopPositionIncrements = StopFilter.GetEnablePositionIncrementsVersionDefault(matchVersion);
            replaceInvalidAcronym = matchVersion.OnOrAfter(Version.LUCENE_24);
          
            this.matchVersion = matchVersion;
        }


        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new StopFilter(enableStopPositionIncrements, 
                new LowerCaseFilter(new StandardFilter(new StandardTokenizer(matchVersion, reader)
            {
                MaxTokenLength = MaxTokenLength
            })), stopSet);
        }

        public override TokenStream ReusableTokenStream(string fieldName, TextReader reader)
        {
            if (overridesTokenStreamMethod)
                return TokenStream(fieldName, reader);


            SavedStreams savedStreams = (SavedStreams)PreviousTokenStream;
            if (savedStreams == null)
            {
                savedStreams = new SavedStreams();
                PreviousTokenStream = savedStreams;
                savedStreams.tokenStream = new StandardTokenizer(matchVersion, reader);
                savedStreams.filteredTokenStream = new StandardFilter(savedStreams.tokenStream);
                savedStreams.filteredTokenStream = new LowerCaseFilter(savedStreams.filteredTokenStream);
                savedStreams.filteredTokenStream = new StopFilter(enableStopPositionIncrements, savedStreams.filteredTokenStream, stopSet);
            }
            else
                savedStreams.tokenStream.Reset(reader);
            savedStreams.tokenStream.MaxTokenLength = MaxTokenLength;
            savedStreams.tokenStream.SetReplaceInvalidAcronym(replaceInvalidAcronym);
            return savedStreams.filteredTokenStream;
        }

        private sealed class SavedStreams
        {
            internal StandardTokenizer tokenStream;
            internal TokenStream filteredTokenStream;
        }
    }
}