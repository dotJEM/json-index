using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Sharding.Documents;
using DotJEM.Json.Index.Sharding.Resolvers;
using DotJEM.Json.Index.Sharding.Schemas;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Configuration
{
    public interface IJsonIndexContextConfiguration
    {
        IJsonIndexConfiguration Default { get; set; }
        IJsonIndexConfiguration this[string name] { get; set; }
    }

    public class LuceneJsonIndexContextConfiguration : IJsonIndexContextConfiguration
    {
        private readonly ConcurrentDictionary<string, IJsonIndexConfiguration> configurations 
            = new ConcurrentDictionary<string, IJsonIndexConfiguration>();

        public IJsonIndexConfiguration Default { get; set; } = new DefaultJsonIndexConfiguration();

        public IJsonIndexConfiguration this[string name]
        {
            set { configurations[name] = value; }
            get
            {
                IJsonIndexConfiguration config;
                return configurations.TryGetValue(name, out config) ? config : Default;
            }
        }
    }

    public interface IJsonIndexConfiguration
    {
        IMetaFieldResolver MetaFieldResolver { get; }
        ILuceneDocumentFactory DocumentFactory { get; }

        IJsonIndexShardsConfiguration Shards { get; }


    }

    public class DefaultJsonIndexConfiguration : IJsonIndexConfiguration
    {
        private readonly IServiceResolver services = DefaultServices.CreateDefaultResolver();

        public IMetaFieldResolver MetaFieldResolver => services.Resolve<IMetaFieldResolver>();
        public ILuceneDocumentFactory DocumentFactory => services.Resolve<ILuceneDocumentFactory>();

        public IJsonIndexShardsConfiguration Shards { get; } = new JsonIndexShardsConfiguration();
    }

    public interface IJsonIndexShardsConfiguration
    {
        IJsonIndexShardConfiguration Default { get; set; }
        IJsonIndexShardConfiguration this[string name] { get; set; }

    }

    public class JsonIndexShardsConfiguration : IJsonIndexShardsConfiguration
    {
        private readonly ConcurrentDictionary<string, IJsonIndexShardConfiguration> configurations
            = new ConcurrentDictionary<string, IJsonIndexShardConfiguration>();

        public IJsonIndexShardConfiguration Default { get; set; } = new DefaultJsonIndexShardConfiguration();

        public IJsonIndexShardConfiguration this[string name]
        {
            set { configurations[name] = value; }
            get
            {
                IJsonIndexShardConfiguration config;
                return configurations.TryGetValue(name, out config) ? config : Default;
            }
        }
    }

    public interface IJsonIndexShardConfiguration
    {
        bool Partitioned { get; }
    }

    public class DefaultJsonIndexShardConfiguration : IJsonIndexShardConfiguration
    {
        public bool Partitioned { get; } = false;

        public DefaultJsonIndexShardConfiguration()
        {
        }
    }


}