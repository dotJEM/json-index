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
        void Write(JObject entity);
        void WriteAll(IEnumerable<JObject> entities);
        void Delete(JObject entity);
        void DeleteAll(IEnumerable<JObject> entities);
        void Optimize();
        ILuceneWriteContext WriteContext();
    }

    public interface ILuceneWriteContext : IDisposable
    {
        Task Create(JObject entity);
        Task CreateAll(IEnumerable<JObject> entities);
        Task Write(JObject entity);
        Task WriteAll(IEnumerable<JObject> entities);
        Task Delete(JObject entity);
        Task DeleteAll(IEnumerable<JObject> entities);
    }

    internal class LuceneWriteContext : ILuceneWriteContext
    {
        private readonly IndexWriter writer;
        private readonly LuceneStorageIndex index;
        private readonly IDocumentFactory factory;

        public LuceneWriteContext(IndexWriter writer, IDocumentFactory factory, LuceneStorageIndex index)
        {
            this.writer = writer;
            this.factory = factory;
            this.index = index;
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
                ParallelQuery<int> executed = from entity in entities.AsParallel()
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
                ParallelQuery<int> executed = from entity in entities.AsParallel()
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
            await Task.Run(() =>
            {
                writer.DeleteDocuments(entities.Select(CreateIdentityTerm).Where(x => x != null).ToArray());
            });
        }

        public void Dispose()
        {
            writer.Commit();
        }

        private Term CreateIdentityTerm(JObject entity)
        {
            try
            {
                return index.Configuration.IdentityResolver.CreateTerm(entity);
            }
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
                return 1;
            }
        }

        private Document InternalCreateDocument(JObject entity)
        {
            try
            {
                return factory.Create(entity);
            }
            catch (Exception)
            {
                return null;
            }

        }
    }

    internal class LuceneWriter : ILuceneWriter
    {
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

        public ILuceneWriteContext WriteContext()
        {
            return new LuceneWriteContext(index.Storage.GetWriter(index.Analyzer), factory, index);
        }

        public void Write(JObject entity)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);
            writer.UpdateDocument(CreateIdentityTerm(entity), factory.Create(entity));
            writer.Commit();
        }

        private static int InternalUpdateDocument(IndexWriter writer, Term term, Document doc)
        {
            try
            {
                writer.UpdateDocument(term, doc);
                return 0;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private Document InternalCreateDocument(JObject entity)
        {
            try
            {
                return factory.Create(entity);
            }
            catch (Exception)
            {
                return null;
            }

        }

        public void WriteAll(IEnumerable<JObject> entities)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);

            ParallelQuery<int> executed = from entity in entities.AsParallel()
                let term = CreateIdentityTerm(entity)
                where term != null
                let document = InternalCreateDocument(entity)
                where document != null
                select InternalUpdateDocument(writer, term, document);
            int failed = executed.Sum();

            writer.Commit();
        }


        public void Delete(JObject entity)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);
            writer.DeleteDocuments(CreateIdentityTerm(entity));
            writer.Commit();
        }

        public void DeleteAll(IEnumerable<JObject> entities)
        {
            IndexWriter writer = index.Storage.GetWriter(index.Analyzer);
            writer.DeleteDocuments(entities.Select(CreateIdentityTerm).Where(x => x != null).ToArray());
            writer.Commit();
        }

        public void Optimize()
        {
            index.Storage.GetWriter(index.Analyzer).Optimize();
        }

        private Term CreateIdentityTerm(JObject entity)
        {
            try
            {
                return index.Configuration.IdentityResolver.CreateTerm(entity);
            }
            catch (Exception ex)
            {
                //ignore
                return null;
            }
        }
    }
}
