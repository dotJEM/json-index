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
        public string ContentType(JObject json)
        {
            return (string)json["contentType"];
        }

        public string Area(JObject json)
        {
            return (string)json["area"];
        }

        public string Shard(JObject json)
        {
            DateTime created = (DateTime)json["created"];
            return (string)json[ContentType(json) + "." + created.Year];
        }

        public Term Identity(JObject json)
        {
            return new Term("id", (string)json["id"]);
        }
    }
}