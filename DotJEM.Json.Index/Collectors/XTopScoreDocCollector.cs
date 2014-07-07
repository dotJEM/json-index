using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

// TODO: This collector is an extract from the Lucene source, it's here for sort of documentation on how to do collectors, so no warnings on this.
// ReSharper disable All
namespace DotJEM.Json.Index.Collectors
{
    //Note: For Inspirational purposes.
    public class XTopScoreDocCollector : Collector
    {
        protected internal static readonly TopDocs EMPTY_TOPDOCS = new TopDocs(0, new ScoreDoc[0], float.NaN);

        protected internal PriorityQueue<ScoreDoc> pq;
        internal ScoreDoc pqTop;
        internal int docBase;
        internal Scorer scorer;

        protected internal int internalTotalHits;

        public virtual int TotalHits
        {
            get { return internalTotalHits; }
        }

        private XTopScoreDocCollector(int numHits)
        {
            pq = new HitQueue(numHits, true);
            pqTop = pq.Top();
        }

        protected internal virtual void PopulateResults(ScoreDoc[] results, int howMany)
        {
            for (int index = howMany - 1; index >= 0; --index)
                results[index] = pq.Pop();
        }

        public TopDocs TopDocs()
        {
            return TopDocs(0, internalTotalHits < pq.Size() ? internalTotalHits : pq.Size());
        }

        public TopDocs TopDocs(int start)
        {
            return TopDocs(start, internalTotalHits < pq.Size() ? internalTotalHits : pq.Size());
        }

        public TopDocs NewTopDocs(ScoreDoc[] results, int start)
        {
            if (results == null)
                return EMPTY_TOPDOCS;

            float score;
            if (start == 0)
            {
                score = results[0].Score;
            }
            else
            {
                for (int index = pq.Size(); index > 1; --index)
                    pq.Pop();
                score = pq.Pop().Score;
            }
            return new TopDocs(internalTotalHits, results, score);
        }

        public override void SetNextReader(IndexReader reader, int base_Renamed)
        {
            docBase = base_Renamed;
        }

        public override void SetScorer(Scorer scorer)
        {
            this.scorer = scorer;
        }
        public override bool AcceptsDocsOutOfOrder
        {
            get
            {
                return false;
            }
        }

        public override void Collect(int doc)
        {
            float num1 = scorer.Score();
            int num2 = internalTotalHits + 1;
            internalTotalHits = num2;
            if ((double)num1 <= pqTop.Score)
                return;

            pqTop.Doc = doc + docBase;
            pqTop.Score = num1;
            pqTop = pq.UpdateTop();
        }

        public TopDocs TopDocs(int start, int howMany)
        {
            int num = internalTotalHits < pq.Size() ? internalTotalHits : pq.Size();
            if (start < 0 || start >= num || howMany <= 0)
                return NewTopDocs(null, start);
            howMany = Math.Min(num - start, howMany);
            ScoreDoc[] results = new ScoreDoc[howMany];
            for (int index = pq.Size() - start - howMany; index > 0; --index)
                pq.Pop();
            PopulateResults(results, howMany);
            return NewTopDocs(results, start);
        }
    }
}