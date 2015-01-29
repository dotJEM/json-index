using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    //NOTE: This is taken directly from LUCENE as of now. (Classes was internal so could not reuse directly)
    public class CachedMemoryLockFactory : LockFactory
    {
        private readonly DirectoryInfo cache;
        private readonly HashSet<string> locks = new HashSet<string>();

        public CachedMemoryLockFactory(DirectoryInfo cache)
        {
            this.cache = cache;
        }

        public override Lock MakeLock(string lockName)
        {
            return new CachedMemoryLock(locks, lockName);
        }

        public override void ClearLock(string lockName)
        {
            lock (locks)
            {
                if (!locks.Contains(lockName))
                    return;
                locks.Remove(lockName);
            }
        }
    }
}
