using System;
using DotJEM.Index.Searching;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Results;

namespace DotJEM.Json.Index.QueryParsers
{
    public static class IndexSearcherExtensions
    {
        public static IJsonIndexConfigurator UseSimplifiedLuceneQueryParser(this IJsonIndexConfigurator self)
        {
            self.Services.Use<ILuceneQueryParser, SimplifiedLuceneQueryParser>();
            return self;
        }
        public static IServiceCollection UseSimplifiedLuceneQueryParser(this IServiceCollection self)
        {
            self.Use<ILuceneQueryParser, SimplifiedLuceneQueryParser>();
            return self;
        }

        public static Search Search(this IJsonIndexSearcher self, string query)
        {
            ILuceneQueryParser parser = self.Index.ResolveParser();
            LuceneQueryInfo queryInfo = parser.Parse(query);
            return self.Search(queryInfo.Query).OrderBy(queryInfo.Sort);
        }

        public static Search Search(this ILuceneJsonIndex self, string query)
        {
            ILuceneQueryParser parser = self.ResolveParser();
            LuceneQueryInfo queryInfo = parser.Parse(query);


            //SortField sortField = new SortField("", );
            //Sort sort = new Sort();

            return self.CreateSearcher().Search(queryInfo.Query).OrderBy(queryInfo.Sort);
        }

        private static ILuceneQueryParser ResolveParser(this ILuceneJsonIndex self)
        {
            //TODO: Fail and ask for configuration instead.
            return self.Services.Resolve<ILuceneQueryParser>() ?? throw new Exception("Query parser not configured.");
        }

    }
}
