using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    [JsonConverter(typeof(JSchemeConverter))]
    public class JSchema
    {
        public string Schema { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public bool Required { get; set; }

        public JsonSchemaType Type { get; set; }
        public JsonSchemaExtendedType ExtendedType { get; set; }
        public JSchema Items { get; set; }
        public IDictionary<string, JSchema> Properties { get; set; }

        //NOTE: Custom fields
        public string Field { get; set; }
        public bool Indexed { get; set; }

        public bool IsRoot { get; set; }

        public JSchema(JsonSchemaType type, JsonSchemaExtendedType extendedType)
        {
            ExtendedType = extendedType;
            Type = type;
            Required = false;
        }

        public IEnumerable<JSchema> Traverse()
        {
            var all = Enumerable.Empty<JSchema>()
                .Union(new[] { this });

            if (Items != null)
                all = all.Union(Items.Traverse());

            if (Properties != null)
                all = all.Union(Properties.Values.SelectMany(property => property.Traverse()));

            return all;
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
            ExtendedType = ExtendedType | other.ExtendedType;
            Indexed = Indexed || other.Indexed;
            Required = Required || other.Required;

            Title = MostQualifying(Title, other.Title);
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

        public JsonSchemaExtendedType LookupExtentedType(string field)
        {
            if (!field.StartsWith(this.Field))
                return JsonSchemaExtendedType.None;

            if (this.Field == field)
                return this.ExtendedType;

            var extendedTypes = Items != null ? Items.LookupExtentedType(field) : JsonSchemaExtendedType.None;

            if (Properties != null)
            {
                extendedTypes = extendedTypes | Properties.Aggregate(JsonSchemaExtendedType.None,
                    (types, next) => next.Value.LookupExtentedType(field) | types);
            }

            return extendedTypes;
        }
    }
}
