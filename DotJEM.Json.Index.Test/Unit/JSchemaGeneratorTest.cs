using System;
using DotJEM.Json.Index.Schema;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit
{
    [TestFixture]
    public class JSchemaGeneratorTest
    {
        [Test]
        public void Test()
        {
            var generator = new JSchemaGenerator(new Uri("http://www.dotjem.com/api/schema"));
            var json = JObject.Parse("{ simple: 'test', complex: { child: 42 }, array: [ 'str', 'str2', { ups: 45 } ] }");
            var schema = generator.Generate(json);

            Console.WriteLine(JObject.FromObject(schema));

        }
    }
}