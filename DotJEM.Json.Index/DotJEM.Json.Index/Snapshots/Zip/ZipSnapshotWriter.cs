using System.IO;
using System.IO.Compression;
using DotJEM.Json.Index.Util;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Snapshots.Zip
{
    public class ZipSnapshotWriter : Disposable, ISnapshotWriter
    {
        private readonly ZipArchive archive;

        public ZipSnapshotWriter(string path)
        {
            this.archive = ZipFile.Open(path, ZipArchiveMode.Create);
        }

        public void WriteFile(string fileName, Directory dir)
        {
            using IndexInputStream source = new IndexInputStream(dir.OpenInput(fileName, IOContext.READ_ONCE));
            using Stream target = archive.CreateEntry(fileName).Open();
            source.CopyTo(target);
        }

        public void WriteSegmentsFile(string segmentsFile, Directory dir)
        {
            this.WriteFile(segmentsFile, dir);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                archive?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}