using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
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
            catch (Exception)
            {
                //ignore
                return null;
            }
        }
    }
}
