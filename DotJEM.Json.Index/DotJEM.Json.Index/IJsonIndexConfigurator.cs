using System;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Storage;

namespace DotJEM.Json.Index
{
    public static class Extensions
    {
        public static ILuceneJsonIndexBuilder UseSimpleFileStorage(this ILuceneJsonIndexBuilder self, string path)
        {
            self.AddFacility(() => new LuceneSimpleFileSystemStorageFactory(path));
            return self;
        }

        public static ILuceneJsonIndexBuilder UseMemoryStorage(this ILuceneJsonIndexBuilder self)
        {
            self.AddFacility(() => new LuceneRamStorageFactory());
            return self;
        }
    }

    public interface ILuceneJsonIndexBuilder 
    {
        IServiceCollection Services { get; }
        ILuceneJsonIndex Build();
        ILuceneJsonIndexBuilder AddFacility(Func<ILuceneStorageFactory> facility);
    }

    public class LuceneJsonIndexBuilder : ILuceneJsonIndexBuilder
    {
        private readonly string name;
        private Func<ILuceneStorageFactory> storageFacilty;

        public IServiceCollection Services { get; }


        public IServiceResolver ServiceResolver => new ServiceResolver(Services);
        public ILuceneStorageFactory StorageFactory => CreateStorage();

        public IJsonIndexConfiguration Configuration => new JsonIndexConfiguration();

        public LuceneJsonIndexBuilder(string name)
        : this(name, ServiceCollection.CreateDefault())
        {
        }
        public LuceneJsonIndexBuilder(string name,  IServiceCollection services)
        {
            this.name = name;
            this.Services = services;
        }

        public virtual ILuceneJsonIndex Build()
        {
            return new LuceneJsonIndex(StorageFactory, Configuration, Services);
        }

        private ILuceneStorageFactory CreateStorage()
        {
            Func<ILuceneStorageFactory> facility = storageFacilty;
            return facility.Invoke();
        }

        public ILuceneJsonIndexBuilder AddFacility(Func<ILuceneStorageFactory> facility)
        {
            storageFacilty = facility;
            return this;
        }
    }

    public interface ILuceneStorageFactoryProvider
    {
        Func<ILuceneStorageFactory> Create(string name);
        ILuceneStorageFactory Get(string name);
    }
}
