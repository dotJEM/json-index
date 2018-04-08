using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Contexts;
using DotJEM.Json.Index.IO;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.QueryParsers.IntegrationTest
{
    [TestFixture]
    public class SimplifiedQueryParserIntegrationTest
    {
        [TestCase("name: Peter", "DOC_001")]
        [TestCase("name = Peter", "DOC_001")]
        [TestCase("age = 20", "DOC_001")]
        [TestCase("age = 20 and name = Peter", "DOC_001")]
        [TestCase("age = 30 or name = Peter", "DOC_001,DOC_002")]

        public void Search_Persons_YieldDocuments(string query, string expected)
        {
            ILuceneJsonIndex index = BuildPersonIndex()
                .Defaults(defaults => defaults.Services.UseSimplifiedLuceneQueryParser())
                .Build().Result;

            string docs1 = index.Search(query).Result.Select(hit => hit.Json.key).Aggregate((left, right) => $"{left},{right}");
            string docs2 = index.Search(new MatchAllDocsQuery()).Result.Select(hit => hit.Json.key).Aggregate((left, right) => $"{left},{right}");
            Console.WriteLine(docs1);
            Console.WriteLine(docs2);
            Assert.That(docs1, Is.EqualTo(expected));
        }

        private ITestIndexBuilder BuildPersonIndex()
        {
            return new TestIndexBuilder()
                .With("person", new { key = "DOC_001", name = "Peter", gender = "male", age = 20, created = DateTime.Now.AddDays(-7), updated = DateTime.Now.AddDays(-1), description = "Peter pan flies to Never Never land!", num = new JArray(10, 20) })
                .With("person", new { key = "DOC_002", name = "Lars", gender = "male", age = 30, created = DateTime.Now.AddDays(-6), updated = DateTime.Now.AddDays(-2), description = "Lars does not care much for coffee, so he takes a beer!", num = new JArray(50, 100) })
                .With("person", new { key = "DOC_003", name = "John", gender = "male", age = 40, created = DateTime.Now.AddDays(-7), updated = DateTime.Now.AddDays(-3), description = "John went to the store to get some new shoes!", num = new JArray(1, 2) })
                .With("person", new { key = "DOC_004", name = "James", gender = "male", age = 50, created = DateTime.Now.AddDays(-8), updated = DateTime.Now.AddDays(-4), description = "James is a real crook!", num = new JArray(5, 10) })
                .With("person", new { key = "DOC_005", name = "Erik", gender = "male", age = 8, created = DateTime.Now.AddDays(-9), updated = DateTime.Now.AddDays(-5), description = "Erik likes to play with legos!", num = new JArray(50, 100) })
                .With("person", new { key = "DOC_006", name = "Sarah", gender = "female", age = 8, created = DateTime.Now.AddDays(-9), updated = DateTime.Now.AddDays(-5), description = "Sarah makes lovely cakes!", num = new JArray(50, 100) });
        }
    }

    public class TestIndexBuilder : ITestIndexBuilder
    {
        private readonly ILuceneIndexContext context = new LuceneIndexContext();
        private readonly List<JObject> objects = new List<JObject>();

        public ITestIndexBuilder With(string contentType, object template)
        {
            JObject json = (template is string str) ? JObject.Parse(str) : JObject.FromObject(template);
            json["$id"] = Guid.NewGuid();
            json["$contentType"] = contentType;
            objects.Add(json);
            return this;
        }

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

    public interface ITestIndexBuilder
    {
        ITestIndexBuilder With(string contentType, object template);
        ITestIndexBuilder Defaults(Action<ILuceneIndexBuilderDefaults> configurator);

        Task<ILuceneJsonIndex> Build(string name = "main");
    }
}
