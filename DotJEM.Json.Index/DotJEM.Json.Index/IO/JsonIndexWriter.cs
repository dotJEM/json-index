using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.IO
{
    public interface IJsonIndexWriter : IDisposable
    {
        ILuceneJsonIndex Index { get; }

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

    public interface ISlot
    {
        void Complete(IEnumerable<Document> docs);
    }

    public interface IInflowManager
    {
        IInflowScheduler Scheduler { get; }
        ISlot Reserve(string queue = null);

    }

    public interface IInflowJob
    {
        void Execute(IInflowScheduler scheduler);
    }

    public interface IInflowScheduler
    {
        void Enqueue(IInflowJob job, InflowPriority priority);
    }

    public class ConvertDocuments : IInflowJob
    {
        private readonly ISlot slot;
        private readonly IEnumerable<JObject> docs;
        private readonly ILuceneDocumentFactory factory;

        public ConvertDocuments(ISlot slot, IEnumerable<JObject> docs, ILuceneDocumentFactory factory)
        {
            this.slot = slot;
            this.docs = docs;
            this.factory = factory;
        }

        public void Execute(IInflowScheduler scheduler)
        {
            IEnumerable<Document> documents = factory
                .Create(docs)
                .Select(tuple => tuple.Document);
            slot.Complete(documents);
        }
    }

    public enum InflowPriority { Highest, High, Medium, Low, Lowest }

    public class JsonIndexWriter : Disposable, IJsonIndexWriter
    {
        private readonly IInflowManager inflow;
        private readonly IIndexWriterManager manager;
        private readonly ILuceneDocumentFactory factory;

        public ILuceneJsonIndex Index { get; }
        public IndexWriter UnderlyingWriter => manager.Writer;

        public JsonIndexWriter(ILuceneJsonIndex index, ILuceneDocumentFactory factory, IIndexWriterManager manager, IInflowManager inflow)
        {
            Index = index;
            this.factory = factory;
            this.manager = manager;
            this.inflow = inflow;
        }

        public IJsonIndexWriter Create(JObject doc) => Create(new[] { doc });

        public IJsonIndexWriter Create(IEnumerable<JObject> docs)
        {
            //inflow.Scheduler.Enqueue(new ConvertDocuments(inflow.Reserve(), docs, factory), InflowPriority.Medium);


            IEnumerable<Document> documents = factory
                .Create(docs)
                .Select(tuple => tuple.Document);
            UnderlyingWriter.AddDocuments(documents);
            return this;
        }

        public IJsonIndexWriter Update(JObject doc) => Update(new[] { doc });
        public IJsonIndexWriter Update(IEnumerable<JObject> docs)
        {
            IEnumerable<LuceneDocumentEntry> documents = factory.Create(docs);
            foreach ((Term key, Document doc) in documents)
                UnderlyingWriter.UpdateDocument(key, doc);
            return this;
        }

        public IJsonIndexWriter Delete(JObject doc) => Delete(new[] { doc });
        public IJsonIndexWriter Delete(IEnumerable<JObject> docs)
        {
            IEnumerable<LuceneDocumentEntry> documents = factory.Create(docs);
            foreach ((Term key, Document _) in documents)
                UnderlyingWriter.DeleteDocuments(key);
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
