using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotJEM.AdvParsers;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast
{
    public enum FieldOperator
    {
        None, Equals, NotEquals, GreaterThan, GreaterThanOrEquals, LessThan, LessThanOrEquals, In, NotIt, Similar, NotSimilar
    }

    public enum FieldOrder
    {
        None, Ascending, Descending
    }

    public abstract class QueryAst
    {
        private readonly Dictionary<string, object> metaData = new Dictionary<string, object>();

        public IEnumerable<(string, object)> MetaData => metaData.Select(kv => (kv.Key, kv.Value));

        public TData Get<TData>(string key) where TData : class => metaData[key] as TData;
        public void Put<TData>(string key, TData value) where TData : class => metaData[key] = value;

        public abstract TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context);
    }

    public class OrderBy : QueryAst
    {
        private readonly OrderField[] orderFields;
        public int Count => orderFields.Length;
        public IEnumerable<OrderField> OrderFields => orderFields;

        public OrderBy(OrderField[] orderFields) 
        {
            this.orderFields = orderFields;
        }

        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }

    public class OrderField : QueryAst
    {
        public string Name { get; }

        public FieldOrder SpecifiedOrder { get; }

        public OrderField(string name, FieldOrder order)
        {
            Name = name;
            SpecifiedOrder = order;
        }
        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }

    public class OrderedQuery : QueryAst
    {
        public QueryAst Query { get; }
        public QueryAst Ordering { get; }

        public OrderedQuery(QueryAst query, QueryAst order)
        {
            this.Query = query;
            this.Ordering = order;
        }

        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }

    public abstract class CompositeQuery : QueryAst
    {
        private readonly QueryAst[] queries;

        public int Count => queries.Length;
        public IEnumerable<QueryAst> Queries => queries;

        protected CompositeQuery(QueryAst[] queries)
        {
            this.queries = queries;
        }
    }

    public class ImplicitCompositeQuery : CompositeQuery
    {
        public ImplicitCompositeQuery(QueryAst[] queries) : base(queries)
        {
        }

        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }

    public class OrQuery : CompositeQuery {
        public OrQuery(QueryAst[] queries) 
            : base(queries)
        {
        }

        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }
    
    public class AndQuery : CompositeQuery
    {
        public AndQuery(QueryAst[] queries) : base(queries)
        {
        }

        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }

    public class NotQuery : QueryAst
    {
        public QueryAst Not { get; }

        public NotQuery(QueryAst not)
        {
            Not = not;
        }

        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }

    public class FieldQuery : QueryAst
    {
        public string Name { get; }
        public FieldOperator Operator { get; }
        public Value Value { get; }

        public FieldQuery(string name, FieldOperator fieldOperator, Value value)
        {
            Name = name;
            Operator = fieldOperator;
            Value = value;
        }
        public override TResult Accept<TResult, TContext>(ISimplifiedQueryAstVisitor<TResult, TContext> visitor, TContext context) => visitor.Visit(this, context);
    }

    public abstract class Value
    {

    }

    public class StringValue : Value
    {
        public string Value { get; }

        public StringValue(string value)
        {
            this.Value = value;
        }
    }

    public class WildcardValue : StringValue {
        public WildcardValue(string value) : base(value)
        {
        }
    }
    public class PhraseValue : Value
    {
        public string Value { get; }

        public PhraseValue(string value)
        {
            this.Value = value;
        }
    }

    public class NumberValue : Value
    {
        public double Value { get; }

        public NumberValue(double value)
        {
            this.Value = value;
        }
    }

    public class DateTimeValue : Value
    {
        public DateTime Value { get; }
        public Kind DateTimeKind { get; }

        public DateTimeValue(DateTime value, Kind dateTimeKind)
        {
            Value = value;
            DateTimeKind = dateTimeKind;
        }

        public enum Kind
        {
            Date, Time, DateTime
        }
    }

    public class OffsetDateTime : Value
    {
        public static Regex pattern = new Regex("^(?'r'NOW|TODAY)?(?'s'[+-])(?'v'.*)", RegexOptions.Compiled);

        public string Raw { get; }
        public DateTime Now { get; }
        public TimeSpan Offset { get; }

        public DateTime Value { get; }


        private OffsetDateTime(string raw, TimeSpan offset, DateTime now)
        {
            Raw = raw;
            Offset = offset;
            Now = now;

            Value = now.Add(offset);
        }

        public static OffsetDateTime Parse(DateTime now, string text)
        {
            TimeSpanParser parser = new TimeSpanParser();
            Match match = pattern.Match(text.Trim());

            if (!match.Success)
                throw new ArgumentException($"Could not parse OffsetDateTime: {text}");

            string r = match.Groups['r']?.Value;
            string s = match.Groups['s']?.Value;
            string v = match.Groups['v']?.Value;

            TimeSpan offset = parser.Parse(v);
            offset = s == "+" ? offset : offset.Negate();
            now = r?.ToLower() == "now" ? now : now.Date;
                
            return new OffsetDateTime(text, offset, now);
        }
    }

    public class IntegerValue : Value
    {
        public long Value { get; }

        public IntegerValue(long value)
        {
            this.Value = value;
        }
    }

    public class MatchAllValue : Value
    {
    }

    public class ListValue : Value
    {
        private readonly Value[] values;

        public int Count => values.Length;
        public IEnumerable<Value> Values => values;

        public ListValue(Value[] values)
        {
            this.values = values;
        }
    }

}
