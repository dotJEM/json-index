using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
        ILuceneWriteContext WriteContext(ILuceneWriteContextSettings settings = null);
    }

    // TODO: Need to work in a async full implementation instead of this.
    public interface ILuceneWriteContext : IDisposable
    {
        //TODO: Need a better design, but similar to lucene's info streams.
        event EventHandler<IndexWriterInfoEventArgs> InfoEvent;

        int WriteCount { get; }

        Task Create(JObject entity);
        Task CreateAll(IEnumerable<JObject> entities);
        Task Write(JObject entity);
        Task WriteAll(IEnumerable<JObject> entities);
        Task Delete(JObject entity);
        Task DeleteAll(IEnumerable<JObject> entities);

        Task Commit();
    }

    public interface ILuceneWriteContextSettings
    {
        double BufferSize { get; }
        void AfterWrite(IndexWriter writer, LuceneStorageIndex index, int counterValue);
    }

    public class DefaultLuceneWriteContextSettings : ILuceneWriteContextSettings
    {
        public double BufferSize { get; } = 512;
        public void AfterWrite(IndexWriter writer, LuceneStorageIndex index, int counterValue) { }
    }

    internal class LuceneWriteContext : ILuceneWriteContext
    {
        public event EventHandler<IndexWriterInfoEventArgs> InfoEvent;

        private readonly IndexWriter writer;
        private readonly LuceneStorageIndex index;
        private readonly ILuceneWriteContextSettings settings;
        private readonly IDocumentFactory factory;

        private readonly InterlockedCounter counter = new InterlockedCounter();
        private readonly double buffersize;

        public int WriteCount => counter.Value;

        private class InterlockedCounter
        {
            public int Value { get; private set; }

            public int Add(int value)
            {
                lock (this)
                {
                    return Value += value;
                }
            }
        }

        public LuceneWriteContext(IndexWriter writer, IDocumentFactory factory, LuceneStorageIndex index, ILuceneWriteContextSettings settings = null)
        {
            this.buffersize = writer.GetRAMBufferSizeMB();
            this.writer = writer;
            this.factory = factory;
            this.index = index;
            this.settings = settings ?? new DefaultLuceneWriteContextSettings();
            writer.SetRAMBufferSizeMB(this.settings.BufferSize);
        }

        public Task Create(JObject entity)
        {
            return Task.Run(() =>
            {
                writer.AddDocument(factory.Create(entity));
                PostWrite();
            });
        }

        public Task CreateAll(IEnumerable<JObject> entities)
        {
            return Task.Run(() =>
            {
                IEnumerable<int> executed = from entity in entities
                                              let document = InternalCreateDocument(entity)
                                              where document != null
                                              select InternalAddDocument(document);
                PostWrite(executed.Sum());
            });
        }

        private void PostWrite(int count = 1)
        {
            settings.AfterWrite(writer, index, counter.Add(count));
        }

        public Task Write(JObject entity)
        {
            return Task.Run(() =>
            {
                writer.UpdateDocument(CreateIdentityTerm(entity), factory.Create(entity));
                PostWrite();
            });
        }

        public Task WriteAll(IEnumerable<JObject> entities)
        {
            return Task.Run(() =>
            {
                //Note: We cannot do this in paralel as it could potentially apply two updates for the same document in the wrong order.
                //      So we have to group by "ID"'s before we can consider doing it in paralel.
                IEnumerable<int> executed = from entity in entities
                                              let term = CreateIdentityTerm(entity)
                                              where term != null
                                              let document = InternalCreateDocument(entity)
                                              where document != null
                                              select InternalUpdateDocument(term, document);
                PostWrite(executed.Sum());
            });
        }

        public Task Delete(JObject entity)
        {
            return Task.Run(() => writer.DeleteDocuments(CreateIdentityTerm(entity)));
        }

        public Task DeleteAll(IEnumerable<JObject> entities)
        {
            return Task.Run(() => writer.DeleteDocuments(entities.Select(CreateIdentityTerm).Where(x => x != null).ToArray()));
        }

        public Task Commit()
        {
            return Task.Run(writer.Commit);
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
                return 1;
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to add document: " + document, ex));
                return 0;
            }
        }

        private int InternalUpdateDocument(Term term, Document document)
        {
            try
            {
                writer.UpdateDocument(term, document);
                return 1;
            }
            catch (Exception ex)
            {
                OnInfoEvent(new IndexWriterExceptionEventArgs("Failed to update document: " + document, ex));
                return 0;
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

        public ILuceneWriteContext WriteContext(ILuceneWriteContextSettings settings = null)
        {
            return new LuceneWriteContext(index.Storage.Writer, factory, index, settings);
        }

        public void Write(JObject entity)
        {
            IndexWriter writer = index.Storage.Writer;
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
            IndexWriter writer = index.Storage.Writer;
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
            IndexWriter writer = index.Storage.Writer;
            writer.DeleteDocuments(CreateIdentityTerm(entity));
        }

        public void DeleteAll(IEnumerable<JObject> entities)
        {
            IndexWriter writer = index.Storage.Writer;
            writer.DeleteDocuments(entities.Select(CreateIdentityTerm).Where(x => x != null).ToArray());
        }

        public void Optimize()
        {
            index.Storage.Writer.Optimize();
        }

        public void Commit()
        {
            index.Storage.Writer.Commit();
        }

        public void Flush(bool triggerMerge = false, bool flushDocStores = false, bool flushDeletes = false)
        {
            index.Storage.Writer.Flush(triggerMerge, flushDocStores, flushDeletes);
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
