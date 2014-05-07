using System.Linq;
using Lucene.Net.Documents;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public interface IDocumentFactory
    {
        Document Create(JObject value);
    }

    public class LuceneDocumentFactory : IDocumentFactory
    {
        private readonly IFieldFactory factory;
        private readonly IJsonIndex index;
        private readonly IJObjectEnumarator enumarator;

        public LuceneDocumentFactory(IJsonIndex index)
            : this(index, new FieldFactory(index.Configuration), new JObjectEnumerator())
        {
        }

        public LuceneDocumentFactory(IJsonIndex index, IFieldFactory factory,  IJObjectEnumarator enumarator)
        {
            this.index = index;
            this.factory = factory;
            this.enumarator = enumarator;
        }

        public Document Create(JObject value)
        {
            string contentType = index.Configuration.TypeResolver.Resolve(value);

            Document doc = new Document();
            foreach (IFieldable field in enumarator
                .Flatten(value, (fn, v) => factory.Create(fn, contentType, v))
                .Where(field => field != null))
            {
                index.Fields.Add(contentType, field.Name, field.IsIndexed);
                doc.Add(field);
            }
            doc.Add(new Field(index.Configuration.RawField, value.ToString(), Field.Store.YES, Field.Index.NO));
            return doc;
        }
    }
}