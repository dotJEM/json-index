using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Visitors
{
    public interface IDocumentBuilder
    {
        DocumentBuilderContext BuildDocument(JObject json);
    }

    public abstract class AbstractDocumentBuilder : AbstractJTokenVisitor<DocumentBuilderContext>, IDocumentBuilder
    {
        public DocumentBuilderContext BuildDocument(JObject json)
        {
            DocumentBuilderContext context = new DocumentBuilderContext(json);
            Visit(json, context);
            return context;
        }

        public override void VisitProperty(JProperty json, DocumentBuilderContext context)
        {
            base.VisitProperty(json, context.Child(json.Name));
        }
    }
}