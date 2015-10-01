using System.IO;
using System.Linq;
using DotJEM.Json.Index.Storage;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index
{
    public interface IIndexStorage
    {
        IndexReader OpenReader();
        IndexWriter GetWriter(Analyzer analyzer);
        bool Exists { get; }
        void Close();
        void Flush();
    }

    public abstract class AbstractLuceneIndexStorage : IIndexStorage
    {
        private readonly object padlock = new object();

        protected Directory Directory { get; private set; }
        public virtual bool Exists => Directory.ListAll().Any();

        private IndexWriter writer;

        protected AbstractLuceneIndexStorage(Directory directory)
        {
            Directory = directory;
        }

        public IndexWriter GetWriter(Analyzer analyzer)
        {
            //TODO: The storage should define the analyzer, not the writer.
            lock (padlock)
            {
                return writer ?? (writer = new IndexWriter(Directory, analyzer, !Exists, IndexWriter.MaxFieldLength.UNLIMITED));
            }
        }

        public IndexReader OpenReader()
        {
            return Exists ? IndexReader.Open(Directory, true) : null;
        }

        public void Close()
        {
            lock (padlock)
            {
                writer?.Dispose();
                writer = null;
            }
        }

        public void Flush()
        {
            lock (padlock)
            {
                writer?.Flush(true, true, true);
            }
        }
    }

    public class LuceneMemmoryIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneMemmoryIndexStorage()
            : base(new RAMDirectory())
        {
        }
    }

    public class LuceneFileIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneFileIndexStorage(string path)
            //Note: Ensure Directory.
            : base(FSDirectory.Open(System.IO.Directory.CreateDirectory(path).FullName))
        {
        }
    }

    public class LuceneCachedMemmoryIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneCachedMemmoryIndexStorage(string path)
            //Note: Ensure cacheDirectory.
            : base(new MemoryCachedDirective(System.IO.Directory.CreateDirectory(path).FullName))
        {
        }
    }

    public class LuceneMemmoryMappedFileIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneMemmoryMappedFileIndexStorage(string path)
            //Note: Ensure cacheDirectory.
            : base(new MMapDirectory(System.IO.Directory.CreateDirectory(path)))
        {
        }
    }
}