using DotJEM.Json.Index.Configuration;
using Lucene.Net.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public interface IDocumentBuilder
    {
        Document Document { get; }
        Document Build(JObject json);

        void AddField(IFieldable field);
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

        public void AddField(IFieldable field) => Document.Add(field);

        public Document Build(JObject json)
        {
            DocumentBuilderContext context = new DocumentBuilderContext(configuration, contentType, json);
            Document.Add(new Field(configuration.RawField, json.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            Visit(json, context);
            return Document;
        }

        protected override void VisitProperty(JProperty json, IDocumentBuilderContext context)
        {
            base.VisitProperty(json, context.Child(json.Name));
        }
    }
}