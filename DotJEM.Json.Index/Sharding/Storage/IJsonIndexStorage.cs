using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Sharding.Analyzers;
using DotJEM.Json.Index.Sharding.Storage.Readers;
using DotJEM.Json.Index.Sharding.Storage.Writers;
using DotJEM.Json.Index.Storage;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Sharding.Storage
{
    public interface IJsonIndexStorage
    {
        bool Exists { get; }
        LuceneDirectory Directory { get; }
        IIndexReaderManager Reader { get; }
        IIndexWriterManager Writer { get; }
        void Unlock();
    }

    public abstract class AbstractJsonIndexStorage : IJsonIndexStorage
    {
        public IIndexWriterManager Writer { get; }
        public IIndexReaderManager Reader { get; }
        public LuceneDirectory Directory { get; }
        public Analyzer Analyzer { get; }

        public virtual bool Exists => Directory.ListAll().Any();

        protected AbstractJsonIndexStorage(LuceneDirectory directory)
        {
            Directory = directory;
            //TODO: (jmd 2015-10-08) Analyzer should get injected. 
            Analyzer = new JsonFieldAnalyzer();

            Writer = new IndexWriterManager(this);
            Reader = new IndexReaderManager(this);
        }

        public void Unlock()
        {
            if (IndexWriter.IsLocked(Directory))
                IndexWriter.Unlock(Directory);
        }
    }

    public class MemmoryJsonIndexStorage : AbstractJsonIndexStorage
    {
        public MemmoryJsonIndexStorage()
            : base(new RAMDirectory())
        {
        }
    }

    public class JsonFileIndexStorage : AbstractJsonIndexStorage
    {
        public JsonFileIndexStorage(string path)
            //Note: Ensure Directory.
            : base(FSDirectory.Open(System.IO.Directory.CreateDirectory(path).FullName))
        {
        }
    }

    public class JsonMemmoryMappedFileIndexStorage : AbstractJsonIndexStorage
    {
        public JsonMemmoryMappedFileIndexStorage(string path)
            //Note: Ensure cacheDirectory.
            : base(new MMapDirectory(System.IO.Directory.CreateDirectory(path)))
        {
        }
    }
}