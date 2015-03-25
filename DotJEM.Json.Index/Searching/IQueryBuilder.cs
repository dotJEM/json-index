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
        Query Build(string query);
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

        public Query Build(string query)
        {
            MultiFieldQueryParser parser = new MultiFieldQueryParser(query, index);
            parser.AllowLeadingWildcard = true;
            parser.DefaultOperator = QueryParser.Operator.AND;
            return DebugLog(parser.Parse(query));
        }

        //TODO: Remove content type.
        public Query Build(JObject queryobj, string contentType = null)
        {
            BooleanQuery query = enumarator
                .Enumerate(queryobj)
                .Where(node => node.IsLeaf)
                .Select(node => index.Configuration.Field.Strategy(contentType, node.Path).BuildQuery(node.Path, node.Token.Value<string>()))
                .Aggregate(new BooleanQuery(), (bq, q) => bq.Put(q, Occur.MUST));
            return DebugLog(query);
        }

        private Query DebugLog(Query query)
        {
            Debug.WriteLine("QUERY: " + query);
            return query;
        }
    }
}