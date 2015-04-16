using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace DotJEM.Json.Index.Schema
{
    public class JSchemaProperties : IDictionary<string, JSchema>
    {
        private readonly Dictionary<string, JSchema> map = new Dictionary<string, JSchema>();

        public int Count { get { return map.Count; } }
        public ICollection<string> Keys { get { return map.Keys; } }
        public ICollection<JSchema> Values { get { return map.Values; } }

        public JSchema this[string key]
        {
            get { return map[key]; }
            set
            {
                if (value == null) throw new ArgumentNullException("value", "value was null when trying to access setter in JSchemaProperties indexer");
                if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key", "key was null when trying to access setter in JSchemaProperties indexer");
                map[key] = value;
            }
        }

        public void Add(string key, JSchema value)
        {
            if (value == null) throw new ArgumentNullException("value", "value was null when trying to add item to JSchemaProperties");
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key", "key was null when trying to add item to JSchemaProperties");
            map.Add(key, value);
        }

        public void Clear()
        {
            map.Clear();
        }

        public bool ContainsKey(string key)
        {
            return map.ContainsKey(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, JSchema>> GetEnumerator()
        {
            return map.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return map.Remove(key);
        }

        public bool TryGetValue(string key, out JSchema value)
        {
            return map.TryGetValue(key, out value);
        }

        #region ICollection<KeyValuePair<string, JSchema>> Explicit Implementation
        private ICollection<KeyValuePair<string, JSchema>> Collection
        {
            get { return map; }
        }

        bool ICollection<KeyValuePair<string, JSchema>>.IsReadOnly { get { return Collection.IsReadOnly; } }

        void ICollection<KeyValuePair<string, JSchema>>.Add(KeyValuePair<string, JSchema> item)
        {
            if (item.Value == null) throw new ArgumentNullException("item", "item.Value was null when trying to add item to JSchemaProperties");
            if (string.IsNullOrEmpty(item.Key)) throw new ArgumentNullException("item", "item.Key was null when trying to add item to JSchemaProperties");
            Collection.Add(item);
        }

        bool ICollection<KeyValuePair<string, JSchema>>.Contains(KeyValuePair<string, JSchema> item)
        {
            return Collection.Contains(item);
        }

        void ICollection<KeyValuePair<string, JSchema>>.CopyTo(KeyValuePair<string, JSchema>[] array, int arrayIndex)
        {
            Collection.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, JSchema>>.Remove(KeyValuePair<string, JSchema> item)
        {
            return Collection.Remove(item);
        }
        #endregion
    }

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
            {
                all = all.Union(Items.Traverse());
            }

            if (Properties != null)
            {
                all = all.Union(Properties.Values.SelectMany(property => property.Traverse()));
            }

            return all;
        }

        public virtual JObject Serialize(string url)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JSchemeConverter(url));
            return JObject.FromObject(this, serializer);
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
            Area = MostQualifying(Area, other.Area);
            ContentType = MostQualifying(ContentType, other.ContentType);
            Field = MostQualifying(Field, other.Field);

            Items = Items != null ? Items.Merge(other.Items) : other.Items;

            if (other.Properties == null)
                return this;

            if (Properties == null)
            {
                Properties = other.Properties;
                return this;
            }

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
            return this;
        }

        private string MostQualifying(string self, string other)
        {
            return string.IsNullOrEmpty(other) ? (self ?? other) : other;
        }

        public JsonSchemaExtendedType LookupExtentedType(string field)
        {
            if (Field == null || !field.StartsWith(Field))
                return JsonSchemaExtendedType.None;

            if (Field == field)
                return ExtendedType;

            var extendedTypes = Items != null ? Items.LookupExtentedType(field) : JsonSchemaExtendedType.None;

            if (Properties != null)
            {
                extendedTypes = extendedTypes | Properties.Aggregate(JsonSchemaExtendedType.None,
                    (types, next) => next.Value.LookupExtentedType(field) | types);
            }

            return extendedTypes;
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
