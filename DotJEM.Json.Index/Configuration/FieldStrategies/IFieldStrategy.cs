using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotJEM.Json.Index.Configuration.FieldStrategies.Querying;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Visitors;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.FieldStrategies
{
    public interface IFieldStrategy
    {
        Query BuildQuery(string path, string value);

        IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type);

        IIndexingVisitorStrategy IndexingStrategy { get; }
    }

    public class FieldStrategy : IFieldStrategy
    {
        public virtual IIndexingVisitorStrategy IndexingStrategy => new DefaultIndexingVisitorStrategy();

        public virtual IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type)
        {
            return new FieldQueryBuilder(parser, fieldName, type);
        }

        //NOTE: This is temporary for now.
        private static readonly char[] delimiters = " ".ToCharArray();
        public virtual Query BuildQuery(string field, string value)
        {
            value = value.ToLowerInvariant();
            string[] words = value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (!words.Any())
                return null;

            BooleanQuery query = new BooleanQuery();
            foreach (string word in words)
            {
                //Note: As for the WildcardQuery, we only add the wildcard to the end for performance reasons.
                query.Add(new FuzzyQuery(new Term(field, word)), Occur.SHOULD);
                query.Add(new WildcardQuery(new Term(field, word + "*")), Occur.SHOULD);
            }
            return query;
        }

    }

    public class NullFieldStrategy : FieldStrategy
    {
        public override IIndexingVisitorStrategy IndexingStrategy => new NullFieldIndexingVisitorStrategy();
    }

    public class TermFieldStrategy : FieldStrategy
    {
        public override IIndexingVisitorStrategy IndexingStrategy => new TermFieldIndexingVisitorStrategy();

        public override Query BuildQuery(string field, string value)
        {
            return new TermQuery(new Term(field, value));
        }

        //TODO: Select Builder implementation pattern instead.
        public override IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type)
        {
            return new TermFieldQueryBuilder(parser, fieldName, type);
        }
    }

    public class NumericFieldStrategy : FieldStrategy
    {
        
    }

}