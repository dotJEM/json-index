using System.Collections.Generic;
using System.Threading.Tasks;
using DotJEM.Json.Index.Diagnostics;
using DotJEM.Json.Index.Documents.Builder;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Documents.Info;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace DotJEM.Json.Index.QueryParsers.Test
{
    [TestFixture]
    public class SimplifiedLuceneQueryParserTest
    {
        [TestCase("value", ""), Explicit("Requires implementation.")]
        public void Parse_Input_Query(string query, string result)
        {
            SimplifiedLuceneQueryParser parser = new SimplifiedLuceneQueryParser(
                new FakeFieldsInformationManager(),  new StandardAnalyzer(LuceneVersion.LUCENE_48, CharArraySet.EMPTY_SET));

            LuceneQueryInfo queryInfo = parser.Parse(query);
            Assert.That(queryInfo.Query.ToString(), Is.EqualTo(result));
        }
    }

    public class FakeFieldsInformationManager : IFieldInformationManager
    {
        public IEventInfoStream InfoStream { get; }
        public IFieldResolver Resolver { get; }
        public IEnumerable<string> ContentTypes { get; }
        public IEnumerable<IIndexableJsonFieldInfo> AllFields { get; }
        public IEnumerable<IIndexableFieldInfo> AllIndexedFields { get; }
        public void Merge(string contentType, IContentTypeInfo info)
        {
            throw new System.NotImplementedException();
        }

        public IIndexableJsonFieldInfo Lookup(string fieldName)
        {
            throw new System.NotImplementedException();
        }

        public IIndexableJsonFieldInfo Lookup(string contentType, string fieldName)
        {
            throw new System.NotImplementedException();
        }
    }
}
