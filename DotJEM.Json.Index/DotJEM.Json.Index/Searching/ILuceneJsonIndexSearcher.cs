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
        IEventInfoStream InfoStream { get; }
        ISearch Search(Query query);
    }

    public class LuceneJsonIndexSearcher : Disposable, ILuceneJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }
        public IEventInfoStream InfoStream { get; } = EventInfoStream.Default.Bind<LuceneJsonIndexSearcher>();

        public LuceneJsonIndexSearcher(ILuceneJsonIndex index)
        {
            Index = index;
        }

        public ISearch Search(Query query)
        {
            return new Search(Index.SearcherManager, InfoStream, query);
        }
    }

    public static class IndexSearcherExtensions
    {
        public static ISearch Search(this ILuceneJsonIndex self, Query query)
        {
            return self.CreateSearcher().Search(query);
        }
    }
}