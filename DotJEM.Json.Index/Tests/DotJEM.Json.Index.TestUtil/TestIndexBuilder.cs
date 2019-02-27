using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Contexts;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.IO;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.TestUtil
{
    public interface ITestIndexBuilder
    {
        ITestIndexBuilder With(string contentType, object template);
        ITestIndexBuilder With(TestObject to);
        ITestIndexBuilder With(IEnumerable<TestObject> tos);

        ITestIndexBuilder With(Action<IServiceCollection> configurator);

        Task<ILuceneJsonIndex> Build();
    }

    public interface ITestIndexContextBuilder
    {
        ILuceneIndexContext Context { get; }
        ILuceneIndexContextBuilder ContextBuilder { get; }
        ITestIndexContextBuilder WithIndex(string name, Action<TestIndexBuilder> builderConfig);
        ITestIndexContextBuilder With(Action<IServiceCollection> configurator);
        Task<ILuceneIndexContext> Build();
        ILuceneIndexContextBuilder Configure(string name, Action<ILuceneJsonIndexBuilder> config);
    }

    public class TestIndexContextBuilder : ITestIndexContextBuilder
    {
        private Lazy<ILuceneIndexContext> context;

        public ILuceneIndexContextBuilder ContextBuilder { get; } = new LuceneIndexContextBuilder();
        public ILuceneIndexContext Context => context.Value;

        private readonly Dictionary<string, TestIndexBuilder> indexBuilders = new Dictionary<string, TestIndexBuilder>();

        public TestIndexContextBuilder()
        {
            context = new Lazy<ILuceneIndexContext>(() => ContextBuilder.Build());
        }

        public ITestIndexContextBuilder WithIndex(string name, Action<TestIndexBuilder> builderConfig)
        {
            if (!indexBuilders.TryGetValue(name, out TestIndexBuilder builder))
                indexBuilders.Add(name, builder = new TestIndexBuilder(name, this));
            builderConfig(builder);
            return this;
        }

        public ITestIndexContextBuilder With(Action<IServiceCollection> configurator)
        {
            configurator(ContextBuilder.Services);
            return this;
        }

        public async Task<ILuceneIndexContext> Build()
        {
            foreach (TestIndexBuilder builder in indexBuilders.Values)
                await builder.Build().ConfigureAwait(false);
            return context.Value;
        }

        public ILuceneIndexContextBuilder Configure(string name, Action<ILuceneJsonIndexBuilder> config)
        {
            return ContextBuilder.Configure(name, config);
        }
    }

    public class TestIndexBuilder : ITestIndexBuilder
    {
        private readonly string name;
        private readonly ITestIndexContextBuilder contextBuilder;
        private readonly List<JObject> objects = new List<JObject>();

        public TestIndexBuilder(string name = "main", ITestIndexContextBuilder context = null)
        {
            this.name = name;
            this.contextBuilder = context ?? new TestIndexContextBuilder();
        }

        public ITestIndexBuilder With(string contentType, object template) => With((contentType, template));

        public ITestIndexBuilder With(TestObject to)
        {
            objects.Add(to.Object);
            return this;
        }

        public ITestIndexBuilder With(IEnumerable<TestObject> tos) => tos.Aggregate((ITestIndexBuilder)this, (builder, to) => builder.With(to));

        public ITestIndexBuilder With(Action<IServiceCollection> configurator)
        {
            contextBuilder.ContextBuilder.Configure(name, builder => configurator(builder.Services));
            return this;
        }

        public async Task<ILuceneJsonIndex> Build()
        {
            
            
            //context.Configure(name, config =>
            //{
            //    config.UseMemoryStorage();
            //});
            ILuceneIndexContext context = contextBuilder.Context;
            ILuceneJsonIndex index = context.Open(name);
            index.InfoStream.Subscribe(new TestInfoStreamObserver());
            index.Storage.Delete();

            IJsonIndexWriter writer = index.CreateWriter();
            try
            {
                writer.Create(objects);
                writer.Commit();
            }
            catch (AggregateException e)
            {
                ExceptionDispatchInfo.Capture(e.GetBaseException()).Throw();
            }

            return index;
        }

    }

    public class TestInfoStreamObserver : IObserver<InfoEventArgs> {
        public void OnCompleted()
        {
            Console.WriteLine("INFO STREAM SIGNALED COMPLETE");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine("ERROR ON RECEIVER INFO EVENT");
        }

        public void OnNext(InfoEventArgs value)
        {
            Console.WriteLine(value);
        }
    }

    public sealed class TestObject
    {
        private readonly Lazy<JObject> testObject;

        public Guid Id { get; } = Guid.NewGuid();

        public string ContentType { get; }

        public JObject Template { get; }

        public JObject Object => testObject.Value;

        public TestObject(string contentType, JObject template)
        {
            ContentType = contentType;
            Template = template;

            testObject = new Lazy<JObject>(() =>
            {
                Template["$id"] = Guid.NewGuid();
                Template["$contentType"] = contentType;
                return Template;
            });
        }

        public static implicit operator TestObject(ValueTuple<string, object> template)
        {
            JObject json = template.Item2 is string str 
                ? JObject.Parse(str) 
                : template.Item2 is JObject jo 
                ? jo
                : JObject.FromObject(template.Item2);
            json["$id"] = Guid.NewGuid();
            json["$contentType"] = template.Item1;
            return new TestObject(template.Item1, json);
        }
    }
}