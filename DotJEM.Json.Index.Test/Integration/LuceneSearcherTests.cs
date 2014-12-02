using System;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    public class TestIndexBuilder
    {
        private readonly IStorageIndex index;
        private int count;

        public TestIndexBuilder() : this(new LuceneStorageIndex())
        {
        }

        public TestIndexBuilder(IStorageIndex index)
        {
            this.index = index;
            index.Configuration.SetTypeResolver("contentType").ForAll().SetIdentity("id");
        }

        public TestIndexBuilder Document(Func<TestDocumentBuilder, TestDocumentBuilder> build)
        {
            return Document("Document", build);
        }

        public TestIndexBuilder Document(string contentType, Func<TestDocumentBuilder, TestDocumentBuilder> build)
        {
            index.Write(build(new TestDocumentBuilder(contentType, ToGuid(count++))).Build());
            return this;
        }

        public static Guid ToGuid(long value)
        {
            byte[] guidData = new byte[16];
            Array.Copy(BitConverter.GetBytes(value), guidData, 8);
            return new Guid(guidData);
        }

        public static long ToLong(Guid guid)
        {
            if (BitConverter.ToInt64(guid.ToByteArray(), 8) != 0)
                throw new OverflowException("Value was either too large or too small for an Int64.");
            return BitConverter.ToInt64(guid.ToByteArray(), 0);
        }

        public TestIndexBuilder Insert(TestDocumentBuilder testDocumentBuilder, JObject json)
        {
            return this;
        }

        public IStorageIndex Build()
        {
            return index;
        }
    }

    public class TestDocumentBuilder
    {
        private readonly JObject json = new JObject();

        private string contentType;
        private Guid id;

        public Guid Id
        {
            get { return id; }
            set
            {
                id = value;
                json["id"] = value;
            }
        }

        public string ContentType
        {
            get { return contentType; }
            set
            {
                contentType = value;
                json["contentType"] = value;
            }
        }

        public TestDocumentBuilder(string contentType, Guid id)
        {
            //TODO: Dummy Data based on ContentType

            ContentType = contentType;
            Id = id;
        }

        public TestDocumentBuilder Set(string key, dynamic obj)
        {
            json[key] = obj;
            return this;
        }

        public JObject Build()
        {
            return json;
        }
    }

    [TestFixture]
    public class LuceneSearcherTests
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var builder = new TestIndexBuilder(index);
            builder
                .Document(db => db.Set("date", new DateTime(2014, 9, 10, 12, 0, 0)))
                .Document(db => db.Set("date", new DateTime(2014, 9, 11, 12, 0, 0)))
                .Document(db => db.Set("date", new DateTime(2014, 9, 12, 12, 0, 0)))
                .Document(db => db.Set("date", new DateTime(2014, 9, 13, 12, 0, 0)));
        }

        [TestCase("2014-09-10 11:00", "2014-09-10 13:00", 1)]
        [TestCase("2014-09-10 11:00", "2014-09-11 13:00", 2)]
        [TestCase("2014-09-10 11:00", "2014-09-11 11:00", 1)]
        [TestCase("2014-09-10 11:00", "2014-09-13 13:00", 4)]
        public void Search_DateRanges_ReturnsResultsWitinRanges(string from, string to, int results)
        {
            Query query = NumericRangeQuery
                .NewLongRange("date", NumericUtils.PRECISION_STEP_DEFAULT, DateTime.Parse(from).Ticks, DateTime.Parse(to).Ticks, true, true);

            var result = index.Search(query).All().ToArray();
            Assert.That(result, Has.Length.EqualTo(results));
        }

        [TestCase("2014-09-10 11:00", 4)]
        [TestCase("2014-09-11 11:00", 3)]
        [TestCase("2014-09-12 11:00", 2)]
        [TestCase("2014-09-13 11:00", 1)]
        public void Search_DateAbove_ReturnsResultsWitinRanges(string date, int results)
        {
            Query query = NumericRangeQuery
                .NewLongRange("date", NumericUtils.PRECISION_STEP_DEFAULT, DateTime.Parse(date).Ticks, null, true, true);

            var result = index.Search(query).All().ToArray();
            Assert.That(result, Has.Length.EqualTo(results));
        }

        [TestCase("2014-09-10 13:00", 1)]
        [TestCase("2014-09-11 13:00", 2)]
        [TestCase("2014-09-12 13:00", 3)]
        [TestCase("2014-09-13 13:00", 4)]
        public void Search_DateBelow_ReturnsResultsWitinRanges(string date, int results)
        {
            Query query = NumericRangeQuery
                .NewLongRange("date", NumericUtils.PRECISION_STEP_DEFAULT, null, DateTime.Parse(date).Ticks, true, true);

            var result = index.Search(query).All().ToArray();
            Assert.That(result, Has.Length.EqualTo(results));
        }
    }
}
