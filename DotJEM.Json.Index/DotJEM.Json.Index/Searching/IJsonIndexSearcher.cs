using System;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Util;
using Lucene.Net.Search;

namespace DotJEM.Index.Searching
{
    public interface IJsonIndexSearcher : IDisposable
    {
        ILuceneJsonIndex Index { get; }

        Search Search(Query query);
    }

    public class JsonIndexSearcher : Disposable, IJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }

        private readonly IIndexSearcherManager manager;

        public JsonIndexSearcher(ILuceneJsonIndex index, IIndexSearcherManager manager)
        {
            Index = index;
            this.manager = manager;
        }

        public Search Search(Query query)
        {
            return new Search(manager, query);
        }
    }
}