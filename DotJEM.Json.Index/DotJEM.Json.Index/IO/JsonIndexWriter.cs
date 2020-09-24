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

        IJsonIndexWriter Create(JObject doc);
        IJsonIndexWriter Create(IEnumerable<JObject> docs);
        IJsonIndexWriter Update(JObject doc);
        IJsonIndexWriter Update(IEnumerable<JObject> docs);
        IJsonIndexWriter Delete(JObject doc);
        IJsonIndexWriter Delete(IEnumerable<JObject> docs);

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

        public IJsonIndexWriter Create(JObject doc) => Create(new[] { doc });
        private int counter = 0;
        public IJsonIndexWriter Create(IEnumerable<JObject> docs)
        {
            //List<LuceneDocumentEntry> documents = Factory
            //    .Create(docs)
            //    .ToList();
            //UnderlyingWriter.AddDocuments(documents.Select(d => d.Document));
            int nr = counter++;
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                Debug.WriteLine($"Completed Create: {nr}");
                Console.WriteLine($"Completed Create: {nr}");
                writerManager.Writer.AddDocuments(documents.Select(x => x.Document));
                wait.Set();
            }, id:nr);



            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.High);
            //wait.WaitOne();
            return this;
        }

        public IJsonIndexWriter Update(JObject doc) => Update(new[] { doc });
        public IJsonIndexWriter Update(IEnumerable<JObject> docs)
        {
            //List<LuceneDocumentEntry> documents = Factory
            //    .Create(docs)
            //    .ToList();
            //foreach (LuceneDocumentEntry entry in documents)
            //    UnderlyingWriter.UpdateDocument(entry.Key, entry.Document);

            int nr = counter++;
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                Debug.WriteLine($"Completed Update: {nr}");
                Console.WriteLine($"Completed Update: {nr}");
                foreach ((Term key, Document doc) in documents)
                    writerManager.Writer.UpdateDocument(key, doc);
                wait.Set();
            }, id: nr);
            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.High);
            //wait.WaitOne();
            return this;
        }

        public IJsonIndexWriter Delete(JObject doc) => Delete(new[] { doc });
        public IJsonIndexWriter Delete(IEnumerable<JObject> docs)
        {
            //List<LuceneDocumentEntry> documents = Factory
            //    .Create(docs)
            //    .ToList();
            //foreach (LuceneDocumentEntry entry in documents)
            //    UnderlyingWriter.DeleteDocuments(entry.Key);

            int nr = counter++;
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                Debug.WriteLine($"Completed Delete: {nr}");
                Console.WriteLine($"Completed Delete: {nr}");
                foreach ((Term key, Document _) in documents)
                    writerManager.Writer.DeleteDocuments(key);
                wait.Set();
            }, id: nr);
            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.High);
            //wait.WaitOne();
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
                Debug.WriteLine($"Completed Flush: {nr}");
                Console.WriteLine($"Completed Flush: {nr}");
                writerManager.Writer.Flush(triggerMerge, applyDeletes);
                wait.Set();
            }, id: nr);
            Inflow.Scheduler.Enqueue(new CommonInflowJob(slot), Priority.Medium);
            wait.WaitOne();

            //UnderlyingWriter.Flush(triggerMerge,applyDeletes);
            return this;
        }

        public IJsonIndexWriter Commit()
        {
            int nr = counter++;
            AutoResetEvent wait = new AutoResetEvent(false);
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                Debug.WriteLine($"Completed Commit: {nr}");
                Console.WriteLine($"Completed Commit: {nr}");
                writerManager.Writer.Commit();
                wait.Set();
            }, id: nr);
            Inflow.Scheduler.Enqueue(new CommonInflowJob(slot), Priority.Medium);
            wait.WaitOne();

            //UnderlyingWriter.Commit();
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
            slot.Complete();
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
