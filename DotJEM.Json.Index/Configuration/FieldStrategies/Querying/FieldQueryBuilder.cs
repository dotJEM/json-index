using System;
using System.Collections.Generic;
using System.Globalization;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using Lucene.Net.QueryParsers;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.FieldStrategies.Querying
{
    public interface IFieldQueryBuilder
    {
        Query BuildFieldQuery(CallContext call, string query, int slop);
        Query BuildFieldQuery(CallContext call, string query, bool quoted);
        Query BuildFuzzyQuery(CallContext call, string query, float similarity);
        Query BuildPrefixQuery(CallContext call, string query);
        Query BuildWildcardQuery(CallContext call, string query);
        Query BuildRangeQuery(CallContext call, string part1, string part2, bool startInclusive, bool endInclusive);
    }

    public class FieldQueryBuilder : IFieldQueryBuilder
    {
        protected IQueryParser Parser { get; }
        protected string Field { get; }
        protected JsonSchemaExtendedType Type { get; }

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
        
        public virtual Query BuildFieldQuery(CallContext call, string query, bool quoted)
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
        
        public virtual Query BuildRangeQuery(CallContext call, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            //TODO: Try parse and separate type generators.
            IList<BooleanClause> clauses = new List<BooleanClause>();
            //TODO: this is hacky atm. 
            if (Field.EndsWith(".@count"))
            {
                try
                {
                    clauses.Add(
                        new BooleanClause(
                            NumericRangeQuery.NewInt32Range(Field,
                                part1 == null ? (int?)null : int.Parse(part1),
                                part2 == null ? (int?)null : int.Parse(part2),
                                startInclusive,
                                endInclusive), Occur.MUST));
                }
                catch (FormatException ex)
                {
                    throw new ParseException("Invalid format for count.", ex);
                }
                return Parser.BooleanQuery(clauses, true);
            }

            if (Type.HasFlag(JsonSchemaExtendedType.Date))
            {
                try
                {
                    clauses.Add(new BooleanClause(new DateRangeFieldQueryFactory().Create(Field, call, part1, part2, startInclusive,endInclusive), 
                        Type == JsonSchemaExtendedType.Date ? Occur.MUST : Occur.SHOULD));
                }
                catch (FormatException ex)
                {
                    if (Type == JsonSchemaExtendedType.Date)
                    {
                        throw new ParseException("Invalid DateTime format", ex);
                    }
                }
            }
            
            if (Type.HasFlag(JsonSchemaExtendedType.Integer))
            {
                try
                {
                    clauses.Add(
                        new BooleanClause(
                            NumericRangeQuery.NewInt64Range(Field,
                                part1 == null ? (long?)null : long.Parse(part1),
                                part2 == null ? (long?)null : long.Parse(part2),
                                startInclusive,
                                endInclusive), Type == JsonSchemaExtendedType.Integer ? Occur.MUST : Occur.SHOULD));
                }
                catch (FormatException ex)
                {
                    if (Type == JsonSchemaExtendedType.Integer)
                    {
                        throw new ParseException("Invalid Integer format", ex);
                    }
                }
            }


            if (Type != JsonSchemaExtendedType.Date && Type != JsonSchemaExtendedType.Integer)
            {
                clauses.Add(new BooleanClause(call.CallDefault(), Occur.SHOULD));
            }

            return Parser.BooleanQuery(clauses, true);
        }
    }
}