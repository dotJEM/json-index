using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Sharding.Documents;
using DotJEM.Json.Index.Sharding.Schemas;
using DotJEM.Json.Index.Sharding.Visitors;

namespace DotJEM.Json.Index.Sharding.Resolvers
{
    public static class DefaultServices
    {
        static DefaultServices()
        {
            Register<IMetaFieldResolver, DefaultMetaFieldResolver>(
                r => new DefaultMetaFieldResolver());

            Register<IDocumentBuilder, DefaultDocumentBuilder>(
                r => new DefaultDocumentBuilder());

            Register<ILuceneDocumentFactory, DefaultLuceneDocumentFactory>(
                r => new DefaultLuceneDocumentFactory(r.Resolve<IJSchemaManager>(), r.Resolve<IDocumentBuilder>()));

            Register<IJSchemaManager, JSchemaManager>(
                sr => new JSchemaManager(
                    sr.Resolve<IMetaFieldResolver>(),
                    sr.Resolve<IJSchemaGenerator>()
                    ));

            Register<IJSchemaGenerator, JSchemaGenerator>(sr => new JSchemaGenerator());
        }

        #region Implementations
        private static readonly Dictionary<Type, Implementation> impls = new Dictionary<Type, Implementation>();

        public static IDictionary<Type, Type> Implementations
        {
            get { return impls.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Type); }
        }

        public static IServiceResolver CreateDefaultResolver()
        {
            return impls
                .Aggregate(new DefaultServiceResolver(), (resolver, pair) => resolver.Register(pair.Key, sr => pair.Value.Create(sr)));
        }

        private static void Register<TService, TImpl>(Func<IServiceResolver, object> factory) where TImpl : TService
        {
            impls.Add(typeof(TService), new Implementation(typeof(TImpl), factory));
        }

        internal class Implementation
        {
            private readonly Func<IServiceResolver, object> factory;

            public Type Type { get; }

            public Implementation(Type type, Func<IServiceResolver, object> factory)
            {
                this.Type = type;
                this.factory = factory;
            }

            public object Create(IServiceResolver resolver)
            {
                return factory(resolver);
            }
        }
        #endregion
    }
}