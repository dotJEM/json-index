using System;
using System.Threading;

namespace DotJEM.Json.Index.Util
{
    public class ResetableLazy<T>
    {
        private Lazy<T> lazy;
        private readonly Func<Lazy<T>> factory;
        private readonly object padLock = new object();

        public ResetableLazy()
        {
            factory = () => new Lazy<T>();
            Reset();
        }

        public ResetableLazy(bool isThreadSafe)
        {
            factory = () => new Lazy<T>(isThreadSafe);
            Reset();
        }

        public ResetableLazy(Func<T> valueFactory)
        {
            factory = () => new Lazy<T>(valueFactory);
            Reset();
        }

        public ResetableLazy(Func<T> valueFactory, bool isThreadSafe)
        {
            factory = () => new Lazy<T>(valueFactory, isThreadSafe);
            Reset();
        }

        public ResetableLazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
        {
            factory = () => new Lazy<T>(valueFactory, mode);
            Reset();
        }

        public ResetableLazy(LazyThreadSafetyMode mode)
        {
            factory = () => new Lazy<T>(mode);
            Reset();
        }

        public void Reset()
        {
            lock (padLock)
            {
                lazy = factory();
            }
        }

        public bool IsValueCreated
        {
            get
            {
                lock (padLock)
                {
                    return lazy.IsValueCreated;
                }
            }
        }

        public T Value
        {
            get
            {
                lock (padLock)
                {
                    return lazy.Value;
                }
            }
        }
    }
}