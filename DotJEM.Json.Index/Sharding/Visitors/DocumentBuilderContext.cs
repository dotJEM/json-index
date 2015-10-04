using Lucene.Net.Documents;

namespace DotJEM.Json.Index.Sharding.Visitors
{
    public class DocumentBuilderContext
    {
        public string Path { get; }

        public DocumentBuilderContext Parent { get; }

        public Document Document { get; }

        public DocumentBuilderContext()
        {
            Path = "";
            Document = new Document();
        }

        protected DocumentBuilderContext(DocumentBuilderContext parent, string path)
        {
            Path = path;
            Parent = parent;
            Document = parent.Document;
        }

        public DocumentBuilderContext Child(string name)
        {
            return new DocumentBuilderContext(this, Path + "." + name);
        }

        public void AddField(IFieldable field)
        {
            Document.Add(field);
        }
    }
}