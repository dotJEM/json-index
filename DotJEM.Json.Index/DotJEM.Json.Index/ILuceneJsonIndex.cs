using System;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Storage;

namespace DotJEM.Json.Index
{

    public interface ILuceneJsonIndexSearcherProvider
    {
        ILuceneJsonIndexSearcher CreateSearcher();
    }
    public interface ILuceneJsonIndex : ILuceneJsonIndexSearcherProvider
    {
        IEventInfoStream InfoStream { get; }
        IServiceResolver Services { get; }
        IJsonIndexStorage Storage { get; }
        IJsonIndexConfiguration Configuration { get; }
        IIndexWriterManager WriterManager { get; }
        IIndexSearcherManager SearcherManager { get; }
        IJsonIndexWriter CreateWriter();

        void Close();
    }

    public class LuceneJsonIndex : ILuceneJsonIndex
    {
        public IEventInfoStream InfoStream { get; } = EventInfoStream.Default.Bind<LuceneJsonIndex>();
        public IJsonIndexStorage Storage { get; }
        public IJsonIndexConfiguration Configuration { get; }
        public IServiceResolver Services { get; }

        public IIndexWriterManager WriterManager => Storage.WriterManager;
        public IIndexSearcherManager SearcherManager => Storage.SearcherManager;

        public LuceneJsonIndex()
            : this(new LuceneRamStorageFactory(), new JsonIndexConfiguration(), ServiceCollection.CreateDefault())
        {
        }

        public LuceneJsonIndex(string path)
            : this(new LuceneSimpleFileSystemStorageFactory(path), new JsonIndexConfiguration(), ServiceCollection.CreateDefault())
        {
        }

        public LuceneJsonIndex(ILuceneStorageFactory storage, IJsonIndexConfiguration configuration, IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            Configuration = configuration.AsReadOnly() ?? throw new ArgumentNullException(nameof(configuration));

            //TODO: Ehhh... could we perhaps provide this differently, e.g. a Generic Provider pattern that would allow us to inject'
            //      both the LuceneVersion and the Index.
            services.Use<ILuceneJsonIndex>(p => this);
            Services = new ServiceResolver(services);

            Storage = storage.Create(this, configuration.Version);
        }

        public LuceneJsonIndex(ILuceneStorageFactory storage, IJsonIndexConfiguration configuration, IServiceResolver services)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Services = services ?? throw new ArgumentNullException(nameof(services));

            Storage = storage.Create(this, configuration.Version);
        }

        public ILuceneJsonIndexSearcher CreateSearcher()
        {
            return new LuceneJsonIndexSearcher(this);
        }

        public IJsonIndexWriter CreateWriter()
        {
            return new JsonIndexWriter(this, Services.Resolve<ILuceneDocumentFactory>(), WriterManager);
        }

        public void Close()
        {
            WriterManager.Close();
            Storage.Close();
        }
    }

  
}