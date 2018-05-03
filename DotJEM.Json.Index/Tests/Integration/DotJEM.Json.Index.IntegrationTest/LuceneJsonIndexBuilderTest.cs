using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Documents.Strategies;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace DotJEM.Json.Index.IntegrationTest
{
    [TestFixture]
    public class LuceneJsonIndexBuilderTest
    {
        [Test]
        public void Test()
        {
            ILuceneJsonIndexBuilder builder = new LuceneJsonIndexBuilder("main");

            builder.Services.Use<IFieldStrategyCollection>(() => new FieldStrategyCollectionBuilder().Build());

            //BooleanQuery.MaxClauseCount = 65535;
            //index.Configuration.SetTypeResolver("contentType");
            //index.Configuration.SetRawField("$raw");
            //index.Configuration.SetScoreField("$score");
            //index.Configuration.SetIdentity("id");

            //index.Configuration.ForAll().Index("users.identifier", As.Term);
            //index.Configuration.ForAll().Index("groups.identifier", As.Term);

            //index.Configuration.ForAll().Index("updatedBy.user", As.Term);
            //index.Configuration.ForAll().Index("createdBy.user", As.Term);

            //index.Configuration.ForAll().Index("owner", As.Term);

            //index.Configuration.For("ship").Index("imo", As.Term);

            //index.Configuration.SetSerializer(new ZipJsonDocumentSerializer());
        }
    }
}
