using System;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Util;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Searching
{
    public interface ILuceneJsonIndexSearcher : IDisposable
    {
        ILuceneJsonIndex Index { get; }
        IInfoStream InfoStream { get; }
        Search Search(Query query);
    }

    public class LuceneJsonIndexSearcher : Disposable, ILuceneJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }
        public IInfoStream InfoStream { get; } = new InfoStream();

        private readonly IIndexSearcherManager manager;

        public LuceneJsonIndexSearcher(ILuceneJsonIndex index, IIndexSearcherManager manager)
        {
            Index = index;
            this.manager = manager;
        }

        public Search Search(Query query)
        {
            //TODO: Retrieve InfoStream
            return new Search(manager, InfoStream, query);
        }
    }
}