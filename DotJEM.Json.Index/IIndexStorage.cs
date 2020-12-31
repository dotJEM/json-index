using System;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index
{
    public interface IIndexStorage
    {
        IndexReader OpenReader();
        IndexWriter GetWriter(Analyzer analyzer);

        ReferenceContext<IndexSearcher> OpenSearcher();
        bool Exists { get; }
        void Close();
        void Flush();

        bool Purge();
    }

    public abstract class AbstractLuceneIndexStorage : IIndexStorage
    {
        private readonly object padlock = new object();

        protected Directory Directory { get; }
        public virtual bool Exists => Directory.ListAll().Any();

        private Analyzer analyzer;
        private Lazy<IndexWriter> writer;
        private Lazy<SearcherManager> manager;

        protected AbstractLuceneIndexStorage(Directory directory)
        {
            Directory = directory;
            manager = new Lazy<SearcherManager>(CreateManager);
            writer = new Lazy<IndexWriter>(CreateIndexWriter);
        }

        private SearcherManager CreateManager() => new SearcherManager(writer.Value, true, new SearcherFactory());

        public IndexWriter GetWriter(Analyzer analyzer)
        {
            this.analyzer = analyzer;
            return writer.Value;
        }

        public IndexReader OpenReader()
        {
            if (!Exists)
                return null;

            if (!writer.IsValueCreated)
                return null;
            
            return writer.Value.GetReader(true);
        }

        public ReferenceContext<IndexSearcher> OpenSearcher()
        {
            if (!Exists)
                return null;

            if (!writer.IsValueCreated)
                return null;
 
            return manager.Value.GetContext();
        }

        public void Close()
        {
            writer?.Value.Dispose();
            writer = null;
        }

        public bool Purge()
        {
            lock (padlock)
            {
                Close();
                IndexWriterConfig cfg = new IndexWriterConfig(LuceneVersion.LUCENE_48, new SimpleAnalyzer(LuceneVersion.LUCENE_48));
                cfg.OpenMode = OpenMode.CREATE;

                var temp = new IndexWriter(Directory, cfg);
                temp.Commit();
                temp.Dispose();
                manager = new Lazy<SearcherManager>(CreateManager);
            }

            return true;
        }

        public void Flush()
        {
            writer?.Value.Flush(true, true);
        }
        
        private IndexWriter CreateIndexWriter()
        {
            IndexWriterConfig cfg = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
            cfg.OpenMode = Exists ? OpenMode.APPEND : OpenMode.CREATE;
            return new IndexWriter(Directory, cfg);
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
    

    public class LuceneMemmoryMappedFileIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneMemmoryMappedFileIndexStorage(string path)
            //Note: Ensure cacheDirectory.
            : base(new MMapDirectory(System.IO.Directory.CreateDirectory(path)))
        {
        }
    }
}