using System;
using System.Collections.Generic;
using DotJEM.Json.Index.Analyzation;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index
{
    //TODO: -> IIndexContext to align to IStorageContext, then allow for multiple indexes.
    public interface IStorageIndex
    {
        Version Version { get; }
        Analyzer Analyzer { get; }

        ISchemaCollection Schemas { get; }
        IIndexStorage Storage { get; }
        IIndexConfiguration Configuration { get; }

        ILuceneWriter Writer { get; }
        ILuceneSearcher Searcher { get; }
        
        IStorageIndex Write(JObject entity);
        IStorageIndex WriteAll(IEnumerable<JObject> entities);
        IStorageIndex Delete(JObject entity);
        IStorageIndex DeleteAll(IEnumerable<JObject> entities);

        ISearchResult Search(string query);
        ISearchResult Search(Query query);
        ISearchResult Search(object query);
        ISearchResult Search(JObject query);

        IEnumerable<string> Terms(string field);

        void Close();
    }

    public class LuceneStorageIndex : IStorageIndex
    {
        public Version Version { get; private set; }
        public Analyzer Analyzer { get; private set; }

        public ISchemaCollection Schemas { get; private set; }
        public IIndexStorage Storage { get; private set; }
        public IIndexConfiguration Configuration { get; private set; }

        #region Constructor Overloads
        public LuceneStorageIndex()
            : this(new IndexConfiguration(), new LuceneMemmoryIndexStorage())
        {
        }

        public LuceneStorageIndex(string path)
            : this(new IndexConfiguration(), new LuceneCachedMemmoryIndexStorage(path))
        {
        }

        public LuceneStorageIndex(IIndexStorage storage)
            : this(new IndexConfiguration(), storage)
        {
        }

        public LuceneStorageIndex(IIndexStorage storage, Analyzer analyzer)
            : this(new IndexConfiguration(), storage, analyzer)
        {
        }

        public LuceneStorageIndex(IIndexConfiguration configuration)
            : this(configuration, new LuceneMemmoryIndexStorage(), new DotJemAnalyzer(Version.LUCENE_30, configuration))
        {
        }

        public LuceneStorageIndex(IIndexConfiguration configuration, IIndexStorage storage)
            : this(configuration, storage, new DotJemAnalyzer(Version.LUCENE_30, configuration))
        {
        }
        #endregion

        public LuceneStorageIndex(IIndexConfiguration configuration, IIndexStorage storage, Analyzer analyzer)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (storage == null) throw new ArgumentNullException("storage");
            if (analyzer == null) throw new ArgumentNullException("analyzer");

            Schemas = new SchemaCollection();
            Analyzer = analyzer;
            Version = Version.LUCENE_30;

            Storage = storage;
            Configuration = configuration;

            writer = new Lazy<ILuceneWriter>(() => new LuceneWriter(this));
            searcher = new Lazy<ILuceneSearcher>(() => new LuceneSearcher(this));
        }

        //TODO: Do we need to be able to release these?
        private readonly Lazy<ILuceneWriter> writer;
        private readonly Lazy<ILuceneSearcher> searcher;

        public void Close()
        {
            Storage.Close();
        }

        public ILuceneWriter Writer { get { return writer.Value; } }
        public ILuceneSearcher Searcher { get { return searcher.Value; } }

        #region Short hand helpers

        public ISearchResult Search(string query)
        {
            return Searcher.Search(query);
        }

        public ISearchResult Search(Query query)
        {
            return Searcher.Search(query);
        }
        
        public ISearchResult Search(object query)
        {
            string stringQuery = query as string;
            if (stringQuery != null)
                return Search(stringQuery);

            Query queryObject = query as Query;
            if (queryObject != null)
                return Search(queryObject);

            return Searcher.Search(query as JObject ?? JObject.FromObject(query));
        }

        public ISearchResult Search(JObject query)
        {
            return Searcher.Search(query);
        }

        public IStorageIndex Write(JObject entity)
        {
            Writer.Write(entity);
            return this;
        }

        public IStorageIndex WriteAll(IEnumerable<JObject> entities)
        {
            Writer.WriteAll(entities);
            return this;
        }

        public IStorageIndex Delete(JObject entity)
        {
            Writer.Delete(entity);
            return this;
        }

        public IStorageIndex DeleteAll(IEnumerable<JObject> entities)
        {
            Writer.DeleteAll(entities);
            return this;
        }

        public IEnumerable<string> Terms(string field)
        {
            return Searcher.Terms(field);
        }

        #endregion
    }
}