using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Snapshots;
using DotJEM.Json.Index.Util;
using Lucene.Net.Store;
using Newtonsoft.Json.Linq;
using Directory = Lucene.Net.Store.Directory;


namespace DotJEM.Json.Index.Manager
{
    //TODO: Index Ingest Manager.
    public interface IIndexIngestManager
    {

    }

    public interface IIndexIngestHandler
    {
    }

    public class IndexIngestHandler : IIndexIngestHandler
    {
        private readonly ILuceneJsonIndex index;
        private readonly IIngestDataSource source;

        public IndexIngestHandler(ILuceneJsonIndex index, IIngestDataSource source)
        {
            this.index = index;
            this.source = source;
        }

        public void Initialize()
        {
            source.Subscribe(new ActionSink<IJsonIndexWriterCommand>(OnSource));
        }

        private void OnSource(IJsonIndexWriterCommand update)
        {
            try
            {
                IJsonIndexWriter writer = index.CreateWriter();
                update.Execute(writer);
                writer.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public ISnapshot TakeSnapshot()
        {
            IIngestDataSourceState state = new IngestDataSourceState();
            source.SaveState(state);

            IIndexSnapshotTarget target = new IngestIndexZipSnapshotTarget("");
            ISnapshot snapshot = index.Snapshot(target);
            return null;
        }
    }

    public class ActionSink<T> : IObserver<T>
    {
        private readonly Action<T> onNext;
        private readonly Action<Exception> onError;
        private readonly Action onComplete;

        public ActionSink(Action<T> onNext, Action<Exception> onError = null, Action onComplete = null)
        {
            this.onNext = onNext;
            this.onError = onError;
            this.onComplete = onComplete;
        }

        public void OnNext(T value) => onNext(value);
        public void OnError(Exception error) => onError?.Invoke(error);
        public void OnCompleted() => onComplete?.Invoke();
    }

    /// <summary>
    /// In context of DotJEM.Json.Storage as a DataSource, a data source can be one or more storage areas, this 
    /// </summary>
    public interface IIngestDataSource : IObservable<IJsonIndexWriterCommand>
    {
        void SaveState(IIngestDataSourceState state);
        void RestoreState(IIngestDataSourceState state);
    }

    public class CompositeIngestDataSource : IIngestDataSource
    {
        private readonly IIngestDataSource[] sources;

        public CompositeIngestDataSource(params IIngestDataSource[] sources)
        {
            this.sources = sources;
        }

        public IDisposable Subscribe(IObserver<IJsonIndexWriterCommand> observer)
        {
            return new CompositeDisposeable(Array.ConvertAll(sources, source => source.Subscribe(observer)));
        }

        public void SaveState(IIngestDataSourceState state)
        {
            foreach (IIngestDataSource source in sources)
                source.SaveState(state);
        }

        public void RestoreState(IIngestDataSourceState state)
        {
            foreach (IIngestDataSource source in sources)
                source.RestoreState(state);
        }

        private class CompositeDisposeable : Disposable
        {
            private readonly IDisposable[] targets;

            public CompositeDisposeable(IDisposable[] targets)
            {
                this.targets = targets;
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing) return;
                foreach (IDisposable disposable in targets)
                    disposable.Dispose();
            }
        }
    }

    public interface IIngestDataSourceState
    {
        void WriteState(string key, JToken state);
        JToken ReadState(string key);
    }

    public class IngestDataSourceState : IIngestDataSourceState
    {
        private readonly JObject state = new JObject();

        public void WriteState(string key, JToken state)
        {
            state[key] = state;
        }

        public JToken ReadState(string key)
        {
            return state[key];
        }
    }



    public class IngestIndexZipSnapshotTarget : IIndexSnapshotTarget
    {
        private readonly string path;
        private readonly List<SingleFileSnapshot> snapShots = new List<SingleFileSnapshot>();

        public IReadOnlyCollection<ISnapshot> Snapshots => snapShots.AsReadOnly(); 

        public IngestIndexZipSnapshotTarget(string path)
        {
            this.path = path;
        }

        public virtual IIndexSnapshotWriter Open(long generation)
        {
            string snapshotPath = Path.Combine(path, $"{generation:x8}.zip");
            snapShots.Add(new SingleFileSnapshot(snapshotPath));
            return new Writer(snapshotPath);
        }

        private class Writer : Disposable, IIndexSnapshotWriter
        {
            private readonly ZipArchive archive;

            public Writer(string path)
            {
                this.archive = ZipFile.Open(path, ZipArchiveMode.Create);
            }

            public void WriteFile(string fileName, Directory dir)
            {
                using IndexInputStream source = new IndexInputStream(dir.OpenInput(fileName, IOContext.READ_ONCE));
                using Stream target = archive.CreateEntry(fileName).Open();
                source.CopyTo(target);
            }

            public void WriteSegmentsFile(string segmentsFile, Directory dir)
            {
                this.WriteFile(segmentsFile, dir);
            }

            public void WriteProperties(ISnapshotProperties properties)
            {
                using Stream target = archive.CreateEntry("_properties").Open();
                properties.WriteTo(target);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    archive?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}