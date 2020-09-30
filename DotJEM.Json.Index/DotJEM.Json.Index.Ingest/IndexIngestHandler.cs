using System;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Manager;
using DotJEM.Json.Index.Snapshots;
using DotJEM.Json.Index.Util;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Ingest
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

            ISnapshotTarget target = new IngestZipSnapshotTarget("", JObject.FromObject(state));
            return index.Snapshot(target);
        }

        public void RestoreSnapshot()
        {
            IngestIndexZipSnapshotSource snapshotSource = new IngestIndexZipSnapshotSource("");
            index.Restore(snapshotSource);

            IIngestDataSourceState state = snapshotSource.RecentProperties.ToObject<IngestDataSourceState>();
            this.source.RestoreState(state);
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
}