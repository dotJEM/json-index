using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Snapshots
{
    public class LuceneZipDirectory : Directory
    {
        private readonly ZipArchive archive;

        public LuceneZipDirectory(string path)
        {
            this.archive = ZipFile.Open(path, ZipArchiveMode.Read);
        }

        public override string[] ListAll()
        {
            return this.archive.Entries.Select(entry => entry.Name).ToArray();
        }

        public override bool FileExists(string name)
        {
            return this.archive.GetEntry(name) != null;
        }

        public override void DeleteFile(string name)
        {
            throw new NotSupportedException();
        }

        public override long FileLength(string name)
        {
            return this.archive.GetEntry(name).Length;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            throw new NotSupportedException();
        }

        public override void Sync(ICollection<string> names)
        {
            throw new NotImplementedException();
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            throw new NotImplementedException();
        }

        public override Lock MakeLock(string name)
        {
            throw new NotImplementedException();
        }

        public override void ClearLock(string name)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }

        public override void SetLockFactory(LockFactory lockFactory)
        {
            throw new NotImplementedException();
        }

        public override LockFactory LockFactory { get; }
    }
}