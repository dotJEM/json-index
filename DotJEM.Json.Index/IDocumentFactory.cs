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
    public interface IDocumentBuilderFactory
    {
        IDocumentBuilder Create(string contentType);
    }

    public class DefaultDocumentBuilderFactory : IDocumentBuilderFactory
    {
        private readonly IStorageIndex index;

        public DefaultDocumentBuilderFactory(IStorageIndex index)
        {
            this.index = index;
        }

        public virtual IDocumentBuilder Create(string contentType) => new DefaultDocumentBuilder(index, contentType);
    }

    public interface IDocumentFactory
    {
        Document Create(JObject value);
    }

    public class DefaultDocumentFactory : IDocumentFactory
    {
        private readonly IStorageIndex index;
        private readonly IDocumentBuilderFactory factory;
        private readonly IJSchemaGenerator generator;

        public DefaultDocumentFactory(IStorageIndex index)
            : this(index, new DefaultDocumentBuilderFactory(index), new JSchemaGenerator())
        {
        }

        public DefaultDocumentFactory(IStorageIndex index, IDocumentBuilderFactory factory, IJSchemaGenerator generator)
        {
            this.index = index;
            this.factory = factory;
            this.generator = generator;
        }

        public virtual Document Create(JObject value)
        {
            string contentType = index.Configuration.TypeResolver.Resolve(value);
            string storageArea = index.Configuration.AreaResolver.Resolve(value);

            JSchema schema = index.Schemas[contentType];
            schema = schema == null
                ? generator.Generate(value, contentType, storageArea)
                : schema.Merge(generator.Generate(value, contentType, storageArea));
            index.Schemas[contentType] = schema;

            Document document = factory
                .Create(contentType)
                .Build(value);

            document.Add(new Field(index.Configuration.RawField, value.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            return document;
        }
    }
}