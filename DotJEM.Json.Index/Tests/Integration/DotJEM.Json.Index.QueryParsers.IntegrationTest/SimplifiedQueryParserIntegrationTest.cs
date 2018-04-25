using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using DotJEM.Json.Index.TestData;
using DotJEM.Json.Index.TestUtil;
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
        [TestCase("gender: female OR ( (name: Peter OR name: John) AND gender: male)", "DOC_006,DOC_001,DOC_003")]
        [TestCase("male name=Peter", "DOC_001")]
        [TestCase("name=Peter NOT (gender=female)", "")]
        [TestCase("name=P*r", "DOC_001")]
        [TestCase("name = John", "DOC_003")]
        [TestCase("description=\"care coffee\"", "DOC_002")]
        [TestCase("description=\"care much\"", "DOC_002")]
        [TestCase("age > 30", "DOC_004,DOC_003")]
        [TestCase("name IN (Peter, Lars)", "DOC_002,DOC_001")]
        [TestCase("name NOT IN (Peter, Lars)", "DOC_005,DOC_004,DOC_003,DOC_006")]
        [TestCase("name~Poter", "DOC_001")]
        [TestCase("name = * ORDER BY age", "DOC_005,DOC_006,DOC_001,DOC_002,DOC_003,DOC_004")]
        [TestCase("name = * ORDER BY age ASC", "DOC_005,DOC_006,DOC_001,DOC_002,DOC_003,DOC_004")]
        [TestCase("name = * ORDER BY age DESC", "DOC_004,DOC_003,DOC_002,DOC_001,DOC_006,DOC_005")]
        [TestCase("updated > -2days", "DOC_001,DOC_002")]
        [TestCase("name = Peter OR name = John OR num = 10", "DOC_001,DOC_003,DOC_004")]

        [TestCase("$contentType: person AND name = *", "DOC_004,DOC_003,DOC_002,DOC_001,DOC_006,DOC_005")]
        [TestCase("($contentType: person AND name = *) OR ($contenttype: car and foo = x)", "DOC_004,DOC_003,DOC_002,DOC_001,DOC_006,DOC_005")]
        public void Search_Persons_YieldDocuments(string query, string expected)
        {
            bool orderMatters = query.IndexOf("ORDER", StringComparison.OrdinalIgnoreCase) != -1;

            ILuceneJsonIndex index = new TestIndexBuilder()
                .With(TestObjects.Persons)
                .With(services => services.UseSimplifiedLuceneQueryParser())
                .Build().Result;

            IEnumerable<string> keys = index.Search(query).Result.Result.Select(hit => (string)hit.Json.key);
            if (!orderMatters)
            {
                keys = keys.OrderBy(key => key);
                expected = string.Join(",", expected.Split(',').OrderBy(key => key));
            }
            string results = string.Join(",", keys);
            Assert.That(results, Is.EqualTo(expected));
        }

        [TestCase("username: Bret", "1")]
        public void Search_JsonPlaceholder_YieldDocuments(string query, string expected)
        {
            bool orderMatters = query.IndexOf("ORDER", StringComparison.OrdinalIgnoreCase) != -1;

            ILuceneJsonIndex index = new TestIndexBuilder()
                .With(JsonPlaceholder.Albums.Select(data => new TestObject("album", (JObject)data)))
                .With(JsonPlaceholder.Comments.Select(data => new TestObject("comment", (JObject)data)))
                .With(JsonPlaceholder.Photos.Select(data => new TestObject("photo", (JObject)data)))
                .With(JsonPlaceholder.Posts.Select(data => new TestObject("post", (JObject)data)))
                .With(JsonPlaceholder.Todos.Select(data => new TestObject("todo", (JObject)data)))
                .With(JsonPlaceholder.Users.Select(data => new TestObject("user", (JObject)data)))
                .With(services => services.UseSimplifiedLuceneQueryParser())
                .Build().Result;

            IEnumerable<string> keys = index.Search(query).Result.Result.Select(hit => (string)hit.Json.id);
            if (!orderMatters)
            {
                keys = keys.OrderBy(key => key);
                expected = string.Join(",", expected.Split(',').OrderBy(key => key));
            }
            string results = string.Join(",", keys);
            Assert.That(results, Is.EqualTo(expected));
        }

    }


    public class TestObjects
    {
        public static IEnumerable<TestObject> Persons
        {
            get
            {
                string contentType = "person";

                yield return (contentType,
                    new
                    {
                        key = "DOC_001",
                        name = "Peter",
                        gender = "male",
                        age = 20,
                        created = 7.Days().Ago(),
                        updated = 1.Days().Ago(),
                        description = "Peter pan flies to Never Never land!",
                        num = new JArray(10, 20)
                    });
                
                yield return (contentType,
                    new
                    {
                        key = "DOC_002",
                        name = "Lars",
                        gender = "male",
                        age = 30,
                        created = 6.Days().Ago(),
                        updated = 2.Days().Ago(),
                        description = "Lars does not care much for coffee, so he takes a beer!",
                        num = new JArray(50, 100)
                    });

                yield return (contentType, new
                {
                    key = "DOC_003",
                    name = "John",
                    gender = "male",
                    age = 40,
                    created = 7.Days().Ago(),
                    updated = 3.Days().Ago(),
                    description = "John went to the store to get some new shoes!",
                    num = new JArray(1, 2)
                });

                yield return (contentType, new
                {
                    key = "DOC_004",
                    name = "James",
                    gender = "male",
                    age = 50,
                    created = 8.Days().Ago(),
                    updated = 4.Days().Ago(),
                    description = "James is a real crook!",
                    num = new JArray(5, 10)
                });

                yield return (contentType, new
                {
                    key = "DOC_005",
                    name = "Erik",
                    gender = "male",
                    age = 8,
                    created = 9.Days().Ago(),
                    updated = 5.Days().Ago(),
                    description = "Erik likes to play with legos!",
                    num = new JArray(50, 100)
                });

                yield return (contentType, new
                {
                    key = "DOC_006",
                    name = "Sarah",
                    gender = "female",
                    age = 9,
                    created = 9.Days().Ago(),
                    updated = 5.Days().Ago(),
                    description = "Sarah makes lovely cakes!",
                    num = new JArray(50, 100)
                });

            }
        }
    }
}