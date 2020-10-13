using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Util;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Contexts.Searching
{
    public class LuceneJsonMultiIndexSearcher : Disposable, ILuceneJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }

        public IEventInfoStream InfoStream { get; } = EventInfoStream.Default.Bind<LuceneJsonMultiIndexSearcher>();

        private readonly ILuceneJsonIndex[] indicies;

        public LuceneJsonMultiIndexSearcher(IEnumerable<ILuceneJsonIndex> indicies)
        {
            this.indicies = indicies.ToArray();
        }

        public ISearch Search(Query query)
        {
            return new Search(new MultiIndexJsonSearcherManager(indicies, null), InfoStream, query);
        }
    }
}