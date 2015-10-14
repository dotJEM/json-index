using Lucene.Net.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Visitors
{
    public sealed class DocumentBuilderContext
    {
        public string Path { get; }

        public DocumentBuilderContext Parent { get; }

        public Document Document { get; }

        public DocumentBuilderContext(JObject json)
        {
            Path = "";
            Document = new Document();
            Document.Add(new Field("$raw", json.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
        }

        private DocumentBuilderContext(DocumentBuilderContext parent, string path)
        {
            Path = path;
            Parent = parent;
            Document = parent.Document;
        }

        public DocumentBuilderContext Child(string name)
        {
            string childPath = Path == "" ? name : Path + "." + name;
            return new DocumentBuilderContext(this, childPath);
        }

        public void AddField(IFieldable field)
        {
            Document.Add(field);
        }
    }
}