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

    public class JsonIndexWriter : Disposable, IJsonIndexWriter
    {
        private readonly IIndexWriterManager manager;
        private readonly ILuceneDocumentFactory factory;

        public ILuceneJsonIndex Index { get; }
        public IndexWriter UnderlyingWriter => manager.Writer;

        public JsonIndexWriter(ILuceneJsonIndex index, ILuceneDocumentFactory factory, IIndexWriterManager manager)
        {
            Index = index;
            this.factory = factory;
            this.manager = manager;
        }

        public IJsonIndexWriter Create(JObject doc) => Create(new[] { doc });

        public IJsonIndexWriter Create(IEnumerable<JObject> docs)
        {
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
}
