using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lucene.Net.Store;
using Lucene.Net.Util.Cache;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Storage
{
    public sealed class MemoryCachedDirective : Directory
    {
        private readonly string cacheDirectory;
        private readonly Dictionary<string, ILuceneFile> files = new Dictionary<string, ILuceneFile>();
        private readonly object padLock = new object();

        //private IDirectoryStrategy strategy = new MemoryCashedDirectoryStrategy();
        private readonly IDirectoryStrategy strategy = new MemoryDirectoryStrategy();
        
        public MemoryCachedDirective(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
            SetLockFactory(new MemoryCashedLockFactory(cacheDirectory));

            System.IO.Directory.CreateDirectory(cacheDirectory);
            foreach (string file in System.IO.Directory.GetFiles(cacheDirectory).Select(Path.GetFileName))
            {
                files.Add(file, new MemoryCachedFile(file));
            }
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
                ILuceneFile file = GetFile(name);
                return file.LastModified;
            }
        }

        public override void TouchFile(string name)
        {
            lock (padLock)
            {
                ILuceneFile file = GetFile(name);
                file.Touch();
            }
        }

        public override void DeleteFile(string name)
        {
            lock (padLock)
            {
                ILuceneFile file = GetFile(name);
                file.Delete();
                files.Remove(name);
            }
        }
        
        public override long FileLength(string name)
        {
            ILuceneFile file = GetFile(name);
            return file.Length;
        }

        public override IndexOutput CreateOutput(string name)
        {
            ILuceneFile file = strategy.CreateFile(Path.Combine(cacheDirectory, name));
            lock (padLock)
            {
                ILuceneFile existing;
                if (files.TryGetValue(name, out existing))
                {
                    existing.Delete();
                }

                files[name] = file;
            }
            return strategy.CreateOutput(file);
        }

        public override IndexInput OpenInput(string name)
        {
            ILuceneFile file = GetFile(name);
            return strategy.CreateInput(file);
        }

        protected override void Dispose(bool disposing)
        {
            isOpen = false;
        }

        private ILuceneFile GetFile(string name)
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

    public interface IDirectoryStrategy
    {
        IndexInput CreateInput(ILuceneFile file);
        IndexOutput CreateOutput(ILuceneFile file);

        ILuceneFile CreateFile(string cache);
    }

    class MemoryCashedDirectoryStrategy : IDirectoryStrategy
    {
        public IndexInput CreateInput(ILuceneFile file)
        {
            return new RamInputStream((RamFile)file);
        }

        public IndexOutput CreateOutput(ILuceneFile file)
        {
            return new RamOutputStream((RamFile)file);
        }

        public ILuceneFile CreateFile(string cache)
        {
            return new RamFile();
        }
    }

    class MemoryDirectoryStrategy : IDirectoryStrategy
    {
        public IndexInput CreateInput(ILuceneFile file)
        {
            return new MemoryInputStream((MemoryCachedFile)file);
        }

        public IndexOutput CreateOutput(ILuceneFile file)
        {
            return new MemoryOutputStream((MemoryCachedFile)file);
        }

        public ILuceneFile CreateFile(string cache)
        {
            return new MemoryCachedFile(cache);
        }
    }

    public interface ILuceneFile
    {
        long LastModified { get; }
        long Length { get; }
        long Touch();
        void Delete();
    }
}