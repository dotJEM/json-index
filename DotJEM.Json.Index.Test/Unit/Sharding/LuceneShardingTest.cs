using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Sharding;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Json.Index.Test.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit.Sharding
{
    [TestFixture]
    public class LuceneShardingTest
    {
        private readonly IJsonIndexContext context = new LuceneJsonIndexContext();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var index = context.Open("test");
            index.Write(TestObjects());
        }

        [Test]
        public void Search_ForMustangWithSpecifiedFields_Returns()
        {
            var index = context.Open("test");

            index.Search("contentType: Person");
        }


        private IEnumerable<JObject> TestObjects()
        {
            int count = 0;
            Func<Guid> nextId = () => new Guid("00000000-0000-0000-0000-" + (count++).ToString("000000000000"));


            yield return JObject.FromObject(new { id = nextId(), contentType = "person", Name = "John", LastName = "Doe", created = DateTime.Now, area= "test" });
            yield return JObject.FromObject(new { id = nextId(), contentType = "person", Name = "Peter", LastName = "Pan", created = DateTime.Now, area = "test" });
            yield return JObject.FromObject(new { id = nextId(), contentType = "person", Name = "Alice", created = DateTime.Now, area = "test" });

            yield return JObject.FromObject(new { id = nextId(), contentType = "car", Brand = "Ford", Model = "Mustang", Number = 5, created = DateTime.Now, area = "test" });
            yield return JObject.FromObject(new { id = nextId(), contentType = "car", Brand = "Dodge", Model = "Charger", Number = 10, created = DateTime.Now, area = "test" });
            yield return JObject.FromObject(new { id = nextId(), contentType = "car", Brand = "Chevrolet", Model = "Camaro", Number = 15, created = DateTime.Now, area = "test" });

            yield return JObject.FromObject(new { id = nextId(), contentType = "flower", Name = "Lilly", Meaning = "Majesty", Number = 5, created = DateTime.Now, area = "test" });
            yield return JObject.FromObject(new { id = nextId(), contentType = "flower", Name = "Freesia", Meaning = "Innocence", Number = 10, created = DateTime.Now, area = "test" });
            yield return JObject.FromObject(new { id = nextId(), contentType = "flower", Name = "Aster", Meaning = "Patience", Number = 15, created = DateTime.Now, area = "test" });
        }
        
    }
}
