using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Visitors
{
    public abstract class AbstractDocumentBuilder : AbstractJTokenVisitor<DocumentBuilderContext>
    {
        public override void VisitProperty(JProperty json, DocumentBuilderContext context)
        {
            base.VisitProperty(json, context.Child(json.Name));
        }
    }
}