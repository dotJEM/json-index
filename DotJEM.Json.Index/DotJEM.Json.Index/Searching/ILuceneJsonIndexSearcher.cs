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
        IInfoEventStream InfoStream { get; }
        Search Search(Query query);
    }

    public class LuceneJsonIndexSearcher : Disposable, ILuceneJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }
        public IInfoEventStream InfoStream { get; } = InfoEventStream.DefaultStream.Bind<LuceneJsonIndexSearcher>();

        private readonly IIndexSearcherManager manager;

        public LuceneJsonIndexSearcher(ILuceneJsonIndex index, IIndexSearcherManager manager)
        {
            Index = index;
            this.manager = manager;
        }

        public Search Search(Query query)
        {
            //TODO: Retrieve InfoEventStream
            return new Search(manager, InfoStream, query);
        }
    }
}