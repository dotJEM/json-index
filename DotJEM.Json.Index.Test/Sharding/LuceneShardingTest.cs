using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Sharding;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Sharding
{
    [TestFixture]
    public class LuceneShardingTest
    {
        private readonly IJsonIndexContext context = new LuceneJsonIndexContext();

        [Test]
        public void T001_Write_ForMustangWithSpecifiedFields_Returns()
        {
            var index = context.Open("test");
            index.Write(TestJson());
        }

        [Test]
        public void T002_Search_ForMustangWithSpecifiedFields_Returns()
        {
            var index = context.Open("test");

            Query query = new TermQuery(new Term("contentType", "person"));


            var result = index.Search(query);//"contentType: person"

            Assert.That(result.Result.TotalHits, Is.EqualTo(3));
        }

        private IEnumerable<JObject> TestJson()
        {
            return TestObjects().Select(JObject.Parse);
        }

        private IEnumerable<string> TestObjects()
        {
            int count = 0;
            Func<string> nextId = () => $"'{new Guid("00000000-0000-0000-0000-" + (count++).ToString("000000000000"))}'";

            yield return $"{{ id:{nextId()}, contentType: 'person', name: 'John', surname: 'Doe', created: '{DateTime.Now:o}', area: 'test' }}";
            yield return $"{{ id:{nextId()}, contentType: 'person', name: 'Peter', surname: 'Pan', created: '{DateTime.Now:o}', area: 'test' }}";
            yield return $"{{ id:{nextId()}, contentType: 'person', name: 'Alice', created: '{DateTime.Now:o}', area: 'test' }}";

            yield return $"{{ id:{nextId()}, contentType: 'car', brand: 'Ford', surname: 'Mustang', num: 5, created: '{DateTime.Now:o}', area: 'test' }}";
            yield return $"{{ id:{nextId()}, contentType: 'car', brand: 'Dodge', surname: 'Charger', num: 10, created: '{DateTime.Now:o}', area: 'test' }}";
            yield return $"{{ id:{nextId()}, contentType: 'car', brand: 'Chevrolet', surname: 'Camaro', num: 15, created: '{DateTime.Now:o}', area: 'test' }}";

            yield return $"{{ id:{nextId()}, contentType: 'flower', name: 'Lilly', meaning: 'Majesty', num: 5, created: '{DateTime.Now:o}', area: 'test' }}";
            yield return $"{{ id:{nextId()}, contentType: 'flower', name: 'Freesia', meaning: 'Innocence', num: 10, created: '{DateTime.Now:o}', area: 'test' }}";
            yield return $"{{ id:{nextId()}, contentType: 'flower', name: 'Aster', meaning: 'Patience', num: 15, created: '{DateTime.Now:o}', area: 'test' }}";
        }

        
        
    }
}
