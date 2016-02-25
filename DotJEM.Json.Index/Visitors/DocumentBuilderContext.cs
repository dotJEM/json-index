using System;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Configuration.FieldStrategies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public interface IDocumentBuilderContext
    {
        JObject Json { get; }

        string Path { get; }
        string ContentType { get; }

        IIndexingVisitorStrategy Strategy { get; }
        IDocumentBuilderContext Parent { get; }
        IDocumentBuilderContext Child(string name);
    }

    public class DocumentBuilderContext : IDocumentBuilderContext
    {
        private readonly Lazy<IIndexingVisitorStrategy> strategy;

        public IDocumentBuilderContext Parent { get; }
        public IIndexConfiguration Configuration { get; }

        public string Path { get; }
        public JObject Json { get; }
        public string ContentType { get; }

        public IIndexingVisitorStrategy Strategy => strategy.Value;

        public DocumentBuilderContext(IIndexConfiguration configuration, string contentType, JObject json)
            : this(configuration, null, contentType, json, string.Empty)
        {
        }

        private DocumentBuilderContext(DocumentBuilderContext parent, string path)
            : this(parent.Configuration, parent, parent.ContentType, parent.Json, path)
        {
        }

        private DocumentBuilderContext(IIndexConfiguration configuration, DocumentBuilderContext parent, string contentType, JObject json, string path)
        {
            Configuration = configuration;
            Parent = parent;
            Path = path;
            Json = json;
            ContentType = contentType;

            strategy = new Lazy<IIndexingVisitorStrategy>(() => Configuration.Field.Strategy(ContentType, Path).IndexingStrategy);
        }

        public IDocumentBuilderContext Child(string name)
        {
            string childPath = Path == "" ? name : Path + "." + name;
            return new DocumentBuilderContext(this, childPath);
        }
    }
}