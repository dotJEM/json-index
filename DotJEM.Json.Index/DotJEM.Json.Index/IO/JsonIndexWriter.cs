using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.Inflow;
using DotJEM.Json.Index.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.IO
{
    public interface IJsonIndexWriter : IDisposable
    {
        ILuceneJsonIndex Index { get; }
        IInflowManager Inflow { get; }

        //TODO: Get around exposing this, there should be an easier way to generate convert inflow tasks.
        ILuceneDocumentFactory Factory { get; }

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

    public static class EnumerablePartitionExtensions
    {
        public static IEnumerable<T[]> Partition<T>(this IEnumerable<T> self, int size)
        {
            int i = 0;
            T[] partition = new T[size];
            foreach (T item in self)
            {
                if (i == size)
                {
                    yield return partition;
                    partition = new T[size];
                    i = 0;
                }
                partition[i++] = item;
            }

            if (i <= 0) yield break;
            
            Array.Resize(ref partition, i);
            yield return partition;
        }
    }

    public class JsonIndexWriter : Disposable, IJsonIndexWriter
    {
        private readonly IIndexWriterManager manager;
        public ILuceneDocumentFactory Factory { get; }

        public ILuceneJsonIndex Index { get; }
        public IInflowManager Inflow { get; }

        public IndexWriter UnderlyingWriter => manager.Writer;

        public JsonIndexWriter(ILuceneJsonIndex index, ILuceneDocumentFactory factory, IIndexWriterManager manager)
        {
            Index = index;
            this.Factory = factory;
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
                Inflow.Scheduler.Enqueue(new ConvertInflow(slot, objects, Factory), Priority.High);
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
                Inflow.Scheduler.Enqueue(new ConvertInflow(slot, objects, Factory), Priority.High);
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
                Inflow.Scheduler.Enqueue(new ConvertInflow(slot, objects, Factory), Priority.High);
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
            Inflow.Scheduler.Enqueue(new CommonInflowJob(slot), Priority.High);
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
            Inflow.Scheduler.Enqueue(new CommonInflowJob(slot), Priority.Highest);
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

    public class CommonInflowJob : IInflowJob
    {
        private readonly IReservedSlot slot;
        public int EstimatedCost { get; } = 1;

        public CommonInflowJob(IReservedSlot slot)
        {
            this.slot = slot;
        }

        public void Execute(IInflowScheduler scheduler)
        {
            slot.Ready(null);
        }
    }


    public static class IndexWriterExtensions
    {
        public static ILuceneJsonIndex Create(this ILuceneJsonIndex self, JObject doc)
        {
            self.CreateWriter().Create(doc);
            return self;
        }

        public static ILuceneJsonIndex Create(this ILuceneJsonIndex self, IEnumerable<JObject> docs)
        {
            self.CreateWriter().Create(docs);
            return self;
        }

        public static ILuceneJsonIndex Update(this ILuceneJsonIndex self, JObject doc)
        {
            self.CreateWriter().Update(doc);
            return self;
        }

        public static ILuceneJsonIndex Update(this ILuceneJsonIndex self, IEnumerable<JObject> docs)
        {
            self.CreateWriter().Update(docs);
            return self;
        }
        
        public static ILuceneJsonIndex Delete(this ILuceneJsonIndex self, JObject doc)
        {
            self.CreateWriter().Delete(doc);
            return self;
        }

        public static ILuceneJsonIndex Delete(this ILuceneJsonIndex self, IEnumerable<JObject> docs)
        {
            self.CreateWriter().Delete(docs);
            return self;
        }
        
        public static ILuceneJsonIndex Flush(this ILuceneJsonIndex self, bool triggerMerge, bool applyDeletes)
        {
            self.CreateWriter().Flush(triggerMerge, applyDeletes);
            return self;
        }

        public static ILuceneJsonIndex Commit(this ILuceneJsonIndex self)
        {
            self.CreateWriter().Commit();
            return self;
        }
    }
}
