using System.Collections.Generic;
using System.IO;
using DotJEM.Json.Index.Configuration;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace DotJEM.Json.Index.Analyzation
{
    public class DotJemAnalyzer : Analyzer
    {
        private readonly ISet<string> stopSet = StopAnalyzer.ENGLISH_STOP_WORDS_SET;
        private readonly bool replaceInvalidAcronym;
        private readonly bool enableStopPositionIncrements;
        private readonly Version matchVersion;

        private readonly IIndexConfiguration configuration;

        public int MaxTokenLength { get; set; }

        public DotJemAnalyzer(Version matchVersion, IIndexConfiguration configuration)
        {
            MaxTokenLength = byte.MaxValue;

            enableStopPositionIncrements = StopFilter.GetEnablePositionIncrementsVersionDefault(matchVersion);
            replaceInvalidAcronym = matchVersion.OnOrAfter(Version.LUCENE_24);
          
            this.matchVersion = matchVersion;
            this.configuration = configuration;
        }


        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            //var strategy = configuration.

            return new StopFilter(enableStopPositionIncrements, 
                new LowerCaseFilter(new StandardFilter(new StandardTokenizer(matchVersion, reader)
            {
                MaxTokenLength = MaxTokenLength
            })), stopSet);
        }

        public override TokenStream ReusableTokenStream(string fieldName, TextReader reader)
        {

            SavedStreams savedStreams = (SavedStreams)PreviousTokenStream;
            if (savedStreams != null)
            {
                savedStreams.tokenStream.Reset(reader);
            }
            else
            {
                savedStreams = new SavedStreams();
                PreviousTokenStream = savedStreams;
                savedStreams.tokenStream = new StandardTokenizer(matchVersion, reader);
                savedStreams.filteredTokenStream = new StandardFilter(savedStreams.tokenStream);
                savedStreams.filteredTokenStream = new LowerCaseFilter(savedStreams.filteredTokenStream);
                savedStreams.filteredTokenStream = new StopFilter(enableStopPositionIncrements, savedStreams.filteredTokenStream, stopSet);
            }
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