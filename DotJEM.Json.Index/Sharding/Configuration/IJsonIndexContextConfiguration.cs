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
        IJsonIndexConfiguration this[string name] { get; set; }
    }

    public class LuceneJsonIndexContextConfiguration : IJsonIndexContextConfiguration
    {
        private readonly ConcurrentDictionary<string, IJsonIndexConfiguration> configurations = new ConcurrentDictionary<string, IJsonIndexConfiguration>();

        public IJsonIndexConfiguration this[string name]
        {
            set { configurations[name] = value; }
            get { return configurations.GetOrAdd(name, key => new DefaultJsonIndexConfiguration()); }
        }
    }

    public interface IJsonIndexConfiguration
    {
        IMetaFieldResolver MetaFieldResolver { get; }
        ILuceneDocumentFactory DocumentFactory { get; }
    }

    public class DefaultJsonIndexConfiguration : IJsonIndexConfiguration
    {
        private readonly IServiceResolver services = DefaultServices.CreateDefaultResolver();

        public IMetaFieldResolver MetaFieldResolver => services.Resolve<IMetaFieldResolver>();
        public ILuceneDocumentFactory DocumentFactory => services.Resolve<ILuceneDocumentFactory>();

    }


    
}