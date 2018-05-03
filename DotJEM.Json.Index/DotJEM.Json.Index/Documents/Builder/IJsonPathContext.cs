using System;
using DotJEM.Json.Visitor;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{
    public interface IFieldBuilderProvider
    {
        IFieldBuilder<TValue> FieldBuilder<TValue>();
        IFieldBuilder<TValue> FieldBuilder<TValue>(Func<JToken, TValue> converter);
    }

    public interface IJsonPathContext : IJsonVisitorContext<IJsonPathContext>, IFieldBuilderProvider
    {
        string Path { get; }

        IJsonPathContext Next(int index, JToken value);

        IJsonPathContext Next(string name, JToken value);
    }
}