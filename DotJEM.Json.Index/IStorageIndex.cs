using System.Collections.Generic;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Searching;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index
{
    public interface IStorageIndex
    {
        IFieldCollection Fields { get; }
        IIndexStorage Storage { get; }
        IIndexConfiguration Configuration { get; }

        ILuceneWriter CreateWriter();
        ILuceneSearcher CreateSearcher();
    }

    public class LuceneStorageIndex : IStorageIndex
    {
        public IFieldCollection Fields { get; private set; }
        public IIndexStorage Storage { get; private set; }
        public IIndexConfiguration Configuration { get; private set; }

        public LuceneStorageIndex() 
            : this(new IndexConfiguration(), new LuceneMemmoryIndexStorage())
        {
        }

        public LuceneStorageIndex(string path)
            : this(new IndexConfiguration(), new LuceneMemmoryMappedFileIndexStorage(path))
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

        public LuceneStorageIndex(IIndexConfiguration configuration, IIndexStorage storage)
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
        public static ISearchResult Find(this IStorageIndex self, string query)
        {
            return self.CreateSearcher().Search(query);
        }

        public static ISearchResult Find(this IStorageIndex self, JObject query)
        {
            return self.CreateSearcher().Search(query);
        }

        public static ISearchResult Find(this IStorageIndex self, dynamic query)
        {
            return self.CreateSearcher().Search(JObject.FromObject((object)query));
        }

        public static IStorageIndex Write(this IStorageIndex self, JObject entity)
        {
            self.CreateWriter().Write(entity);
            return self;
        }

        public static IStorageIndex WriteAll(this IStorageIndex self, IEnumerable<JObject> entities)
        {
            self.CreateWriter().WriteAll(entities);
            return self;
        }

        public static IStorageIndex Delete(this IStorageIndex self, JObject entity)
        {
            self.CreateWriter().Delete(entity);
            return self;
        }

        public static IEnumerable<string> Terms(this IStorageIndex self, string field)
        {
            return self.CreateSearcher().Terms(field);
        }

    }
}