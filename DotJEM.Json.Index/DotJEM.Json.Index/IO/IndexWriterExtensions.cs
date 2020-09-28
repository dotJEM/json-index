using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.IO
{
    public static class IndexWriterExtensions
    {
        public static ILuceneJsonIndex Create(this ILuceneJsonIndex self, JObject doc)
        {
            self.CreateWriter().Create(doc);
            return self;
        }

        public static ILuceneJsonIndex Create(this ILuceneJsonIndex self, IEnumerable<JObject> docs)
        {
            self.CreateWriter().Create(docs);
            return self;
        }

        public static ILuceneJsonIndex Update(this ILuceneJsonIndex self, JObject doc)
        {
            self.CreateWriter().Update(doc);
            return self;
        }

        public static ILuceneJsonIndex Update(this ILuceneJsonIndex self, IEnumerable<JObject> docs)
        {
            self.CreateWriter().Update(docs);
            return self;
        }
        
        public static ILuceneJsonIndex Delete(this ILuceneJsonIndex self, JObject doc)
        {
            self.CreateWriter().Delete(doc);
            return self;
        }

        public static ILuceneJsonIndex Delete(this ILuceneJsonIndex self, IEnumerable<JObject> docs)
        {
            self.CreateWriter().Delete(docs);
            return self;
        }
        
        public static ILuceneJsonIndex Flush(this ILuceneJsonIndex self, bool triggerMerge, bool applyDeletes)
        {
            self.CreateWriter().Flush(triggerMerge, applyDeletes);
            return self;
        }

        public static ILuceneJsonIndex Commit(this ILuceneJsonIndex self)
        {
            self.CreateWriter().Commit();
            return self;
        }
    }
}