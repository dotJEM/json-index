using System;

namespace DotJEM.Json.Index.Sharding.Util
{
    public abstract class Disposeable : IDisposable
    {
        protected volatile bool Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
            Disposed = true;
        }

        ~Disposeable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}