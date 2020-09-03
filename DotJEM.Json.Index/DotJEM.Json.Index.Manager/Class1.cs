﻿using System;
using System.Linq;
using DotJEM.Json.Index.Backup;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Util;
using Newtonsoft.Json.Linq;


namespace DotJEM.Json.Index.Manager
{
    public interface IIndexSynchronizationHandler
    {
    }

    public class IndexSynchronizationHandler : IIndexSynchronizationHandler
    {
        private readonly ILuceneJsonIndex index;
        private readonly ILuceneJsonIndexDataSource source;

        public IndexSynchronizationHandler(ILuceneJsonIndex index, ILuceneJsonIndexDataSource source)
        {
            this.index = index;
            this.source = source;
        }

        public void Initialize()
        {
            source.Subscribe(new ActionSink<IndexUpdate>(OnSource));
        }

        public void OnSource(IndexUpdate update)
        {
            IJsonIndexWriter writer = index.CreateWriter();
            switch (update.Action)
            {
                case IndexAction.Create:
                    writer.Create(update.Entity);
                    break;
                case IndexAction.Update:
                    writer.Update(update.Entity);
                    break;
                case IndexAction.Delete:
                    writer.Delete(update.Entity);
                    break;
            }
        }

        public ISnapshotInfo TakeSnapshot()
        {
            IIndexSnapshotTarget target = new IndexZipSnapshotTarget("");
            ISnapshot snapshot = index.Snapshot(target);
            

            return source.TakeSnapshot();
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
    public interface ILuceneJsonIndexDataSource : IObservable<IndexUpdate>
    {
        ISnapshotInfo TakeSnapshot();
    }

    public class CompositeLuceneJsonIndexDataSource : ILuceneJsonIndexDataSource
    {
        private readonly ILuceneJsonIndexDataSource[] sources;

        public CompositeLuceneJsonIndexDataSource(params ILuceneJsonIndexDataSource[] sources)
        {
            this.sources = sources;
        }

        public IDisposable Subscribe(IObserver<IndexUpdate> observer)
        {
            return new CompositeDisposeable(Array.ConvertAll(sources, source => source.Subscribe(observer)));
        }

        public ISnapshotInfo TakeSnapshot()
        {
            return new CompositeSnapshotInfo(Array.ConvertAll(sources, source => source.TakeSnapshot()));
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

    public interface ISnapshotInfo
    {
    }

    public class CompositeSnapshotInfo : ISnapshotInfo
    {
        public ISnapshotInfo[] Snapshots { get; }

        public CompositeSnapshotInfo(ISnapshotInfo[] snapshots)
        {
            Snapshots = snapshots;
        }
    }

    public enum IndexAction
    {
        Create, Update, Delete
    }

    public struct IndexUpdate
    {
        public IndexAction Action { get; }
        public JObject Entity { get; }
        
    }
}