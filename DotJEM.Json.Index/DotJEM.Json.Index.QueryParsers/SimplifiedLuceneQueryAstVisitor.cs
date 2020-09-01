using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DotJEM.Json.Index.Documents.Builder;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Info;
using DotJEM.Json.Index.Documents.Strategies;
using DotJEM.Json.Index.QueryParsers.Ast;
using DotJEM.Json.Index.QueryParsers.Query;
using DotJEM.Json.Index.QueryParsers.Simplified;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Scanner.Matchers;
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

        public override string ToString()
        {
            return $"ContentTypes(" +string.Join(";", contentTypes)+")";
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


        private ContentTypeContext GetContentTypes(BaseQuery ast)
        {
            if (ast.TryGetAs("contentTypes", out string[] contentTypes))
            {
                return new ContentTypeContext(contentTypes);
            }
            return new ContentTypeContext(fields.ContentTypes.ToArray());
        }

        public override LuceneQueryInfo Visit(BaseQuery ast, ContentTypeContext context) => throw new NotImplementedException();

        public override LuceneQueryInfo Visit(OrderedQuery ast, ContentTypeContext context)
        {
            LuceneQueryInfo query = ast.Query.Accept(this, GetContentTypes(ast));
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
                // If we have a contentType context, use that.
                IIndexableJsonFieldInfo field = fields.Lookup(ast.Name);
                if (field != null)
                {
                    foreach (IIndexableFieldInfo info in field.LuceneFieldInfos)
                    {
                        //Note: in the most common scenarios, there should only be one FieldInfo here.
                    }
                }
                

                //IReadOnlyFieldinformation fieldInfo = fields.Lookup(ast.Name);
                //foreach (IFieldMetaData metaData in fieldInfo.MetaData)
                //{
                //}
            }
            else
            {
                // Calculate relevant fields from context.
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
                        //new ExpandedDateTimeFieldStrategy().CreateQuery(new )

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

        private LuceneQueryInfo VisitComposite(CompositeQuery ast, ContentTypeContext context, Occur occur)
        {
            BooleanQuery query = new BooleanQuery();
            foreach (BaseQuery child in ast.Queries)
            {
                //TODO: Branch context.
                query.Add(child.Accept(this, context).Query, occur);
            }
            return query;

        }

        public override LuceneQueryInfo Visit(OrQuery ast, ContentTypeContext context)
        {
            //TODO: Branch context.
            return VisitComposite(ast, GetContentTypes(ast), Occur.SHOULD);
        }

        public override LuceneQueryInfo Visit(AndQuery ast, ContentTypeContext context)
        {
            return VisitComposite(ast, GetContentTypes(ast), Occur.MUST);
        }

        public override LuceneQueryInfo Visit(ImplicitCompositeQuery ast, ContentTypeContext context)
        {
            throw new NotSupportedException("ImplicitCompositeQuery not supported by SimplifiedLuceneQueryAstVisitor, use an optimizer to remove these.");
            //field: x field2:y -> implicit AND (Make configureable)
            //return VisitComposite(ast, () => context, Occur.MUST);
        }

        public override LuceneQueryInfo Visit(NotQuery ast, ContentTypeContext context)
        {
            LuceneQuery not = ast.Not.Accept(this, GetContentTypes(ast)).Query;

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

            attribute = default;
            return false;
        }
    }

}