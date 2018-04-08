using System;
using DotJEM.Index.Searching;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Storage;
using Lucene.Net.Search;

namespace DotJEM.Json.Index
{
    public interface ILuceneJsonIndex
    {
        IServiceResolver Services { get; }
        IJsonIndexStorage Storage { get; }
        IJsonIndexConfiguration Configuration { get; }

        IJsonIndexWriter CreateWriter();
        IJsonIndexSearcher CreateSearcher();
    }
    public class LuceneJsonIndex : ILuceneJsonIndex
    {
        public IJsonIndexStorage Storage { get; }
        public IJsonIndexConfiguration Configuration { get; }
        public IServiceResolver Services { get; }

        public LuceneJsonIndex()
            : this(StorageProviders.RamStorage(), new JsonIndexConfiguration(), new DefaultServiceCollection())
        {
        }

        public LuceneJsonIndex(string path)
            : this(StorageProviders.SimpleFileStorage(path), new JsonIndexConfiguration(), new DefaultServiceCollection())
        {
        }

        public LuceneJsonIndex(ILuceneStorageProvider storage, IJsonIndexConfiguration configuration, IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            //TODO: Ehhh... could we perhaps provide this differently, e.g. a Generic Provider pattern that would allow us to inject both the LuceneVersion and the Index.
            services.Use<ILuceneJsonIndex>(p => this);
            Services = new ServiceResolver(services);

            Storage = storage.Create(this, configuration.Version);
        }

        public LuceneJsonIndex(ILuceneStorageProvider storage, IJsonIndexConfiguration configuration, IServiceResolver services)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Services = services ?? throw new ArgumentNullException(nameof(services));

            Storage = storage.Create(this, configuration.Version);
            //Configuration.Services.Use<ILuceneJsonIndex>(this);
        }

        public IJsonIndexSearcher CreateSearcher()
        {
            return new JsonIndexSearcher(this, Storage.SearcherManager);
        }


        public IJsonIndexWriter CreateWriter()
        {
            return new JsonIndexWriter(this, Services.Resolve<ILuceneDocumentFactory>(), Storage.WriterManager);
        }

    }

    public static class LuceneIndexExtension
    {
        public static Search Search(this ILuceneJsonIndex self, Query query)
        {
            using (var searcher = self.CreateSearcher())
            {
                return searcher.Search(query);
            }
        }
    }
}