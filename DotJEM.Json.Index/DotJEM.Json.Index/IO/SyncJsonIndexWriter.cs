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
    public class SyncJsonIndexWriter : Disposable, IJsonIndexWriter
    {
        private readonly IIndexWriterManager manager;
        private readonly ILuceneDocumentFactory factory;

        public ILuceneJsonIndex Index { get; }
        public IndexWriter UnderlyingWriter => manager.Writer;

        public SyncJsonIndexWriter(ILuceneJsonIndex index, ILuceneDocumentFactory factory, IIndexWriterManager manager)
        {
            Index = index;
            this.factory = factory;
            this.manager = manager;
        }

        public IJsonIndexWriter Create(JObject doc, IReservedSlot reservedSlot = null) => Create(new[] { doc }, reservedSlot);

        public IJsonIndexWriter Create(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            IEnumerable<Document> documents = factory
                .Create(docs)
                .Select(tuple => tuple.Document);
            UnderlyingWriter.AddDocuments(documents);
            return this;
        }

        public IJsonIndexWriter Update(JObject doc, IReservedSlot reservedSlot = null) => Update(new[] { doc }, reservedSlot);
        public IJsonIndexWriter Update(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
        {
            IEnumerable<LuceneDocumentEntry> documents = factory.Create(docs);
            foreach ((Term key, Document doc) in documents)
                UnderlyingWriter.UpdateDocument(key, doc);
            return this;
        }

        public IJsonIndexWriter Delete(JObject doc, IReservedSlot reservedSlot = null) => Delete(new[] { doc }, reservedSlot);
        public IJsonIndexWriter Delete(IEnumerable<JObject> docs, IReservedSlot reservedSlot = null)
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
}