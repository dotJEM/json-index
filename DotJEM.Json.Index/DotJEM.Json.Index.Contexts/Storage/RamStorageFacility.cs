using System;
using DotJEM.Json.Index.Storage;

namespace DotJEM.Json.Index.Contexts.Storage
{
    public class RamStorageFacility : ILuceneStorageFactoryProvider
    {
        public Func<ILuceneStorageFactory> Create(string name)
        {
            return () => new LuceneRamStorageFactory();
        }

        public ILuceneStorageFactory Get(string name)
        {
            return new LuceneRamStorageFactory();
        }
    }
}