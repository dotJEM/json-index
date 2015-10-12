using System;
using DotJEM.Json.Index.Sharding.Infos;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Resolvers
{
    public interface IMetaFieldResolver
    {
        string ContentType(JObject json);
        ShardInfo Shard(JObject json);

        Term Identity(JObject json);
    }

    public class DefaultMetaFieldResolver : IMetaFieldResolver
    {
        //TODO: (jmd 2015-10-09) Configurable field names and redo all 
        private string contentTypeField;

        public DefaultMetaFieldResolver(string contentTypeField = "contentType")
        {
            this.contentTypeField = contentTypeField;
        }


        public string ContentType(JObject json)
        {
            string contentType = (string)json[contentTypeField];
            return contentType;
        }
        
        public ShardInfo Shard(JObject json)
        {
            //TODO: (jmd 2015-10-09) Over time sharding and named sharding should be reconsidered.
            //                       since over time sharding is not easy to target from configuration. 

            DateTime created = (DateTime)json["created"];
            return new ShardInfo(ContentType(json), created.ToString("yyyyMM"));
        }

        public Term Identity(JObject json)
        {
            return new Term("id", (string)json["id"]);
        }
    }
}