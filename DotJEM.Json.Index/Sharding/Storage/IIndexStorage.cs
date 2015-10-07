using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Sharding.Storage
{
    public interface IIndexStorage
    {
    }

    public class MemmoryIndexStorage : IIndexStorage
    {
        private Directory directory;
        

        public MemmoryIndexStorage(Directory directory)
        {
            this.directory = directory;
        }
    }

    public interface IJsonIndexWriter : IDisposable
    {
        
    }

    public class ReuseableIndexWriter : Disposeable, IJsonIndexWriter
    {
        private readonly IIndexStorage owner;

        public IndexWriter InternalWriter { get; }

        public ReuseableIndexWriter(IIndexStorage owner, IndexWriter writer)
        {
            this.owner = owner;

            InternalWriter = writer;
        }

        protected override void Dispose(bool disposing)
        {
            
        }
    }

    public abstract class Disposeable : IDisposable
    {
        protected volatile bool Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            Disposed = true;
        }

        ~Disposeable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}