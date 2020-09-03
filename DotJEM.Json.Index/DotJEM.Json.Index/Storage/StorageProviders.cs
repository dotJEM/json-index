using System.IO;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Storage
{
    public interface ILuceneStorageFactory
    {
        IJsonIndexStorage Create(ILuceneJsonIndex index, LuceneVersion version);
    }

    public class LuceneRamStorageFactory : ILuceneStorageFactory
    {
        public IJsonIndexStorage Create(ILuceneJsonIndex index, LuceneVersion version) => new RamJsonIndexStorage(index);
    }

    public class LuceneSimpleFileSystemStorageFactory : ILuceneStorageFactory
    {
        private readonly string path;
        public LuceneSimpleFileSystemStorageFactory(string path) => this.path = path;
        public IJsonIndexStorage Create(ILuceneJsonIndex index, LuceneVersion version) => new SimpleFSJsonIndexStorage(index, path);
    }


    public interface IJsonIndexStorage
    {
        bool Exists { get; }
        LuceneDirectory Directory { get; }
        IIndexWriterManager WriterManager { get; }
        IIndexSearcherManager SearcherManager { get; }
        void Unlock();
        void Delete();
        void Close();
    }

    public abstract class JsonIndexStorage : IJsonIndexStorage
    {
        private readonly ILuceneJsonIndex index;
        private readonly object padlock = new object();

        private LuceneDirectory directory;

        public IIndexWriterManager WriterManager { get; }
        public IIndexSearcherManager SearcherManager { get; }

        public bool Exists => DirectoryReader.IndexExists(Directory);

        public LuceneDirectory Directory
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

        protected JsonIndexStorage(ILuceneJsonIndex index)
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
        protected abstract LuceneDirectory Create();
    }

    public class RamJsonIndexStorage : JsonIndexStorage
    {
        public RamJsonIndexStorage(ILuceneJsonIndex index) : base(index)
        {
        }

        protected override LuceneDirectory Create()
        {
            return new RAMDirectory();
        }

        public override void Delete()
        {
            Close();
            Unlock();
            this.Directory.Dispose();
            this.Directory = null;
        }
    }

    public class SimpleFSJsonIndexStorage : JsonIndexStorage
    {
        private readonly string path;

        public SimpleFSJsonIndexStorage(ILuceneJsonIndex index, string path) : base(index)
        {
            this.path = path;
        }

        protected override LuceneDirectory Create()
        {
            return new SimpleFSDirectory(path);
        }

        public override void Delete()
        {
            Close();
            Unlock();
            this.Directory.Dispose();
            this.Directory = null;
            
            DirectoryInfo dir = new DirectoryInfo(path);
            if(!dir.Exists)
                return;

            foreach (FileInfo fileInfo in dir.GetFiles())
                fileInfo.Delete();
        }
    }
}
