using System;
using System.Collections.Generic;

namespace DotJEM.Json.Index.Sharding.Resolvers
{
    /// <summary>
    /// Simplest implementation of a service resolver, in IoC this can be replaced by a resolver targeting the IoC Container.
    /// </summary>
    public class DefaultServiceResolver : IServiceResolver
    {
        private readonly Dictionary<Type, Lazy<object>> services;

        public DefaultServiceResolver()
        {
            services = new Dictionary<Type, Lazy<object>>();
        }

        public DefaultServiceResolver Register(Type service, Func<IServiceResolver, object> factory)
        {
            services[service] = new Lazy<object>(() => factory(this));
            return this;
        }

        public TService Resolve<TService>()
        {
            return (TService)services[typeof(TService)].Value;
        }
    }
}