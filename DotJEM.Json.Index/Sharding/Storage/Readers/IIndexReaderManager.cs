using Lucene.Net.Index;

namespace DotJEM.Json.Index.Sharding.Storage.Readers
{
    public interface IIndexReaderManager
    {
        IJsonIndexReader Aquire();

        void ReOpen();
        void Close();
    }

    public class IndexReaderManager : IIndexReaderManager
    {
        private IndexReader reader;

        private readonly object padlock = new object();
        private readonly AbstractJsonIndexStorage storage;

        public IndexReader UnderlyingReader
        {
            get
            {
                if (reader == null)
                {
                    CreateReader();
                }
                return reader;
            }
        }

        public IndexReaderManager(AbstractJsonIndexStorage storage)
        {
            this.storage = storage;
        }

        public IJsonIndexReader Aquire()
        {
            return new JsonIndexReader(this);
        }

        public void ReOpen()
        {
            lock (padlock)
            {
                reader = reader?.Reopen();
            }
        }

        public void Close()
        {
            lock (padlock)
            {
                reader?.Dispose();
            }
        }

        private void CreateReader()
        {
            lock (padlock)
            {
                reader = IndexReader.Open(storage.Directory,true);
            }
        }

        internal void Release(JsonIndexReader jsonIndexWriter)
        {
        }
    }
}