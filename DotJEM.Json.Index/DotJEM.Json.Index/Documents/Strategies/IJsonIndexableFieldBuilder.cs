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
        private readonly JToken token;
        private readonly IFieldStrategy strategy;
        public IPathContext Context { get; }
        public TValue DeserializedValue { get; }

        private readonly List<IIndexableField> fields = new List<IIndexableField>();

        public JsonIndexableFieldBuilder(IFieldStrategy strategy, JToken token, IPathContext context, Func<JToken, TValue> converter = null)
        {
            this.strategy = strategy;
            this.token = token;
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
            return new IndexableJsonField<TValue>(Context.Path, token.Type, fields, strategy.GetType());
        }
    }

}