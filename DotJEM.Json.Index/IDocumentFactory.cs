using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Newtonsoft.Json;
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
        private readonly IStorageIndex index;
        private readonly IJObjectEnumarator enumarator;

        public LuceneDocumentFactory(IStorageIndex index)
            : this(index, new FieldFactory(index), new JObjectEnumerator())
        {
        }

        public LuceneDocumentFactory(IStorageIndex index, IFieldFactory factory, IJObjectEnumarator enumarator)
        {
            this.index = index;
            this.factory = factory;
            this.enumarator = enumarator;
        }

        public Document Create(JObject value)
        {
            string contentType = index.Configuration.TypeResolver.Resolve(value);
            
            Document document = enumarator
                .Flatten(value, (fn, v) => factory.Create(fn, contentType, v))
                .SelectMany(enumerable => enumerable.ToArray())
                .Aggregate(new Document(), (doc, field) =>
                {
                    index.Fields.Add(contentType, field.Name, field.IsIndexed);
                    doc.Add(field);
                    return doc;
                });

            document.Add(new Field(index.Configuration.RawField, value.ToString(Formatting.None), Field.Store.YES, Field.Index.NO));
            return document;
        }
    }
}