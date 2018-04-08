using System;

namespace DotJEM.Json.Index.Util
{
    public class Disposable : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;
        }

        ~Disposable()
        {
            Dispose(false);
        }
    }
}