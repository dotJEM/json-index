using DotJEM.Json.Index.Configuration;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public interface IDocumentBuilder
    {
        Document Document { get; }
        Document Build(JObject json);

        void AddField(IIndexableField field);
    }

    public abstract class AbstractDocumentBuilder : AbstractJTokenVisitor<IDocumentBuilderContext>, IDocumentBuilder
    {
        private readonly string contentType;
        private readonly IIndexConfiguration configuration;

        public Document Document { get; }

        protected AbstractDocumentBuilder(IStorageIndex index, string contentType)
        {
            this.contentType = contentType;
            Document = new Document();

            configuration = index.Configuration;
        }

        public void AddField(IIndexableField field) => Document.Add(field);

        public Document Build(JObject json)
        {
            DocumentBuilderContext context = new DocumentBuilderContext(configuration, contentType, json);
            Document.Add(configuration.Serializer.Serialize(configuration.RawField, json));
            Visit(json, context);
            return Document;
        }

        protected override void VisitProperty(JProperty json, IDocumentBuilderContext context)
        {
            base.VisitProperty(json, context.Child(json.Name));
        }
    }
}