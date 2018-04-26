using System.Linq;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Contexts.Searching
{
    public class MultiIndexJsonSearcherManager : Disposable, IIndexSearcherManager
    {
        private readonly DirectoryReader[] readers;

        public MultiIndexJsonSearcherManager(DirectoryReader[] readers)
        {
            this.readers = readers;
        }

        public IIndexSearcherContext Acquire()
        {
            MultiReader reader = new MultiReader(readers.Select(r => DirectoryReader.OpenIfChanged(r) ?? r).Cast<IndexReader>().ToArray(), false);
            return new IndexSearcherContext(new IndexSearcher(reader), searcher => {});
        }

        //private readonly SearcherManager manager;


        //public IndexSearcherManager(IIndexWriterManager writerManager)
        //{
        //    manager = new SearcherManager(writerManager.Writer, true, new SearcherFactory());
        //}

        //public IIndexSearcherContext Acquire()
        //{
        //    manager.MaybeRefreshBlocking();
        //    return new IndexSearcherContext(manager.Acquire(), searcher => manager.Release(searcher));
        //}
    }
}