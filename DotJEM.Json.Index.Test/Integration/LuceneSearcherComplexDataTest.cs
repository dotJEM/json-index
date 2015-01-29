using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Configuration.IdentityStrategies;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Json.Index.Test.Util;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;

namespace DotJEM.Json.Index.Test.Integration
{
    [TestFixture]
    public class LuceneSearcherComplexDataTest
    {
        private readonly IStorageIndex index = new LuceneStorageIndex("C:\\temp\\test-index");

        private readonly DateTime now = DateTime.Now;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = index.Configuration;
            config.SetTypeResolver("Type")
                .ForAll().SetIdentity(new FieldIdentityStrategy("Id"));

            config.For("Person")
                .Index("Created", As.DateTime())
                .Query("Created", Using.Range().When.Specified());

            config.For("Person")
                //.Index("Age", As.Integer())
                .Query("Age", Using.Range().When.Specified());

            config.For("Person")
                //.Index("Special.Rank", As.Integer())
                .Query("Special.Rank", Using.Range().When.Specified());

            //TODO: Compound fields something like: ???
            //config.For("Person")
            //    .Index("Skills", As.Compound(item => item.Brand + ":" item.Model))


            config.For("Person")
                //.Index("Skills.Level", As.Integer())
                .Query("Skills.Level", Using.Range().When.Specified());

            //TODO: Move to test file.
            //Note: Use http://www.jsoneditoronline.org/ to edit.
            JArray raw =
                JArray.Parse(
                    @"[{'Id':1,'Type':'Person','Name':'John','LasName':'Doe','Age':24,'Created':'2014-03-30 15:18:53Z','Special':{'Status':'Presumed Dead','Rank':15},'Skills':[{'Name':'.NET','Level':4},{'Name':'JavaScript','Level':3},{'Name':'Lucene','Level':2}]},{'Id':2,'Type':'Person','Name':'Peter','LasName':'Pan','Age':12,'Created':'2014-03-20 15:18:53Z','Special':{'Status':'Fictional','Rank':10},'Skills':[{'Name':'Flying','Level':5},{'Name':'JavaScript','Level':1},{'Name':'Lucene','Level':4}]},{'Id':3,'Type':'Person','Name':'Alice','LasName':'Pan','Age':16,'Created':'2014-03-10 15:18:53Z','Special':{'Status':'Fictional','Rank':20},'Friends':[{'Name':'White Rabbit','Gender':'Male','Species':'Rabbit'},{'Name':'Dodo','Gender':'Male','Species':'Dodo'},{'Name':'Lory ','Gender':'Female','Species':'Lory '},{'Name':'Eaglet','Gender':'Female','Species':'Eagle'}]}]");

            ILuceneWriter writer = index.Writer;
            foreach (JObject child in raw.Children<JObject>())
            {
                writer.Write(child);
            }

            //writer.WriteAll(raw.Children<JObject>());

            writer.Write(JObject.FromObject(new { Id = 4, Type = "Car", Brand = "Ford", Model = "Mustang", Created = now, Motor = new { Created = now } }));
            writer.Write(
                JObject.FromObject(
                new {Id = 5, Type = "Car", Brand = "Dodge", Model = "Charger", Created = now.AddDays(1), Motor = new { Created = now.AddDays(1) }}));
            writer.Write(
                JObject.FromObject(
                    new { Id = 6, Type = "Car", Brand = "Chevrolet", Model = "Camaro", Created = now.AddDays(2), Motor = new { Created = now.AddDays(2) } }));

            writer.Write(
                JObject.FromObject(
                    new { Id = 7, Type = "Flower", Name = "Lilly", Meaning = "Majesty", Created = now.AddDays(3), Motor = new { Created = now.AddDays(3) } }));
            writer.Write(
                JObject.FromObject(
                    new { Id = 8, Type = "Flower", Name = "Freesia", Meaning = "Innocence", Created = now.AddDays(4), Motor = new { Created = now.AddDays(4) } }));
            writer.Write(
                JObject.FromObject(
                    new { Id = 9, Type = "Flower", Name = "Aster", Meaning = "Patience", Created = now.AddDays(5), Motor = new { Created = now.AddDays(5) } }));
        }

