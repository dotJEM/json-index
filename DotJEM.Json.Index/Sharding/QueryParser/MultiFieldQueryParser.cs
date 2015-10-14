using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using DotJEM.Json.Index.Configuration.FieldStrategies;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Lucene.Net.Util;
using CallContext = DotJEM.Json.Index.Searching.CallContext;

namespace DotJEM.Json.Index.Sharding.QueryParser
{
    public class JsonIndexQueryParser : Lucene.Net.QueryParsers.QueryParser, IQueryParser
    {
        private readonly string[] fields;


        public JsonIndexQueryParser(params string[] fields)
            : base(Version.LUCENE_30, null, new KeywordAnalyzer())
        {
            this.fields = fields;
        }

        public Query BooleanQuery(IList<BooleanClause> clauses, bool disableCoord)
        {
            return GetBooleanQuery(clauses, disableCoord);
        }

        //public override Query Parse(string query)
        //{
        //    return base.Parse(query);
        //}

        private IFieldQueryBuilder PrepareBuilderFor(string field)
        {
            return new FieldQueryBuilder(this,field, JsonSchemaExtendedType.Any);
            //JsonSchemaExtendedType type = index.Schemas.ExtendedType(field);
            ////TODO: Use "ForAll" strategy for now, we need to be able to extract possible contenttypes from the query and
            ////      target their strategies. But this may turn into being very complex as different branches of a Query
            ////      may target different 
            //return index.Configuration.Field.Strategy(field)
            //    .PrepareBuilder(this, field, type);
        }

        protected override Query GetFieldQuery(string fieldName, string queryText, int slop)
        {
            if (fieldName != null)
            {
                var query = PrepareBuilderFor(fieldName)
                    .BuildFieldQuery(new CallContext(() => base.GetFieldQuery(fieldName, queryText)), queryText, slop)
                    .ApplySlop(slop);
                return query;
            }

            IList<BooleanClause> clauses = fields
                .Select(field => base.GetFieldQuery(field, queryText))
                .Where(field => field != null)
                .Select(query => query.ApplySlop(slop))
                .Select(query => new BooleanClause(query, Occur.SHOULD))
                .ToList();

            return clauses.Any() ? GetBooleanQuery(clauses, true) : null;
        }

        protected override Query GetFieldQuery(string field, string queryText)
        {
            return GetFieldQuery(field, queryText, 0);
        }

        protected override Query GetFuzzyQuery(string field, string termStr, float minSimilarity)
        {
            if (field != null)
            {
                var query = PrepareBuilderFor(field)
                    .BuildFuzzyQuery(new CallContext(() => base.GetFuzzyQuery(field, termStr, minSimilarity)), termStr, minSimilarity);
                return query;
            }

            return GetBooleanQuery(fields
                .Select(t => new BooleanClause(GetFuzzyQuery(t, termStr, minSimilarity), Occur.SHOULD))
                .ToList(), true);
        }

        protected override Query GetPrefixQuery(string field, string termStr)
        {
            if (field != null)
            {
                var query = PrepareBuilderFor(field)
                    .BuildPrefixQuery(new CallContext(() => base.GetPrefixQuery(field, termStr)), termStr);
                return query;
            }

            return GetBooleanQuery(fields
                .Select(t => new BooleanClause(GetPrefixQuery(t, termStr), Occur.SHOULD))
                .ToList(), true);
        }

        protected override Query GetWildcardQuery(string field, string termStr)
        {
            if (field != null)
            {
                var query = PrepareBuilderFor(field)
                    .BuildWildcardQuery(new CallContext(() => base.GetWildcardQuery(field, termStr)), termStr);
                return query;
            }

            return GetBooleanQuery(fields
                .Select(t => new BooleanClause(GetWildcardQuery(t, termStr), Occur.SHOULD))
                .ToList(), true);
        }

        protected override Query GetRangeQuery(string field, string part1, string part2, bool inclusive)
        {
            part1 = (part1 == "*" ? "null" : part1);
            part2 = (part2 == "*" ? "null" : part2);

            if (field != null)
            {
                var query = PrepareBuilderFor(field)
                    .BuildRangeQuery(new CallContext(() => base.GetRangeQuery(field, part1, part2, inclusive)), part1, part2, inclusive);
                return query;
            }
            return GetBooleanQuery(fields
                .Select(t => new BooleanClause(GetRangeQuery(t, part1, part2, inclusive), Occur.SHOULD))
                .ToList(), true);
        }
    }
}