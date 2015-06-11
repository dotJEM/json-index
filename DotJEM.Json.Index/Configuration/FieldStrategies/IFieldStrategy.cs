using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Configuration.FieldStrategies
{
    public interface IFieldQueryBuilder
    {
        Query BuildFieldQuery(CallContext call, string query, int slop);
        Query BuildFuzzyQuery(CallContext call, string query, float similarity);
        Query BuildPrefixQuery(CallContext call, string query);
        Query BuildWildcardQuery(CallContext call, string query);
        Query BuildRangeQuery(CallContext call, string part1, string part2, bool inclusive);
    }

    public class FieldQueryBuilder : IFieldQueryBuilder
    {
        protected IQueryParser Parser { get; private set; }
        protected string Field { get; private set; }
        protected JsonSchemaExtendedType Type { get; private set; }

        public FieldQueryBuilder(IQueryParser parser, string field, JsonSchemaExtendedType type)
        {
            Parser = parser;
            Field = field;
            Type = type;
        }
        
        public virtual Query BuildFieldQuery(CallContext call, string query, int slop)
        {
            return call.CallDefault();
        }

        public virtual Query BuildFuzzyQuery(CallContext call, string query, float similarity)
        {
            return call.CallDefault();
        }

        public virtual Query BuildPrefixQuery(CallContext call, string query)
        {
            return call.CallDefault();
        }

        public virtual Query BuildWildcardQuery(CallContext call, string query)
        {
            return call.CallDefault();
        }
        
        public virtual Query BuildRangeQuery(CallContext call, string part1, string part2, bool inclusive)
        {
            IList<BooleanClause> clauses = new List<BooleanClause>();
            if (Type.HasFlag(JsonSchemaExtendedType.Date))
            {
                try
                {
                    clauses.Add(
                        new BooleanClause(
                            NumericRangeQuery.NewLongRange(Field,
                            part1 == "*" ? (long?) null : DateTime.Parse(part1, CultureInfo.InvariantCulture).Ticks,
                            part2 == "*" ? (long?) null : DateTime.Parse(part2, CultureInfo.InvariantCulture).Ticks,
                            inclusive,
                            inclusive), Occur.SHOULD));
                }
                catch (FormatException ex)
                {
                    if (Type == JsonSchemaExtendedType.Date)
                    {
                        throw new ParseException("Invalid DateTime format", ex);
                    }
                }
            }

            if (Type != JsonSchemaExtendedType.Date)
            {
                clauses.Add(new BooleanClause(call.CallDefault(), Occur.SHOULD));
            }

            return Parser.BooleanQuery(clauses, true);
        }
    }

    public class TermFieldQueryBuilder : FieldQueryBuilder
    {
        public TermFieldQueryBuilder(IQueryParser parser, string field, JsonSchemaExtendedType type)
            : base(parser, field, type)
        {
        }

        public override Query BuildFieldQuery(CallContext call, string query, int slop)
        {
            return new TermQuery(new Term(Field, query));
        }
    }

    public interface IFieldStrategy
    {
        Query BuildQuery(string path, string value);

        IEnumerable<IFieldable> BuildFields(string fieldName, JValue value);
        IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type);
    }

    public class FieldStrategy : IFieldStrategy
    {
        public virtual IEnumerable<IFieldable> BuildFields(string fieldName, JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Integer:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetLongValue(value.Value<long>());
                    break;

                case JTokenType.Float:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetDoubleValue(value.Value<double>());
                    break;

                case JTokenType.Date:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetLongValue(value.Value<DateTime>().Ticks);
                    break;

                case JTokenType.TimeSpan:
                    yield return new NumericField(fieldName, Field.Store.NO, true)
                        .SetLongValue(value.Value<TimeSpan>().Ticks);
                    break;

                case JTokenType.Null:
                case JTokenType.Undefined:
                    yield return new Field(fieldName, "NULL", Field.Store.NO, Field.Index.NOT_ANALYZED);
                    yield break;

                case JTokenType.Bytes:
                case JTokenType.None:
                case JTokenType.Object:
                case JTokenType.Array:
                case JTokenType.Constructor:
                case JTokenType.Property:
                case JTokenType.Comment:
                case JTokenType.Raw:
                    yield break;

                //Note: Let these fall thought to the standard tostring handler.
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Guid:
                case JTokenType.Uri:
                    break;
            }
            //NOTE: Always add as string.
            string str = value.ToString(CultureInfo.InvariantCulture);
            yield return new Field(fieldName, str, Field.Store.NO, Field.Index.NOT_ANALYZED);
            yield return new Field(fieldName, str, Field.Store.NO, Field.Index.ANALYZED);
        }

        public virtual IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type)
        {
            return new FieldQueryBuilder(parser, fieldName, type);
        }

        //NOTE: This is temporary for now.
        private static readonly char[] delimiters = " ".ToCharArray();
        public virtual Query BuildQuery(string field, string value)
        {
            value = value.ToLowerInvariant();
            string[] words = value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (!words.Any())
                return null;

            BooleanQuery query = new BooleanQuery();
            foreach (string word in words)
            {
                //Note: As for the WildcardQuery, we only add the wildcard to the end for performance reasons.
                query.Add(new FuzzyQuery(new Term(field, word)), Occur.SHOULD);
                query.Add(new WildcardQuery(new Term(field, word + "*")), Occur.SHOULD);
            }
            return query;
        }

    }

    public class NullFieldStrategy : FieldStrategy
    {
        public override IEnumerable<IFieldable> BuildFields(string fieldName, JValue value)
        {
            yield break;
        }
    }

    public class TermFieldStrategy : FieldStrategy
    {
        public override IEnumerable<IFieldable> BuildFields(string fieldName, JValue value)
        {
            string str = value.ToString(CultureInfo.InvariantCulture);
            yield return new Field(fieldName, str, Field.Store.NO, Field.Index.NOT_ANALYZED);
            yield return new Field(fieldName, str, Field.Store.NO, Field.Index.ANALYZED);
        }

        public override Query BuildQuery(string field, string value)
        {
            return new TermQuery(new Term(field, value));
        }

        //TODO: Select Builder implementation pattern instead.
        public override IFieldQueryBuilder PrepareBuilder(IQueryParser parser, string fieldName, JsonSchemaExtendedType type)
        {
            return new TermFieldQueryBuilder(parser, fieldName, type);
        }
    }

    public abstract class NumericFieldStrategy : FieldStrategy
    {
        private readonly Func<NumericField, JValue, NumericField> fieldSetter;

        protected NumericFieldStrategy(Func<NumericField, JValue, NumericField> setter)
        {
            fieldSetter = setter;
        }

        public override IEnumerable<IFieldable> BuildFields(string fieldName, JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.Null:
                case JTokenType.Undefined:
                    yield return new Field(fieldName, "NULL", Field.Store.NO, Field.Index.NOT_ANALYZED);
                    break;

                default:
                    yield return fieldSetter(new NumericField(fieldName, Field.Store.NO, true), value);
                    break;
            }
        }
    }

    public class IntegerFieldStragety : NumericFieldStrategy
    {
        public IntegerFieldStragety()
            : base((field, value) => field.SetIntValue(value.Value<int>()))
        {
        }
    }

    public class LongFieldStragety : NumericFieldStrategy
    {
        public LongFieldStragety()
            : base((field, value) => field.SetLongValue(value.Value<long>()))
        {
        }
    }

    public class FloatFieldStragety : NumericFieldStrategy
    {
        public FloatFieldStragety()
            : base((field, value) => field.SetFloatValue(value.Value<float>()))
        {
        }
    }

    public class DoubleFieldStragety : NumericFieldStrategy
    {
        public DoubleFieldStragety()
            : base((field, value) => field.SetDoubleValue(value.Value<double>()))
        {
        }
    }

    public class DateTimeFieldStragety : NumericFieldStrategy
    {
        public DateTimeFieldStragety()
            : base((field, value) => field.SetLongValue(value.Value<DateTime>().Ticks))
        {
        }
    }

    public class TimeSpanFieldStragety : NumericFieldStrategy
    {
        public TimeSpanFieldStragety()
            : base((field, value) => field.SetLongValue(value.Value<TimeSpan>().Ticks))
        {
        }
    }
}