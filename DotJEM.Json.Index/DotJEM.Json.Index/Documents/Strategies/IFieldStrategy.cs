using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Documents.Builder;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Documents.Strategies
{
    public interface IFieldStrategy
    {
        IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context);
    }
    public class ExpandedDateTimeFieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            //TODO: Can we do better here? Lucene it self seems to use a lexical format for dateTimes
            //
            //   Examples: 
            //      2014-09-10T11:00 => 0hzwfs800
            //      2014-09-10T13:00 => 0hzxzie7z
            //      
            //      The fields below may however provide other search capabilities such as all things creating during the morning etc.

            yield return new JsonIndexableFieldBuilder<DateTime>(this, token, context)
                .CreateStringField(v => v.ToString("s"))
                .CreateInt64Field("@ticks", v => v.Ticks)
                .CreateInt32Field("@year", v => v.Year)
                .CreateInt32Field("@month", v => v.Month)
                .CreateInt32Field("@day", v => v.Day)
                .CreateInt32Field("@hour", v => v.Hour)
                .CreateInt32Field("@minute", v => v.Minute)
                .Build();
        }
    }


    public class ExpandedTimeSpanFieldStrategy : IFieldStrategy
    {
        //public Query CreateQuery(IPathContext context)
        //{
        //    throw new NotImplementedException();
        //}

        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<TimeSpan>(this, token, context)
                .CreateStringField(v => v.ToString("g"))
                .CreateInt64Field("@ticks", v => v.Ticks)
                .CreateInt32Field("@days", v => v.Days)
                .CreateInt32Field("@hours", v => v.Hours)
                .CreateInt32Field("@minutes", v => v.Minutes)
                .Build();
        }
    }

    public class IdentityFieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<string>(this, token, context).CreateStringField().Build();
        }
    }

    public class StringFieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<string>(this, token, context).CreateStringField().Build();
        }
    }

    public class TextFieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<string>(this, token, context).CreateTextField().Build();
        }
    }
    public class ArrayFieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<JArray>(this, token, context).CreateInt32Field("@count", v => v.Count).Build();
        }
    }

    public class Int64FieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<int>(this, token, context).CreateInt64Field().Build();
        }
    }

    public class DoubleFieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<double>(this, token, context).CreateDoubleField().Build();
        }
    }

    public class BooleanFieldStrategy : IFieldStrategy
    {
        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<bool>(this, token, context).CreateStringField(b => b.ToString()).Build();
        }
    }

    public class NullFieldStrategy : IFieldStrategy
    {
        private readonly string nullValue;

        public NullFieldStrategy(string nullValue)
        {
            this.nullValue = nullValue;
        }

        public IEnumerable<IIndexableJsonField> CreateFields(JToken token, IPathContext context)
        {
            yield return new JsonIndexableFieldBuilder<object>(this, token, context).CreateStringField(b => nullValue).Build();
        }
    }
}