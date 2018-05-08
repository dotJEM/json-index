using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Documents.Builder;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public interface IFieldStrategy
    {
        Query CreateQuery(IFieldContext context);
        IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context);
    }
    public class ExpandedDateTimeFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            //TODO: Can we do better here? Lucene it self seems to use a lexical format for dateTimes
            //
            //   Examples: 
            //      2014-09-10T11:00 => 0hzwfs800
            //      2014-09-10T13:00 => 0hzxzie7z
            //      
            //      The fields below may however provide other search capabilities such as all things creating during the morning etc.

            var factory = new FieldFactory<DateTime>(context);
            yield return factory.CreateStringField(v => v.ToString("s"));
            yield return factory.CreateInt64Field("@ticks", v => v.Ticks);
            yield return factory.CreateInt32Field("@year", v => v.Year);
            yield return factory.CreateInt32Field("@month", v => v.Month);
            yield return factory.CreateInt32Field("@day", v => v.Day);
            yield return factory.CreateInt32Field("@hour", v => v.Hour);
            yield return factory.CreateInt32Field("@minute", v => v.Minute);
        }
    }

    public class ExpandedTimeSpanFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<TimeSpan>(context);
            yield return factory.CreateStringField(v => v.ToString("g"));
            yield return factory.CreateInt64Field("@ticks", v => v.Ticks);
            yield return factory.CreateInt32Field("@days", v => v.Days);
            yield return factory.CreateInt32Field("@hours", v => v.Hours);
            yield return factory.CreateInt32Field("@minutes", v => v.Minutes);
        }
    }

    public class IdentityFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            return new TermQuery(new Term(context.Field, context.Value.ToObject<string>()));
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<string>(context);
            yield return factory.CreateStringField();
        }
    }

    public class StringFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<string>(context);
            yield return factory.CreateStringField();
        }
    }

    public class TextFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<string>(context);
            yield return factory.CreateTextField();
        }
    }
    public class ArrayFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<JArray>(context);
            yield return factory.CreateInt32Field("@count", v => v.Count);
        }
    }

    public class Int64FieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<int>(context);
            yield return factory.CreateInt64Field();
        }
    }

    public class DoubleFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<double>(context);
            yield return factory.CreateDoubleField();
        }
    }

    public class BooleanFieldStrategy : IFieldStrategy
    {
        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<bool>(context);
            yield return factory.CreateDoubleField();
        }
    }

    public class NullFieldStrategy : IFieldStrategy
    {
        private readonly string nullValue;

        public NullFieldStrategy(string nullValue)
        {
            this.nullValue = nullValue;
        }

        public Query CreateQuery(IFieldContext context)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IJsonIndexableField> CreateFields(IFieldContext context)
        {
            var factory = new FieldFactory<object>(context);
            yield return factory.CreateStringField(s => nullValue);
        }
    }
}