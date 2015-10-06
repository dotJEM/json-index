using DotJEM.Json.Index.Sharding.Configuration;
using DotJEM.Json.Index.Sharding.Schemas;
using DotJEM.Json.Index.Sharding.Visitors;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Documents
{
    public interface IDocumentFactory
    {
        Document Create(JObject value);
    }



    public class LuceneDocumentFactory : IDocumentFactory
    {
        private readonly IJSchemaManager schemas;
        private readonly IMetaFieldResolver resolver;

        public LuceneDocumentFactory(IMetaFieldResolver resolver, IJSchemaManager schemas)
        {
            this.resolver = resolver;
            this.schemas = schemas;
        }

        public Document Create(JObject value)
        {
            schemas.Update(value);
            AbstractDocumentBuilder builder = new DefaultDocumentBuilder();
            DocumentBuilderContext context = new DocumentBuilderContext();
            builder.Visit(value, context);
            return context.Document;
        }
    }
}