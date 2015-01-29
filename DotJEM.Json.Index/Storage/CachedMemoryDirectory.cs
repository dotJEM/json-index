using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Storage
{
    public sealed class CachedMemoryDirectory : Directory
    {
        private readonly DirectoryInfo cache;
        private readonly Dictionary<string, CachedMemoryFile> files = new Dictionary<string, CachedMemoryFile>();
        private readonly object padLock = new object();

        public CachedMemoryDirectory(DirectoryInfo cache)
        {
            this.cache = cache;
            SetLockFactory(new CachedMemoryLockFactory(cache));
        }

        public override string[] ListAll()
        {
            EnsureOpen();
            lock (padLock)
            {
                return files.Keys.ToArray();
            }
        }

        public override bool FileExists(string name)
        {
            lock (padLock)
            {
                return files.ContainsKey(name);
            }
        }

        public override long FileModified(string name)
        {
            lock (padLock)
            {
                return GetFile(name).LastModified;
            }
        }

        public override void TouchFile(string name)
        {
            lock (padLock)
            {
                CachedMemoryFile file = GetFile(name);
                file.Touch();
            }
        }

        public override void DeleteFile(string name)
        {
            lock (padLock)
            {
                CachedMemoryFile file = GetFile(name);
                file.Delete();
                files.Remove(name);
            }
        }
        
        public override long FileLength(string name)
        {
            CachedMemoryFile file = GetFile(name);
            return file.Length;
        }

        public override IndexOutput CreateOutput(string name)
        {
            CachedMemoryFile file = new CachedMemoryFile();
            lock (padLock)
            {
                CachedMemoryFile existing;
                if (files.TryGetValue(name, out existing))
                    existing.Delete();

                files[name] = file;
            }
            return new CachedMemoryFileOutputStream(file);
        }

        public override IndexInput OpenInput(string name)
        {
            CachedMemoryFile file = GetFile(name);
            return new CachedMemoryFileInputStream(file);
        }

        protected override void Dispose(bool disposing)
        {
            isOpen = false;
        }

        private CachedMemoryFile GetFile(string name)
        {
            EnsureOpen();
            try
            {
                return files[name];
            }
            catch (KeyNotFoundException)
            {
                throw new FileNotFoundException("Could not find the specified file.", name);
            }
        }
    }
}