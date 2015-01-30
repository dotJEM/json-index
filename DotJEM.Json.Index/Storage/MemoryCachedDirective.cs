using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Storage
{
    public sealed class MemoryCachedDirective : Directory
    {
        private readonly DirectoryInfo cache;
        private readonly Dictionary<string, MemoryCachedFile> files = new Dictionary<string, MemoryCachedFile>();
        private readonly object padLock = new object();

        public MemoryCachedDirective(DirectoryInfo cache)
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
                Debug.WriteLine("bool FileExists(" + name + ")");
                return files.ContainsKey(name);
            }
        }

        public override long FileModified(string name)
        {
            lock (padLock)
            {
                MemoryCachedFile file = GetFile(name);
                Debug.WriteLine("long FileModified(" + name + ") = " + file.LastModified);
                return file.LastModified;
            }
        }

        public override void TouchFile(string name)
        {
            lock (padLock)
            {
                Debug.WriteLine("void TouchFile(" + name + ")");
                MemoryCachedFile file = GetFile(name);
                file.Touch();
            }
        }

        public override void DeleteFile(string name)
        {
            lock (padLock)
            {
                Debug.WriteLine("void DeleteFile(" + name + ")");
                MemoryCachedFile file = GetFile(name);
                file.Delete();
                files.Remove(name);
            }
        }
        
        public override long FileLength(string name)
        {
            MemoryCachedFile file = GetFile(name);
            Debug.WriteLine("long FileLength(" + name + ") = " + file.Length);
            return file.Length;
        }

        public override IndexOutput CreateOutput(string name)
        {
            MemoryCachedFile file = new MemoryCachedFile();
            lock (padLock)
            {
                Debug.WriteLine("IndexOutput CreateOutput(" + name + ")");

                MemoryCachedFile existing;
                if (files.TryGetValue(name, out existing))
                    existing.Delete();

                files[name] = file;
            }
            return new CachedMemoryFileOutputStream(file);
        }

        public override IndexInput OpenInput(string name)
        {
            Debug.WriteLine("IndexInput CreateOutput(" + name + ")");

            MemoryCachedFile file = GetFile(name);
            return new CachedMemoryFileInputStream(file);
        }

        protected override void Dispose(bool disposing)
        {
            isOpen = false;
        }

        private MemoryCachedFile GetFile(string name)
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