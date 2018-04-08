using System;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Storage;

namespace DotJEM.Json.Index
{
    public interface IJsonIndexConfigurator
    {
        IServiceCollection Services { get; }
        IJsonIndexConfigurator AddFacility(Func<ILuceneStorageProvider> facility);
    }

    public static class Extensions
    {
        public static IJsonIndexConfigurator UseSimpleFileStorage(this IJsonIndexConfigurator self, string path)
        {
            self.AddFacility(() => new LuceneSimpleFileSystemStorageProvider(path));
            return self;
        }

        public static IJsonIndexConfigurator UseMemoryStorage(this IJsonIndexConfigurator self)
        {
            self.AddFacility(() => new LuceneRamStorageProvider());
            return self;
        }
    }

    public interface IJsonIndexBuilder : IJsonIndexConfigurator
    {
        ILuceneJsonIndex Build();
    }

    public class JsonIndexBuilder : IJsonIndexBuilder, IJsonIndexConfigurator
    {
        private readonly string name;
        private readonly ILuceneIndexBuilderDefaults context;

        public IServiceCollection Services { get; }

        private Func<ILuceneStorageProvider> storageFacilty;

        public IServiceResolver ServiceResolver => new ServiceResolver(Services);
        public ILuceneStorageProvider StorageProvider => CreateStorage();

        public IJsonIndexConfiguration Configuration => new JsonIndexConfiguration();

        public JsonIndexBuilder(string name, ILuceneIndexBuilderDefaults context)
            : this(name, context, context.Services) { }

        public JsonIndexBuilder(string name, ILuceneIndexBuilderDefaults context, IServiceCollection services)
        {
            this.name = name;
            this.context = context;
            this.Services = services;
        }

        public virtual ILuceneJsonIndex Build()
        {
            IServiceResolver serviceResolver = new ServiceResolver(Services);
            return new LuceneJsonIndex(StorageProvider, Configuration, serviceResolver);
        }

        private ILuceneStorageProvider CreateStorage()
        {
            Func<ILuceneStorageProvider> facility = storageFacilty ?? context.Storage.Create(name);
            return facility.Invoke();
        }

        public IJsonIndexConfigurator AddFacility(Func<ILuceneStorageProvider> facility)
        {
            storageFacilty = facility;
            return this;
        }
    }

    public interface ILuceneIndexBuilderDefaults
    {
        IServiceCollection Services { get; set; }
        IStorageFacility Storage { get; set; }
    }

    public class LuceneIndexBuilderDefaults : ILuceneIndexBuilderDefaults
    {
        public IServiceCollection Services { get; set; } = DefaultServiceCollection.CreateDefault();

        public IStorageFacility Storage { get; set; }
    }

    public interface IStorageFacility
    {
        Func<ILuceneStorageProvider> Create(string name);
    }
}
