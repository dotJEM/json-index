using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;
using LuceneQuery = Lucene.Net.Search.Query;
namespace DotJEM.Json.Index.QueryParsers.Query
{
    public class InQuery : LuceneQuery
    {
        public override string ToString(string field)
        {
            throw new NotImplementedException();
        }
    }



    [Serializable]
    public class CustomTermQuery : LuceneQuery
    {
        private readonly Term term;
        private readonly TermContext perReaderTermState;

        public CustomTermQuery(Term t)
        {
            term = t;
            perReaderTermState = null;
        }


        public virtual Term Term => term;

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            IndexReaderContext topReaderContext = searcher.TopReaderContext;
            TermContext termStates = perReaderTermState == null || perReaderTermState.TopReaderContext != topReaderContext
                ? TermContext.Build(topReaderContext, term)
                : perReaderTermState;

            return new CustomTermWeight(this, searcher, termStates);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            terms.Add(Term);
        }

        public override string ToString(string field)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!term.Field.Equals(field, StringComparison.Ordinal))
            {
                stringBuilder.Append(term.Field);
                stringBuilder.Append(":");
            }
            stringBuilder.Append(term.Text());
            stringBuilder.Append(ToStringUtils.Boost(Boost));
            return stringBuilder.ToString();
        }

        public override bool Equals(object o)
        {
            if (!(o is CustomTermQuery))
                return false;
            CustomTermQuery termQuery = (CustomTermQuery)o;
            return Math.Abs(Boost - termQuery.Boost) < 0.00000001 && term.Equals(termQuery.term);
        }

        public override int GetHashCode()
        {
            return Boost.GetHashCode() ^ term.GetHashCode();
        }

        internal sealed class CustomTermWeight : Weight
        {
            private readonly CustomTermQuery outerInstance;
            internal readonly Similarity similarity;
            internal readonly Similarity.SimWeight stats;
            internal readonly TermContext termStates;

            public CustomTermWeight(CustomTermQuery outerInstance, IndexSearcher searcher, TermContext termStates)
            {
                this.outerInstance = outerInstance;
                this.termStates = termStates;
                similarity = searcher.Similarity;
                stats = similarity
                    .ComputeWeight(outerInstance.Boost, searcher.CollectionStatistics(outerInstance.term.Field), searcher.TermStatistics(outerInstance.term, termStates));
            }

            public override string ToString() => $"weight({outerInstance})";

            public override LuceneQuery Query => outerInstance;

            public override float GetValueForNormalization() => stats.GetValueForNormalization();

            public override void Normalize(float queryNorm, float topLevelBoost) => stats.Normalize(queryNorm, topLevelBoost);

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                TermsEnum termsEnum = GetTermsEnum(context);
                return termsEnum == null ? null : new CustomTermScorer(this, termsEnum.Docs(acceptDocs, null), similarity.GetSimScorer(stats, context));
            }

            private TermsEnum GetTermsEnum(AtomicReaderContext context)
            {
                TermState state = termStates.Get(context.Ord);
                if (state == null)
                    return null;
                TermsEnum iterator = context.AtomicReader.GetTerms(outerInstance.term.Field).GetIterator(null);
                iterator.SeekExact(outerInstance.term.Bytes, state);
                return iterator;
            }

            private bool TermNotInReader(AtomicReader reader, Term term)
            {
                return reader.DocFreq(term) == 0;
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Scorer scorer = GetScorer(context, context.AtomicReader.LiveDocs);
                if (scorer == null || scorer.Advance(doc) != doc)
                    return new ComplexExplanation(false, 0.0f, "no matching term");
                float freq = scorer.Freq;
                Similarity.SimScorer simScorer = similarity.GetSimScorer(stats, context);
                ComplexExplanation complexExplanation = new ComplexExplanation();
                complexExplanation.Description = "weight(" + Query + " in " + doc + ") [" + similarity.GetType().Name + "], result of:";
                Explanation detail = simScorer.Explain(doc, new Explanation(freq, "termFreq=" + freq));
                complexExplanation.AddDetail(detail);
                complexExplanation.Value = detail.Value;
                complexExplanation.Match = true;
                return complexExplanation;
            }
        }
    }

    internal sealed class CustomTermScorer : Scorer
    {
        private readonly DocsEnum docsEnum;
        private readonly Similarity.SimScorer docScorer;

        internal CustomTermScorer(Weight weight, DocsEnum td, Similarity.SimScorer docScorer)
          : base(weight)
        {
            this.docScorer = docScorer;
            docsEnum = td;
        }

        public override int DocID => docsEnum.DocID;

        public override int Freq => docsEnum.Freq;

        public override int NextDoc()
        {
            return docsEnum.NextDoc();
        }

        public override float GetScore()
        {
            return docScorer.Score(docsEnum.DocID, docsEnum.Freq);
        }

        public override int Advance(int target)
        {
            return docsEnum.Advance(target);
        }

        public override long GetCost()
        {
            return docsEnum.GetCost();
        }

        public override string ToString()
        {
            return "scorer(" + m_weight + ")";
        }
    }
}
