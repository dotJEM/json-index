using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Visitors;
using Lucene.Net.Documents;
using Lucene.Net.Index;
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
        private readonly IJsonDocumentSerializer serializer;

        public DefaultDocumentFactory(IStorageIndex index)
            : this(index, new DefaultDocumentBuilderFactory(index), new JSchemaGenerator())
        {
        }

        public DefaultDocumentFactory(IStorageIndex index, IDocumentBuilderFactory factory, IJSchemaGenerator generator)
        {
            this.index = index;
            this.factory = factory;
            this.generator = generator;
            this.serializer = index.Configuration.Serializer;
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

            document.Add(serializer.Serialize(index.Configuration.RawField, value));
            return document;
        }

    }

    public class DefaultJsonDocumentSerializer : IJsonDocumentSerializer
    {
        public IIndexableField Serialize(string rawfield, JObject value)
        {
            return new StoredField(rawfield, value.ToString(Formatting.None));
        }

        public JObject Deserialize(string rawfield, Document document)
        {
            return JObject.Parse(document.GetField(rawfield).GetStringValue());
        }
    }
}