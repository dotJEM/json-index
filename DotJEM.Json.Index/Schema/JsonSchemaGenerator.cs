using System;
using System.Collections.Generic;
using System.Data;
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
        JSchema Generate(JObject json, string contentType, string storageArea);
    }

    public class JSchemaGenerator : IJSchemaGenerator
    {
        public JSchema Generate(JObject json, string contentType = "", string storageArea = "")
        {
            JSchema schema = InternalGenerate(json, "");
            schema.ContentType = contentType;
            schema.Area = storageArea;
            return schema;
        }

        private JSchema InternalGenerate(JObject json, JPath path, bool isRoot = false)
        {
            if (json == null) return null;

            JSchema schema = new JSchema(JsonSchemaType.Object, JsonSchemaExtendedType.Object);
            schema.IsRoot = isRoot;
            schema.Id = path.ToString("/");
            schema.Field = path.ToString(".");
            schema.Properties = GenerateProperties(json, path);

            return schema;
        }

        private JSchemaProperties GenerateProperties(JObject json, JPath path)
        {
            JSchemaProperties properties = new JSchemaProperties();
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
                    (schema, token) => schema.Merge(
                           InternalGenerate(token as JObject, path) ??
                           InternalGenerate(token as JValue, path) ??
                           InternalGenerate(token as JArray, path))
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