using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Lucene.Net.QueryParsers;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    public interface IJSchemaGenerator
    {
        JSchema Generate(JObject json);
    }

    public class JSchemaGenerator : IJSchemaGenerator
    {
        public JSchema Generate(JObject json)
        {
            return InternalGenerate(json, "");
        }

        private JSchema InternalGenerate(JObject json, JPath path, bool isRoot = false)
        {
            if (json == null) return null;

            JSchema schema = isRoot
                ? new JRootSchema(JsonSchemaType.Object, JsonSchemaExtendedType.Object) 
                : new JSchema(JsonSchemaType.Object, JsonSchemaExtendedType.Object);

            schema.Id = path.ToString("/");
            schema.Field = path.ToString(".");
            schema.Properties = GenerateProperties(json, path);

            return schema;
        }

        private IDictionary<string, JSchema> GenerateProperties(JObject json, JPath path)
        {
            IDictionary<string, JSchema> properties = new Dictionary<string, JSchema>();
            foreach (JProperty property in json.Properties())
            {
                JPath child = path + property.Name;
                var prop = InternalGenerate(property.Value as JValue, child)
                           ?? InternalGenerate(property.Value as JArray, child)
                           ?? InternalGenerate(property.Value as JObject, child);
                if (prop != null) properties[property.Name] = prop;
            }
            return properties.Any() ? properties : null;
        }

        private JSchema InternalGenerate(JValue json, JPath path)
        {
            if (json == null) return null;

            var schema = new JSchema(json.Type.ToSchemaType(), json.Type.ToSchemaExtendedType());
            schema.Id = path.ToString("/");
            schema.Field = path.ToString(".");
            return schema;
        }

        private JSchema InternalGenerate(JArray json, JPath path)
        {
            if (json == null) return null;

            return new JSchema(JsonSchemaType.Array, JsonSchemaExtendedType.Array)
            {
                Id = path.ToString("/"),
                Field = path.ToString("."),
                Items = json.Aggregate(
                    new JSchema(JsonSchemaType.None, JsonSchemaExtendedType.None), 
                    (schema, token) => schema.Merge(InternalGenerate(token as JObject, path))
                    )
            };
        }
    }

    //    bool Indexed { get; }
        //bool IsContentType { get; }
        //string Path { get; }
        //string ContentType { get; }
        //IEnumerable<JTokenType> Types { get; }
        //void AddType(JTokenType type, bool indexed);
}