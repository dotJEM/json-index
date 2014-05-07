using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    [TestFixture]
    public class LuceneSearcherTests
    {
        private readonly IJsonIndex index = new LuceneJsonIndex();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {

        }

        [Test]
        public void JsonNetTest()
        {
            JObject json = new JObject();
            json["date"] = DateTime.Now;
            json["long"] = long.MaxValue;

            Debug.WriteLine("date type: " + json["date"].Type);
            Debug.WriteLine("long type: " + json["long"].Type);



        }

        [Test]
        public void JsonNetParesTest()
        {
            string j = "{ \"date\": \"2014-04-14T09:23:27.9420589+02:00\", \"long\": 9223372036854775807 }";
            JObject json = JObject.Parse(j);

            Debug.WriteLine("date type: " + json["date"].Type);
            Debug.WriteLine("long type: " + json["long"].Type);

            Debug.WriteLine(json.ToString());


        }
    }
}
