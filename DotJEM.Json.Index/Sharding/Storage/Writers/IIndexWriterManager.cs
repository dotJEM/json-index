using Lucene.Net.Index;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Sharding.Storage.Writers
{
    public interface IIndexWriterManager
    {
        IJsonIndexWriter Aquire();

        bool IsOpen();
        void Close();
    }

    public class IndexWriterManager : IIndexWriterManager
    {
        private IndexWriter writer;

        private readonly object padlock = new object();
        private readonly AbstractJsonIndexStorage storage;

        public IndexWriter UnderlyingWriter
        {
            get
            {
                if (!IsOpen())
                {
                    CreateWriter();
                }
                return writer;
            }
        }

        public IndexWriterManager(AbstractJsonIndexStorage storage)
        {
            this.storage = storage;
        }

        public IJsonIndexWriter Aquire()
        {
            return new JsonIndexWriter(this);
        }

        public bool IsOpen()
        {
            try
            {
                //Note: This will call the internal "EnsureOpen" which makes sure that the writer is not closed or closing down.
                //      Otherwise it will throw an AlreadyClosedException, since the writer doesn't have any direct handles for this.
                //      we have to use this hack :(.
                lock (padlock)
                {
                    return writer?.Analyzer != null;
                }
            }
            catch (AlreadyClosedException)
            {
                return false;
            }
        }

        public void Close()
        {
            try
            {
                lock (padlock)
                {
                    writer?.Dispose();
                }
            }
            finally
            {
                storage.Unlock();
            }
        }

        private void CreateWriter()
        {
            lock (padlock)
            {
                writer = new IndexWriter(storage.Directory, storage.Analyzer, !storage.Exists, IndexWriter.MaxFieldLength.UNLIMITED);
            }
        }

        internal void Release(JsonIndexWriter jsonIndexWriter)
        {
        }
    }
}