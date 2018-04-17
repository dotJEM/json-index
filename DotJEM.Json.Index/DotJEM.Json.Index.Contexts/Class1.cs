using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Storage;
using DotJEM.Json.Index.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Contexts
{
    public interface IJsonIndexConfigurationProvider
    {
        IJsonIndexConfiguration Acquire(string name);
        IJsonIndexConfigurationProvider Use(string name, IJsonIndexConfiguration config);
    }


    public class LuceneIndexConfigurationProvider : IJsonIndexConfigurationProvider
    {
        public IJsonIndexConfiguration Global { get; set; } = new JsonIndexConfiguration();

        private readonly ConcurrentDictionary<string, IJsonIndexConfiguration> configurations
            = new ConcurrentDictionary<string, IJsonIndexConfiguration>();

        public IJsonIndexConfiguration Acquire(string name)
            => configurations.GetOrAdd(name, s => new JsonContextIndexConfiguration(Global));

        public IJsonIndexConfigurationProvider Use(string name, IJsonIndexConfiguration config)
        {
            configurations[name] = config;
            return this;
        }
    }

    public interface ILuceneIndexContext
    {
        ILuceneIndexBuilderDefaults Defaults { get; }

        ILuceneIndexContext Configure(string name, Action<IJsonIndexConfigurator> config);
        ILuceneJsonIndex Open(string name);
    }

    public class LuceneIndexContext : ILuceneIndexContext
    {
        private readonly ConcurrentDictionary<string, ILuceneJsonIndex> indices = new ConcurrentDictionary<string, ILuceneJsonIndex>();

        public LuceneIndexContext()
            : this(new LuceneIndexBuilderDefaults { Storage = new RamStorageFacility() }) { }

        public LuceneIndexContext(string path)
            : this(new LuceneIndexBuilderDefaults { Storage = new SimpleFileSystemRootStorageFacility(path) }) { }

        public LuceneIndexContext(ILuceneIndexBuilderDefaults defaults)
        {
            this.Defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        }

        public ILuceneJsonIndex Open(string name)
        {
            return indices.GetOrAdd(name, key => builders.TryGetValue(key, out IJsonIndexBuilder builder)
            ? builder.Build()
            : new JsonIndexBuilder(name, Defaults).Build());
        }

        public ILuceneIndexBuilderDefaults Defaults { get; }

        public ILuceneIndexContext Configure(string name, Action<IJsonIndexConfigurator> config)
        {
            return Configure(name, builder =>
            {
                config(builder);
                return builder;
            });
        }

        private ILuceneIndexContext Configure(string name, Func<JsonIndexBuilder, IJsonIndexBuilder> config)
        {
            IJsonIndexBuilder builder = config(new ContextedJsonIndexBuilder(name, Defaults));
            builders.AddOrUpdate(name, builder, (s, a) => builder);
            return this;
        }

        private readonly ConcurrentDictionary<string, IJsonIndexBuilder> builders
            = new ConcurrentDictionary<string, IJsonIndexBuilder>();

        public ILuceneJsonIndexSearcher CreateSearcher()
        {
            return new LuceneJsonMultiIndexSearcher(indices.Values);
        }
    }

    public class LuceneJsonMultiIndexSearcher : Disposable, ILuceneJsonIndexSearcher
    {
        public ILuceneJsonIndex Index { get; }
        public IInfoStream InfoStream { get; } = new InfoStream();

        private readonly IIndexSearcherManager manager;

        public LuceneJsonMultiIndexSearcher(IEnumerable<ILuceneJsonIndex> indicies)
        {
            IndexReader[] readers = indicies
                .Select(idx => (IndexReader)idx.Storage.WriterManager.Writer.GetReader(true))
                .ToArray();
            MultiReader reader = new MultiReader(readers, false);
            IndexSearcher searcher = new IndexSearcher(reader);

        }

        public Search Search(Query query)
        {
            return new Search(null, InfoStream, query);
        }
    }

    public class ContextedJsonIndexBuilder : JsonIndexBuilder
    {
        public ContextedJsonIndexBuilder(string name, ILuceneIndexBuilderDefaults context)
            : this(name, context, context.Services)
        {
        }

        public ContextedJsonIndexBuilder(string name, ILuceneIndexBuilderDefaults context, IServiceCollection services)
            : base(name, context, new PerIndexServiceCollection(services))
        {
        }
    }


    public class RamStorageFacility : IStorageFacility
    {
        public Func<ILuceneStorageProvider> Create(string name)
        {
            return () => new LuceneRamStorageProvider();
        }
    }

    public class SimpleFileSystemRootStorageFacility : IStorageFacility
    {
        private readonly string rootDirectory;

        public SimpleFileSystemRootStorageFacility(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
        }

        public Func<ILuceneStorageProvider> Create(string name)
        {
            return () => new LuceneSimpleFileSystemStorageProvider(Path.Combine(rootDirectory, name));
        }
    }




    //public class LuceneContextIndexBuilder : ILuceneIndexBuilder
    //{
    //    private readonly IServiceCollection services = new DefaultServiceCollection();

    //    public IServiceResolver ServiceResolver => new ServiceResolver(services);
    //    public ILuceneStorageProvider StorageProvider => new LuceneRamStorageProvider();
    //    public ILuceneIndexConfiguration Configuration => new LuceneIndexConfiguration();

    //    public virtual ILuceneJsonIndex Build()
    //    {
    //        return new LuceneJsonIndex(StorageProvider, Configuration, ServiceResolver);
    //    }
    //}



    public class JsonContextIndexConfiguration : JsonIndexConfiguration
    {
        public IJsonIndexConfiguration ContextConfiguration { get; }

        public JsonContextIndexConfiguration(IJsonIndexConfiguration contextConfiguration)
        {
            ContextConfiguration = contextConfiguration;
        }
    }

    public class PerIndexServiceCollection : DefaultServiceCollection
    {
        private readonly IServiceCollection contextCollection;

        public PerIndexServiceCollection(IServiceCollection contextCollection)
        {
            this.contextCollection = contextCollection;
        }

        public override bool Contains(Type type)
        {
            return base.Contains(type) || contextCollection.Contains(type);
        }

        public override bool TryObtainFactory(Type key, out Func<IServiceResolver, object> value)
        {
            return base.TryObtainFactory(key, out value) || contextCollection.TryObtainFactory(key, out value);
        }
    }


}