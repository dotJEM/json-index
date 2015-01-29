using System.Collections.Generic;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Storage
{
    public class CachedMemoryLock : Lock
    {
        private readonly string name;
        private readonly HashSet<string> locks;

        public CachedMemoryLock(HashSet<string> locks, string name)
        {
            this.locks = locks;
            this.name = name;
        }

        public override bool Obtain()
        {
            lock (locks)
            {
                if (locks.Contains(name))
                    return false;

                locks.Add(name);
                return true;
            }
        }

        public override void Release()
        {
            lock (locks)
            {
                locks.Remove(name);
            }
        }

        public override bool IsLocked()
        {
            lock (locks)
            {
                return locks.Contains(name);
            }
        }

        public override string ToString()
        {
            return base.ToString() + ": " + name;
        }
    }
}