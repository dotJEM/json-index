using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Contexts.Searching
{
    public class LuceneJsonMultiIndexSearcher : Disposable, ILuceneJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }

        public IInfoEventStream InfoStream { get; } = new InfoEventStream();

        private readonly ILuceneJsonIndex[] indicies;

        public LuceneJsonMultiIndexSearcher(IEnumerable<ILuceneJsonIndex> indicies)
        {
            this.indicies = indicies.ToArray();
        }

        public Search Search(Query query)
        {
            return new Search(new MultiIndexJsonSearcherManager(indicies), InfoStream, query);
        }
    }
}