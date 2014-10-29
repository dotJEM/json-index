using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Json.Index.Test.Util;
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
        public void TestFixtureSetUp()
        {
            var config = index.Configuration;
            config.SetTypeResolver("Type")
                .ForAll().SetIdentity("Id");

            //config.For("ship").Index("number", As.Long());

            //config.ForAll().Index("Number", As.Term());

            //config.For("Car").Index("Model", As.Default().Analyzed(Field.Index.NOT_ANALYZED))
            //                 .Query("Model", Using.Term().When.Always());

            ILuceneWriter writer = index.Writer;
            writer.Write(JObject.FromObject(new { Id = 1, Type = "Person", Name = "John", LastName = "Doe" }));
            writer.Write(JObject.FromObject(new { Id = 2, Type = "Person", Name = "Peter", LastName = "Pan" }));
            writer.Write(JObject.FromObject(new { Id = 3, Type = "Person", Name = "Alice" }));

            writer.Write(JObject.FromObject(new { Id = 4, Type = "Car", Brand = "Ford", Model = "Mustang", Number = 5 }));
            writer.Write(JObject.FromObject(new { Id = 5, Type = "Car", Brand = "Dodge", Model = "Charger", Number = 10 }));
            writer.Write(JObject.FromObject(new { Id = 6, Type = "Car", Brand = "Chevrolet", Model = "Camaro", Number = 15 }));

            writer.Write(JObject.FromObject(new { Id = 7, Type = "Flower", Name = "Lilly", Meaning = "Majesty", Number = 5 }));
            writer.Write(JObject.FromObject(new { Id = 8, Type = "Flower", Name = "Freesia", Meaning = "Innocence", Number = 10 }));
            writer.Write(JObject.FromObject(new { Id = 9, Type = "Flower", Name = "Aster", Meaning = "Patience", Number = 15 }));
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
        public void Search_ForMustangWithSpecifiedFields_ReturnsCarMustang()
        {
            List<dynamic> result = index.Searcher.Search("Mustang", "Model".Split(',')).Select(hit=>hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 4, Type: 'Car', Brand: 'Ford', Model: 'Mustang' }"))));
        }

        [Test]
        public void Search_ForMustang_ReturnsCarMustang()
        {
            List<dynamic> result = index.Searcher.Search("Mustang").Select(hit => hit.Json).ToList();
            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 4, Type: 'Car', Brand: 'Ford', Model: 'Mustang' }"))));
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
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 4, Type: 'Car', Brand: 'Ford', Model: 'Mustang' }"))));
        }
    }
}
