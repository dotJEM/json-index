using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index
{

    public interface ILuceneWriter
    {
        //TODO: Need a better design, but similar to lucene's info streams.
        event EventHandler<IndexWriterInfoEventArgs> InfoEvent;

        void Write(JObject entity);
        void WriteAll(IEnumerable<JObject> entities);
        void Delete(JObject entity);
        void DeleteAll(IEnumerable<JObject> entities);
        void Optimize();

        void Commit();
        void Flush(bool triggerMerge = false, bool flushDocStores = false, bool flushDeletes = false);
        ILuceneWriteContext WriteContext(int buffersize = 512);
    }

    // TODO: Need to work in a async full implementation instead of this.
    public interface ILuceneWriteContext : IDisposable
    {
        //TODO: Need a better design, but similar to lucene's info streams.
        event EventHandler<IndexWriterInfoEventArgs> InfoEvent;

        Task Create(JObject entity);
        Task CreateAll(IEnumerable<JObject> entities);
        Task Write(JObject entity);
        Task WriteAll(IEnumerable<JObject> entities);
        Task Delete(JObject entity);
        Task DeleteAll(IEnumerable<JObject> entities);
    }

    internal class LuceneWriteContext : ILuceneWriteContext
    {
        public event EventHandler<IndexWriterInfoEventArgs> InfoEvent;

        private readonly IndexWriter writer;
        private readonly LuceneStorageIndex index;
        private readonly double buffersize;
        private readonly IDocumentFactory factory;

        public LuceneWriteContext(IndexWriter writer, IDocumentFactory factory, LuceneStorageIndex index, double buffersize)
        {
            this.buffersize = writer.GetRAMBufferSizeMB();

            this.writer = writer;
            this.factory = factory;
            this.index = index;

            writer.SetRAMBufferSizeMB(buffersize);
        }


        public async Task Create(JObject entity)
        {
            await Task.Run(() =>
            {
                writer.AddDocument(factory.Create(entity));
            });
        }

        public async Task CreateAll(IEnumerable<JObject> entities)
        {
            await Task.Run(() =>
            {
                IEnumerable<int> executed = from entity in entities
                                              let document = InternalCreateDocument(entity)
                                              where document != null
                                              select InternalAddDocument(document);
                return executed.Sum();
            });
        }


        public async Task Write(JObject entity)
        {
            await Task.Run(() =>
            {
                writer.UpdateDocument(CreateIdentityTerm(entity), factory.Create(entity));
            });
        }

        public async Task WriteAll(IEnumerable<JObject> entities)
        {
            await Task.Run(() =>
            {
                //Note: We cannot do this in paralel as it could potentially apply two updates for the same document in the wrong order.
                //      So we have to group by "ID"'s before we can consider doing it in paralel.
                IEnumerable<int> executed = from entity in entities
                                              let term = CreateIdentityTerm(entity)
                                              where term != null
                                              let document = InternalCreateDocument(entity)
                                              where document != null
                                              select InternalUpdateDocument(term, document);
                return executed.Sum();
            });
        }

        public async Task Delete(JObject entity)
        {
            await Task.Run(() =>
            {
                writer.DeleteDocuments(CreateIdentityTerm(entity));
            });
        }

        public async Task DeleteAll(IEnumerable<JObject> entities)
        {
            await Task.Run(() => {
                writer.DeleteDocuments(entities.Select(CreateIdentityTerm).Where(x => x != null).ToArray());
            });
        }

        public void Dispose()
        {
            writer.Commit();
            writer.SetRAMBufferSizeMB(buffersize);
        }

        private Term CreateIdentityTerm(JObject entity)
        {
            try
            {
                return index.Configuration.IdentityResolver.CreateTerm(entity);
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to create identity term from entity: " + entity, ex));
                //ignore
                return null;
            }
        }

        private int InternalAddDocument(Document document)
        {
            try
            {
                writer.AddDocument(document);
                return 0;
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to add document: " + document, ex));
                return 1;
            }
        }

        private int InternalUpdateDocument(Term term, Document document)
        {
            try
            {
                writer.UpdateDocument(term, document);
                return 0;
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to update document: " + document, ex));
                return 1;
            }
        }

        private Document InternalCreateDocument(JObject entity)
        {
            try
            {
                return factory.Create(entity);
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to create document from entity: " + entity, ex));
                return null;
            }

        }

        protected virtual void OnInfoEvent(IndexWriterInfoEventArgs e)
        {
            InfoEvent?.Invoke(this, e);
        }
    }

    internal class LuceneWriter : ILuceneWriter
    {
        public event EventHandler<IndexWriterInfoEventArgs> InfoEvent;

        private readonly IDocumentFactory factory;
        private readonly LuceneStorageIndex index;

        public LuceneWriter(LuceneStorageIndex index) 
            : this(index, new DefaultDocumentFactory(index))
        {
        }

        public LuceneWriter(LuceneStorageIndex index, IDocumentFactory factory)
        {
            this.index = index;
            this.factory = factory;
        }

        public ILuceneWriteContext WriteContext(int buffersize = 512)
        {
            return new LuceneWriteContext(index.Storage.GetWriter(index.Analyzer), factory, index, buffersize);
        }

        public void Write(JObject entity)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);
            writer.UpdateDocument(CreateIdentityTerm(entity), factory.Create(entity));
        }

        private int InternalUpdateDocument(IndexWriter writer, Term term, Document doc)
        {
            try
            {
                writer.UpdateDocument(term, doc);
                return 0;
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to update document from entity: " + doc, ex));
                return 1;
            }
        }

        private Document InternalCreateDocument(JObject entity)
        {
            try
            {
                return factory.Create(entity);
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to create document from entity: " + entity, ex));
                return null;
            }

        }

        public void WriteAll(IEnumerable<JObject> entities)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);
            IEnumerable<int> executed = from entity in entities
                let term = CreateIdentityTerm(entity)
                where term != null
                let document = InternalCreateDocument(entity)
                where document != null
                select InternalUpdateDocument(writer, term, document);
            int failed = executed.Sum();
        }


        public void Delete(JObject entity)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);
            writer.DeleteDocuments(CreateIdentityTerm(entity));
        }

        public void DeleteAll(IEnumerable<JObject> entities)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);
            writer.DeleteDocuments(entities.Select(CreateIdentityTerm).Where(x => x != null).ToArray());
        }

        public void Optimize()
        {
            index.Storage.GetWriter(index.Analyzer).Optimize();
        }

        public void Commit()
        {
            index.Storage.GetWriter(index.Analyzer).Commit();
        }

        public void Flush(bool triggerMerge = false, bool flushDocStores = false, bool flushDeletes = false)
        {
            index.Storage.GetWriter(index.Analyzer).Flush(triggerMerge, flushDocStores, flushDeletes);
        }


        private Term CreateIdentityTerm(JObject entity)
        {
            try
            {
                return index.Configuration.IdentityResolver.CreateTerm(entity);
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to create identity term from entity: " + entity, ex));
                //ignore
                return null;
            }
        }
        protected virtual void OnInfoEvent(IndexWriterInfoEventArgs e)
        {
            InfoEvent?.Invoke(this, e);
        }
    }
}
