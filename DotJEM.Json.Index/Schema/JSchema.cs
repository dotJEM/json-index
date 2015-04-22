using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    [JsonConverter(typeof(JSchemeConverter))]
    public class JSchema : DynamicObject
    {
        private readonly JObject extensions = new JObject();

        public IEnumerable<JProperty> Extensions { get { return extensions.Properties(); } }

        public JToken this[string key]
        {
            get { return extensions[key]; }
            set { extensions[key] = value; }
        }

        public string Schema { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Area { get; set; }
        public string ContentType { get; set; }

        public bool Required { get; set; }

        public JsonSchemaType Type { get; set; }
        public JsonSchemaExtendedType ExtendedType { get; set; }
        public JSchema Items { get; set; }
        public JSchemaProperties Properties { get; set; }

        //NOTE: Custom fields
        public string Field { get; set; }
        public bool Indexed { get; set; }

        public bool IsRoot { get; set; }

        internal JObject Ext
        {
            get { return extensions; }
        }

        public JSchema(JsonSchemaType type, JsonSchemaExtendedType extendedType)
        {
            ExtendedType = extendedType;
            Type = type;
            Required = false;
        }

        public virtual JObject Serialize(string url)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JSchemeConverter(url));
            return JObject.FromObject(this, serializer);
        }

        public JSchema MergeExtensions(JSchema other)
        {
            extensions.Merge(other.extensions);
            return this;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            extensions[binder.Name] = JToken.FromObject(value);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            JToken token;
            if (extensions.TryGetValue(binder.Name, out token))
            {
                result = token.ToObject(binder.ReturnType);
                return true;
            }
            result = null;
            return false;
        }
    }
}
