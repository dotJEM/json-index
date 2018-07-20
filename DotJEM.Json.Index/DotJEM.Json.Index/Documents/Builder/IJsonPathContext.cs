using System;
using DotJEM.Json.Index.Documents.Strategies;
using DotJEM.Json.Visitor;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Builder
{

    public interface IJsonPathContext : IJsonVisitorContext<IJsonPathContext>
    {
        string Path { get; }
        IFieldContext FieldContext { get; }
        IJsonPathContext Next(int index, JToken value);
        IJsonPathContext Next(string name, JToken value);
        void Apply(IFieldStrategy strategy);
    }
    public interface IFieldContext
    {
        string Field { get; }
        JToken Value { get; }
    }

    public class FieldContext : IFieldContext
    {
        public string Field { get; }
        public JToken Value { get; }

        public FieldContext(string field, JToken value)
        {
            Field = field;
            Value = value;
        }
    }
}