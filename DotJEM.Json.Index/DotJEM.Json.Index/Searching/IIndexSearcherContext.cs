using System;
using DotJEM.Json.Index.Util;
using Lucene.Net.Search;

namespace DotJEM.Index.Searching
{
    public interface IIndexSearcherContext : IDisposable
    {
        IndexSearcher Searcher { get; }
    }

    public class IndexSearcherContext : Disposable, IIndexSearcherContext
    {
        private readonly Action<IndexSearcher> release;

        public IndexSearcher Searcher { get; }

        public IndexSearcherContext(IndexSearcher searcher, Action<IndexSearcher> release)
        {
            this.Searcher = searcher;
            this.release = release;
        }

        protected override void Dispose(bool disposing)
        {
            release(Searcher);

            base.Dispose(disposing);
        }
    }
}