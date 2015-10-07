using DotJEM.Json.Index.Sharding.Configuration;
using DotJEM.Json.Index.Sharding.Resolvers;
using DotJEM.Json.Index.Sharding.Schemas;
using DotJEM.Json.Index.Sharding.Visitors;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Documents
{
    public interface ILuceneDocumentFactory
    {
        Document Create(JObject value);
    }

    public class DefaultLuceneDocumentFactory : ILuceneDocumentFactory
    {
        private readonly IJSchemaManager schemas;
        private readonly IDocumentBuilder builder;
        //private readonly IDo

        public DefaultLuceneDocumentFactory(IJSchemaManager schemas, IDocumentBuilder builder)
        {
            this.schemas = schemas;
            this.builder = builder;
        }

        public Document Create(JObject json)
        {
            schemas.Update(json);
            return builder.BuildDocument(json).Document;
        }
    }
}