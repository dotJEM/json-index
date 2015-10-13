using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Sharding.Configuration;
using DotJEM.Json.Index.Sharding.Documents;
using DotJEM.Json.Index.Sharding.Infos;
using DotJEM.Json.Index.Sharding.Resolvers;
using DotJEM.Json.Index.Sharding.Schemas;
using DotJEM.Json.Index.Sharding.Storage;
using DotJEM.Json.Index.Sharding.Storage.Writers;
using DotJEM.Json.Index.Sharding.Visitors;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Newtonsoft.Json.Linq;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index.Sharding
{
    public class Usage
    {
        public void Test()
        {
            IJsonIndexContext context = new LuceneJsonIndexContext();

            DefaultJsonIndexConfiguration configuration = new DefaultJsonIndexConfiguration();

            configuration.Shards["person"] = new DefaultJsonIndexShardConfiguration();
            //configuration.Shards["person.*"] = new DefaultJsonIndexShardConfiguration();
            //configuration.Shards["person.*"] = new DefaultJsonIndexShardConfiguration();


            context.Configuration["content"] = configuration;



        }
    }


    public interface IJsonIndexContext
    {
        IJsonIndexContextConfiguration Configuration { get; }

        IJsonIndex Open(string name);
    }

    public class LuceneJsonIndexContext : IJsonIndexContext
    {
        private readonly ConcurrentDictionary<string, IJsonIndex> indices = new ConcurrentDictionary<string, IJsonIndex>();

        public IJsonIndexContextConfiguration Configuration { get; }

        public LuceneJsonIndexContext() : this(new LuceneJsonIndexContextConfiguration())
        {
        }

        public LuceneJsonIndexContext(IJsonIndexContextConfiguration configuration)
        {
            Configuration = configuration;
        }


        public IJsonIndex Open(string name)
        {
            return indices.GetOrAdd(name, key => new JsonIndex(Configuration[name]));
        }
    }

    //https://github.com/NielsKuhnel/NrtManager
    public interface IJsonIndex
    {
        //Version Version { get; }
        //Analyzer Analyzer { get; }

        //ISchemaCollection Schemas { get; }
        //IIndexStorage Storage { get; }
        //IIndexConfiguration Configuration { get; }

        //ILuceneWriter Writer { get; }
        //ILuceneSearcher Searcher { get; }

        //IStorageIndex Write(JObject entity);
        //IStorageIndex WriteAll(IEnumerable<JObject> entities);
        //IStorageIndex Delete(JObject entity);
        //IStorageIndex DeleteAll(IEnumerable<JObject> entities);

        //IStorageIndex Optimize();

        IJsonIndex Write(IEnumerable<JObject> entities);
        IJsonIndex Delete(IEnumerable<JObject> entities);

        //ISearchResult Search(object query);
        DummySearchResult Search(string query, params object[] args);
        DummySearchResult Search(Query query);

        //IEnumerable<string> Terms(string field);

        //void Close();
    }

    public class JsonIndex : IJsonIndex
    {
        private readonly IJsonIndexConfiguration configuration;
        //TODO: (jmd 2015-10-12) Inject but as trancient?
        private readonly IJsonIndexShardsManager shardManager;
        private readonly IMetaFieldResolver resolver;

        public JsonIndex(IJsonIndexConfiguration configuration)
        {
            shardManager = new JsonIndexShardsManager(configuration);
            this.configuration = configuration;
            this.resolver = configuration.MetaFieldResolver;
        }

        public IJsonIndex Write(IEnumerable<JObject> entities)
        {

            IEnumerable<ShardChanges> changes = from json in entities
                                                group json by resolver.Shard(json) into updatesInShard
                                                select new ShardChanges(updatesInShard.Key, updatesInShard);

            foreach (ShardChanges update in changes)
            {
                shardManager[update.Shard].Write(update.Changes);
            }
            return this;
        }

        public IJsonIndex Delete(IEnumerable<JObject> entities)
        {
            IEnumerable<ShardChanges> changes = from json in entities
                                                group json by resolver.Shard(json) into deletesInShard
                                                select new ShardChanges(deletesInShard.Key, deletesInShard);

            foreach (ShardChanges update in changes)
            {
                shardManager[update.Shard].Delete(update.Changes);
            }
            return this;
        }

        public DummySearchResult Search(string query, params object[] args)
        {
            //TODO: Preanalyze query and determine which shards to go to.

            //var searcher = new ParallelMultiSearcher(new IndexSearcher(), new IndexSearcher());

            Lucene.Net.QueryParsers.MultiFieldQueryParser parser = new Lucene.Net.QueryParsers.MultiFieldQueryParser(Version.LUCENE_30, "contentType".Split(';'), new KeywordAnalyzer());
            //MultiFieldQueryParser parser = new MultiFieldQueryParser(index, query);
            parser.AllowLeadingWildcard = true;
            //parser.DefaultOperator = QueryParser.Operator.AND;
            //return DebugLog(parser.Parse(query));
            if(args.Any())
                query = string.Format(query, args);

            Query queryObj = parser.Parse(query);
            return Search(queryObj);
        }

        public DummySearchResult Search(Query query)
        {
            var searchers = shardManager.Searchers();
            foreach (IndexSearcher sear in searchers)
            {
                var results = 
                sear.Search(query, 10);
            }


            var searcher = new MultiSearcher(searchers);
            var result = searcher.Search(query, 1000);
            return new DummySearchResult(result);
        }
    }

    public class DummySearchResult 
    {
        public TopDocs Result { get; }

        public DummySearchResult(TopDocs result)
        {
            this.Result = result;
        }
    }

    public interface IJsonIndexShard
    {
        //        public Version Version { get; private set; }
        //public Analyzer Analyzer { get; private set; }

        //public ISchemaCollection Schemas { get; private set; }
        //public IIndexStorage Storage { get; private set; }
        //public IIndexConfiguration Configuration { get; private set; }

        //#region Constructor Overloads
        //public LuceneStorageIndex()
        //    : this(new IndexConfiguration(), new LuceneMemmoryIndexStorage())
        //{
        //}

        //public LuceneStorageIndex(string path)
        //    : this(new IndexConfiguration(), new LuceneCachedMemmoryIndexStorage(path))
        //{
        //}

        //public LuceneStorageIndex(IIndexStorage storage)
        //    : this(new IndexConfiguration(), storage)
        //{
        //}

        //public LuceneStorageIndex(IIndexStorage storage, Analyzer analyzer)
        //    : this(new IndexConfiguration(), storage, analyzer)
        //{
        //}

        //public LuceneStorageIndex(IIndexConfiguration configuration)
        //    : this(configuration, new LuceneMemmoryIndexStorage(), new DotJemAnalyzer(Version.LUCENE_30, configuration))
        //{
        //}

        //public LuceneStorageIndex(IIndexConfiguration configuration, IIndexStorage storage)
        //    : this(configuration, storage, new DotJemAnalyzer(Version.LUCENE_30, configuration))
        //{
        //}
        //#endregion

        //public LuceneStorageIndex(IIndexConfiguration configuration, IIndexStorage storage, Analyzer analyzer)
        //{
        //    if (configuration == null) throw new ArgumentNullException("configuration");
        //    if (storage == null) throw new ArgumentNullException("storage");
        //    if (analyzer == null) throw new ArgumentNullException("analyzer");

        //    Schemas = new SchemaCollection();
        //    Analyzer = analyzer;
        //    Version = Version.LUCENE_30;

        //    Storage = storage;
        //    Configuration = configuration;

        //    writer = new Lazy<ILuceneWriter>(() => new LuceneWriter(this));
        //    searcher = new Lazy<ILuceneSearcher>(() => new LuceneSearcher(this));
        //}

        ////TODO: Do we need to be able to release these?
        //private readonly Lazy<ILuceneWriter> writer;
        //private readonly Lazy<ILuceneSearcher> searcher;

        //public void Close()
        //{
        //    Storage.Close();
        //}

        //public ILuceneWriter Writer { get { return writer.Value; } }
        //public ILuceneSearcher Searcher { get { return searcher.Value; } }

        IJsonIndexWriter AquireWriter();

        void Write(IEnumerable<JObject> entities);
        void Delete(IEnumerable<JObject> entities);
        Searchable AquireSearcher();
    }

    public class JsonIndexShard : IJsonIndexShard
    {
        private readonly IJsonIndexStorage storage;
        private readonly ILuceneDocumentFactory factory;
        private readonly IMetaFieldResolver resolver;
        private readonly IJsonIndexShardConfiguration configuration;
        

        public JsonIndexShard(IJsonIndexShardConfiguration configuration, ILuceneDocumentFactory factory, IMetaFieldResolver resolver)
        {
            this.configuration = configuration;
            this.storage = new MemmoryJsonIndexStorage();
            this.factory = factory;
            this.resolver = resolver;
        }

        public IJsonIndexWriter AquireWriter()
        {
            return storage.Writer.Aquire();
        }

        public void Write(IEnumerable<JObject> entities)
        {
            var writer = AquireWriter();
            foreach (JObject entity in entities)
            {
                Term id = resolver.Identity(entity);
                Document doc = factory.Create(entity);

                writer.UpdateDocument(id, doc);
            }
            writer.Commit();
        }

        public void Delete(IEnumerable<JObject> entities)
        {
        }

        public Searchable AquireSearcher()
        {
            return new IndexSearcher(storage.Directory);
        }
    }


    public interface IJsonIndexShardsManager
    {
        IJsonIndexShard this[ShardInfo key] { get; }
        Searchable[] Searchers();
    }

    public class JsonIndexShardsManager : IJsonIndexShardsManager
    {
        private readonly IJsonIndexConfiguration configuration;
        private readonly ConcurrentDictionary<string, IJsonIndexShard> shards = new ConcurrentDictionary<string, IJsonIndexShard>();

        public JsonIndexShardsManager(IJsonIndexConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IJsonIndexShard this[ShardInfo key]
        {
            get
            {
                Console.WriteLine("Aquireing shard: " + key);
                string name = key.Name;
                IJsonIndexShardConfiguration config = configuration.Shards[name];
                ILuceneDocumentFactory factory = configuration.DocumentFactory;
                IMetaFieldResolver resolver = configuration.MetaFieldResolver;

                return shards.GetOrAdd(name, k => new JsonIndexShard(config, factory, resolver));
            }
        }

        public Searchable[] Searchers()
        {
            return shards.Values.Select(shard => shard.AquireSearcher()).ToArray();
        }
    }


    //public interface IIndexStorage
    //{
    //    IndexReader OpenReader();
    //    IndexWriter GetWriter(Analyzer analyzer);
    //    bool Exists { get; }
    //    void Close();
    //}

    //public abstract class AbstractLuceneIndexStorage : IIndexStorage
    //{
    //    protected Directory Directory { get; private set; }
    //    public virtual bool Exists { get { return Directory.ListAll().Any(); } }

    //    private IndexWriter writer;

    //    protected AbstractLuceneIndexStorage(Directory directory)
    //    {
    //        Directory = directory;
    //    }

    //    public IndexWriter GetWriter(Analyzer analyzer)
    //    {
    //        //TODO: The storage should define the analyzer, not the writer.
    //        return writer ?? (writer = new IndexWriter(Directory, analyzer, !Exists, IndexWriter.MaxFieldLength.UNLIMITED));


    //    }

    //    public IndexReader OpenReader()
    //    {
    //        return Exists ? IndexReader.Open(Directory, true) : null;
    //    }

    //    public void Close()
    //    {
    //        if (writer != null)
    //        {
    //            writer.Dispose();
    //            writer = null;
    //        }
    //    }
    //}

    //public class LuceneMemmoryIndexStorage : AbstractLuceneIndexStorage
    //{
    //    public LuceneMemmoryIndexStorage()
    //        : base(new RAMDirectory())
    //    {
    //    }
    //}

    //public class LuceneFileIndexStorage : AbstractLuceneIndexStorage
    //{
    //    public LuceneFileIndexStorage(string path)
    //        : base(FSDirectory.Open(path))
    //    {
    //        //Note: Ensure cacheDirectory.
    //        System.IO.Directory.CreateDirectory(path);
    //    }
    //}

    //public class LuceneCachedMemmoryIndexStorage : AbstractLuceneIndexStorage
    //{
    //    public LuceneCachedMemmoryIndexStorage(string path)
    //        : base(new MemoryCachedDirective(path))
    //    {
    //        //Note: Ensure cacheDirectory.
    //        System.IO.Directory.CreateDirectory(path);
    //    }
    //}

    //public class LuceneMemmoryMappedFileIndexStorage : AbstractLuceneIndexStorage
    //{
    //    public LuceneMemmoryMappedFileIndexStorage(string path)
    //        : base(new MMapDirectory(new DirectoryInfo(path)))
    //    {
    //        //Note: Ensure cacheDirectory.
    //        System.IO.Directory.CreateDirectory(path);
    //    }
    //}
}
