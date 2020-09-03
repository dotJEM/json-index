using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    public class RamJsonIndexStorage : AbstractJsonIndexStorage
    {
        public RamJsonIndexStorage(ILuceneJsonIndex index) : base(index)
        {
        }

        protected override Directory Create()
        {
            return new RAMDirectory();
        }

        public override void Delete()
        {
            Close();
            Unlock();
            this.Directory.Dispose();
            this.Directory = null;
        }
    }
}