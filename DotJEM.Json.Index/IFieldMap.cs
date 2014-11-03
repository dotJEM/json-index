using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public interface IFieldMap
    {
        void AddOrUpdate(string contentType, string path, JTokenType type, bool b);

        IEnumerable<string> AllFields();
        IEnumerable<string> Fields(string contentType);
    }

    public class FieldMap : IFieldMap
    {
        private readonly IDictionary<string, IDictionary<string, FieldDefinition>> map = new Dictionary<string, IDictionary<string, FieldDefinition>>();

        public void AddOrUpdate(string contentType, string path, JTokenType type, bool indexed)
        {
            FieldDefinition definition;
            if (!map.ContainsKey(contentType))
            {
                definition = new FieldDefinition(contentType, path);
                map[contentType] = new Dictionary<string, FieldDefinition> {{path, definition}};
            }
            else
            {
                IDictionary<string, FieldDefinition> fields = map[contentType];
                if (!fields.ContainsKey(path)) definition = fields[path] = new FieldDefinition(contentType, path);
                else definition = fields[path];
            }
            definition.AddType(type, indexed);
        }

        public IEnumerable<string> AllFields()
        {
            return map.Values.SelectMany(fields => fields.Values.Where(def => def.Indexed).Select(def => def.Path)).Distinct();
        }

        public IEnumerable<string> Fields(string contentType)
        {
            return !map.ContainsKey(contentType)
                ? Enumerable.Empty<string>()
                : map[contentType].Values.Where(def => def.Indexed).Select(def => def.Path);
        }

        private class FieldDefinition
        {
            private readonly HashSet<JTokenType> types = new HashSet<JTokenType>();

            public string Path { get; private set; }
            public string ContentType { get; private set; }
            public bool Indexed { get; private set; }

            public FieldDefinition(string contentType, string path)
            {
                ContentType = contentType;
                Path = path;
            }

            public void AddType(JTokenType type, bool indexed)
            {
                types.Add(type);
                Indexed = Indexed || indexed;
            }
        }
    }

    
}