using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit
{
    [TestFixture]
    public class JObjectEnumeratorTest
    {
        [Test]
        public void Test()
        {
            var enumerator = new JObjectEnumerator();

            var json = JObject.Parse("{ simple: 'test', complex: { child: 42 }, array: [ 'str', 'str2', { ups: 45 } ] }");


            foreach (JNode node in enumerator.Enumerate(json, "ship"))
            {
                Console.WriteLine(node.Path + " = " + node.Type);
            }

        }

    }
}
