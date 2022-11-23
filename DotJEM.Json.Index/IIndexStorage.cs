using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using DotJEM.Json.Index.Storage;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Directory = Lucene.Net.Store.Directory;
using System.Collections.Generic;
using DotJEM.Json.Index.Storage.Snapshot;
using static Lucene.Net.Documents.Field;
using ILuceneFile = DotJEM.Json.Index.Storage.Snapshot.ILuceneFile;

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
        bool Snapshot(ISnapshotTarget snapshot);
        bool Restore(ISnapshotSource snapshot);
    }

    public abstract class AbstractLuceneIndexStorage : IIndexStorage
    {
        private readonly object padlock = new object();

        protected Directory Directory { get; private set; }
        public virtual bool Exists => Directory.ListAll().Any();

        private IndexWriter writer;
        private IndexReader reader;
        private Analyzer analyzer;
        private SnapshotDeletionPolicy deletePolicy = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());

        protected AbstractLuceneIndexStorage(Directory directory)
        {
            Directory = directory;
        }

        public IndexWriter GetWriter(Analyzer analyzer)
        {
            //TODO: The storage should define the analyzer, not the writer.
            lock (padlock)
            {
                return writer ??= new IndexWriter(Directory, this.analyzer = analyzer, !Exists, deletePolicy, IndexWriter.MaxFieldLength.UNLIMITED);
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
                    var temp = new IndexWriter(Directory, new SimpleAnalyzer(), true, deletePolicy, IndexWriter.MaxFieldLength.UNLIMITED);
                    temp.Commit();
                    temp.Dispose();
                }
                else
                {
                    writer = new IndexWriter(Directory, analyzer, true, deletePolicy, IndexWriter.MaxFieldLength.UNLIMITED);
                    writer.Commit();
                }
            }

            return true;
        }

        public bool Snapshot(ISnapshotTarget snapshotTarget)
        {
            if (writer == null)
                return false;

            if (snapshotTarget == null) throw new ArgumentNullException(nameof(snapshotTarget));
            lock (padlock)
            {
                IndexCommit commit = deletePolicy.Snapshot();
                try
                {
                    ISnapshotWriter writer = snapshotTarget.Open(commit.Generation);
                    foreach (string file in commit.FileNames.Where(file => !file.Equals(commit.SegmentsFileName, StringComparison.Ordinal)))
                    {
                        using IndexInputStream source = new (file, Directory.OpenInput(file));
                        writer.WriteFile(source);
                    }

                    using IndexInputStream segmentsSource = new (commit.SegmentsFileName, Directory.OpenInput(commit.SegmentsFileName));
                    writer.WriteSegmentsFile(segmentsSource);
                }
                finally
                {
                    deletePolicy.Release();
                }
            }
            return true;
        }

        public bool Restore(ISnapshotSource snapshotSource)
        {
            lock (padlock)
            {
                Close();
                foreach (string file in Directory.ListAll())
                    Directory.DeleteFile(file);

                ISnapshot snapshot = snapshotSource.Open();
                foreach (ILuceneFile file in snapshot.Files)
                {
                    using Stream source = file.Open();
                    using IndexOutputStream target =new (file.Name, Directory.CreateOutput(file.Name));
                    source.CopyTo(target);
                    target.Flush();
                }

                using Stream segmentsSource = snapshot.SegmentsFile.Open();
                using IndexOutputStream segmentsTarget =new (snapshot.SegmentsFile.Name, Directory.CreateOutput(snapshot.SegmentsFile.Name));
                segmentsSource.CopyTo(segmentsTarget);
                segmentsTarget.Flush();

                Directory.Sync(snapshot.SegmentsFile.Name);
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