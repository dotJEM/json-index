using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotJEM.Json.Index.Configuration;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;

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
        private readonly IJObjectEnumarator enumarator;
        private readonly IIndexConfiguration configuration;

        public LuceneQueryBuilder(IIndexConfiguration configuration) 
            : this(configuration, new JObjectEnumerator())
        {
        }

        public LuceneQueryBuilder(IIndexConfiguration configuration, IJObjectEnumarator enumarator)
        {
            this.enumarator = enumarator;
            this.configuration = configuration;
        }

        public Query Build(string querytext)
        {
            QueryParser parser = new QueryParser(Version.LUCENE_30, "*", new StandardAnalyzer(Version.LUCENE_30));
            Query query = parser.Parse(querytext);
            Debug.WriteLine("QUERY: " + query);
            return query;
        }

        public Query Build(string querytext, IEnumerable<string> fields, string contentType)
        {
            BooleanQuery query = new BooleanQuery();
            foreach (Query q in from field in fields
                                let strategy = configuration.Query.Strategy(contentType, field)
                                select strategy.Create(field, querytext)
                                    into q
                                    where q != null
                                    select q)
            {
                query.Add(q, Occur.SHOULD);
            }
            Debug.WriteLine("QUERY: " + query);
            return query;
        }

        public Query Build(JObject queryobj, string contentType = null)
        {
            //TODO: Use MUST
            BooleanQuery query = new BooleanQuery();
            foreach (Query q in enumarator.Flatten(queryobj, 
                (field, value) => configuration.Query.Strategy(contentType, field).Create(field,value.Value<string>())))
            {
                query.Add(q, Occur.MUST);
            }
            Debug.WriteLine("QUERY: " + query);
            return query;
        }
    }
}