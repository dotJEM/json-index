using DotJEM.Json.Index.Configuration;
using Lucene.Net.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Visitors
{
    public interface IDocumentBuilder
    {
        Document Document { get; }
        Document Build(string contentType, JObject json);

        void AddField(IFieldable field);
    }

    public abstract class AbstractDocumentBuilder : AbstractJTokenVisitor<IDocumentBuilderContext>, IDocumentBuilder
    {
        public Document Document { get; }

        protected IIndexConfiguration Configuration { get; }

        protected AbstractDocumentBuilder(IStorageIndex index)
        {
            Document = new Document();

            Configuration = index.Configuration;
        }

        public void AddField(IFieldable field) => Document.Add(field);

        public Document Build(string contentType, JObject json)
        {
            DocumentBuilderContext context = new DocumentBuilderContext(Configuration, contentType, json);
            Document.Add(new Field(Configuration.RawField, json.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            Visit(json, context);
            return Document;
        }

        protected override void VisitProperty(JProperty json, IDocumentBuilderContext context)
        {
            base.VisitProperty(json, context.Child(json.Name));
        }
    }
}