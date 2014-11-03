using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index.Searching
{
    public interface IQueryBuilder
    {
        //TODO: encapsulate Query object so Lucene isn't a core dependency.
        Query Build(string querytext);
        Query Build(string querytext, IEnumerable<string> fields, string contentType = null);
        Query Build(JObject queryobj, string contentType = null);
    }

    public class LuceneQueryBuilder : IQueryBuilder
    {
        private readonly IStorageIndex index;
        private readonly IJObjectEnumarator enumarator;

        public LuceneQueryBuilder(IStorageIndex index)
            : this(index, new JObjectEnumerator())
        {
        }

        public LuceneQueryBuilder(IStorageIndex index, IJObjectEnumarator enumarator)
        {
            this.index = index;
            this.enumarator = enumarator;
        }

        public Query Build(string querytext)
        {
            QueryParser parser = new MultiFieldQueryParser(Version.LUCENE_30, index.Fields.AllFields().ToArray(), new StandardAnalyzer(Version.LUCENE_30));
            parser.AllowLeadingWildcard = true;
            parser.DefaultOperator = QueryParser.Operator.AND;

            Query query = parser.Parse(querytext);
            Debug.WriteLine("QUERY: " + query);
            return query;
        }

        //TODO: Get rid of...
        public Query Build(string querytext, IEnumerable<string> fields, string contentType)
        {
            BooleanQuery query = new BooleanQuery();
            foreach (Query q in from field in fields
                                let strategy = index.Configuration.Query.Strategy(contentType, field)
                                select strategy.Create(field, querytext) into q where q != null select q)
            {
                query.Add(q, Occur.SHOULD);
            }
            Debug.WriteLine("QUERY: " + query);
            return query;
        }

        //TODO: Remove content type.
        public Query Build(JObject queryobj, string contentType = null)
        {
            BooleanQuery query = enumarator
                .Enumerate(queryobj)
                .Where(node => node.IsLeaf)
                .Select(node => index.Configuration.Query.Strategy(contentType, node.Path).Create(node.Path, node.Token.Value<string>()))
                .Aggregate(new BooleanQuery(), (bq, q) => bq.Put(q, Occur.MUST));

            Debug.WriteLine("QUERY: " + query);
            return query;
        }
    }

    public class MultiFieldQueryParser : QueryParser
    {
        private readonly string[] fields;

        public MultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer)
            : base(matchVersion, null, analyzer)
        {
            this.fields = fields;
        }

        protected override Query GetFieldQuery(string field, string queryText, int slop)
        {
            Query fieldQuery;
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                foreach (string t in fields)
                {
                    fieldQuery = base.GetFieldQuery(t, queryText);
                    if (fieldQuery == null) continue;
                    ApplySlop(fieldQuery, slop);
                    clauses.Add(new BooleanClause(fieldQuery, Occur.SHOULD));
                }
                return clauses.Count == 0 ? null : GetBooleanQuery(clauses, true);
            }
            fieldQuery = base.GetFieldQuery(field, queryText);
            ApplySlop(fieldQuery, slop);
            return fieldQuery;
        }

        private static bool ApplySlop(PhraseQuery query, int slop)
        {
            if (query != null) query.Slop = slop;
            return query !=null;
        }

        private static bool ApplySlop(MultiPhraseQuery query, int slop)
        {
            if (query != null) query.Slop = slop;
            return query != null;
        }

        // ReSharper disable UnusedMethodReturnValue.Local
        private static bool ApplySlop(Query q, int slop)
        {
            return ApplySlop(q as PhraseQuery, slop) || ApplySlop(q as MultiPhraseQuery, slop);
        }
        // ReSharper restore UnusedMethodReturnValue.Local

        protected override Query GetFieldQuery(string field, string queryText)
        {
            return GetFieldQuery(field, queryText, 0);
        }

        protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (field != null)
                return base.GetFuzzyQuery(field, termStr, minSimilarity);
            IList<BooleanClause> clauses = fields.Select(t => new BooleanClause(GetFuzzyQuery(t, termStr, minSimilarity), Occur.SHOULD)).ToList();
            return GetBooleanQuery(clauses, true);
        }

        protected override Query GetPrefixQuery(string field, string termStr)
        {
            if (field != null)
                return base.GetPrefixQuery(field, termStr);
            IList<BooleanClause> clauses = fields.Select(t => new BooleanClause(GetPrefixQuery(t, termStr), Occur.SHOULD)).ToList();
            return GetBooleanQuery(clauses, true);
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            if (field != null)
                return base.GetWildcardQuery(field, termStr);
            IList<BooleanClause> clauses = fields.Select(t => new BooleanClause(GetWildcardQuery(t, termStr), Occur.SHOULD)).ToList();
            return GetBooleanQuery(clauses, true);
        }

        protected override Query GetRangeQuery(string field, string part1, string part2, bool inclusive)
        {
            if (field != null)
                return base.GetRangeQuery(field, part1, part2, inclusive);
            IList<BooleanClause> clauses = fields.Select(t => new BooleanClause(GetRangeQuery(t, part1, part2, inclusive), Occur.SHOULD)).ToList();
            return GetBooleanQuery(clauses, true);
        }
    }
}