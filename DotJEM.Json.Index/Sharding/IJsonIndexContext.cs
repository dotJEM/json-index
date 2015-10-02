using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding
{
    public class Usage
    {
        public void Test()
        {
            IJsonIndexContext context = new LuceneJsonIndexContext();

            JsonIndexConfiguration configuration = new JsonIndexConfiguration();

            configuration.Storage<MemmoryIndexStorage>();
            configuration.Analyzer<KeywordAnalyzer>();

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

        public IJsonIndexContextConfiguration Configuration { get; } = new LuceneJsonIndexContextConfiguration();


        public IJsonIndex Open(string name)
        {
            return indices.GetOrAdd(name, key => new JsonIndex());
        }
    }

    public interface IJsonIndexContextConfiguration
    {
        IJsonIndexConfiguration this[string name] { get; set; }
    }

    public class LuceneJsonIndexContextConfiguration : IJsonIndexContextConfiguration
    {
        private readonly ConcurrentDictionary<string, IJsonIndexConfiguration> configurations 
            = new ConcurrentDictionary<string, IJsonIndexConfiguration>();

        public IJsonIndexConfiguration this[string name]
        {
            set { configurations[name] = value; }
            get
            {
                return configurations.GetOrAdd(name, key => new JsonIndexConfiguration());
            }
        }
    }

    public interface IJsonIndexConfiguration
    {
        IJsonIndexConfiguration Storage<TStorageImpl>();
        IJsonIndexConfiguration Analyzer<TAnalyzerImpl>();

    }

    public class JsonIndexConfiguration : IJsonIndexConfiguration
    {
        public JsonIndexConfiguration()
        {
        }

        public IJsonIndexConfiguration Storage<TStorageImpl>()
        {
            throw new NotImplementedException();
        }

        public IJsonIndexConfiguration Analyzer<TAnalyzerImpl>()
        {
            throw new NotImplementedException();
        }
    }



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

        //ISearchResult Search(string query);
        //ISearchResult Search(string queryFormat, params object[] args);
        //ISearchResult Search(Query query);
        //ISearchResult Search(object query);
        //ISearchResult Search(JObject query);

        //IEnumerable<string> Terms(string field);

        //void Close();


        IJsonIndex Write(IEnumerable<JObject> entities);
        
        ISearchResult Search(string query, params object[] args);
    }

    public class JsonIndex : IJsonIndex
    {
        public IJsonIndex Write(IEnumerable<JObject> entities)
        {
            IndexWriter writer = new IndexWriter(new RAMDirectory(), new KeywordAnalyzer(), new KeepOnlyLastCommitDeletionPolicy(), IndexWriter.MaxFieldLength.UNLIMITED);

            //TODO: Select shards.

            return this;
        }


        public ISearchResult Search(string query, params object[] args)
        {
            //var searcher = new ParallelMultiSearcher(new IndexSearcher(), new IndexSearcher());



            return null;
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


    }

    public class JsonIndexShard : IJsonIndexShard { }

    public interface IJsonIndexShardCollection { }

    public class JsonIndexShardCollection : IJsonIndexShardCollection
    {

    }

    public interface IIndexStorage
    {
        IndexReader Reader { get; }
        IndexWriter Writer { get; }
    }

    public class MemmoryIndexStorage : IIndexStorage
    {
        private Directory directory;

        public IndexReader Reader { get; }
        public IndexWriter Writer { get; }

        public MemmoryIndexStorage(Directory directory)
        {
            this.directory = directory;

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
