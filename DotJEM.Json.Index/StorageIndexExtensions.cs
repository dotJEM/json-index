using System.Collections.Generic;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public static class StorageIndexExtensions
    {
        public static ISearchResult Find(this IStorageIndex self, string query)
        {
            return self.Searcher.Search(query);
        }

        public static ISearchResult Find(this IStorageIndex self, JObject query)
        {
            return self.Searcher.Search(query);
        }

        public static ISearchResult Find(this IStorageIndex self, dynamic query)
        {
            return self.Searcher.Search(JObject.FromObject((object)query));
        }

        public static ISearchResult Find(this IStorageIndex self, Query query)
        {
            return self.Searcher.Search(query);
        }



        public static IStorageIndex Write(this IStorageIndex self, JObject entity)
        {
            self.Writer.Write(entity);
            return self;
        }

        public static IStorageIndex WriteAll(this IStorageIndex self, IEnumerable<JObject> entities)
        {
            self.Writer.WriteAll(entities);
            return self;
        }

        public static IStorageIndex Delete(this IStorageIndex self, JObject entity)
        {
            self.Writer.Delete(entity);
            return self;
        }

        public static IEnumerable<string> Terms(this IStorageIndex self, string field)
        {
            return self.Searcher.Terms(field);
        }

    }
}