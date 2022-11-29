using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;

namespace DotJEM.Json.Index.Configuration
{
    public interface IServiceCollection
    {
        ISchemaCollection SchemaCollection { get; }
        IDocumentFactory DocumentFactory { get; }
        ILuceneSearcher Searcher { get; }

    }

    public interface IServiceCollectionFactory
    {
        IServiceCollection Create(IStorageIndex index);
    }

    public class DefaultServiceFactory : IServiceCollectionFactory, IServiceCollection
    {
        public ISchemaCollection SchemaCollection { get; private set; }
        public IDocumentFactory DocumentFactory { get; private set; }
        public ILuceneSearcher Searcher { get; private set; }

        public IServiceCollection Create(IStorageIndex index)
        {
            SchemaCollection = new SchemaCollection();
            DocumentFactory = new DefaultDocumentFactory(index);
            Searcher = new LuceneSearcher(index);
            return this;
        }
    }
}
