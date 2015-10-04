using DotJEM.Json.Index.Sharding.Visitors;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Documents
{
    public interface IDocumentFactory
    {
        IDocumentCommand Create(JObject value);
    }
    public interface IDocumentCommand
    {
        void Execute(IndexWriter writer);
    }

    public class UpdateDocumentCommand : IDocumentCommand
    {
        private readonly Term term;
        private readonly Document document;

        public UpdateDocumentCommand(Term term, Document document)
        {
            this.term = term;
            this.document = document;
        }

        public void Execute(IndexWriter writer)
        {
            writer.UpdateDocument(term, document);
        }
    }

    public class LuceneDocumentFactory : Documents.IDocumentFactory
    {
        private readonly IJSchemaManager schemas;
        private readonly IMetaFieldResolver resolver;

        public LuceneDocumentFactory(IMetaFieldResolver resolver, IJSchemaManager schemas)
        {
            this.resolver = resolver;
            this.schemas = schemas;
        }

        public IDocumentCommand Create(JObject value)
        {
            schemas.Update(value);
            AbstractDocumentBuilder builder = new DefaultDocumentBuilder();
            DocumentBuilderContext context = new DocumentBuilderContext();
            builder.Visit(value, context);
            return new UpdateDocumentCommand(resolver.Identifier(value), context.Document);
        }
    }
}