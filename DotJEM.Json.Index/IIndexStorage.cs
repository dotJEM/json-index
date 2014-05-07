using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index
{
    public interface IIndexStorage
    {
        IndexReader OpenReader();
        IndexWriter GetWriter(Analyzer analyzer);
    }

    public abstract class AbstractLuceneIndexStorage : IIndexStorage
    {
        protected Directory Directory { get; private set; }
        protected virtual bool Exists { get { return Directory.ListAll().Any(); } }

        private IndexWriter writer;

        protected AbstractLuceneIndexStorage(Directory directory)
        {
            Directory = directory;
        }

        public IndexWriter GetWriter(Analyzer analyzer)
        {
            //TODO: The storage should define the analyzer, not the writer.
            return writer ?? (writer = new IndexWriter(Directory, analyzer, !Exists, IndexWriter.MaxFieldLength.UNLIMITED));
        }

        public IndexReader OpenReader()
        {
            return IndexReader.Open(Directory, true);
        }
    }

    public class LuceneMemmoryIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneMemmoryIndexStorage()
            : base(new RAMDirectory())
        {
        }
    }

    public class LuceneSimpleFileIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneSimpleFileIndexStorage(string path)
            : base(FSDirectory.Open(path))
        {
            //Note: Ensure directory.
            System.IO.Directory.CreateDirectory(path);
        }
    }

    public class LuceneMemmoryMappedFileIndexStorage : AbstractLuceneIndexStorage
    {
        public LuceneMemmoryMappedFileIndexStorage(string path)
            : base(new MMapDirectory(new DirectoryInfo(path)))
        {
            //Note: Ensure directory.
            System.IO.Directory.CreateDirectory(path);
        }
    }
}