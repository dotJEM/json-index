using DotJEM.Json.Index.Configuration.QueryStrategies;

namespace DotJEM.Json.Index.Configuration
{
    public static class Using
    {
        public static TermQueryStrategy Term()
        {
            return new TermQueryStrategy();
        }

        public static RangeQueryStrategy Range()
        {
            return new RangeQueryStrategy();
        }

        public static FreeTextQueryStrategy FreeText()
        {
            return new FreeTextQueryStrategy();
        }
    }

    public static class QueryStrategiesExt
    {
        public static TermQueryStrategy Term(this IQueryStrategyBuilder self)
        {
            return Using.Term();
        }

        public static RangeQueryStrategy Range(this IIndexStrategyBuilder self)
        {
            return Using.Range();
        }

        public static FreeTextQueryStrategy FreeText(this IIndexStrategyBuilder self)
        {
            return Using.FreeText();
        }
    }
}