using System;
using System.Collections.Generic;
using System.Linq;
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

        public IJsonIndexWriter Create(IEnumerable<JObject> docs)
        {
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) => writerManager.Writer.AddDocuments(documents.Select(x => x.Document)));
            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.Medium);
            //IEnumerable<Document> documents = factory
            //    .Create(docs)
            //    .Select(tuple => tuple.Document);
            //UnderlyingWriter.AddDocuments(documents);
            return this;
        }

        public IJsonIndexWriter Update(JObject doc) => Update(new[] { doc });
        public IJsonIndexWriter Update(IEnumerable<JObject> docs)
        {
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                foreach ((Term key, Document doc) in documents)
                    writerManager.Writer.UpdateDocument(key, doc);
            });
            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.Medium);

            //IEnumerable<LuceneDocumentEntry> documents = factory.Create(docs);
            //foreach ((Term key, Document doc) in documents)
            //    UnderlyingWriter.UpdateDocument(key, doc);
            return this;
        }

        public IJsonIndexWriter Delete(JObject doc) => Delete(new[] { doc });
        public IJsonIndexWriter Delete(IEnumerable<JObject> docs)
        {
            IReservedSlot slot = Inflow.Queue.Reserve((writerManager, documents) =>
            {
                foreach ((Term key, Document _) in documents)
                    writerManager.Writer.DeleteDocuments(key);
            });
            Inflow.Scheduler.Enqueue(new ConvertInflow(slot, docs, Factory), Priority.Medium);
            //IEnumerable<LuceneDocumentEntry> documents = factory.Create(docs);
            //foreach ((Term key, Document _) in documents)
            //    UnderlyingWriter.DeleteDocuments(key);
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
            UnderlyingWriter.Flush(triggerMerge, applyDeletes);
            return this;
        }

        public IJsonIndexWriter Commit()
        {
            UnderlyingWriter.Commit();
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
