using System;
using System.IO;
using System.Linq;
using DotJEM.Json.Index.Storage;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;
using DotJEM.Json.Index.Storage.Snapshot;
using ILuceneFile = DotJEM.Json.Index.Storage.Snapshot.ILuceneFile;
using DotJEM.Json.Index.Analyzation;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Lucene.Net.Search;

namespace DotJEM.Json.Index
{
    public interface IIndexStorage
    {
        Analyzer Analyzer { get; }
        IndexReader OpenReader();
        ReferenceContext<IndexSearcher> OpenSearcher();
        IndexWriter Writer { get; }
        bool Exists { get; }
        void Close();
        void Flush();
        bool Purge();
        bool Snapshot(ISnapshotTarget snapshot);
        bool Restore(ISnapshotSource snapshot);
    }

    public abstract class AbstractLuceneIndexStorage : IIndexStorage
    {
        private readonly object padlock = new object();

        protected Directory Directory { get; private set; }
        public virtual bool Exists => Directory.ListAll().Any();


        private readonly IndexDeletionPolicy deletePolicy;
        private Lazy<IndexWriter> writer;
        private Lazy<SearcherManager> manager;

        public Analyzer Analyzer { get; private set; }

        protected AbstractLuceneIndexStorage(Directory directory, Analyzer analyzer = null, IndexDeletionPolicy deletionPolicy = null)
        {
            Directory = directory;
            Analyzer = analyzer ?? new StandardAnalyzer(LuceneVersion.LUCENE_48);
            deletePolicy = deletionPolicy ??  new SnapshotDeletionPolicy(new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer).IndexDeletionPolicy);

            manager = new Lazy<SearcherManager>(CreateManager);
            writer = new Lazy<IndexWriter>(CreateIndexWriter);
        }

        public IndexWriter Writer => writer.Value;

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

        public bool Snapshot(ISnapshotTarget snapshotTarget)
        {
            // TODO: Copy from new lucene instead.
            //if (writer == null)
            //    return false;

            //if (snapshotTarget == null)
            //    throw new ArgumentNullException(nameof(snapshotTarget));

            //if (deletePolicy is not SnapshotDeletionPolicy snapshotDeletionPolicy)
            //    throw new InvalidOperationException($"Snaptshots requires a {nameof(SnapshotDeletionPolicy)} but the configured policy was {deletePolicy.GetType().Name}");
            
            //lock (padlock)
            //{
            //    IndexCommit commit = snapshotDeletionPolicy.Snapshot();
            //    try
            //    {
            //        using ISnapshotWriter writer = snapshotTarget.Open(commit);
            //        foreach (string file in commit.FileNames.Where(file => !file.Equals(commit.SegmentsFileName, StringComparison.Ordinal)))
            //        {
            //            using IndexInputStream source = new (file, Directory.OpenInput(file));
            //            writer.WriteFile(source);
            //        }

            //        using IndexInputStream segmentsSource = new (commit.SegmentsFileName, Directory.OpenInput(commit.SegmentsFileName));
            //        writer.WriteSegmentsFile(segmentsSource);

            //        using IndexInputStream genFile = new IndexInputStream("segments.gen", Directory.OpenInput("segments.gen"));
            //        writer.WriteSegmentsGenFile(genFile);

            //    }
            //    finally
            //    {
            //        snapshotDeletionPolicy.Release();
            //    }
            //}
            return true;
        }

        public bool Restore(ISnapshotSource snapshotSource)
        {
            // TODO: Copy from new lucene instead.
            
            //lock (padlock)
            //{
            //    Close();
            //    foreach (string file in Directory.ListAll())
            //        Directory.DeleteFile(file);

            //    using ISnapshot snapshot = snapshotSource.Open();
            //    foreach (ILuceneFile file in snapshot.Files)
            //        CopyFile(file);

            //    CopyFile(snapshot.SegmentsFile);
            //    //TODO: This should be generated instead.
            //    CopyFile(snapshot.SegmentsGenFile);
            //    Directory.Sync(snapshot.SegmentsFile.Name);
            //}
            //return true;

            //void CopyFile(ILuceneFile file)
            //{
            //    using Stream source = file.Open();
            //    using IndexOutputStream target = new(file.Name, Directory.CreateOutput(file.Name));
            //    source.CopyTo(target);
            //    target.Flush();
            //}
            return false;
        }
        
        private IndexWriter CreateIndexWriter()
        {
            IndexWriterConfig cfg = new IndexWriterConfig(LuceneVersion.LUCENE_48, Analyzer);
            cfg.OpenMode = Exists ? OpenMode.APPEND : OpenMode.CREATE;
            cfg.RAMBufferSizeMB = 512;
            cfg.IndexDeletionPolicy = deletePolicy;
            return new IndexWriter(Directory, cfg);
        }
        private SearcherManager CreateManager() => new SearcherManager(writer.Value, true, new SearcherFactory());

        public void Flush()
        {
            lock (padlock)
            {
                writer?.Value.Flush(true, true);
            }
        }
    }


    public class LuceneMemmoryIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneMemmoryIndexStorage(Analyzer analyzer = null, IndexDeletionPolicy deletionPolicy = null)
            : base(new RAMDirectory(), analyzer, deletionPolicy)
        {
        }
    }

    public class LuceneFileIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneFileIndexStorage(string path, Analyzer analyzer = null, IndexDeletionPolicy deletionPolicy = null)
            //Note: Ensure Directory.
            : base(FSDirectory.Open(System.IO.Directory.CreateDirectory(path).FullName), analyzer, deletionPolicy)
        {
        }
    }

    //public class LuceneCachedMemmoryIndexStorage : AbstractLuceneIndexStorage
    //{
    //    public LuceneCachedMemmoryIndexStorage(string path, Analyzer analyzer = null, IndexDeletionPolicy deletionPolicy = null)
    //        //Note: Ensure cacheDirectory.
    //        : base(new MemoryCachedDirective(System.IO.Directory.CreateDirectory(path).FullName), analyzer, deletionPolicy)
    //    {
    //    }
    //}

    public class LuceneMemmoryMappedFileIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneMemmoryMappedFileIndexStorage(string path, Analyzer analyzer = null, IndexDeletionPolicy deletionPolicy = null)
            //Note: Ensure cacheDirectory.
            : base(new MMapDirectory(System.IO.Directory.CreateDirectory(path)), analyzer, deletionPolicy)
        {
        }
    }

}