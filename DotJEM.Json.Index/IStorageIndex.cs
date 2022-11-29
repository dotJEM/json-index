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
        IServiceCollection Services { get; }

        ILuceneWriter Writer { get; }
        ILuceneSearcher Searcher { get; }
        
        IStorageIndex Write(JObject entity);
        IStorageIndex WriteAll(IEnumerable<JObject> entities);
        IStorageIndex Delete(JObject entity);
        IStorageIndex DeleteAll(IEnumerable<JObject> entities);

        IStorageIndex Optimize();
        IStorageIndex Commit();

        ISearchResult Search(string query);
        ISearchResult Search(string queryFormat, params object[] args);
        ISearchResult Search(Query query);
        ISearchResult Search(object query);
        ISearchResult Search(JObject query);

        IEnumerable<string> Terms(string field);

        void Close();
        void Flush();
    }

    public class LuceneStorageIndex : IStorageIndex
    {
        public Version Version { get; }
        public Analyzer Analyzer { get; }

        public ISchemaCollection Schemas { get; }
        public IIndexStorage Storage { get; }
        public IIndexConfiguration Configuration { get; }
        public IServiceCollection Services { get; }

        public LuceneStorageIndex(Analyzer analyzer)
            : this(new IndexConfiguration(), new LuceneMemmoryIndexStorage(analyzer))
        {
        }

        public LuceneStorageIndex(string path, Analyzer analyzer = null)
            : this(new IndexConfiguration(), new LuceneFileIndexStorage(path, analyzer))
        {
        }

        public LuceneStorageIndex(IIndexStorage storage)
            : this(new IndexConfiguration(), storage)
        {
        }

        public LuceneStorageIndex(IIndexConfiguration configuration = null, IIndexStorage storage= null, IServiceCollectionFactory factory = null)
        {
            //TODO: Version should come from outside
            Version = Version.LUCENE_30;
            Storage = storage ?? new LuceneMemmoryIndexStorage();
            Analyzer = Storage.Analyzer;

            Configuration = configuration ?? new IndexConfiguration();

            factory ??= new DefaultServiceFactory();
            Services = factory.Create(this);
            
            Schemas = Services.SchemaCollection;
            writer = new Lazy<ILuceneWriter>(() => new LuceneWriter(this, Services.DocumentFactory));
            searcher = new Lazy<ILuceneSearcher>(() => Services.Searcher);
        }

        //TODO: Do we need to be able to release these?
        private readonly Lazy<ILuceneWriter> writer;
        private readonly Lazy<ILuceneSearcher> searcher;

        public void Close()
        {
            Storage.Close();
        }

        public void Flush()
        {
            Storage.Flush();
        }

        public ILuceneWriter Writer => writer.Value;
        public ILuceneSearcher Searcher => searcher.Value;

        public ISearchResult Search(string query)
        {
            return Searcher.Search(query);
        }

        public ISearchResult Search(string queryFormat, params object[] args)
        {
            return Searcher.Search(string.Format(queryFormat, args));
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

        public IStorageIndex Optimize()
        {
            Writer.Optimize();
            return this;
        }
        public IStorageIndex Commit()
        {
            Writer.Commit();
            return this;
        }

        public IEnumerable<string> Terms(string field) => Searcher.Terms(field);
    }
}