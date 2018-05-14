using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DotJEM.Json.Index.Documents.Builder;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
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



    public class Use
    {
        public void HowToUse()
        {
            IFieldStrategyCollectionBuilder builder = new FieldStrategyCollectionBuilder();
            builder.Use<ExpandedDateTimeFieldStrategy>().For(new TypeFilter<DateTime>());
            builder.Use<ExpandedDateTimeFieldStrategy>().For<DateTime>();

            //builder.For("contentType").Use<ExpandedDateTimeFieldStrategy>().On("field");
            //builder.Use<IdentityFieldStrategy>().For("*", "$created");
        }
    }

    public interface IStrategySelector { }
    public class TypeFilter<T>: IStrategySelector { }
}
