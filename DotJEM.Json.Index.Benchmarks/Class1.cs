using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Test.Util;
using Newtonsoft.Json.Linq;
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

    public class TestObjectGenerator : IEnumerable<Document>
    {
        private bool stop = false;
        private readonly int limit;
        private readonly HashSet<string> contentTypes = new HashSet<string>(new[] { "order", "person", "product", "account", "storage", "address", "payment", "delivery", "token", "shipment" });
        private readonly RandomTextGenerator textGenerator;

        public TestObjectGenerator(int limit) : this(limit, new RandomTextGenerator())
        {
        }

        public TestObjectGenerator(int limit, RandomTextGenerator textGenerator)
        {
            this.limit = limit;
            this.textGenerator = textGenerator;
        }

        public IEnumerator<Document> GetEnumerator()
        {
            int count = 0;
            while (!stop && count++ < limit)
            {
                string contentType = RandomContentType();
                yield return new Document(contentType, RandomDocument(contentType));
            }
        }

        private string RandomContentType()
        {
            return contentTypes.RandomItem();
        }

        private JObject RandomDocument(string contentType)
        {
            string text = textGenerator.RandomText();
            //TODO: Bigger document and use contentype for propper stuff.
            return JObject.FromObject(new
            {
                source = text,
                content = textGenerator.Paragraph(text),
                keys = textGenerator.Words(text, 4, 5)
            });
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Stop()
        {
            stop = true;
        }
    }

    public static class RandomHelper
    {
        private static readonly Random rand = new Random();

        public static T RandomItem<T>(this IEnumerable<T> items)
        {
            ICollection<T> list = items as ICollection<T> ?? items.ToArray();
            return list.ElementAt(rand.Next(0, list.Count()));
        }

        public static T[] RandomItems<T>(this IEnumerable<T> items, int take)
        {
            ICollection<T> list = items as ICollection<T> ?? items.ToArray();
            return list.Skip(rand.Next(list.Count-take)).Take(take).ToArray();
        }

        public static IEnumerable<int> RandomSequence(int lenght, int maxValue, bool allowRepeats)
        {
            var sequence = RandomSequence(maxValue);
            if (!allowRepeats)
                sequence = sequence.Distinct();
            return sequence.Take(lenght);
        }

        private static IEnumerable<int> RandomSequence(int maxValue)
        {
            while (true) yield return rand.Next(maxValue);
        }
    }

    public class RandomTextGenerator
    {
        private readonly string[] texts = "Childharold,Decameron,Faust,Inderfremde,Lebateauivre,Lemasque,Loremipsum,Nagyonfaj,Omagyar,Robinsonokruso,Theraven,Tierrayluna".Split(',');

        public string RandomText()
        {
            return texts.RandomItem();
        }

        public string Paragraph(string @from, int count = 20)
        {
            return Open(from).RandomItems(count).Aggregate((s, s1) => s + " " + s1);
        }

        public string Word(string @from, int minLength = 2)
        {
            return Open(from).Where(w => w.Length >= minLength).RandomItem();
        }

        private IEnumerable<string> Open(string @from)
        {
            if(!texts.Contains(@from))
                throw new ArgumentException(string.Format("The text '{0}' was unknown.", @from),"from");

            Debug.Assert(LoremIpsums.ResourceManager != null, "LoremIpsums.ResourceManager != null");

            string text = LoremIpsums.ResourceManager.GetString(@from, LoremIpsums.Culture);
            Debug.Assert(text != null, "text != null");

            return text.Split(new []{' '},StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] Words(string @from, int minLength = 2, int count = 20)
        {
            HashSet<string> unique = new HashSet<string>(Open(from).Where(w => w.Length >= minLength));
            return Enumerable.Repeat("", count)
                .Select(s => unique.RandomItem())
                .ToArray();
        }
    }

}
