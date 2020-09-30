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
            this.Inflow = new InflowManager(manager, index.Services.Resolve<IInflowCapacity>());
        }

        public IJsonIndexWriter Create(JObject doc, IReservedSlot reservedSlot = null) => Create(new[] { doc }, reservedSlot);
        private int counter = 0;
        public IJsonIndexWriter Create(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                writerManager.Writer.AddDocuments(documents.Select(x => x.Document));
                wait.Set();
            });
            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.High);
            wait.WaitOne();
            return this;
        }

        public IJsonIndexWriter Update(JObject doc, IReservedSlot reservedSlot = null) => Update(new[] { doc }, reservedSlot);
        public IJsonIndexWriter Update(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                foreach ((Term key, Document doc) in documents)
                    writerManager.Writer.UpdateDocument(key, doc);
                wait.Set();
            });
            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.High);
            wait.WaitOne();
            return this;
        }

        public IJsonIndexWriter Delete(JObject doc, IReservedSlot reservedSlot = null) => Delete(new[] { doc }, reservedSlot);
        public IJsonIndexWriter Delete(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                foreach ((Term key, Document _) in documents)
                    writerManager.Writer.DeleteDocuments(key);
                wait.Set();
            });

            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.High);
            wait.WaitOne();
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
            int nr = counter++;
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                writerManager.Writer.Flush(triggerMerge, applyDeletes);
                wait.Set();
            }, id: nr);
            Inflow.Scheduler.Enqueue(new CommonInflowJob(slot), Priority.Medium);
            wait.WaitOne();
            return this;
        }

        public IJsonIndexWriter Commit()
        {
            int nr = counter++;
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                writerManager.Writer.Commit();
                wait.Set();
            });
            Inflow.Scheduler.Enqueue(new CommonInflowJob(slot), Priority.Medium);
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
