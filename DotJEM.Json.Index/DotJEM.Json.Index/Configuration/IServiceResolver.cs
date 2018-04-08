using System;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Json.Index.Configuration
{
    public interface IServiceResolver
    {
        T Resolve<T>();
        object Resolve(Type type);

        bool Contains<T>();
        bool Contains(Type type);
    }
    public class ServiceResolver : IServiceResolver
    {
        private readonly Dictionary<Type, object> instances = new Dictionary<Type, object>();
        private readonly object padlock = new object();

        private readonly IServiceCollection services;

        public ServiceResolver(IServiceCollection services)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public virtual T Resolve<T>() => (T)Resolve(typeof(T));

        public virtual object Resolve(Type type)
        {
            if (instances.ContainsKey(type))
                return instances[type];

            lock (padlock)
            {
                if (instances.ContainsKey(type))
                    return instances[type];

                if (services.Contains(type))
                    return instances[type] = services.ObtainFactory(type).Invoke(this);

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IFactory<>))
                    return instances[type] = Activator.CreateInstance(typeof(ResolvingFactory<>).MakeGenericType(type.GetGenericArguments().Single()), this, services);

                return null;
            }
        }

        public bool Contains<T>() => Contains(typeof(T));

        public bool Contains(Type type) => instances.ContainsKey(type) || services.Contains(type);

        private class ResolvingFactory<TService> : IFactory<TService>
        {
            private readonly Lazy<Func<TService>> factory;

            public ResolvingFactory(IServiceResolver resolver, IServiceCollection services)
            {
                factory = new Lazy<Func<TService>>(() =>
                {
                    var fac = services.ObtainFactory<TService>();
                    return () => fac(resolver);
                });
            }

            public TService Create() => factory.Value.Invoke();
        }
    }
}