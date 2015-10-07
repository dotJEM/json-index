using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Sharding.Configuration;
using DotJEM.Json.Index.Sharding.Resolvers;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Schemas
{
    public interface IJSchemaManager
    {
        void Update(JObject value);
    }

    public class JSchemaManager : IJSchemaManager
    {
        //TODO: (jmd 2015-10-07) Replace ISchemaCollection completely with Manager. 
        private readonly ConcurrentDictionary<string, JSchema> schemas = new ConcurrentDictionary<string, JSchema>();

        private readonly IMetaFieldResolver resolver;
        private readonly IJSchemaGenerator generator;

        public JSchemaManager(IMetaFieldResolver resolver, IJSchemaGenerator generator)
        {
            this.resolver = resolver;
            this.generator = generator;
        }

        public void Update(JObject value)
        {
            string contentType = resolver.ContentType(value);
            //TODO: (jmd 2015-10-07) Index should now have knowledge of storage areas
            //                       They should however have knowledge of what index they belong to
            //                       That said, customization should be possible. 
            //string storageArea = resolver.Area(value);

            if (!schemas.ContainsKey(contentType))
            {
                schemas[contentType] = generator.Generate(value, contentType, "NAN");
            }
            else
            {
                JSchema schema = schemas[contentType];
                schemas[contentType] = schema.Merge(generator.Generate(value, contentType, "NAN"));
            }
            //JSchema schema = schemas[contentType];
            //schema = schema == null
            //    ? generator.Generate(value, contentType, storageArea)
            //    : schema.Merge(generator.Generate(value, contentType, storageArea));
            //schemas[contentType] = schema;
        }

        //private readonly IDictionary<string, JSchema> schemas = new ConcurrentDictionary<string, JSchema>();

        //public IEnumerable<string> ContentTypes { get { return schemas.Keys; } }

        //public JSchema this[string contentType]
        //{
        //    get
        //    {
        //        return schemas.ContainsKey(contentType) ? schemas[contentType] : null;
        //    }
        //    set
        //    {
        //        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentNullException("contentType");
        //        if (value == null) throw new ArgumentNullException("value");

        //        schemas[contentType] = value;
        //    }
        //}

        //public JSchema Add(string contentType, JSchema schema)
        //{
        //    if (contentType == null) throw new ArgumentNullException("contentType");
        //    if (schema == null) throw new ArgumentNullException("schema");

        //    schema.ContentType = contentType;
        //    if (schemas.ContainsKey(contentType))
        //    {
        //        return this[contentType] = this[contentType].Merge(schema);
        //    }
        //    schemas.Add(contentType, schema);
        //    return schema;
        //}

        //public IEnumerable<string> AllFields()
        //{
        //    return schemas.Values
        //        .SelectMany(s => s.Traverse())
        //        .Select(s => s.Field)
        //        .Where(f => !string.IsNullOrEmpty(f))
        //        .Distinct();
        //}

        //public JsonSchemaExtendedType ExtendedType(string field)
        //{
        //    return schemas.Aggregate(JsonSchemaExtendedType.None,
        //        (types, next) => next.Value.LookupExtentedType(field) | types);
        //}

        //public IEnumerable<string> Fields(string contentType)
        //{
        //    JSchema schema = this[contentType];

        //    return schema == null
        //        ? Enumerable.Empty<string>()
        //        : schema.Traverse().Select(s => s.Field).Where(f => !string.IsNullOrEmpty(f));
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return GetEnumerator();
        //}

        //public IEnumerator<JSchema> GetEnumerator()
        //{
        //    return schemas.Values.GetEnumerator();
        //}
    }
}