using System;
using DotJEM.Json.Index.IO;
using Newtonsoft.Json.Linq;


namespace DotJEM.Json.Index.Manager
{
    public interface IIndexSyncronizationHandler
    {
    }

    public class IndexSyncronizationHandler : IIndexSyncronizationHandler
    {
        private ILuceneJsonIndexDataSource source;
        private ILuceneJsonIndex index;


        public void Initialize()
        {
            source.Subscribe(new FuncObserver(OnSource));
        }

        public void OnSource(JObject entity)
        {
            IJsonIndexWriter writer = index.CreateWriter();
            //writer.Update()

        }

        public ISnapshotInfo TakeSnapshot()
        {
            return null;
        }
    }

    public class FuncObserver : IObserver<JObject>
    {
        private readonly Action<JObject> onSource;

        public FuncObserver(Action<JObject> onSource)
        {
            this.onSource = onSource;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(JObject value) => onSource(value);
    }

    /// <summary>
    /// In context of DotJEM.Json.Storage as a DataSource, a data source can be one or more storage areas, this 
    /// </summary>
    public interface ILuceneJsonIndexDataSource : IObservable<JObject>
    {


        ISnapshotInfo TakeSnapshot();
    }

    class LuceneJsonIndexDataSource : ILuceneJsonIndexDataSource
    {
        public IDisposable Subscribe(IObserver<JObject> observer)
        {
            throw new NotImplementedException();
        }

        public ISnapshotInfo TakeSnapshot()
        {
            throw new NotImplementedException();
        }
    }

    public interface ISnapshotInfo
    {

    }


}