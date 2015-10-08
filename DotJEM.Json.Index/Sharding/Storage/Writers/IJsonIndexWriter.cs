using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Sharding.Util;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Sharding.Storage.Writers
{
    public interface IJsonIndexWriter : IDisposable
    {
        void AddDocument(Document doc);
        void AddIndexes(params IndexReader[] readers);
        void AddIndexesNoOptimize(params Directory[] dirs);
        void Commit(IDictionary<string, string> commitUserData);
        void Commit();
        void DeleteAll();
        void DeleteDocuments(params Query[] queries);
        void DeleteDocuments(Query query);
        void DeleteDocuments(params Term[] terms);
        void DeleteDocuments(Term term);
        void ExpungeDeletes(bool wait = true);
        void Flush(bool triggerMerge = true, bool flushDocStores = true, bool flushDeletes = true);
        bool HasDeletions();
        void Optimize(bool wait = true);
        void Optimize(int maxNumSegments, bool wait = true);
        void PrepareCommit();
        long RamSizeInBytes();
        void UpdateDocument(Term term, Document doc, Analyzer analyzer);
        void UpdateDocument(Term term, Document doc);
        void WaitForMerges();
    }

    public class JsonIndexWriter : Disposeable, IJsonIndexWriter
    {
        private readonly IndexWriterManager manager;

        public JsonIndexWriter(IndexWriterManager manager)
        {
            this.manager = manager;
        }

        #region Delegated Members

        public void AddDocument(Document doc)
        {
            TryInvocation(w => w.AddDocument(doc));
        }

        public void AddIndexes(params IndexReader[] readers)
        {
            TryInvocation(w => w.AddIndexes(readers));
        }

        public void AddIndexesNoOptimize(params Directory[] dirs)
        {
            TryInvocation(w => w.AddIndexesNoOptimize(dirs));
        }

        public void Commit(IDictionary<string, string> commitUserData)
        {
            TryInvocation(w => w.Commit(commitUserData));
        }

        public void Commit()
        {
            TryInvocation(w => w.Commit());
        }

        public void DeleteAll()
        {
            TryInvocation(w => w.DeleteAll());
        }

        public void DeleteDocuments(params Query[] queries)
        {
            TryInvocation(w => w.DeleteDocuments(queries));
        }

        public void DeleteDocuments(Query query)
        {
            TryInvocation(w => w.DeleteDocuments(query));
        }

        public void DeleteDocuments(params Term[] terms)
        {
            TryInvocation(w => w.DeleteDocuments(terms));
        }

        public void DeleteDocuments(Term term)
        {
            TryInvocation(w => w.DeleteDocuments(term));
        }

        public void ExpungeDeletes(bool wait = true)
        {
            TryInvocation(w => w.ExpungeDeletes(wait));
        }

        public void Flush(bool triggerMerge = true, bool flushDocStores = true, bool flushDeletes = true)
        {
            TryInvocation(w => w.Flush(triggerMerge, flushDocStores, flushDeletes));
        }

        public bool HasDeletions()
        {
            return TryInvocation(w => w.HasDeletions());
        }

        public void Optimize(bool wait = true)
        {
            TryInvocation(w => w.Optimize(wait));
        }

        public void Optimize(int maxNumSegments, bool wait = true)
        {
            TryInvocation(w => w.Optimize(maxNumSegments, wait));
        }

        public void PrepareCommit()
        {
            TryInvocation(w => w.PrepareCommit());
        }

        public long RamSizeInBytes()
        {
            return TryInvocation(w => w.RamSizeInBytes());
        }

        public void UpdateDocument(Term term, Document doc, Analyzer analyzer)
        {
            TryInvocation(w => w.UpdateDocument(term, doc, analyzer));
        }

        public void UpdateDocument(Term term, Document doc)
        {
            TryInvocation(w => w.UpdateDocument(term, doc));
        }

        public void WaitForMerges()
        {
            TryInvocation(w => w.WaitForMerges());
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            manager.Release(this);
        }

        private T TryInvocation<T>(Func<IndexWriter, T> func)
        {
            T result = default(T);
            TryInvocation(w => { result = func(w); });
            return result;
        }

        private void TryInvocation(Action<IndexWriter> act)
        {
            try
            {
                act(manager.UnderlyingWriter);
            }
            catch (OutOfMemoryException)
            {
                manager.Close();
            }
        }
    }
}