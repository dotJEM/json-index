﻿using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Searching
{
    public interface ILuceneSearcher
    {
        ISearchResult Search(Query query);
        ISearchResult Search(string query);
        ISearchResult Search(JObject query, string contentType = "");

        IEnumerable<string> Terms(string field);
    }

    public class LuceneSearcher : ILuceneSearcher
    {
        private readonly IStorageIndex index;
        private readonly IQueryBuilder queryBuilder;

        public LuceneSearcher(IStorageIndex index)
            : this(index, new LuceneQueryBuilder(index))
        {
        }

        public LuceneSearcher(IStorageIndex index, IQueryBuilder queryBuilder)
        {
            this.index = index;
            this.queryBuilder = queryBuilder;
        }

        public ISearchResult Search(Query query)
        {
            var something = new SearchResultCollector(query, index);
            return something;
        }

        public ISearchResult Search(string query)
        {
            return Search(queryBuilder.Build(query));
        }

        public ISearchResult Search(JObject query, string contentType = "")
        {
            return Search(queryBuilder.Build(query, contentType));
        }

        public IEnumerable<string> Terms(string field)
        {
            if (!index.Storage.Exists)
                yield break;


            yield break;

            //IndexReader reader = index.Storage.OpenReader();
            //TermEnum terms = reader.Terms(new Term(field));
            //do
            //{
            //    if (terms.Term.Field != field)
            //        yield break;
            //    yield return terms.Term.Text;
            //} while (terms.Next());
        }
    }
}