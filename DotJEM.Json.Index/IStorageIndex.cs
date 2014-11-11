using System;
using System.Collections.Generic;
using System.Configuration;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index
{
    public interface IStorageIndex
    {
        ISchemaCollection Schemas { get; }
        IIndexStorage Storage { get; }
        IIndexConfiguration Configuration { get; }

        ILuceneWriter Writer { get; }
        ILuceneSearcher Searcher { get; }

        ISearchResult Search(string query);
        ISearchResult Search(JObject query);
        ISearchResult Search(Query query);

        IStorageIndex Write(JObject entity);
        IStorageIndex WriteAll(IEnumerable<JObject> entities);
        IStorageIndex Delete(JObject entity);
        
        IEnumerable<string> Terms(string field);
    }

    public class LuceneStorageIndex : IStorageIndex
    {
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
            : this(new IndexConfiguration(), new LuceneFileIndexStorage(path))
        {
        }

        public LuceneStorageIndex(IIndexStorage storage)
            : this(new IndexConfiguration(), storage)
        {
        }

        public LuceneStorageIndex(IIndexConfiguration configuration)
            : this(configuration, new LuceneMemmoryIndexStorage())
        {
        }
        #endregion

        public LuceneStorageIndex(IIndexConfiguration configuration, IIndexStorage storage)
        {
            Schemas = new SchemaCollection();
            Analyzer = new StandardAnalyzer(Version.LUCENE_30);

            Storage = storage;
            Configuration = configuration;

            writer = new Lazy<ILuceneWriter>(() => new LuceneWriter(this));
            searcher = new Lazy<ILuceneSearcher>(() => new LuceneSearcher(this));
        }

        //TODO: Do we need to be able to release these?
        private readonly Lazy<ILuceneWriter> writer;
        private readonly Lazy<ILuceneSearcher> searcher;

        public ILuceneWriter Writer { get { return writer.Value; } }
        public ILuceneSearcher Searcher { get { return searcher.Value; } }

        public ISearchResult Search(string query)
        {
            return Searcher.Search(query);
        }

        public ISearchResult Search(JObject query)
        {
            return Searcher.Search(query);
        }

        public ISearchResult Search(Query query)
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

        public IEnumerable<string> Terms(string field)
        {
            return Searcher.Terms(field);
        }
    }
}