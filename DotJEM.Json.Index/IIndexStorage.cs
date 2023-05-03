using System;
using System.Collections.Generic;
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
using System.Text.RegularExpressions;
using static Lucene.Net.Documents.Field;

namespace DotJEM.Json.Index;

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
            foreach (string file in Directory.ListAll())
                Directory.DeleteFile(file);

            manager = new Lazy<SearcherManager>(CreateManager);
            writer = new Lazy<IndexWriter>(CreateIndexWriter);
        }

        return true;
    }

    public bool Snapshot(ISnapshotTarget snapshotTarget)
    {
        // TODO: Copy from new lucene instead.
        if (writer == null)
            return false;

        if (snapshotTarget == null)
            throw new ArgumentNullException(nameof(snapshotTarget));

        if (deletePolicy is not SnapshotDeletionPolicy snapshotDeletionPolicy)
            throw new InvalidOperationException($"Snaptshots requires a {nameof(SnapshotDeletionPolicy)} but the configured policy was {deletePolicy.GetType().Name}");
            
        lock (padlock)
        {
            IndexCommit commit = null;
            try
            {
                commit = snapshotDeletionPolicy.Snapshot();
                Directory directory = commit.Directory;
                string segmentsFile = commit.SegmentsFileName;
                    
                using ISnapshotWriter writer = snapshotTarget.Open(commit);
                foreach (string file in commit.FileNames.Where(file => !file.Equals(commit.SegmentsFileName, StringComparison.Ordinal)))
                {
                    using IndexInputStream source = new (file, Directory.OpenInput(file, IOContext.READ_ONCE));
                    writer.WriteFile(source);
                }

                using IndexInputStream segmentsSource = new (commit.SegmentsFileName, Directory.OpenInput(commit.SegmentsFileName, IOContext.READ_ONCE));
                writer.WriteSegmentsFile(segmentsSource);
            }
            finally
            {
                if (commit != null) snapshotDeletionPolicy.Release(commit);
            }
        }
        return true;
    }

    public bool Restore(ISnapshotSource snapshotSource)
    {
        // TODO: Copy from new lucene instead.

        lock (padlock)
        {
            using ISnapshot snapshot = snapshotSource.Open();
            Purge();
            
            List<string> files = snapshot.Files
                .Select(file => CopyFile(file).Name)
                .ToList();
            Directory.Sync(files);

            CopyFile(snapshot.SegmentsFile);
            Directory.Sync(new[] { snapshot.SegmentsFile.Name });

            SegmentInfos.WriteSegmentsGen(Directory, snapshot.Generation);

            return writer.Value != null;
        }

        ILuceneFile CopyFile(ILuceneFile file)
        {
            using Stream source = file.Open();
            using IndexOutputStream target = new(file.Name, Directory.CreateOutput(file.Name, IOContext.DEFAULT));
            source.CopyTo(target);
            target.Flush();
            return file;
        }
    }
        
    private IndexWriter CreateIndexWriter()
    {
        IndexWriterConfig cfg = new (LuceneVersion.LUCENE_48, Analyzer);
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

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, params TSource[] items) => first.Except(items.AsEnumerable());

}