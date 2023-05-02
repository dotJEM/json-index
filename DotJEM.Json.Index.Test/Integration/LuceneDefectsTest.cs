using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    [TestFixture]
    public class LuceneDefectsTest
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            var config = index.Configuration;
            config
                .SetTypeResolver("type")
                .SetAreaResolver("term")
                .ForAll()
                .SetIdentity("id");

            Write("{ id: '00000000-0000-0000-0000-000000000006', term: 'AAA', type: 'port', locode: 'MASFI' }");
            Write("{ id: '00000000-0000-0000-0000-000000000002', term: 'AAA', type: 'port', locode: 'MA888' }");
            Write("{ id: '00000000-0000-0000-0000-000000000005', term: 'AAA', type: 'port', locode: 'MA6KN' }");
            Write("{ id: '00000000-0000-0000-0000-000000000003', term: 'BBB', type: 'port', locode: 'MASUR' }");
            Write("{ id: '00000000-0000-0000-0000-000000000007', term: 'BBB', type: 'port', locode: 'DKAAR' }");
            Write("{ id: '00000000-0000-0000-0000-000000000001', term: 'BBB', type: 'port', locode: 'DEGER' }");
            Write("{ id: '00000000-0000-0000-0000-000000000008', term: 'CCC', type: 'port', locode: 'NOBAR' }");
            Write("{ id: '00000000-0000-0000-0000-000000000004', term: 'CCC', type: 'port', locode: 'FRRAS' }");
            Write("{ id: '00000000-0000-0000-0000-000000000009', term: 'CCC', type: 'port', locode: 'IT888' }");
            index.Commit();

        }

        [Test]
        public void Search_ForMustangWithSpecifiedFields_Returns()
        {
            dynamic[] result = index
                .Search("locode: MA*")
                .Documents.ToArray();
            Assert.That(result, Has.Length.EqualTo(4));
        }

        private void Write(string json)
        {
            JObject jObject = JObject.Parse(json);
            index.Write(jObject);
        }
    }
}