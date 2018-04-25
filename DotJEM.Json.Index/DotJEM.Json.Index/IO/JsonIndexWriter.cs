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

        Task CreateAsync(JObject doc);
        Task CreateAsync(IEnumerable<JObject> docs);
        Task DeleteAsync(JObject doc);
        Task DeleteAsync(IEnumerable<JObject> docs);
        Task UpdateAsync(JObject doc);
        Task UpdateAsync(IEnumerable<JObject> docs);

        Task ForceMergeAsync(int maxSegments);
        Task ForceMergeAsync(int maxSegments, bool wait);
        Task ForceMergeDeletesAsync();
        Task ForceMergeDeletesAsync(bool wait);
        Task RollbackAsync();
        Task FlushAsync(bool triggerMerge, bool applyDeletes);
        Task CommitAsync();
        Task PrepareCommitAsync();
        Task SetCommitDataAsync(IDictionary<string, string> commitUserData);
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

        public async Task CreateAsync(JObject doc) => await CreateAsync(new[] { doc });
        public async Task CreateAsync(IEnumerable<JObject> docs) => await Task.Run(() => Create(docs));
        public async Task UpdateAsync(JObject doc) => await UpdateAsync(new[] { doc });
        public async Task UpdateAsync(IEnumerable<JObject> docs) => await Task.Run(() => Update(docs));
        public async Task DeleteAsync(JObject doc) => await DeleteAsync(new[] { doc });
        public async Task DeleteAsync(IEnumerable<JObject> docs) => await Task.Run(() => Delete(docs));

        public async Task CommitAsync() => await Task.Run(() => Commit());
        public async Task ForceMergeAsync(int maxSegments) => await Task.Run(() => ForceMerge(maxSegments));
        public async Task ForceMergeAsync(int maxSegments, bool wait) => await Task.Run(() => ForceMerge(maxSegments, wait));
        public async Task ForceMergeDeletesAsync() => await Task.Run(() => ForceMergeDeletes());
        public async Task ForceMergeDeletesAsync(bool wait) => await Task.Run(() => ForceMergeDeletes(wait));
        public async Task RollbackAsync() => await Task.Run(() => Rollback());
        public async Task FlushAsync(bool triggerMerge, bool applyDeletes) => await Task.Run(() => Flush(triggerMerge, applyDeletes));
        public async Task SetCommitDataAsync(IDictionary<string, string> commitUserData) => await Task.Run(() => SetCommitData(commitUserData));
        public async Task PrepareCommitAsync() => await Task.Run(() => PrepareCommit());

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Note: CommitAsync data and release the writer.
                //      The storage should handle the life cycle.
                Commit();
            }
            base.Dispose(disposing);
        }
    }
}
