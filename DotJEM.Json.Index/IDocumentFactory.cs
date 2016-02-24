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
        private readonly IFieldFactory factory;
        private readonly IStorageIndex index;
        private readonly IJObjectEnumarator enumarator;
        private readonly IJSchemaGenerator generator;

        public LuceneDocumentFactory(IStorageIndex index)
            : this(index, new FieldFactory(index), new JObjectEnumerator(), new JSchemaGenerator())
        {
        }

        public LuceneDocumentFactory(IStorageIndex index, IFieldFactory factory, IJObjectEnumarator enumarator, IJSchemaGenerator generator)
        {
            this.index = index;
            this.factory = factory;
            this.enumarator = enumarator;
            this.generator = generator;
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
            
            IDocumentBuilder builder = new DefaultDocumentBuilder(index);
            Document document = builder.Build(value);

            Document document2 = enumarator
                .Enumerate(value)
                .Where(node => node.IsLeaf)
                .SelectMany(node => factory.Create(node.Path, contentType, node.Token as JValue))
                .Aggregate(new Document(), (doc, field) => doc.Put(field));

            document.Add(new Field(index.Configuration.RawField, value.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            return document;
        }
    }
}