using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.Documents.Strategies;
using DotJEM.Json.Index.QueryParsers.Simplified;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Support;
using Lucene.Net.Util;
using LuceneQuery = Lucene.Net.Search.Query;

namespace DotJEM.Json.Index.QueryParsers
{
    public class ContentTypeContext
    {
        private readonly List<string> contentTypes;

        public IEnumerable<string> ContentTypes => contentTypes.AsReadOnly();

        public ContentTypeContext(IEnumerable<string> enumerable)
        {
            this.contentTypes = enumerable.ToList();
        }
    }

    public class SimplifiedLuceneQueryAstVisitor : SimplifiedQueryAstVisitor<LuceneQueryInfo, ContentTypeContext>
    {
        private readonly Analyzer analyzer;
        private readonly IFieldInformationManager fields;

        public SimplifiedLuceneQueryAstVisitor(IFieldInformationManager fields, Analyzer analyzer)
        {
            this.fields = fields;
            this.analyzer = analyzer;
        }

        public override LuceneQueryInfo Visit(BaseQuery ast, ContentTypeContext context) => throw new NotImplementedException();

        public override LuceneQueryInfo Visit(OrderedQuery ast, ContentTypeContext context)
        {
            LuceneQueryInfo query = ast.Query.Accept(this, context);
            LuceneQueryInfo order = ast.Ordering?.Accept(this, context);
            return new LuceneQueryInfo(query.Query, order?.Sort);
        }

        public override LuceneQueryInfo Visit(OrderBy ast, ContentTypeContext context)
        {
            IEnumerable<SortField> sort = ast.OrderFields
                .SelectMany(of => of.Accept(this, context).Sort.GetSort());
            return new LuceneQueryInfo(null, new Sort(sort.ToArray()));
        }

        public override LuceneQueryInfo Visit(OrderField ast, ContentTypeContext context)
        {
            return new LuceneQueryInfo(null, new Sort(new SortField(ast.Name, SortFieldType.INT64, ast.SpecifiedOrder == FieldOrder.Descending)));
        }

