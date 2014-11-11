using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
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

        public LuceneDocumentFactory(IStorageIndex index)
            : this(index, new FieldFactory(index), new JObjectEnumerator())
        {
        }

        public LuceneDocumentFactory(IStorageIndex index, IFieldFactory factory, IJObjectEnumarator enumarator)
        {
            this.index = index;
            this.factory = factory;
            this.enumarator = enumarator;
        }

        public Document Create(JObject value)
        {
            string contentType = index.Configuration.TypeResolver.Resolve(value);

            //JSchema schema = new JSchema(JsonSchemaType.Object); //index.Schemas[contentType];

            Document document = enumarator
                .Enumerate(value)
                .Where(node =>
                {
                    if (!node.IsLeaf) index.Schemas.AddOrUpdate(contentType, node.Path, node.Type, false);
                    return node.IsLeaf;
                })
                .Select(node => factory.Create(node.Path, contentType, node.Token as JValue)
                    .Select(field => new { Node = node, Field = field }))
                .SelectMany(enumerable => enumerable.ToArray())
                .Select(token =>
                {
                    index.Schemas.AddOrUpdate(contentType, token.Field.Name, token.Node.Type, token.Field.IsIndexed);
                    return token.Field;
                })
                .Aggregate(new Document(), (doc, field) => doc.Put(field));

            document.Add(new Field(index.Configuration.RawField, value.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            return document;
        }
    }
}