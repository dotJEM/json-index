using System;
using System.Collections;
using System.Collections.Generic;

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
}