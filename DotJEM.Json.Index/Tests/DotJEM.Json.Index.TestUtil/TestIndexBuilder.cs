using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using DotJEM.Json.Index.Contexts;
using DotJEM.Json.Index.IO;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.TestUtil
{
    public interface ITestIndexBuilder
    {
        ITestIndexBuilder With(string contentType, object template);
        ITestIndexBuilder With(TestObject to);
        ITestIndexBuilder With(IEnumerable<TestObject> tos);

        ITestIndexBuilder Defaults(Action<ILuceneIndexBuilderDefaults> configurator);

        Task<ILuceneJsonIndex> Build(string name = "main");
    }

    public class TestIndexBuilder : ITestIndexBuilder
    {
        private readonly ILuceneIndexContext context = new LuceneIndexContext();
        private readonly List<JObject> objects = new List<JObject>();

        public ITestIndexBuilder With(string contentType, object template) => With((contentType, template));

        public ITestIndexBuilder With(TestObject to)
        {
            objects.Add(to.Object);
            return this;
        }

        public ITestIndexBuilder With(IEnumerable<TestObject> tos) => tos.Aggregate((ITestIndexBuilder)this, (builder, to) => builder.With(to));

        public ITestIndexBuilder Defaults(Action<ILuceneIndexBuilderDefaults> configurator)
        {
            configurator(context.Defaults);
            return this;
        }

        public async Task<ILuceneJsonIndex> Build(string name = "main")
        {
            context.Configure(name, config =>
            {
                config.UseMemoryStorage();
            });
            ILuceneJsonIndex index = context.Open(name);
            index.Storage.Delete();

            IJsonIndexWriter writer = index.CreateWriter();
            try
            {
                await writer.CreateAsync(objects);
                await writer.CommitAsync();
            }
            catch (AggregateException e)
            {
                ExceptionDispatchInfo.Capture(e.GetBaseException()).Throw();
            }

            return index;
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