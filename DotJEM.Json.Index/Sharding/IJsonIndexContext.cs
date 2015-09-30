using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Newtonsoft.Json.Linq;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index
{
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


}
