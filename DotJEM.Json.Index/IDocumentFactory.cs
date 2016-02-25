using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Visitors;
using Lucene.Net.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index
{
    public interface IDocumentFactory
    {
        Document Create(JObject value);
    }

    public class LuceneDocumentFactory : IDocumentFactory
    {
        private readonly IStorageIndex index;
        private readonly IJSchemaGenerator generator;
        private readonly IDocumentBuilder builder;

        public LuceneDocumentFactory(IStorageIndex index)
            : this(index, new DefaultDocumentBuilder(index), new JSchemaGenerator())
        {
        }

        public LuceneDocumentFactory(IStorageIndex index, IDocumentBuilder builder, IJSchemaGenerator generator)
        {
            this.index = index;
            this.generator = generator;
            this.builder = builder;
        }

        public Document Create(JObject value)
        {
            string contentType = index.Configuration.TypeResolver.Resolve(value);
            string storageArea = index.Configuration.AreaResolver.Resolve(value);

            JSchema schema = index.Schemas[contentType];
            schema = schema == null
                ? generator.Generate(value, contentType, storageArea)
                : schema.Merge(generator.Generate(value, contentType, storageArea));
            index.Schemas[contentType] = schema;
            
            Document document = builder.Build(contentType, value);

            document.Add(new Field(index.Configuration.RawField, value.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            return document;
        }
    }
}