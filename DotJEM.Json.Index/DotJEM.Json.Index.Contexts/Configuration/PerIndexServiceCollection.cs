using System;
using DotJEM.Json.Index.Configuration;

namespace DotJEM.Json.Index.Contexts
{
    public class PerIndexServiceCollection : ServiceCollection
    {
        private readonly IServiceCollection contextCollection;

        public PerIndexServiceCollection(IServiceCollection contextCollection)
        {
            this.contextCollection = contextCollection;
        }

        public override bool Contains(Type type)
        {
            return base.Contains(type) || contextCollection.Contains(type);
        }

        public override bool TryObtainFactory(Type key, out Func<IServiceResolver, object> value)
        {
            return base.TryObtainFactory(key, out value) || contextCollection.TryObtainFactory(key, out value);
        }
    }
}