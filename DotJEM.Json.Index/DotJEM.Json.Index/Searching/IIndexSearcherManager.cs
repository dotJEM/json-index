using System;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Util;
using Lucene.Net.Search;

namespace DotJEM.Index.Searching
{
    public interface IIndexSearcherManager : IDisposable
    {
        IIndexSearcherContext Acquire();
    }

    public class IndexSearcherManager : Disposable, IIndexSearcherManager
    {
        private readonly SearcherManager manager;

        public IndexSearcherManager(IIndexWriterManager writerManager)
        {
            manager = new SearcherManager(writerManager.Writer, true, new SearcherFactory());
        }

        public IIndexSearcherContext Acquire()
        {
            manager.MaybeRefreshBlocking();
            return new IndexSearcherContext(manager.Acquire(), searcher => manager.Release(searcher));
        }
    }
}