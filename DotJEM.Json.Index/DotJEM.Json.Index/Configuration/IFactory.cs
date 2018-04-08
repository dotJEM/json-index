using System;

namespace DotJEM.Json.Index.Configuration
{
    public interface IFactory<out TService>
    {
        TService Create();
    }

    public class FuncFactory<TService> : IFactory<TService>
    {
        private readonly Func<TService> fac;
        public FuncFactory(Func<TService> fac) => this.fac = fac;
        public TService Create() => fac();
    }

    public class InstanceFactory<TService> : IFactory<TService>
    {
        private readonly TService instance;
        /// <inheritdoc />
        public InstanceFactory(TService instance)
        {
            this.instance = instance;
        }
        public TService Create() => instance;
    }
}