using System;
using DotJEM.Json.Index.Contexts;
using DotJEM.Json.Index.Results;

namespace DotJEM.Json.Index.QueryParsers.Contexts
{
    public static class ContextIndexSearcherExtensions
    {
        public static Search Search(this ILuceneIndexContext self, string query)
        {
            ILuceneQueryParser parser = self.ResolveParser();
            LuceneQueryInfo queryInfo = parser.Parse(query);
            return self.Search(queryInfo.Query).OrderBy(queryInfo.Sort);
        }

        private static ILuceneQueryParser ResolveParser(this ILuceneIndexContext self)
        {
            //TODO: Fail and ask for configuration instead.
            throw new NotImplementedException();
            //return self.Defaults.Resolve<ILuceneQueryParser>() ?? throw new Exception("Query parser not configured.");
        }
    }

}
