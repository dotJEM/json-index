using System;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Serialization;
using DotJEM.Json.Index.Util;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Searching
{
    public interface IIndexSearcherManager : IDisposable
    {
        ILuceneJsonDocumentSerializer Serializer { get; }
        IIndexSearcherContext Acquire();
    }

    public class IndexSearcherManager : Disposable, IIndexSearcherManager
    {
        private readonly ResetableLazy<SearcherManager> manager;
        
        public ILuceneJsonDocumentSerializer Serializer { get; }

        public IndexSearcherManager(IIndexWriterManager writerManager, ILuceneJsonDocumentSerializer serializer)
        {
            Serializer = serializer;
            manager = new ResetableLazy<SearcherManager>(() => new SearcherManager(writerManager.Writer, true, new SearcherFactory()));
            writerManager.OnClose += (sender, args) => manager.Reset();
        }

        public IIndexSearcherContext Acquire()
        {
            manager.Value.MaybeRefreshBlocking();
            return new IndexSearcherContext(manager.Value.Acquire(), searcher => manager.Value.Release(searcher));
        }
    }
}