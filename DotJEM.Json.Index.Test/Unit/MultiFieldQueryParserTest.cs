using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotJEM.Json.Index.Schema;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.QueryParsers;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using MultiFieldQueryParser = DotJEM.Json.Index.Searching.MultiFieldQueryParser;
using Version = Lucene.Net.Util.Version;

namespace DotJEM.Json.Index.Test.Unit
{
    [TestFixture]
    public class MultiFieldQueryParserTest
    {
        [Test]
        public void GetRangeQuery_FieldIsNull_ReturnCorrectBooleanQuery()
        {
            IEnumerable<string> allFields = new string[]{"field1, field2, field3"};
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(allFields);

            var queryParser = new MultiFieldQueryParser("", mocker.GetMock<IStorageIndex>().Object);

            const string queryString = "[2014-09-10T11:00 TO 2014-09-10T13:00]";

            Assert.That(queryParser.Parse(queryString).ToString(), Is.EqualTo("(field1, field2, field3:[2014-09-10t11:00 TO 2014-09-10t13:00])"));
        }

        [Test]
        public void GetRangeQuery_ExtendedTypeHasOnlyDateFlagAndValidDates_ReturnCorrectBooleanQuery()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser("", mocker.GetMock<IStorageIndex>().Object);

            const string queryString = "Created: [2014-09-10T11:00 TO 2014-09-10T13:00]";

            Assert.That(queryParser.Parse(queryString).ToString(), Is.EqualTo("Created:[635459436000000000 TO 635459508000000000]"));
        }

        [Test]
        public void GetRangeQuery_ExtendedTypeHasOnlyDateFlagAndInValidDates_ThrowParseException()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser("", mocker.GetMock<IStorageIndex>().Object);

            const string queryString = "Created: [2014-09-10T11:00 TO Hest]";

            Assert.Throws<ParseException>(() => queryParser.Parse(queryString));
        }

        [Test]
        public void GetRangeQuery_ExtendedTypeHasDateAndOtherFlagAndValidDates_ReturnCorrectBooleanQuery()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date | JsonSchemaExtendedType.String);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser("", mocker.GetMock<IStorageIndex>().Object);

            const string queryString = "Created: [2014-09-10T11:00 TO 2014-09-10T13:00]";

            Assert.That(queryParser.Parse(queryString).ToString(), Is.EqualTo("Created:[635459436000000000 TO 635459508000000000] " +
                                                                              "Created:[2014-09-10t11:00 TO 2014-09-10t13:00]"));
        }

        [Test]
        public void GetRangeQuery_ExtendedTypeHasDateAndOtherFlagAndInValidDates_ReturnCorrectBooleanQuery()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date | JsonSchemaExtendedType.String);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser("", mocker.GetMock<IStorageIndex>().Object);

            const string queryString = "Created: [2014-09-10T11:00 TO Hest]";
            var something = queryParser.Parse(queryString).ToString();

            Assert.That(queryParser.Parse(queryString).ToString(), Is.EqualTo("Created:[2014-09-10t11:00 TO hest]"));
        }
    }
}