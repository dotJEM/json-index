using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Test.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration.Types
{
    public class SearchBooleansTests
    {
        private IStorageIndex index = new LuceneStorageIndex();

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            index.Configuration
                .SetTypeResolver("type")
                .SetAreaResolver("area")
                .ForAll().SetIdentity("id");

            index = TestObjects(1000).Aggregate(
                index, (idx, json) => idx.Write(json));
            index.Commit();
        }

        //[TestCase("[0 TO 10]", 200)]
        //[TestCase("[10 TO 20]", 200)]
        //[TestCase("[0 TO 20]", 300)]
        //[TestCase("[0 TO 9]", 100)]
        //[TestCase("[1 TO 9]", 0)]
        //[TestCase("[* TO 40]", 500)]
        //[TestCase("[* TO *]", 1000)]
        //[TestCase("[50 TO *]", 500)]
        //public void Search_Range_ReturnsPercent(string range, int count)
        //{
        //    ISearchResult result = index.Search($"arr.@count: {range}");
        //    result.Any();
        //    Assert.That(result.TotalCount, Is.EqualTo(count));
        //}

        [Test]
        public void Search_Boolean_Debug()
        {
            ISearchResult result = index.Search($"evenDay: true");
            result.Any();
            Assert.That(result.TotalCount, Is.EqualTo(490));
        }

        [Test]
        public void Search_Boolean_FalseDebug()
        {
            ISearchResult result = index.Search($"evenDay: false");
            result.Any();
            Assert.That(result.TotalCount, Is.EqualTo(510));
        }

        [Test]
        public void Search_Boolean_InverseDebug()
        {
            ISearchResult result = index.Search($"evenDay: (*:* NOT true)");
            result.Any();
            Assert.That(result.TotalCount, Is.EqualTo(510));
        }

        [Test]
        public void Search_Boolean_InverseleapYearDebug()
        {
            ISearchResult result = index.Search($"leapYear: true");
            result.Any();
            Assert.That(result.TotalCount, Is.EqualTo(365));
        }

        private IEnumerable<JObject> TestObjects(int count)
        {
            DateTime now = DateTime.Now;
            DateTime fixedDate = new DateTime(2000, 1, 1);
            Random rnd = new Random();

            ITestDataDecorator[] decorators = { new PersonDataDecorator(), new CarDataDecorator(), new AnimalDataDecorator(), new FlowerDataDecorator() };

            return Enumerable
                .Range(0, count)
                .Select(num => new Guid("00000000-0000-0000-0000-" + num.ToString("000000000000")))
                .Select(id => new
                {
                    id,
                    created = fixedDate = fixedDate.AddDays(1),
                    updated = now = now.AddDays(1),
                    area = "Test",
                    evenDay = fixedDate.Day % 2 == 0,
                    unevenDay = fixedDate.Day % 2 == 1,
                    evenMonth = fixedDate.Month % 2 == 0,
                    unevenMonth = fixedDate.Month % 2 == 1,
                    leapYear = DateTime.IsLeapYear(fixedDate.Year)
                })
                .Select(JObject.FromObject)
                .Select(v => decorators[rnd.Next(decorators.Length - 1)].Decorate(v, rnd));

        }

    }
}

