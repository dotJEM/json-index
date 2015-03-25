using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotJEM.Json.Index.Schema;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Searching
{
    public class MultiFieldQueryParser : QueryParser
    {
        private readonly string[] fields;
        private readonly IStorageIndex index;

        public MultiFieldQueryParser(string query, IStorageIndex index)
            : base(index.Version, null, index.Analyzer)
        {
            fields = index.Schemas.AllFields().ToArray();
            this.index = index;
        }

        public override Query Parse(string query)
        {
            return base.Parse(query);
        }

        protected override Query GetFieldQuery(string fieldName, string queryText, int slop)
        {
            if (fieldName != null)
            {


                var query = ApplySlop(base.GetFieldQuery(fieldName, queryText), slop);
                return query;
            }


            IList<BooleanClause> clauses = fields
                .Select(field => base.GetFieldQuery(field, queryText))
                .Where(field => field != null)
                .Select(query => ApplySlop(query, slop))
                .Select(query => new BooleanClause(query, Occur.SHOULD)).ToList();
            return clauses.Any() ? GetBooleanQuery(clauses, true) : null;
        }

        protected override Query GetFieldQuery(string field, string queryText)
        {
            return GetFieldQuery(field, queryText, 0);
        }

        protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (field != null)
                return base.GetFuzzyQuery(field, termStr, minSimilarity);
            IList<BooleanClause> clauses = fields.Select(t => new BooleanClause(GetFuzzyQuery(t, termStr, minSimilarity), Occur.SHOULD)).ToList();
            return GetBooleanQuery(clauses, true);
        }

        protected override Query GetPrefixQuery(string field, string termStr)
        {
            if (field != null)
                return base.GetPrefixQuery(field, termStr);
            IList<BooleanClause> clauses = fields.Select(t => new BooleanClause(GetPrefixQuery(t, termStr), Occur.SHOULD)).ToList();
            return GetBooleanQuery(clauses, true);
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            if (field != null)
                return base.GetWildcardQuery(field, termStr);
            IList<BooleanClause> clauses = fields.Select(t => new BooleanClause(GetWildcardQuery(t, termStr), Occur.SHOULD)).ToList();
            return GetBooleanQuery(clauses, true);
        }

        protected override Query GetRangeQuery(string field, string part1, string part2, bool inclusive)
        {
            if (field == null)
            {
                var hmm = GetBooleanQuery(fields.Select(t => new BooleanClause(GetRangeQuery(t, part1, part2, inclusive), Occur.SHOULD))
                        .ToList(), true);
                return hmm;
            }

            var extendedType = index.Schemas.ExtendedType(field);

            IList<BooleanClause> clauses = new List<BooleanClause>();
            if (extendedType.HasFlag(JsonSchemaExtendedType.Date))
            {
                try
                {
                    clauses.Add(
                        new BooleanClause(
                            NumericRangeQuery.NewLongRange(field,
                            DateTime.Parse(part1, CultureInfo.InvariantCulture).Ticks,
                            DateTime.Parse(part2, CultureInfo.InvariantCulture).Ticks, inclusive,
                            inclusive), Occur.SHOULD));
                }
                catch (FormatException ex)
                {
                    if (extendedType == JsonSchemaExtendedType.Date)
                    {
                        throw new ParseException("Invalid DateTime format", ex);
                    }
                }
            }

            if (extendedType != JsonSchemaExtendedType.Date)
            {
                clauses.Add(new BooleanClause(base.GetRangeQuery(field, part1, part2, inclusive), Occur.SHOULD));
            }

            return GetBooleanQuery(clauses, true);
        }

        private static bool ApplySlop(PhraseQuery query, int slop)
        {
            if (query != null) query.Slop = slop;
            return query != null;
        }

        private static bool ApplySlop(MultiPhraseQuery query, int slop)
        {
            if (query != null) query.Slop = slop;
            return query != null;
        }

        // ReSharper disable UnusedMethodReturnValue.Local
        private static Query ApplySlop(Query query, int slop)
        {
            if (ApplySlop(query as PhraseQuery, slop) || ApplySlop(query as MultiPhraseQuery, slop))
            {
            }
            return query;
        }
        // ReSharper restore UnusedMethodReturnValue.Local
    }
}