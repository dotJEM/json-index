using System;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Configuration.QueryStrategies
{
    public class FreeTextQueryStrategy : AbstactQueryStrategy
    {
        private static readonly char[] delimiters = " ".ToCharArray();

        public override Query Create(string field, string value)
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
}