using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    public abstract class AbstractJsonIndexStorage : IJsonIndexStorage
    {
        private readonly ILuceneJsonIndex index;
        private readonly object padlock = new object();

        private Directory directory;

        public IIndexWriterManager WriterManager { get; }
        public IIndexSearcherManager SearcherManager { get; }

        public bool Exists => DirectoryReader.IndexExists(Directory);

        public Directory Directory
        {
            get
            {
                if (directory != null)
                    return directory;

                lock (padlock)
                {
                    return directory ??= Create();
                }
            }
            protected set => directory = value;
        }

        protected AbstractJsonIndexStorage(ILuceneJsonIndex index)
        {
            this.index = index;
            WriterManager = new IndexWriterManager(index);
            SearcherManager = new IndexSearcherManager(WriterManager, index.Services.Resolve<ILuceneJsonDocumentSerializer>());
        }

        public void Unlock()
        {
            if (IndexWriter.IsLocked(Directory))
                IndexWriter.Unlock(Directory);
        }
        
        public void Close()
        {
            SearcherManager.Close();
            WriterManager.Close();
        }

        public abstract void Delete();
        protected abstract Directory Create();
    }
}