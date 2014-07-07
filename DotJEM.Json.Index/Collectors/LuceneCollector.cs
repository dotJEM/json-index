using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;

// TODO: This collector is currently not in effect, so we disable all resharper warnings for now.
// ReSharper disable All
namespace DotJEM.Json.Index.Collectors
{
    //Note: Investication in Collectors.
    public class LuceneCollector : Collector
    {
        private Scorer scorer;
        private IndexReader reader;

         

        private readonly Dictionary<int, float> finite;

        public LuceneCollector(IJsonIndex index)
        {
            this.finite = new Dictionary<int, float>();
        }

        public override void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
        }

        public override void Collect(int doc)
        {
            finite.Add(doc, scorer.Score());

            //Document document = reader.Document(doc);

            //Debug.WriteLine("Collect: " + doc + " scores " + scorer.Score());
            //Debug.WriteLine("Document: " + document.Get("$$raw"));
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            this.reader = reader;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return true; }
        }

        public void Findings()
        {
            Debug.WriteLine("Found '{0}' hits", finite.Count);
        }
    }
}
