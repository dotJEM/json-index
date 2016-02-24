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
        public Document Document { get; }

        protected IIndexConfiguration Config { get; }

        protected AbstractDocumentBuilder(IStorageIndex index)
        {
            Document = new Document();

            this.Config = index.Configuration;
        }

        public void AddField(IFieldable field)
        {
            Document.Add(field);
        }

        public Document Build(JObject json)
        {
            DocumentBuilderContext context = new DocumentBuilderContext(json);
            Document.Add(new Field(Config.RawField, json.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            Visit(json, context);
            return Document;
        }


        protected override void VisitProperty(JProperty json, IDocumentBuilderContext context)
        {
            base.VisitProperty(json, context.Child(json.Name));
        }
    }
}