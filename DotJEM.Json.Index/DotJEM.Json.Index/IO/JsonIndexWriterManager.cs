using System;
using System.Threading;
using DotJEM.Json.Index.Util;
using Lucene.Net.Analysis;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.IO
{
    public interface IIndexWriterManager : IDisposable
    {
        event EventHandler<EventArgs> OnClose;

        IndexWriter Writer { get; }
        void Close();
    }

    public class IndexWriterManager : Disposable, IIndexWriterManager
    {
        public event EventHandler<EventArgs> OnClose;

        private readonly ResetableLazy<IndexWriter> writer;

        public IndexWriter Writer => writer.Value;

        public IndexWriterManager(ILuceneJsonIndex index)
        {
            writer = new ResetableLazy<IndexWriter>(() => Open(index));
        }

        protected virtual IndexWriter Open(ILuceneJsonIndex index)
        {
            IndexWriterConfig config = new IndexWriterConfig(index.Configuration.Version, index.Services.Resolve<Analyzer>());
            config.RAMBufferSizeMB = 512;
            config.OpenMode = OpenMode.CREATE_OR_APPEND;
            config.IndexDeletionPolicy = new SnapshotDeletionPolicy(config.IndexDeletionPolicy);
            return new IndexWriter(index.Storage.Directory, config);
        }

        public void Close()
        {
            if (!writer.IsValueCreated)
                return;
            
            writer.Value.Dispose();
            writer.Reset();

            RaiseOnClose();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Writer.Dispose();
            }
            base.Dispose(disposing);
        }

        protected virtual void RaiseOnClose()
        {
            OnClose?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ResetableLazy<T>
    {
        private Lazy<T> lazy;
        private readonly Func<Lazy<T>> factory;
        private readonly object padLock = new object();

        public ResetableLazy()
        {
            factory = () => new Lazy<T>();
            Reset();
        }

        public ResetableLazy(bool isThreadSafe)
        {
            factory = () => new Lazy<T>(isThreadSafe);
            Reset();
        }

        public ResetableLazy(Func<T> valueFactory)
        {
            factory = () => new Lazy<T>(valueFactory);
            Reset();
        }

        public ResetableLazy(Func<T> valueFactory, bool isThreadSafe)
        {
            factory = () => new Lazy<T>(valueFactory, isThreadSafe);
            Reset();
        }

        public ResetableLazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
        {
            factory = () => new Lazy<T>(valueFactory, mode);
            Reset();
        }

        public ResetableLazy(LazyThreadSafetyMode mode)
        {
            factory = () => new Lazy<T>(mode);
            Reset();
        }

        public void Reset()
        {
            lock (padLock)
            {
                lazy = factory();
            }
        }

        public bool IsValueCreated
        {
            get
            {
                lock (padLock)
                {
                    return lazy.IsValueCreated;
                }
            }
        }

        public T Value
        {
            get
            {
                lock (padLock)
                {
                    return lazy.Value;
                }
            }
        }
    }
}
