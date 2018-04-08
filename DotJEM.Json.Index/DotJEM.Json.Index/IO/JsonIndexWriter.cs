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

        Task CreateAsync(JObject doc);
        Task CreateAsync(IEnumerable<JObject> docs);
        Task DeleteAsync(JObject doc);
        Task DeleteAsync(IEnumerable<JObject> docs);
        Task UpdateAsync(JObject doc);
        Task UpdateAsync(IEnumerable<JObject> docs);

        Task CommitAsync();
        Task ForceMergeAsync(int maxSegments);
        Task ForceMergeAsync(int maxSegments, bool wait);
        Task ForgeMergeDeletesAsync();
        Task ForgeMergeDeletesAsync(bool wait);
        Task RollbackAsync();
        Task FlushAsync(bool triggerMerge, bool applyDeletes);
    }

    public class JsonIndexWriter : Disposable, IJsonIndexWriter
    {
        private readonly IIndexWriterManager manager;
        private readonly ILuceneDocumentFactory factory;
        private readonly ConcurrentDictionary<Guid, Task<Guid>> jobs = new ConcurrentDictionary<Guid, Task<Guid>>();

        public ILuceneJsonIndex Index { get; }
        public IndexWriter UnderlyingWriter => manager.Writer;

        public JsonIndexWriter(ILuceneJsonIndex index, ILuceneDocumentFactory factory, IIndexWriterManager manager)
        {
            Index = index;
            this.factory = factory;
            this.manager = manager;
        }

        public async Task CreateAsync(JObject doc) => await CreateAsync(new[] { doc });

        public async Task CreateAsync(IEnumerable<JObject> docs)
        {
            await Async(writer =>
            {
                IEnumerable<Document> documents = factory
                    .Create(docs)
                    .Select(tuple => tuple.Document);
                writer.AddDocuments(documents);
            });
        }

        public async Task DeleteAsync(JObject doc) => await DeleteAsync(new[] { doc });

        public async Task DeleteAsync(IEnumerable<JObject> docs)
        {
            await WhenAll(writer => {
                IEnumerable<LuceneDocumentEntry> documents = factory.Create(docs);
                foreach ((Term key, Document _) in documents)
                {
                    writer.DeleteDocuments(key);
                }
            });
        }

        public async Task UpdateAsync(JObject doc) => await UpdateAsync(new[] { doc });

        public async Task UpdateAsync(IEnumerable<JObject> docs)
        {
            await WhenAll(writer => {
                IEnumerable<LuceneDocumentEntry> documents = factory.Create(docs);
                foreach ((Term key, Document doc) in documents)
                {
                    writer.UpdateDocument(key, doc);
                }
            });
        }

        public async Task CommitAsync() => await WhenAll(writer => writer.Commit());
        public async Task ForceMergeAsync(int maxSegments) => await WhenAll(writer => writer.ForceMerge(maxSegments));
        public async Task ForceMergeAsync(int maxSegments, bool wait) => await WhenAll(writer => writer.ForceMerge(maxSegments, wait));
        public async Task ForgeMergeDeletesAsync() => await WhenAll(writer => writer.ForceMergeDeletes());
        public async Task ForgeMergeDeletesAsync(bool wait) => await WhenAll(writer => writer.ForceMergeDeletes(wait));
        public async Task RollbackAsync() => await WhenAll(writer => writer.Rollback());
        public async Task FlushAsync(bool triggerMerge, bool applyDeletes) => await WhenAll(writer => writer.Flush(triggerMerge, applyDeletes));

        public async Task SetCommitDataAsync(IDictionary<string, string> commitUserData) => await WhenAll(writer => writer.SetCommitData(commitUserData));
        public async Task PrepareCommit() => await WhenAll(writer => writer.PrepareCommit());

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Note: CommitAsync data and release the writer.
                //      The storage should handle the life cycle.
                CommitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            base.Dispose(disposing);
        }

        private async Task Async(Action<IndexWriter> action)
        {
            Task<Guid> task = jobs
                .GetOrAdd(Guid.NewGuid(), key => Task.Run(() => action(UnderlyingWriter)).ThenReturn(key));
            #pragma warning disable 4014
            task.Then(id => jobs.TryRemove(id, out Task<Guid> _));
            #pragma warning restore 4014
            await task.ConfigureAwait(false);
        }

        private async Task WhenAll(Action<IndexWriter> action)
        {
            await Task.WhenAll(jobs.Values).ConfigureAwait(false);
            await Async(action).ConfigureAwait(false);
        }
    }
}
