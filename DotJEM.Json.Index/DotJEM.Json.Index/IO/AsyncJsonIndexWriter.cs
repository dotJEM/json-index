using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.Inflow;
using DotJEM.Json.Index.Inflow.Jobs;
using DotJEM.Json.Index.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.IO
{
    public interface IJsonIndexWriter : IDisposable
    {
        IJsonIndexWriter Create(JObject doc, IReservedSlot reservedSlot = null);
        IJsonIndexWriter Create(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null);
        IJsonIndexWriter Update(JObject doc, IReservedSlot reservedSlot = null);
        IJsonIndexWriter Update(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null);
        IJsonIndexWriter Delete(JObject doc, IReservedSlot reservedSlot = null);
        IJsonIndexWriter Delete(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null);

        IJsonIndexWriter ForceMerge(int maxSegments);
        IJsonIndexWriter ForceMerge(int maxSegments, bool wait);
        IJsonIndexWriter ForceMergeDeletes();
        IJsonIndexWriter ForceMergeDeletes(bool wait);
        IJsonIndexWriter Flush(bool triggerMerge, bool applyDeletes);
        IJsonIndexWriter Commit();
        IJsonIndexWriter Rollback();
        IJsonIndexWriter PrepareCommit();
        IJsonIndexWriter SetCommitData(IDictionary<string, string> commitUserData);
    }

    

    public class AsyncJsonIndexWriter : Disposable, IJsonIndexWriter
    {
        private readonly IIndexWriterManager manager;
        private readonly ILuceneDocumentFactory factory;

        public ILuceneJsonIndex Index { get; }
        public IInflowManager Inflow { get; }

        public IndexWriter UnderlyingWriter => manager.Writer;

        public AsyncJsonIndexWriter(ILuceneJsonIndex index, ILuceneDocumentFactory factory, IIndexWriterManager manager)
        {
            Index = index;
            this.factory = factory;
            this.manager = manager;
            this.Inflow = new InflowManager(index.Services.Resolve<IInflowCapacity>());
        }

        public IJsonIndexWriter Create(JObject doc, IReservedSlot reservedSlot = null) => Create(new[] { doc }, reservedSlot);
        public IJsonIndexWriter Create(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            reservedSlot ??= Inflow.Queue.Reserve();
            foreach (JObject[] objects in docs.Partition(250))
            {
                //TODO: We need to split the slot here...
                IReservedSlot slot = Inflow.Queue.Split(reservedSlot);;
                slot.OnComplete(documents => {
                    UnderlyingWriter.AddDocuments(documents.Select(x => x.Document));
                });
                Inflow.Scheduler.Enqueue(new ConvertInflow(slot, objects, factory), Priority.High);
            }
            reservedSlot.Ready(null);
            return this;
        }

        //public IJsonIndexWriter Update(JObject doc) => Update(new[] { doc }, null);
        public IJsonIndexWriter Update(JObject doc, IReservedSlot reservedSlot = null) => Update(new[] { doc }, reservedSlot);
        //public IJsonIndexWriter Update(IEnumerable<JObject> docs) => Update(docs, Inflow.Queue.Reserve());
        public IJsonIndexWriter Update(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            reservedSlot ??= Inflow.Queue.Reserve();
            foreach (JObject[] objects in docs.Partition(250))
            {
                //TODO: We need to split the slot here...
                IReservedSlot slot = Inflow.Queue.Split(reservedSlot);;
                slot.OnComplete(documents => {
                    foreach ((Term key, Document doc) in documents)
                        UnderlyingWriter.UpdateDocument(key, doc);
                });
                Inflow.Scheduler.Enqueue(new ConvertInflow(slot, objects, factory), Priority.High);
            }
            reservedSlot.Ready(null);
            return this;
        }

        public IJsonIndexWriter Delete(JObject doc, IReservedSlot reservedSlot = null) => Delete(new[] { doc }, reservedSlot);
        public IJsonIndexWriter Delete(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            reservedSlot ??= Inflow.Queue.Reserve();
            foreach (JObject[] objects in docs.Partition(250))
            {
                //TODO: We need to split the slot here...
                IReservedSlot slot = Inflow.Queue.Split(reservedSlot);;
                slot.OnComplete(documents =>
                {
                    foreach ((Term key, Document _) in documents)
                        UnderlyingWriter.DeleteDocuments(key);
                });
                Inflow.Scheduler.Enqueue(new ConvertInflow(slot, objects, factory), Priority.High);
            }
            reservedSlot.Ready(null);
            return this;
        }

        public IJsonIndexWriter ForceMerge(int maxSegments)
        {
            UnderlyingWriter.ForceMerge(maxSegments);
            return this;
        }

        public IJsonIndexWriter ForceMerge(int maxSegments, bool wait)
        {
            UnderlyingWriter.ForceMerge(maxSegments, wait);
            return this;
        }

        public IJsonIndexWriter ForceMergeDeletes()
        {
            UnderlyingWriter.ForceMergeDeletes();
            return this;
        }

        public IJsonIndexWriter ForceMergeDeletes(bool wait)
        {
            UnderlyingWriter.ForceMergeDeletes(wait);
            return this;
        }

        public IJsonIndexWriter Rollback()
        {
            UnderlyingWriter.Rollback();
            return this;
        }

        public IJsonIndexWriter Flush(bool triggerMerge, bool applyDeletes)
        {
            IReservedSlot slot = Inflow.Queue.Reserve();
            slot.OnComplete(documents => UnderlyingWriter.Flush(triggerMerge, applyDeletes));
            Inflow.Scheduler.Enqueue(new NoopInflowJob(slot), Priority.High);
            return this;
        }

        public IJsonIndexWriter Commit()
        {
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve();
            slot.OnComplete(documents =>
            {
                UnderlyingWriter.Commit();
                wait.Set();
            });
            Inflow.Scheduler.Enqueue(new NoopInflowJob(slot), Priority.Highest);
            wait.WaitOne();
            return this;
        }

        public IJsonIndexWriter PrepareCommit()
        {
            UnderlyingWriter.PrepareCommit();
            return this;
        }

        public IJsonIndexWriter SetCommitData(IDictionary<string, string> commitUserData)
        {
            UnderlyingWriter.SetCommitData(commitUserData);
            return this;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Commit();
            }
            base.Dispose(disposing);
        }
    }
}
