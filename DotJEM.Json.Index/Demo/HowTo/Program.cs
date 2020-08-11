using System;
using System.Diagnostics.CodeAnalysis;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Contexts;
using DotJEM.Json.Index.Documents.Strategies;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;

namespace HowTo
{
    class Program
    {
        static void Main(string[] args)
        {
            LuceneIndexContextBuilder builder = new LuceneIndexContextBuilder();

            

            ////TODO: (jmd 2018-05-01) This is unusually high, but we have reached the max of 1024, we should consider some
            //// improvements that could limit number of fields targeted in searches.
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

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        static void LuceneJsonIndexBuilderExample()
        {
            LuceneJsonIndexBuilder index = new LuceneJsonIndexBuilder("indexName");
            index.Configuration.Version = LuceneVersion.LUCENE_48;
            index.Configuration.Analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);


            IFieldStrategyCollectionBuilder builder = new FieldStrategyCollectionBuilder();
            builder.Use<StringFieldStrategy>().For(null);
            builder.Use<ExpandedDateTimeFieldStrategy>().For(new TypeFilter<DateTime>());
            builder.Use<ExpandedDateTimeFieldStrategy>().For(new TypeFilter(JTokenType.Date));
            builder.Use<ExpandedDateTimeFieldStrategy>().For<DateTime>();
            builder.Use<ExpandedDateTimeFieldStrategy>().For(new PatternFilter());
            builder.Use<ExpandedDateTimeFieldStrategy>().For(new PatternFilter());

            //builder.For("contentType").Use<ExpandedDateTimeFieldStrategy>().On("field");
            //builder.Use<IdentityFieldStrategy>().For("*", "$created");

            dynamic builder2 = builder;
            //builder2.When(json => json.contentType == "notification").Use<IdentityFieldStrategy>().For("id");
            //builder2.When("contentType", field => field == "notification").Use<IdentityFieldStrategy>().For("id");
            builder2.When("contentType", builder2.Is("notification")).Use<IdentityFieldStrategy>().For("id");

            index.Services.Use<IFieldStrategyCollection>(builder.Build);
        }
    }
}
