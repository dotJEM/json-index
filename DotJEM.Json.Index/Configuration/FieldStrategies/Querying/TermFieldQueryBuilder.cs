using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.FieldStrategies.Querying
{
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
}