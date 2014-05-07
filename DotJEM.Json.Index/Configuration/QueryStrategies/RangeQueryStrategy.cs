using System.Text.RegularExpressions;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.QueryStrategies
{
    public class RangeQueryStrategy : AbstactQueryStrategy
    {
        private readonly Regex rangeTerm = new Regex("(-?\\d+)-(\\d+)", RegexOptions.Compiled);

        public override Query Create(string field, string value)
        {
            Match match = rangeTerm.Match(value);
            if (!match.Success)
                return null;

            //TODO: Support other than longs.
            long lower = long.Parse(match.Groups[1].Value);
            long upper = long.Parse(match.Groups[2].Value);
            return NumericRangeQuery.NewLongRange(field, lower, upper, true, true);
        }
    }
}