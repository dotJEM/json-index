using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Documents.Builder;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public interface IJsonIndexableFieldBuilder<TValue>
    {
        IPathContext Context { get; }
        TValue DeserializedValue { get; }

        IJsonIndexableFieldBuilder<TValue> Add(IIndexableField field);
        IIndexableJsonField Build();
    }
    public class JsonIndexableFieldBuilder<TValue> : IJsonIndexableFieldBuilder<TValue>
    {
        public IPathContext Context { get; }
        public TValue DeserializedValue { get; }

        private List<IIndexableField> fields = new List<IIndexableField>();

        public JsonIndexableFieldBuilder(JToken token, IPathContext context, Func<JToken, TValue> converter = null)
        {
            converter = converter ?? (t => t.ToObject<TValue>());
            Context = context;
            DeserializedValue = converter(token);
        }

        public IJsonIndexableFieldBuilder<TValue> Add(IIndexableField field)
        {
            fields.Add(field);
            return this;
        }

        public IIndexableJsonField Build()
        {
            return new IndexableJsonField<TValue>(Context.Path, fields);
        }
    }

}