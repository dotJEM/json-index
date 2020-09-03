using System.IO;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Storage
{
    public class SimpleFsJsonIndexStorage : AbstractJsonIndexStorage
    {
        private readonly string path;

        public SimpleFsJsonIndexStorage(ILuceneJsonIndex index, string path) : base(index)
        {
            this.path = path;
        }

        protected override Directory Create()
        {
            return new SimpleFSDirectory(path);
        }

        public override void Delete()
        {
            Close();
            Unlock();
            Directory.Dispose();
            Directory = null;
            
            DirectoryInfo dir = new DirectoryInfo(path);
            if(!dir.Exists)
                return;

            foreach (FileInfo fileInfo in dir.GetFiles())
                fileInfo.Delete();
        }
    }
}