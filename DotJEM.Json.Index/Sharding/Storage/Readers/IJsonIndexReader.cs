using System;
using DotJEM.Json.Index.Sharding.Util;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.Sharding.Storage.Readers
{
    public interface IJsonIndexReader : IDisposable
    {
    }

    public class JsonIndexReader : Disposeable, IJsonIndexReader
    {
        private readonly IndexReaderManager manager;

        public IndexReader UnderlyingReader => manager.UnderlyingReader;

        public JsonIndexReader(IndexReaderManager manager)
        {
            this.manager = manager;
        }

        protected override void Dispose(bool disposing)
        {
            manager.Release(this);
        }
        
    }
}