        public override LuceneQueryInfo Visit(FieldQuery ast, ContentTypeContext context)
        {
            BooleanQuery query = new BooleanQuery(true);

            if (ast.Name != null)
            {
                IReadOnlyFieldinformation fieldInfo = fields.Lookup(ast.Name);
                foreach (IFieldMetaData metaData in fieldInfo.MetaData)
                {
                }
            }


            switch (ast.Operator)
            {
                case FieldOperator.None:
                case FieldOperator.Equals:
                    query.Add(CreateSimpleQuery(ast.Name, ast.Value), Occur.MUST);
                    break;

                case FieldOperator.NotEquals:
                    query.Add(CreateSimpleQuery(ast.Name, ast.Value), Occur.MUST_NOT);
                    break;

                case FieldOperator.GreaterThan:
                    query.Add(CreateGreaterThanQuery(ast.Name, ast.Value, false), Occur.MUST);
                    break;

                case FieldOperator.GreaterThanOrEquals:
                    query.Add(CreateGreaterThanQuery(ast.Name, ast.Value, true), Occur.MUST);
                    break;

                case FieldOperator.LessThan:
                    query.Add(CreateLessThanQuery(ast.Name, ast.Value, false), Occur.MUST);
                    break;

                case FieldOperator.LessThanOrEquals:
                    query.Add(CreateLessThanQuery(ast.Name, ast.Value, true), Occur.MUST);
                    break;

                case FieldOperator.Similar:
                    query.Add(CreateFuzzyQuery(ast.Name, ast.Value), Occur.MUST);
                    break;

                case FieldOperator.NotSimilar:
                    query.Add(CreateFuzzyQuery(ast.Name, ast.Value), Occur.MUST_NOT);
                    break;

                case FieldOperator.In:
                    if (!(ast.Value is ListValue inList))
                        throw new Exception();

                    BooleanQuery inClause = new BooleanQuery();
                    foreach (Value value in inList.Values)
                        inClause.Add(CreateSimpleQuery(ast.Name, value), Occur.SHOULD);
                    query.Add(inClause, Occur.MUST);
                    break;

                case FieldOperator.NotIt:
                    if (!(ast.Value is ListValue notInList))
                        throw new Exception();

                    query.Add(new MatchAllDocsQuery(), Occur.MUST);
                    BooleanQuery notInClause = new BooleanQuery();
                    foreach (Value value in notInList.Values)
                        notInClause.Add(CreateSimpleQuery(ast.Name, value), Occur.SHOULD);
                    query.Add(notInClause, Occur.MUST_NOT);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            LuceneQuery CreateLessThanQuery(string field, Value val, bool inclusive)
            {
                switch (val)
                {
                    case DateTimeValue dateTimeValue:
                        //new ExpandedDateTimeFieldStrategy().CreateQuery()

                        return NumericRangeQuery.NewInt64Range(field + ".@ticks", null, dateTimeValue.Value.Ticks, inclusive, inclusive);

                    case OffsetDateTime offsetDateTime:
                        return NumericRangeQuery.NewInt64Range(field + ".@ticks", null, offsetDateTime.Value.Ticks, inclusive, inclusive);

                    case NumberValue numberValue:
                        return NumericRangeQuery.NewDoubleRange(field, null, numberValue.Value, inclusive, inclusive);

                    case IntegerValue integerValue:
                        return NumericRangeQuery.NewInt64Range(field, null, integerValue.Value, inclusive, inclusive);

                    case StringValue stringValue:
                        return TermRangeQuery.NewStringRange(field, null, stringValue.Value, inclusive, inclusive);
                }
                throw new ArgumentOutOfRangeException();
            }

            LuceneQuery CreateGreaterThanQuery(string field, Value val, bool inclusive)
            {
                //TODO: fix...
                field = field ?? "gender";
                switch (val)
                {
                    case DateTimeValue dateTimeValue:
                        return NumericRangeQuery.NewInt64Range(field + ".@ticks", dateTimeValue.Value.Ticks, null, inclusive, inclusive);

                    case OffsetDateTime offsetDateTime:
                        return NumericRangeQuery.NewInt64Range(field + ".@ticks", offsetDateTime.Value.Ticks, null, inclusive, inclusive);

                    case NumberValue numberValue:
                        return NumericRangeQuery.NewDoubleRange(field, numberValue.Value, null, inclusive, inclusive);

                    case IntegerValue integerValue:
                        return NumericRangeQuery.NewInt64Range(field, integerValue.Value, null, inclusive, inclusive);


                    case StringValue stringValue:
                        return TermRangeQuery.NewStringRange(field, stringValue.Value, null, inclusive, inclusive);

                    
                }
                throw new ArgumentOutOfRangeException();
            }

            LuceneQuery CreateFuzzyQuery(string field, Value val)
            {
                switch (val)
                {
                    case MatchAllValue _:
                        return new WildcardQuery(new Term(field, "*"));
                    case WildcardValue wildcardValue:
                        return new FuzzyQuery(new Term(field, wildcardValue.Value));
                    case StringValue stringValue:
                        return new FuzzyQuery(new Term(ast.Name, stringValue.Value));
                }
                throw new ArgumentOutOfRangeException();
            }

            LuceneQuery CreateSimpleQuery(string field, Value val)
            {
                field = field ?? "gender";
                switch (val)
                {
                    case MatchAllValue _:
                        return new WildcardQuery(new Term(field, "*"));
                    case NumberValue numberValue:
                        return NumericRangeQuery.NewDoubleRange(field, numberValue.Value, numberValue.Value, true, true);
                    case IntegerValue integerValue:
                        return NumericRangeQuery.NewInt64Range(field, integerValue.Value, integerValue.Value, true, true);
                    case PhraseValue phraseValue:

                        TokenStream source = analyzer.GetTokenStream(field, new StringReader(phraseValue.Value));
                        source.Reset();
                        CachingTokenFilter buffer = new CachingTokenFilter(source);
                        buffer.Reset();

                        PhraseQuery phrase = new PhraseQuery();
                        phrase.Slop = 5;
                        if (buffer.TryGetAttribute(out ITermToBytesRefAttribute attribute))
                        {
                            buffer.TryGetAttribute(out IPositionIncrementAttribute position);
                            try
                            {
                                int pos = 0;
                                while (buffer.IncrementToken())
                                {
                                    pos += position.PositionIncrement;
                                    attribute.FillBytesRef();
                                    phrase.Add(new Term(field, BytesRef.DeepCopyOf(attribute.BytesRef)), pos);
                                }
                            }
                            catch (Exception)
                            {
                                // ignore
                            }
                        }
                        source.Dispose();
                        buffer.Dispose();

                        return phrase;
                    case WildcardValue wildcardValue:
                        return new WildcardQuery(new Term(field, wildcardValue.Value));
                    case StringValue stringValue:
                        return new CustomTermQuery(new Term(field, stringValue.Value));
                }
                throw new ArgumentOutOfRangeException();
            }


            return query;
        }

        private LuceneQueryInfo VisitComposite(CompositeQuery ast, Func<ContentTypeContext> contextProvider, Occur occur)
        {
            BooleanQuery query = new BooleanQuery();
            foreach (BaseQuery child in ast.Queries)
            {
                //TODO: Branch context.
                query.Add(child.Accept(this, contextProvider()).Query, occur);
            }
            return query;

        }

        public override LuceneQueryInfo Visit(OrQuery ast, ContentTypeContext context)
        {
            //TODO: Branch context.
            return VisitComposite(ast, () => context, Occur.SHOULD);
        }

        public override LuceneQueryInfo Visit(AndQuery ast, ContentTypeContext context)
        {
            if (ast.TryGetAs("contentTypes", out string[] contentTypes))
            {
                context = new ContentTypeContext(contentTypes);
            }
            return VisitComposite(ast, () => context, Occur.MUST);
        }

        public override LuceneQueryInfo Visit(ImplicitCompositeQuery ast, ContentTypeContext context)
        {
            throw new NotSupportedException("ImplicitCompositeQuery not supported by SimplifiedLuceneQueryAstVisitor, use an optimizer to remove these.");
            //field: x field2:y -> implicit AND (Make configureable)
            //return VisitComposite(ast, () => context, Occur.MUST);
        }

        public override LuceneQueryInfo Visit(NotQuery ast, ContentTypeContext context)
        {
            LuceneQuery not = ast.Not.Accept(this, context).Query;

            BooleanQuery query = new BooleanQuery(true);
            query.Add(new BooleanClause(not, Occur.MUST_NOT));
            return query;
        }


    }

    public static class AttributeSourceExtensions
    {
        public static bool TryGetAttribute<TAttribute>(this AttributeSource self, out TAttribute attribute) where TAttribute : IAttribute
        {
            if (self.HasAttribute<TAttribute>())
            {
                attribute = self.GetAttribute<TAttribute>();
                return true;
            }

            attribute = default(TAttribute);
            return false;
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
            return Number.SingleToInt32Bits(Boost) ^ term.GetHashCode();
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