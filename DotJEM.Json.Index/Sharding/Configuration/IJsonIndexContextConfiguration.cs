using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Sharding.Schemas;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Sharding.Configuration
{
    public interface IJsonIndexContextConfiguration
    {
        IJsonIndexConfiguration this[string name] { get; set; }
    }

    public class LuceneJsonIndexContextConfiguration : IJsonIndexContextConfiguration
    {
        private readonly ConcurrentDictionary<string, IJsonIndexConfiguration> configurations = new ConcurrentDictionary<string, IJsonIndexConfiguration>();

        public IJsonIndexConfiguration this[string name]
        {
            set { configurations[name] = value; }
            get { return configurations.GetOrAdd(name, key => new DefaultJsonIndexConfiguration()); }
        }
    }

    public interface IJsonIndexConfiguration
    {
        IMetaFieldResolver MetaFieldResolver { get; }
        Documents.IDocumentFactory DocumentFactory { get; }
    }

    public class DefaultJsonIndexConfiguration : IJsonIndexConfiguration
    {
        private readonly IServiceResolver services = DefaultServices.CreateDefaultResolver();

        public IMetaFieldResolver MetaFieldResolver => services.Resolve<IMetaFieldResolver>();
        public Documents.IDocumentFactory DocumentFactory => services.Resolve<Documents.IDocumentFactory>();

    }

    public interface IServiceResolver
    {
        TService Resolve<TService>();
    }

    /// <summary>
    /// Simplest implementation of a service resolver, in IoC this can be replaced by a resolver targeting the IoC Container.
    /// </summary>
    public class DefaultServiceResolver : IServiceResolver
    {
        private readonly Dictionary<Type, Lazy<object>> services;

        public DefaultServiceResolver()
        {
            this.services = new Dictionary<Type, Lazy<object>>();
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

    public static class DefaultServices
    {

        static DefaultServices()
        {
            Register<IMetaFieldResolver, DefaultMetaFieldResolver>(
                r => new DefaultMetaFieldResolver());

            Register<Documents.IDocumentFactory, Documents.LuceneDocumentFactory>(
                r => new Documents.LuceneDocumentFactory(r.Resolve<IMetaFieldResolver>(), r.Resolve<IJSchemaManager>()));

            Register<IJSchemaManager, JSchemaManager>(
                sr => new JSchemaManager(
                    sr.Resolve<IMetaFieldResolver>(),
                    sr.Resolve<IJSchemaGenerator>(),
                    sr.Resolve<ISchemaCollection>()
                ));

            Register<IJSchemaGenerator, JSchemaGenerator>(sr => new JSchemaGenerator());
            Register<ISchemaCollection, SchemaCollection>(sr => new SchemaCollection());
        }

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

        private static void Register<TService, TImpl>(Func<IServiceResolver, object> factory)
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
    }

    public interface IMetaFieldResolver
    {
        string ContentType(JObject json);
        string Area(JObject json);
        string Shard(JObject json);
        Term Identifier(JObject json);
    }

    public class DefaultMetaFieldResolver : IMetaFieldResolver
    {

        public string ContentType(JObject json)
        {
            return (string)json["contentType"];
        }

        public string Area(JObject json)
        {
            return (string)json["area"];
        }

        public string Shard(JObject json)
        {
            DateTime created = (DateTime) json["created"];
            return (string)json[ContentType(json)+"."+created.Year];
        }

        public Term Identifier(JObject json)
        {
            return new Term("id", (string)json["id"]);
        }
    }

    
}