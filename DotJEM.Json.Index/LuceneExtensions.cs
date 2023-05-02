using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index
{
    internal static class LuceneExtensions
    {
        public static Document Put(this Document self, IIndexableField field)
        {
            self.Add(field);
            return self;
        }

        public static BooleanQuery Put(this BooleanQuery self, Query query, Occur occur)
        {
            self.Add(query, occur);
            return self;
        }
    }
}