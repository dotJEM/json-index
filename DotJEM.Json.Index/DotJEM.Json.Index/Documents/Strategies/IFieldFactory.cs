using System;
using DotJEM.Json.Index.Documents.Builder;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public interface IFieldFactory<T>
    {
        IFieldContext Context { get; }
        T DeserializedValue { get; }
    }

    public class FieldFactory<T> : IFieldFactory<T>
    {
        public IFieldContext Context { get; }
        public T DeserializedValue { get; }

        public FieldFactory(IFieldContext context, Func<JToken, T> converter = null)
        {
            converter = converter ?? (token => token.ToObject<T>());
            this.Context = context;
            DeserializedValue = converter(context.Value);
        }

    }

}