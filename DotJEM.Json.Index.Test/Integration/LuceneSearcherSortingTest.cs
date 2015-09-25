using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Test.Constraints;
using DotJEM.Json.Index.Test.Util;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    [TestFixture]
    public class LuceneSearcherSortingTest
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var config = index.Configuration;
            config
                .SetTypeResolver("type")
                .SetAreaResolver("term")
                .ForAll()
                .SetIdentity("id");


            Write("{ id: '00000000-0000-0000-0000-000000000006', order: 6, date: '2015-01-06T00:00:00Z', term: 'AAA', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000002', order: 2, date: '2015-01-02T00:00:00Z', term: 'AAA', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000005', order: 5, date: '2015-01-05T00:00:00Z', term: 'AAA', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000003', order: 3, date: '2015-01-03T00:00:00Z', term: 'BBB', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000007', order: 7, date: '2015-01-07T00:00:00Z', term: 'BBB', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000001', order: 1, date: '2015-01-01T00:00:00Z', term: 'BBB', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000008', order: 8, date: '2015-01-08T00:00:00Z', term: 'CCC', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000004', order: 4, date: '2015-01-04T00:00:00Z', term: 'CCC', type: 'date' }");
            Write("{ id: '00000000-0000-0000-0000-000000000009', order: 9, date: '2015-01-09T00:00:00Z', term: 'CCC', type: 'date' }");
        }

        [Test]
        public void Search_ForMustangWithSpecifiedFields_Returns()
        {
            dynamic[] result = index
                .Search("type: date")
                .Sort(new Sort(new SortField("order", new FakeFieldComparerSource())))
                .Documents.ToArray();
            Assert.That(result, Has.Length.EqualTo(9));
        }

        public class FakeFieldComparerSource : FieldComparatorSource
        {
            public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
            {
                return new FakeFieldComparator(fieldname, numHits);
            }
        //                case 6:
        //  return (FieldComparator) new FieldComparator.LongComparator(numHits, this.field, this.parser);
        //case 7:
        //  return (FieldComparator) new FieldComparator.DoubleComparator(numHits, this.field, this.parser);
        //case 8:
        //  return (FieldComparator) new FieldComparator.ShortComparator(numHits, this.field, this.parser);
        //case 9:
        //  return this.comparatorSource.NewComparator(this.field, numHits, sortPos, this.reverse);

            public class FakeFieldComparator : FieldComparator
            {
                private readonly string field;
                private readonly long[] values;
                private long[] currentReaderValues;
                private long bottom;

                public FakeFieldComparator(string field, int hits)
                {
                    this.field = field;
                    this.values = new long[hits];
                }

                public override int Compare(int slot1, int slot2)
                {
                    Debug.WriteLine("Compare( " + slot1 + " , " + slot2 + " )");
                    long num1 = values[slot1];
                    long num2 = values[slot2];
                    return num1.CompareTo(num2);
                }

                public override void SetBottom(int slot)
                {
                    bottom = slot;
                }

                public override int CompareBottom(int doc)
                {
                    Debug.WriteLine("CompareBottom( " + doc + " )");
                    long num = currentReaderValues[doc];
                    return bottom.CompareTo(num);
                }

                public override void Copy(int slot, int doc)
                {
                    values[slot] = currentReaderValues[doc];
                }

                public override void SetNextReader(IndexReader reader, int docBase)
                {
                    currentReaderValues = FieldCache_Fields.DEFAULT.GetLongs(reader, field);
                }

                public override IComparable this[int slot] { get { return values[slot]; } }
            }
        }

        private void Write(string json)
        {
            JObject jObject = JObject.Parse(json);

            JToken token = jObject["date"];

            index.Write(jObject);
        }
    }
}
