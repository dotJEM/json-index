using System;
using System.Diagnostics;
using System.Linq;
using DotJEM.Json.Index.Test.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    [TestFixture]
    public class LuceneSearcherTests
    {
        private readonly IStorageIndex index = new LuceneStorageIndex("C:\\temp\\test-index");
        //private readonly IStorageIndex index = new LuceneStorageIndex();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            TestIndexBuilder builder = new TestIndexBuilder(index);
            builder
                .Document(db => db.Set("date", new DateTime(2014, 9, 10, 12, 0, 0)))
                .Document(db => db.Set("date", new DateTime(2014, 9, 11, 12, 0, 0)))
                .Document(db => db.Set("date", new DateTime(2014, 9, 12, 12, 0, 0)))
                .Document(db => db.Set("date", new DateTime(2014, 9, 13, 12, 0, 0)))
                .Build();
        }

        [TestCase("2014-09-10 11:00", "2014-09-10 13:00", 1)]
        [TestCase("2014-09-10 11:00", "2014-09-11 13:00", 2)]
        [TestCase("2014-09-10 11:00", "2014-09-11 11:00", 1)]
        [TestCase("2014-09-10 11:00", "2014-09-13 13:00", 4)]
        public void Search_DateRanges_ReturnsResultsWitinRanges(string from, string to, int results)
        {
            Query query = NumericRangeQuery
                .NewLongRange("date", NumericUtils.PRECISION_STEP_DEFAULT, DateTime.Parse(from).Ticks, DateTime.Parse(to).Ticks, true, true);

            var result = index.Search("date: [" + from.Replace(' ', 'T') + " TO " + to.Replace(' ', 'T') + "]").All().ToArray();
            Assert.That(result, Has.Length.EqualTo(results));
        }

        [TestCase("date: [2014-09-10T11:00 TO 2014-09-10T13:00]", 1)]
        [TestCase("date: [2014-09-10T11:00 TO 2014-09-11T13:00]", 2)]
        [TestCase("date: [2014-09-10T11:00 TO 2014-09-11T11:00]", 1)]
        [TestCase("date: [2014-09-10T11:00 TO 2014-09-13T13:00]", 4)]
        public void Search_DateRangesByString_ReturnsResultsWitinRanges(string query, int results)
        {
            var result = index.Search(query).All().ToArray();
            Assert.That(result, Has.Length.EqualTo(results));
        }

        [TestCase("2014-09-10 11:00", 4)]
        [TestCase("2014-09-11 11:00", 3)]
        [TestCase("2014-09-12 11:00", 2)]
        [TestCase("2014-09-13 11:00", 1)]
        public void Search_DateAbove_ReturnsResultsWitinRanges(string date, int results)
        {
            Query query = NumericRangeQuery
                .NewLongRange("date", NumericUtils.PRECISION_STEP_DEFAULT, DateTime.Parse(date).Ticks, null, true, true);

            var result = index.Search(query).All().ToArray();
            Assert.That(result, Has.Length.EqualTo(results));
        }

        [TestCase("2014-09-10 13:00", 1)]
        [TestCase("2014-09-11 13:00", 2)]
        [TestCase("2014-09-12 13:00", 3)]
        [TestCase("2014-09-13 13:00", 4)]
        public void Search_DateBelow_ReturnsResultsWitinRanges(string date, int results)
        {
            Query query = NumericRangeQuery
                .NewLongRange("date", NumericUtils.PRECISION_STEP_DEFAULT, null, DateTime.Parse(date).Ticks, true, true);

            var result = index.Search(query).All().ToArray();
            Assert.That(result, Has.Length.EqualTo(results));
        }
    }
}
