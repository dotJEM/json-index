using System.Collections.Generic;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Searching;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public interface IJsonIndex
    {
        IFieldCollection Fields { get; }
        IIndexStorage Storage { get; }
        IIndexConfiguration Configuration { get; }

        ILuceneWriter CreateWriter();
        ILuceneSearcher CreateSearcher();
    }

    public class LuceneJsonIndex : IJsonIndex
    {
        public IFieldCollection Fields { get; private set; }
        public IIndexStorage Storage { get; private set; }
        public IIndexConfiguration Configuration { get; private set; }

        public LuceneJsonIndex() 
            : this(new IndexConfiguration(), new LuceneMemmoryIndexStorage())
        {
        }

        public LuceneJsonIndex(string path)
            : this(new IndexConfiguration(), new LuceneMemmoryMappedFileIndexStorage(path))
        {
        }

        public LuceneJsonIndex(IIndexStorage storage)
            : this(new IndexConfiguration(), storage)
        {
        }

        public LuceneJsonIndex(IIndexConfiguration configuration)
            : this(configuration, new LuceneMemmoryIndexStorage())
        {
        }

        public LuceneJsonIndex(IIndexConfiguration configuration, IIndexStorage storage)
        {
            Fields = new FieldCollection();

            Storage = storage;
            Configuration = configuration;
        }

        public ILuceneWriter CreateWriter()
        {
            return new LuceneWriter(this);
        }

        public ILuceneSearcher CreateSearcher()
        {
            return new LuceneSearcher(this);
        }
    }

    public static class JsonIndexExtensions
    {
        public static ISearchResult Find(this IJsonIndex self, string query)
        {
            return self.CreateSearcher().Search(query);
        }

        public static IJsonIndex Write(this IJsonIndex self, JObject entity)
        {
            self.CreateWriter().Write(entity);
            return self;
        }

        public static IJsonIndex WriteAll(this IJsonIndex self, IEnumerable<JObject> entities)
        {
            self.CreateWriter().WriteAll(entities);
            return self;
        }

        public static IJsonIndex Delete(this IJsonIndex self, JObject entity)
        {
            self.CreateWriter().Delete(entity);
            return self;
        }
    }
}