using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotJEM.Json.Index.Schema;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index.Searching
{
    public class MultiFieldQueryParser : QueryParser
    {
        private readonly string[] fields;
        private readonly IStorageIndex index;

        public MultiFieldQueryParser(Version matchVersion, string[] fields, Analyzer analyzer, IStorageIndex index)
            : base(matchVersion, null, analyzer)
        {
            this.fields = fields;
            this.index = index;
        }

        protected override Query GetFieldQuery(string field, string queryText, int slop)
        {
            Query fieldQuery;
            if (field == null)
            {
                IList<BooleanClause> clauses = new List<BooleanClause>();
                foreach (string t in fields)
                {
                    fieldQuery = base.GetFieldQuery(t, queryText);
                    if (fieldQuery == null) continue;
                    ApplySlop(fieldQuery, slop);
                    clauses.Add(new BooleanClause(fieldQuery, Occur.SHOULD));
                }
                return clauses.Count == 0 ? null : GetBooleanQuery(clauses, true);
            }
            fieldQuery = base.GetFieldQuery(field, queryText);
            ApplySlop(fieldQuery, slop);
            return fieldQuery;
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
        private static bool ApplySlop(Query q, int slop)
        {
            return ApplySlop(q as PhraseQuery, slop) || ApplySlop(q as MultiPhraseQuery, slop);
        }
        // ReSharper restore UnusedMethodReturnValue.Local

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
                return GetBooleanQuery(fields.Select(t => new BooleanClause(GetRangeQuery(t, part1, part2, inclusive), Occur.SHOULD))
                        .ToList(), true);
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
                            DateTime.Parse(part2, CultureInfo.InvariantCulture).Ticks, true,
                            true), Occur.SHOULD));
                }
                catch (FormatException)
                {
                    if (extendedType == JsonSchemaExtendedType.Date)
                    {
                        throw new ParseException();
                    }                    
                }
            }

            if (extendedType != JsonSchemaExtendedType.Date)
            {
                clauses.Add(new BooleanClause(base.GetRangeQuery(field, part1, part2, inclusive), Occur.SHOULD));
            }

            return GetBooleanQuery(clauses, true);
        }
    }
}