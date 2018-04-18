using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Contexts
{
    public class LuceneJsonMultiIndexSearcher : Disposable, ILuceneJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }
        public IInfoStream InfoStream { get; } = new InfoStream();

        private readonly ILuceneJsonIndex[] indicies;

        public LuceneJsonMultiIndexSearcher(IEnumerable<ILuceneJsonIndex> indicies)
        {
            this.indicies = indicies.ToArray();
        }

        private IIndexSearcherManager CreateaOneTimeManager()
        {
            DirectoryReader[] readers = indicies
                .Select(idx => idx.Storage.WriterManager.Writer.GetReader(true))
                .ToArray();
            return new MultiIndexJsonSearcherManager(readers);
        }

        public Search Search(Query query)
        {
            return new Search(CreateaOneTimeManager(), InfoStream, query);
        }
    }
}