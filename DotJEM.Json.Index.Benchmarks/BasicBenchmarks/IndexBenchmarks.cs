using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotJEM.Json.Index.Benchmarks.TestFactories;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Test.Util;
using NUnit.Framework;

namespace DotJEM.Json.Index.Benchmarks
{
    [TestFixture]
    public class IndexBenchmarks
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();
        private readonly RandomTextGenerator generator = new RandomTextGenerator();
        private TestIndexBuilder builder;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            builder = new TestIndexBuilder(index);
        }

        [TestCase("batch 1", 50000), TestCase("batch 2", 50000)]
        //[TestCase("batch 3", 500000), TestCase("batch 4", 500000)]
        //[TestCase("batch 5", 500000), TestCase("batch 6", 500000)]
        //[TestCase("batch 7", 500000), TestCase("batch 8", 500000)]
        //[TestCase("batch 9", 500000), TestCase("batch 0", 500000)]
        public void _Benchmark_GenerateTestData(string batch, int count)
        {
            foreach (Document document in new TestObjectGenerator(count, generator))
            {
                builder.Document(document, db => db);
            }
        }

        [TestCase("contentType: order", 500)]
        [TestCase("contentType: person", 500)]
        [TestCase("contentType: product", 500)]
        [TestCase("contentType: account", 500)]
        [TestCase("contentType: storage", 500)]
        [TestCase("contentType: address", 500)]
        [TestCase("contentType: payment", 500)]
        [TestCase("contentType: delivery", 500)]
        [TestCase("contentType: token", 500)]
        [TestCase("contentType: shipment", 500)]
        public void Benchmark_ByContentType(string query, int boundary)
        {
            BenchmarkResult result = BenchmarkQuery(query);

            Assert.That(result, 
                Has.Property("AverageDelay").LessThan(boundary)
                & Has.Property("CountPass").True);
        }

        [Test,Repeat(500)]
        public void Benchmark_RandomText()
        {
            string text = generator.RandomText();
            string word = generator.Word(text);

            BenchmarkResult result = BenchmarkQuery(string.Format("content: {0}*", word));

            Assert.That(result,
                Has.Property("Count").GreaterThan(0)
                & Has.Property("AverageDelay").LessThan(500)
                & Has.Property("CountPass").True, "Search for '" + word + "' from '" + text + "' failed");
        }

        [TestCase("Childharold","order", 500)]
        [TestCase("Decameron", "order", 500)]
        [TestCase("Faust", "order", 500)]
        [TestCase("Inderfremde", "order", 500)]
        [TestCase("Lebateauivre", "order", 500)]
        [TestCase("Lemasque", "order", 500)]
        [TestCase("Loremipsum", "order", 500)]
        [TestCase("Nagyonfaj", "order", 500)]
        [TestCase("Omagyar", "order", 500)]
        [TestCase("Robinsonokruso", "order", 500)]
        [TestCase("Theraven", "order", 500)]
        [TestCase("Tierrayluna", "order", 500)]

        [TestCase("Childharold", "product", 500)]
        [TestCase("Decameron", "product", 500)]
        [TestCase("Faust", "product", 500)]
        [TestCase("Inderfremde", "product", 500)]
        [TestCase("Lebateauivre", "product", 500)]
        [TestCase("Lemasque", "product", 500)]
        [TestCase("Loremipsum", "product", 500)]
        [TestCase("Nagyonfaj", "product", 500)]
        [TestCase("Omagyar", "product", 500)]
        [TestCase("Robinsonokruso", "product", 500)]
        [TestCase("Theraven", "product", 500)]
        [TestCase("Tierrayluna", "product", 500)]

        [TestCase("Childharold", "delivery", 500)]
        [TestCase("Decameron", "delivery", 500)]
        [TestCase("Faust", "delivery", 500)]
        [TestCase("Inderfremde", "delivery", 500)]
        [TestCase("Lebateauivre", "delivery", 500)]
        [TestCase("Lemasque", "delivery", 500)]
        [TestCase("Loremipsum", "delivery", 500)]
        [TestCase("Nagyonfaj", "delivery", 500)]
        [TestCase("Omagyar", "delivery", 500)]
        [TestCase("Robinsonokruso", "delivery", 500)]
        [TestCase("Theraven", "delivery", 500)]
        [TestCase("Tierrayluna", "delivery", 500)]
        public void Benchmark_ByTextAndContentType(string text, string contentType, long boundary)
        {
            string word = generator.Word(generator.RandomText());
            BenchmarkResult result = BenchmarkQuery(string.Format("contentType: {0} AND content: {1}*", contentType, word));

            Assert.That(result,
                Has.Property("AverageDelay").LessThan(boundary)
                & Has.Property("CountPass").True);
        }

        public void Benchmark_ParalelExecution(string text, string contentType, long boundary)
        {
            string word = generator.Word(generator.RandomText());
            BenchmarkResult result = BenchmarkQuery(string.Format("contentType: {0} AND content: {1}*", contentType, word));

            Assert.That(result,
                Has.Property("Count").GreaterThan(0)
                & Has.Property("AverageDelay").LessThan(boundary)
                & Has.Property("CountPass").True);
        }

        private BenchmarkResult BenchmarkQuery(string query, int take = 10, int skip = 0)
        {
            BenchmarkResult result = new BenchmarkResult();
            for (int i = 0; i < 10; i++)
            {
                Stopwatch timer = Stopwatch.StartNew();
                ISearchResult test = index.Search(query).Take(take).Skip(skip);
                test.ToList();
                timer.Stop();
                result.Record(timer.ElapsedMilliseconds, test.TotalCount);
            }
            return result;
        }

        private class BenchmarkResult
        {
            private readonly List<Tuple<long, long>> results = new List<Tuple<long, long>>();

            public long Count { get { return results.First().Item2; } }
            public bool CountPass { get { return results.Select(t => t.Item2).Distinct().Count() == 1; } }
            public double AverageDelay { get { return results.Select(t => t.Item1).Average(); } }

            public void Record(long elapsed, long count)
            {
                results.Add(new Tuple<long, long>(elapsed, count));
            }


        }
    }
}