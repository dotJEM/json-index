using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotJEM.Json.Index.Schema;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;
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
            QueryParser parser = new MultiFieldQueryParser(Version.LUCENE_30, index.Analyzer, index);
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
}