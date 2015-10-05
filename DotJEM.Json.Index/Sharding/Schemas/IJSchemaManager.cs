using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Sharding.Configuration;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Schemas
{
    public interface IJSchemaManager
    {
        void Update(JObject value);
    }

    public class JSchemaManager : IJSchemaManager
    {
        private readonly IMetaFieldResolver resolver;
        private readonly IJSchemaGenerator generator;
        private readonly ISchemaCollection schemas;

        public JSchemaManager(IMetaFieldResolver resolver, IJSchemaGenerator generator, ISchemaCollection schemas)
        {
            this.resolver = resolver;
            this.generator = generator;
            this.schemas = schemas;
        }

        public void Update(JObject value)
        {
            string contentType = resolver.ContentType(value);
            string storageArea = resolver.Area(value);

            JSchema schema = schemas[contentType];
            schema = schema == null
                ? generator.Generate(value, contentType, storageArea)
                : schema.Merge(generator.Generate(value, contentType, storageArea));
            schemas[contentType] = schema;
        }
    }
}