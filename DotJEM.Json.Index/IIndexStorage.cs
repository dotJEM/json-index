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
        bool Snapshot(ISnapshot snapshot);
        bool Restore(ISnapshot snapshot);
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

        public bool Snapshot(ISnapshot snapshot)
        {
            if (writer == null)
                return false;

            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            lock (padlock)
            {
                IndexCommit commit = deletePolicy.Snapshot();
                try
                {
                    foreach (string file in commit.FileNames)
                    {
                        using IndexInputStream source = new (file, Directory.OpenInput(file));
                        snapshot.WriteFile(source);
                    }
                }
                finally
                {
                    deletePolicy.Release();
                }
            }

            return true;
        }

        public bool Restore(ISnapshot snapshot)
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            lock (padlock)
            {
                writer?.Flush(true, true, true);
            }
        }
    }

    public interface ISnapshot
    {
        void WriteFile(IndexInputStream input);
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

    public class IndexInputStream : Stream
    {
        public string FileName { get; }
        public IndexInput IndexInput { get; }

        public IndexInputStream(string fileName, IndexInput indexInput)
        {
            FileName = fileName;
            IndexInput = indexInput;
        }

        public override void Flush()
        {
            throw new InvalidOperationException("Cannot flush a readonly stream.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Cannot change length of a readonly stream.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = (int)(IndexInput.Length() - IndexInput.FilePointer);
            int readCount = Math.Min(remaining, count);
            IndexInput.ReadBytes(buffer, offset, readCount);
            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidCastException("Cannot write to a readonly stream.");
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => IndexInput.Length();

        public override long Position
        {
            get => IndexInput.FilePointer;
            set => IndexInput.Seek(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IndexInput.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}