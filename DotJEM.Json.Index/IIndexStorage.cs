using System.IO;
using System.Linq;
using DotJEM.Json.Index.Storage;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
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

        bool Purge();
    }

    public abstract class AbstractLuceneIndexStorage : IIndexStorage
    {
        private readonly object padlock = new object();

        protected Directory Directory { get; private set; }
        public virtual bool Exists => Directory.ListAll().Any();

        private IndexWriter writer;
        private IndexReader reader;
        private Analyzer analyzer;

        protected AbstractLuceneIndexStorage(Directory directory)
        {
            Directory = directory;
        }

        public IndexWriter GetWriter(Analyzer analyzer)
        {
            //TODO: The storage should define the analyzer, not the writer.
            lock (padlock)
            {
                return writer ?? (writer = new IndexWriter(Directory, this.analyzer = analyzer, !Exists, IndexWriter.MaxFieldLength.UNLIMITED));
            }
        }

        public IndexReader OpenReader()
        {
            if (!Exists)
                return null;

            lock (padlock)
            {
                return reader = reader?.Reopen() ?? IndexReader.Open(Directory, true);
            }
        }

        public void Close()
        {
            lock (padlock)
            {
                writer?.Dispose();
                writer = null;

                reader?.Dispose();
                reader = null;
            }
        }

        public bool Purge()
        {
            lock (padlock)
            {
                Close();
                if (analyzer == null)
                {
                    var temp = new IndexWriter(Directory, new SimpleAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
                    temp.Commit();
                    temp.Dispose();
                }
                else
                {
                    writer = new IndexWriter(Directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
                    writer.Commit();
                }
            }

            return true;
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