        [Test]
        public void Search_()
        {
            List<dynamic> result = index.Searcher
                .Search("Brand:Dodge")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result,
                Has.Count.EqualTo(1));
        }

        [Test]
        public void Search_1()
        {
            List<dynamic> result = index.Searcher
                .Search("Dodge")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result,
                Has.Count.EqualTo(1));
        }

        [Test]
        public void Search_2()
        {
            List<dynamic> result = index.Searcher
                .Search(JObject.Parse("{ Brand: 'Dodge' }"))
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result,
                Has.Count.EqualTo(1));
        }

        [Test]
        public void Search_ForAllPersons_ReturnsJohnPeterAndAlice()
        {
            List<dynamic> result = index.Searcher
                .Search("Person", "Type".Split(','), "Person")
                .Select(hit => hit.Json)
                .ToList();


            Assert.That(result,
                Has.Count.EqualTo(3) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 1, Name: 'John' }"))) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 2, Name: 'Peter' }"))) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 3, Name: 'Alice' }")))
                );
        }

        [Test]
        public void Search_ForAllPersonsUnder20_ReturnsJohnPeterAndAlice()
        {
            List<dynamic> result = index.Searcher
                .Search("0-20", "Age".Split(','), "Person")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result,
                Has.Count.EqualTo(2) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 2, Name: 'Peter' }"))) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 3, Name: 'Alice' }")))
                );
        }

        //RangeQuery start
        [Test]
        public void Search_dateTimeRange_returnFordMustang()
        {
            List<dynamic> result = index.Searcher
                .Search("Created: [" + DateTimeString(now,-1) + " TO " + DateTimeString(now, 1) + "]")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        public void Search_dateTimeRange_returnFourObjects()
        {
            List<dynamic> result = index.Searcher
                .Search("Created: [" + DateTimeString(now, -1) + " TO " + DateTimeString(now, 4) + "]")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result, Has.Count.EqualTo(4));
        }

        [Test]
        public void Search_dateTimeRangeWithoutTime_returnThreeObjects()
        {
            List<dynamic> result = index.Searcher
                .Search("Created: [" + DateTimeString(now, -1, true) + " TO " + DateTimeString(now, 3, true) + "]")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result, Has.Count.EqualTo(3));
        }

        [Test]
        public void Search_dateTimeRangeNestedSchemas_returnThreeObjects()
        {
            List<dynamic> result = index.Searcher
                .Search("Motor.Created: [" + DateTimeString(now, -1) + " TO " + DateTimeString(now, 3) + "]")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result, Has.Count.EqualTo(3));
        }

        //RangeQuery end

        public static string DateTimeString(DateTime date, int daysToAdd, bool returnOnlyDate = false)
        {
            return returnOnlyDate ? date.AddDays(daysToAdd).ToString("d", CultureInfo.InvariantCulture) : date.AddDays(daysToAdd).ToString("s", CultureInfo.InvariantCulture);
        }

        [Test, Ignore("This currently fails as if finds two objects, It is still debateable if we wan't this test to work or if it's a matter of structuring data differently.")]
        public void Search_ForAllPersonsWithReturnsJohnPeterAndAlice()
        {
            JObject query = JObject.Parse("{ Skills: { Name: 'Lucene', Level: '3-5' } }");

            List<dynamic> result = index.Searcher
                .Search(query, "Person")
                .Select(hit => hit.Json)
                .ToList();

            Assert.That(result,
                Has.Count.EqualTo(1) &
                Has.Exactly(1).That(HAS.JProperties(JObject.Parse("{ Id: 2, Name: 'Peter' }")))
                );
        }
    }
}