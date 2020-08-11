using System;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public interface IFieldStrategyCollection
    {
        IFieldStrategy Resolve(string fieldName, JTokenType type);
    }

    public class NullFieldStrategyCollection : IFieldStrategyCollection
    {
        public IFieldStrategy Resolve(string fieldName, JTokenType type)
        {
            return null;
        }
    }


    public interface IFieldStrategyCollectionBuilder
    {
        IFieldStrategyCollection Build();
        ITargetConfigurator Use<T>() where T : IFieldStrategy, new();
    }

    public class FieldStrategyCollectionBuilder : IFieldStrategyCollectionBuilder
    {
        public IFieldStrategyCollection Build()
        {
            return null;
        }

        public ITargetConfigurator Use<T>() where T : IFieldStrategy, new()
        {
            return null;
        }
    }

    public interface ITargetConfigurator
    {
        void For<T>();
        void For(IStrategySelector filter);
    }

    public interface IStrategySelector
    {

    }

    public class TypeFilter<T> : IStrategySelector
    {

    }

    public class TypeFilter : IStrategySelector
    {
        public TypeFilter(JTokenType date)
        {
            throw new NotImplementedException();
        }
    }

    public class PatternFilter : IStrategySelector
    {
    }
}
