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
        private readonly SearcherManager manager;

        public ILuceneJsonDocumentSerializer Serializer { get; }

        public IndexSearcherManager(IIndexWriterManager writerManager, ILuceneJsonDocumentSerializer serializer)
        {
            Serializer = serializer;
            manager = new SearcherManager(writerManager.Writer, true, new SearcherFactory());
        }

        public IIndexSearcherContext Acquire()
        {
            manager.MaybeRefreshBlocking();
            return new IndexSearcherContext(manager.Acquire(), searcher => manager.Release(searcher));
        }
    }
}