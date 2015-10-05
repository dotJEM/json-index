using Lucene.Net.Index;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Sharding.Storage
{
    public interface IIndexStorage
    {
        IndexReader Reader { get; }
        IndexWriter Writer { get; }
    }

    public class MemmoryIndexStorage : IIndexStorage
    {
        private Directory directory;

        public IndexReader Reader { get; }
        public IndexWriter Writer { get; }

        public MemmoryIndexStorage(Directory directory)
        {
            this.directory = directory;
        }
    }
}