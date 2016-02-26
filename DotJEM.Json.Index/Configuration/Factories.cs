using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;

namespace DotJEM.Json.Index.Configuration
{
    public interface IServiceFactory
    {
        ISchemaCollection CreateSchemaCollection(IStorageIndex index);
        IDocumentFactory CreateDocumentFactory(IStorageIndex index);
        ILuceneSearcher CreateSearcher(IStorageIndex index);
    }

    public class DefaultServiceFactory : IServiceFactory
    {
        public virtual ISchemaCollection CreateSchemaCollection(IStorageIndex index)
        {
            return new SchemaCollection();
        }

        public virtual IDocumentFactory CreateDocumentFactory(IStorageIndex index)
        {
            return new DefaultDocumentFactory(index);
        }

        public virtual ILuceneSearcher CreateSearcher(IStorageIndex index)
        {
            return new LuceneSearcher(index);
        }
    }
}
