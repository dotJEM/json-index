using System.Linq;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Serialization;
using DotJEM.Json.Index.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Contexts.Searching
{
    public class MultiIndexJsonSearcherManager : Disposable, IIndexSearcherManager
    {
        private readonly ILuceneJsonIndex[] indicies;
        public ILuceneJsonDocumentSerializer Serializer { get; }

        public MultiIndexJsonSearcherManager(ILuceneJsonIndex[] indicies, ILuceneJsonDocumentSerializer serializer)
        {
            this.indicies = indicies;
            Serializer = serializer;
        }

        public IIndexSearcherContext Acquire()
        {
            IndexReader[] readers = indicies
                .Select(idx => idx.WriterManager.Writer.GetReader(true))
                .Select(r => DirectoryReader.OpenIfChanged(r) ?? r)
                .Cast<IndexReader>()
                .ToArray();

            MultiReader reader = new MultiReader(readers, false);
            return new IndexSearcherContext(new IndexSearcher(reader), searcher => {});
        }

        public void Close()
        {
        }
    }
}