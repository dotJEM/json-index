using System;
using System.Collections.Generic;
using System.Linq;
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
            var generator = new JSchemaGenerator();
            var json = JObject.Parse("{ simple: 'test', complex: { child: 42 }, array: [ 'str', 'str2', { ups: 45 } ] }");
            var schema = generator.Generate(json);

            Console.WriteLine(schema.Serialize("http://dotjem.com/api/schema"));

            IEnumerable<JSchema> schemata = schema.Traverse();
            foreach (string path in schemata.Select(f => f.Field))
            {
                Console.WriteLine(path);
            }
            


        }
    }
}