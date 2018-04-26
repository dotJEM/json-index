using System;
using System.Collections.Concurrent;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Contexts.Searching;
using DotJEM.Json.Index.Contexts.Storage;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Search;

namespace DotJEM.Json.Index.Contexts
{
    public interface ILuceneIndexContext : ILuceneJsonIndexSearcherProvider
    {
        IServiceResolver Services { get; }

        ILuceneJsonIndex Open(string name);
    }

    public class LuceneIndexContext : ILuceneIndexContext
    {
        private readonly ILuceneJsonIndexFactory factory;
        private readonly ConcurrentDictionary<string, ILuceneJsonIndex> indices = new ConcurrentDictionary<string, ILuceneJsonIndex>();

        public IServiceResolver Services { get; }

        public LuceneIndexContext(IServiceCollection services = null)
            : this(new LuceneIndexContextBuilder(), services) { }

        public LuceneIndexContext(string path, IServiceCollection services = null)
            : this(new LuceneIndexContextBuilder(path), services) { }

        public LuceneIndexContext(ILuceneJsonIndexFactory factory, IServiceCollection services = null)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.Services = new ServiceResolver(services ?? ServiceCollection.CreateDefault());
        }
        public LuceneIndexContext(ILuceneJsonIndexFactory factory, IServiceResolver resolver)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.Services = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public ILuceneJsonIndex Open(string name)
        {
            return indices.GetOrAdd(name, factory.Create);
        }

        public ILuceneJsonIndexSearcher CreateSearcher()
        {
            return new LuceneJsonMultiIndexSearcher(indices.Values);
        }
    }
    
    public interface ILuceneIndexContextBuilder
    {
        IServiceCollection Services { get; }
        ILuceneIndexContextBuilder Configure(string name, Action<ILuceneJsonIndexBuilder> config);
        ILuceneIndexContext Build();
    }

    public interface ILuceneJsonIndexFactory
    {
        ILuceneJsonIndex Create(string name);
    }

    public class LuceneIndexContextBuilder : ILuceneIndexContextBuilder, ILuceneJsonIndexFactory
    {
        private readonly ConcurrentDictionary<string, ILuceneJsonIndexBuilder> builders = new ConcurrentDictionary<string, ILuceneJsonIndexBuilder>();

        public IServiceCollection Services { get; }

        private readonly ILuceneStorageFactoryProvider storage;
        
        public LuceneIndexContextBuilder()
            : this(new RamStorageFacility(), ServiceCollection.CreateDefault()) { }

        public LuceneIndexContextBuilder(string path)
            : this(new SimpleFileSystemRootStorageFacility(path), ServiceCollection.CreateDefault()) { }

        public LuceneIndexContextBuilder(ILuceneStorageFactoryProvider storage, IServiceCollection services)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public ILuceneIndexContext Build()
        {
            return new LuceneIndexContext(this, Services);
        }

        public ILuceneIndexContextBuilder Configure(string name, Action<ILuceneJsonIndexBuilder> config)
        {
            return Configure(name, builder =>
            {
                config(builder);
                return builder;
            });
        }

        private ILuceneIndexContextBuilder Configure(string name, Func<ILuceneJsonIndexBuilder, ILuceneJsonIndexBuilder> config)
        {
            ILuceneJsonIndexBuilder builder = config(new ContextedJsonIndexBuilder(name, Services).AddFacility(storage.Create(name)));
            builders.AddOrUpdate(name, builder, (s, a) => builder);
            return this;
        }

        ILuceneJsonIndex ILuceneJsonIndexFactory.Create(string name)
        {
            if (builders.TryGetValue(name, out ILuceneJsonIndexBuilder builder))
                return builder.Build();

            builder = new ContextedJsonIndexBuilder(name, Services).AddFacility(storage.Create(name));
            return builder.Build();
        }
    }



    //public class LuceneContextIndexBuilder : ILuceneIndexBuilder
    //{
    //    private readonly IServiceCollection services = new DefaultServiceCollection();

    //    public IServiceResolver ServiceResolver => new ServiceResolver(services);
    //    public ILuceneStorageFactory StorageFactory => new LuceneRamStorageFactory();
    //    public ILuceneIndexConfiguration Configuration => new LuceneIndexConfiguration();

    //    public virtual ILuceneJsonIndex Build()
    //    {
    //        return new LuceneJsonIndex(StorageFactory, Configuration, ServiceResolver);
    //    }
    //}
}