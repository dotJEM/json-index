using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.NUnit.Json;
using DotJEM.NUnit.Json.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    [TestFixture]
    public class LuceneSearcherSimpleDataTest
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();

        [TestFixtureSetUp]
        //[Test]
        public void TestFixtureSetUp()
        {
            var config = index.Configuration;
            config
                .SetTypeResolver("Type")
                .SetAreaResolver("Area")
                .ForAll()
                .SetIdentity("Id");
            
            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000001"), Type = "Person", Name = "John", LastName = "Doe", Area = "Foo" }));
            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000002"), Type = "Person", Name = "Peter", LastName = "Pan", Area = "Foo" }));
            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000003"), Type = "Person", Name = "Alice", Area = "Foo" }));

            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000004"), Type = "Car", Brand = "Ford", Model = "Mustang", Number = 5, Area = "Foo" }));
            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000005"), Type = "Car", Brand = "Dodge", Model = "Charger", Number = 10, Area = "Foo" }));
            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000006"), Type = "Car", Brand = "Chevrolet", Model = "Camaro", Number = 15, Area = "Foo" }));

            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000007"), Type = "Flower", Name = "Lilly", Meaning = "Majesty", Number = 5, Area = "Foo" }));
            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000008"), Type = "Flower", Name = "Freesia", Meaning = "Innocence", Number = 10, Area = "Foo" }));
            index.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000009"), Type = "Flower", Name = "Aster", Meaning = "Patience", Number = 15, Area = "Foo" }));
            index.Commit();
        }

        [Test]
        public void Search_ForMustangWithSpecifiedFields_Returns()
        {
            Query query = new TermQuery(new Term("Number", NumericUtils.LongToPrefixCoded(5)));
            //Query query = NumericRangeQuery.NewLongRange("Number", 5, 5, true, true);

            //List<dynamic> result = index.CreateSearcher().Search("Number:5").Select(hit => hit.Json).ToList();
            List<dynamic> result = index.Searcher.Search(query).Select(hit => hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(2));
        }

        [Test]
        public void Search_Id4_ReturnsCarMustang()
        {
            List<dynamic> result = index.Searcher.Search("Id: 00000000-0000-0000-0000-000000000004").Select(hit => hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(JsonHas.Properties("{ Id: '00000000-0000-0000-0000-000000000004', Type: 'Car', Brand: 'Ford', Model: 'Mustang' }")));
        }

        [Test]
        public void Search_ForMustang_ReturnsCarMustang()
        {
            List<dynamic> result = index.Searcher.Search("Mustang").Select(hit => hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(JsonHas.Properties("{ Id: '00000000-0000-0000-0000-000000000004', Type: 'Car', Brand: 'Ford', Model: 'Mustang' }")));
        }

        [Test]
        public void Search_ForMustang3_ReturnsCarMustang()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new WildcardQuery(new Term("Model", "Mustang*")), Occur.SHOULD);
            query.Add(new FuzzyQuery(new Term("Model", "Mustang")), Occur.SHOULD);

            List<dynamic> result = index.Searcher.Search(query).Select(hit => hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(JsonHas.Properties("{ Id: '00000000-0000-0000-0000-000000000004', Type: 'Car', Brand: 'Ford', Model: 'Mustang' }")));
        }
    }
}
