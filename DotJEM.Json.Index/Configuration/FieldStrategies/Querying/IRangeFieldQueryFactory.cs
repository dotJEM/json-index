using DotJEM.Json.Index.Searching;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.FieldStrategies.Querying
{
    public interface IRangeFieldQueryFactory
    {
        Query Create(string field, CallContext call, string part1, string part2,bool startInclusive, bool endInclusive);
    }
}