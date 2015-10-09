using System;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Resolvers
{
    public interface IMetaFieldResolver
    {
        string ContentType(JObject json);
        string Area(JObject json);
        string Shard(JObject json);

        Term Identity(JObject json);
    }

    public class DefaultMetaFieldResolver : IMetaFieldResolver
    {
        //TODO: (jmd 2015-10-09) Configurable field names and redo all 

        public string ContentType(JObject json)
        {
            string contentType = (string)json["contentType"];
            return contentType;
        }

        public string Area(JObject json)
        {
            return (string)json["area"];
        }

        public string Shard(JObject json)
        {
            //TODO: (jmd 2015-10-09) Over time sharding and named sharding should be reconsidered.
            //                       since over time sharding is not easy to target from configuration. 

            DateTime created = (DateTime)json["created"];
            string shard = ContentType(json) + "." + created.Year;

            return shard;
        }

        public Term Identity(JObject json)
        {
            return new Term("id", (string)json["id"]);
        }
    }
}