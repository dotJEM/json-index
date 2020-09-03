using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    public interface IJsonIndexStorage
    {
        bool Exists { get; }
        Directory Directory { get; }
        IIndexWriterManager WriterManager { get; }
        IIndexSearcherManager SearcherManager { get; }
        void Unlock();
        void Delete();
        void Close();
    }
}