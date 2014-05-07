using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.QueryStrategies
{
    public class TermQueryStrategy : AbstactQueryStrategy
    {
        public override Query Create(string field, string value)
        {
            return new TermQuery(new Term(field, value));
        }
    }
}