using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public interface IFieldMap
    {
        void AddOrUpdate(string contentType, string path, JTokenType type, bool b);

        IEnumerable<string> AllFields();
        IEnumerable<string> Fields(string contentType);
        IEnumerable<string> ContentTypes { get; }
        IEnumerable<IFieldDefinition> this[string contentType] { get; }
    }

    public class FieldMap : IFieldMap
    {
        private readonly IDictionary<string, IDictionary<string, FieldDefinition>> map = new Dictionary<string, IDictionary<string, FieldDefinition>>();
        
        public IEnumerable<string> ContentTypes { get { return map.Keys; } }
        public IEnumerable<IFieldDefinition> this[string contentType] { get { return map[contentType].Values; } }

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
    }

    public interface IFieldDefinition
    {
        bool Indexed { get; }
        bool IsContentType { get; }
        string Path { get; }
        string ContentType { get; }
        IEnumerable<JTokenType> Types { get; }
        void AddType(JTokenType type, bool indexed);
    }

    public class FieldDefinition : IFieldDefinition
    {
        private readonly HashSet<JTokenType> types = new HashSet<JTokenType>();

        public bool Indexed { get; private set; }
        public bool IsContentType { get { return string.IsNullOrEmpty(Path); } }
        public string Path { get; private set; }
        public string ContentType { get; private set; }
        public IEnumerable<JTokenType> Types { get { return types; } }

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