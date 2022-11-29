﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Searching;
using DotJEM.Json.Index.Test.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    public class SearchDatesTests
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

        // Dates in this range is 2000-01-01 to 2002-09-27

        [Test]
        public void Search_FixedRange()
        {
            ISearchResult result = index.Search("created: [2000-01-10 TO 2000-01-14]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public void Search_Fixed2002ToInfinite()
        {
            ISearchResult result = index.Search("created: [2002-09-20 TO *]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(8));
        }

        [Test]
        public void Search_Fixed2001ToInfinite_ReturnsFullYear()
        {
            ISearchResult result = index.Search("created: [2001-09-28 TO *]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(365));
        }

        [Test]
        public void Search_InfiniteToFixed()
        {
            ISearchResult result = index.Search("created: [* TO 2000-01-20]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(19));
        }

        [Test]
        public void Search_RelativeRange()
        {
            ISearchResult result = index.Search("updated: [+2days TO +7days]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public void Search_RelativeRangeWithNow()
        {
            ISearchResult result = index.Search("updated: [Now+2days TO Now+7days]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public void Search_RelativeRangeFromNow()
        {
            ISearchResult result = index.Search("updated: [Now TO +7days]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(7));
        }

        [Test]
        public void Search_RelativeRangeToInfinite()
        {
            ISearchResult result = index.Search("updated: [* TO +2d]");
            result.Any();

            Assert.That(result.TotalCount, Is.EqualTo(2));
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
                    area = "Test"
                })
                .Select(JObject.FromObject)
                .Select(v => decorators[rnd.Next(decorators.Length - 1)].Decorate(v, rnd));

        }

    }
}

