using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Schema
{
    public interface ISchemaCollection
    {
        IEnumerable<string> ContentTypes { get; }
        JSchema this[string contentType] { get; set; }

        IEnumerable<string> AllFields();
        IEnumerable<string> Fields(string contentType);

        JSchema Add(string contentType, JSchema schema);
    }

    public class SchemaCollection : ISchemaCollection
    {
        private readonly IDictionary<string, JSchema> schemas = new Dictionary<string, JSchema>();

        public IEnumerable<string> ContentTypes { get { return schemas.Keys; } }

        public JSchema this[string contentType]
        {
            get { return schemas.ContainsKey(contentType) ? schemas[contentType] : null; }
            set { schemas[contentType] = value; }
        }

        public JSchema Add(string contentType, JSchema schema)
        {
            schemas.Add(contentType, schema);
            return schema;
        }
        public IEnumerable<string> AllFields()
        {
            return schemas.Values
                .SelectMany(s => s.Traverse())
                .Select(s => s.Field)
                .Where(f => !string.IsNullOrEmpty(f))
                .Distinct();
        }

        public IEnumerable<string> Fields(string contentType)
        {
            JSchema schema = this[contentType];

            return schema == null 
                ? Enumerable.Empty<string>() 
                : schema.Traverse().Select(s => s.Field).Where(f => !string.IsNullOrEmpty(f));
        }
    }

    //public interface IFieldDefinition
    //{
    //    bool Indexed { get; }
    //    bool IsContentType { get; }
    //    string Path { get; }
    //    string ContentType { get; }
    //    IEnumerable<JTokenType> Types { get; }
    //    void AddType(JTokenType type, bool indexed);
    //}

}