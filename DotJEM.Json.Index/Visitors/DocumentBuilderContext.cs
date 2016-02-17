using Lucene.Net.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public interface IDocumentBuilderContext
    {
        string Path { get; }
        IDocumentBuilderContext Parent { get; }
        IDocumentBuilderContext Child(string name);
    }

    public class DocumentBuilderContext : IDocumentBuilderContext
    {
        public string Path { get; }

        public IDocumentBuilderContext Parent { get; }

        public DocumentBuilderContext(JObject json)
        {
            Path = "";
        }

        private DocumentBuilderContext(IDocumentBuilderContext parent, string path)
        {
            Path = path;
            Parent = parent;
        }

        public IDocumentBuilderContext Child(string name)
        {
            string childPath = Path == "" ? name : Path + "." + name;
            return new DocumentBuilderContext(this, childPath);
        }
    }
}