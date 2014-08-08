using System.Collections.Generic;
using Lucene.Net.Analysis.Standard;
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
    }

    internal class LuceneWriter : ILuceneWriter
    {
        private readonly IStorageIndex index;
        private readonly IDocumentFactory factory;

        private readonly StandardAnalyzer analyzer = new StandardAnalyzer(Version.LUCENE_30);

        public LuceneWriter(IStorageIndex index) 
            : this(index, new LuceneDocumentFactory(index))
        {
        }

        public LuceneWriter(IStorageIndex index, IDocumentFactory factory)
        {
            this.index = index;
            this.factory = factory;
        }

        public void Write(JObject entity)
        {
            //TODO: Try Finaly Release Writer, it also doesn't make sense to keep the analyzer here and pass it each time.
            IndexWriter writer = index.Storage.GetWriter(analyzer);
            InternalWrite(writer, entity);
            writer.Commit();
            //TODO: Optimize after a number of additions. Should be an option, we should also kick off that optimization on a different thread.
            //writer.Optimize();
        }

        public void WriteAll(IEnumerable<JObject> entities)
        {
            IndexWriter writer = index.Storage.GetWriter(analyzer);
            //writer.MergeFactor = 10000;
            foreach (JObject entity in entities)
            {
                InternalWrite(writer, entity);
            }
            writer.Commit();
            //TODO: Remove Optimize and make it explicit instead.
            //writer.Optimize();
        }

        public void Delete(JObject entity)
        {
            IndexWriter writer = index.Storage.GetWriter(analyzer);
            writer.DeleteDocuments(CreateTerm(entity));
            writer.Commit();
            //writer.Optimize();
        }

        private void InternalWrite(IndexWriter writer, JObject entity)
        {
            writer.UpdateDocument(CreateTerm(entity), factory.Create(entity));
        }

        private Term CreateTerm(JObject entity)
        {
            string contentType = index.Configuration.TypeResolver.Resolve(entity);
            Term term = index.Configuration.Identity.Strategy(contentType, null).CreateTerm(entity);
            return term;
        }
    }
}
