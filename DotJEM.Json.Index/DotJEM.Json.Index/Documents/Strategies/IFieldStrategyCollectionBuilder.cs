using System;
using System.Collections.Generic;
using System.Reflection;
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

    public interface IFieldStrategy
    {
        void Apply(IFieldBuilderProvider provider);
    }

    public class ExpandedDateTimeFieldStrategy : IFieldStrategy
    {
        public void Apply(IFieldBuilderProvider provider)
        {
            provider.FieldBuilder<DateTime>()
                .AddStringField(v => v.ToString("s"))
                .AddInt64Field("@ticks", v => v.Ticks)
                .AddInt32Field("@year", v => v.Year)
                .AddInt32Field("@month", v => v.Month)
                .AddInt32Field("@day", v => v.Day)
                .AddInt32Field("@hour", v => v.Hour)
                .AddInt32Field("@minute", v => v.Minute);
        }
    }

    public interface IFieldContext
    {
        string Field { get;  }
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

        public IIndexableField CreateStringField()
        {
            throw new NotImplementedException();
        }
    }

    public static class FieldFactoryExtensions
    {
        public static IIndexableField CreateStringField<TValue>(this IFieldFactory<TValue> self)
            => self.CreateStringField(val => val.ToString());

        public static IIndexableField CreateStringField<TValue>(this IFieldFactory<TValue> self, Func<TValue, string> transform)
            => self.InternalCreateStringField(self.Context.Field, transform);

        public static IIndexableField CreateStringField<TValue>(this IFieldFactory<TValue> self, string extension, Func<TValue, string> transform)
            => self.InternalCreateStringField($"{self.Context.Field}.{extension}", transform);

        private static IIndexableField InternalCreateStringField<TValue>(this IFieldFactory<TValue> self, string fullpath, Func<TValue, string> transform)
        {
            return new StringField(fullpath, transform(self.DeserializedValue), Field.Store.NO);
            //builder.Add(new StringField(fullpath, transform(deserializedValue), Field.Store.NO));
            //builder.Info(path, fullpath, StringField.TYPE_NOT_STORED, tokenType, typeof(string));
        }
    }

    public class IdentityFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            return new TermQuery(new Term(context.Field, context.Value.ToObject<string>()));
        }

        public IEnumerable<IIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<string>(context);
            yield return factory.CreateStringField();
        }

        public void Apply(IFieldBuilderProvider provider)
        {
            provider.FieldBuilder<string>()
                .AddStringField();
        }
    }

    public class StringFieldStrategy : IFieldStrategy
    {
        public void Apply(IFieldBuilderProvider provider)
        {
            provider.FieldBuilder<string>()
                .AddStringField();
        }
    }

    public class TextFieldStrategy : IFieldStrategy
    {
        public void Apply(IFieldBuilderProvider provider)
        {
            provider.FieldBuilder<string>()
                .AddTextField();
        }
    }

    public class Use
    {
        public void HowToUse()
        {
            IFieldStrategyCollectionBuilder builder = new FieldStrategyCollectionBuilder();
            builder.Use<ExpandedDateTimeFieldStrategy>().For(new TypeFilter<DateTime>());
            builder.Use<ExpandedDateTimeFieldStrategy>().For<DateTime>();

            //builder.For("contentType").Use<ExpandedDateTimeFieldStrategy>().On("field");

        }
    }
    public interface IStrategySelector { }
    public class TypeFilter<T>: IStrategySelector { }
}
