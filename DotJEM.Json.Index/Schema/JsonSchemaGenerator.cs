using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography;
using Lucene.Net.QueryParsers;
using Newtonsoft.Json;
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
                ? new JRootSchema(JsonSchemaType.Object) 
                : new JSchema(JsonSchemaType.Object);

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

            var schema = new JSchema(json.Type.ToSchemaType());
            schema.Id = path.ToString("/");
            schema.Field = path.ToString(".");
            return schema;
        }

        private JSchema InternalGenerate(JArray json, JPath path)
        {
            if (json == null) return null;

            return new JSchema(JsonSchemaType.Array)
            {
                Id = path.ToString("/"),
                Field = path.ToString("."),
                Items = json.Aggregate(
                    new JSchema(JsonSchemaType.None), 
                    (schema, token) => schema.Merge(InternalGenerate(token as JObject, path))
                    )
            };
        }
    }

    [JsonConverter(typeof(JSchemeConverter))]
    public class JSchema
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public bool Required { get; set; }

        public JsonSchemaType Type { get; set; }
        public JSchema Items { get; set; }
        public IDictionary<string, JSchema> Properties { get; set; }

        //NOTE: Custom fields
        public string Field { get; set; }
        public bool Indexed { get; set; }
        public bool IsRoot { get; set; }

        public JSchema(JsonSchemaType type)
        {
            Type = type;
            Required = false;
        }

        public IEnumerable<JSchema> Traverse()
        {
            yield return this;
            if (Items != null) yield return Items;
            if (Properties != null)
                foreach (var property in Properties.Values)
                    yield return property;
        }

        public virtual JObject Serialize(string httpDotjemComApiSchema)
        {
            return JObject.FromObject(this);
        }

        public JSchema Merge(JSchema other)
        {
            if (other == null)
                return this;

            Type = Type | other.Type;
            Indexed = Indexed || other.Indexed;
            Indexed = Required || other.Required;

            Description = MostQualifying(Title, other.Title);
            Description = MostQualifying(Description, other.Description);

            Items = Items != null ? Items.Merge(other.Items) : other.Items;

            if (other.Properties != null)
            {
                if (Properties == null)
                {
                    Properties = other.Properties;
                }
                else
                {
                    foreach (KeyValuePair<string, JSchema> pair in other.Properties)
                    {
                        if (Properties.ContainsKey(pair.Key))
                        {
                            Properties[pair.Key] = Properties[pair.Key].Merge(pair.Value);
                        }
                        else
                        {
                            Properties.Add(pair.Key, pair.Value);
                        }
                    }
                }
            }
            return this;
        }

        private string MostQualifying(string self, string other)
        {
            return string.IsNullOrEmpty(other) ? (self ?? other) : other;
        }
    }

        //    bool Indexed { get; }
        //bool IsContentType { get; }
        //string Path { get; }
        //string ContentType { get; }
        //IEnumerable<JTokenType> Types { get; }
        //void AddType(JTokenType type, bool indexed);

    [JsonConverter(typeof(JSchemeConverter))]
    public class JRootSchema : JSchema
    {

        public string Schema { get; set; }

        public JRootSchema(JsonSchemaType type) : base(type)
        {
            Schema = "kk";
        }
    }

}