using System;
using System.Collections.Generic;
using System.Text;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Serialization;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace DotJEM.Json.Index.Storage
{
    public interface ILuceneStorageFactory
    {
        IJsonIndexStorage Create(ILuceneJsonIndex index, LuceneVersion version);
    }

    public interface IJsonIndexStorage
    {
        bool Exists { get; }
        Directory Directory { get; }
        void Unlock();
        void Delete();
    }

    public class LuceneRamStorageFactory : ILuceneStorageFactory
    {
        public IJsonIndexStorage Create(ILuceneJsonIndex index, LuceneVersion configurationVersion) => new JsonIndexStorage(index, new RAMDirectory());
    }

    public class LuceneSimpleFileSystemStorageFactory : ILuceneStorageFactory
    {
        private readonly string path;
        public LuceneSimpleFileSystemStorageFactory(string path) => this.path = path;
        public IJsonIndexStorage Create(ILuceneJsonIndex index, LuceneVersion configurationVersion) => new JsonIndexStorage(index, new SimpleFSDirectory(path));
    }

    public class JsonIndexStorage : IJsonIndexStorage
    {
        private readonly ILuceneJsonIndex index;
        private readonly LuceneVersion version = LuceneVersion.LUCENE_48;

        public bool Exists => DirectoryReader.IndexExists(Directory);
        public Directory Directory { get; }

        public JsonIndexStorage(ILuceneJsonIndex index, Directory directory)
        {
            this.index = index;
            this.Directory = directory;
            SearcherManager = new IndexSearcherManager(WriterManager);
        }

        public void Unlock()
        {
            if (IndexWriter.IsLocked(Directory))
                IndexWriter.Unlock(Directory);
        }

        public virtual void Delete()
        {
            IndexWriterConfig config = new IndexWriterConfig(version, new KeywordAnalyzer());
            config.SetOpenMode(OpenMode.CREATE);

            index.WriterManager.Close();
            //TODO: This will cause all index files to be deleted as we force it to create the index
            new IndexWriter(Directory, config).Dispose();
            SearcherManager = new IndexSearcherManager(WriterManager);
            Unlock();
        }
    }
}
