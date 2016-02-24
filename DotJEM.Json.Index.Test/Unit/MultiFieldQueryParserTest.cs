using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Configuration.FieldStrategies;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Searching;
using Lucene.Net.QueryParsers;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using MultiFieldQueryParser = DotJEM.Json.Index.Searching.MultiFieldQueryParser;

namespace DotJEM.Json.Index.Test.Unit
{
    [TestFixture]
    public class MultiFieldQueryParserTest
    {
        [Test, Ignore("Fails on AppVeyor but not locally, this is due to the Lucene format")]
        public void GetRangeQuery_FieldIsNull_ReturnCorrectBooleanQuery()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.Setup<IStorageIndex>(mock => mock.Configuration).Returns(mocker.GetMock<IIndexConfiguration>().Object);
            mocker.Setup<IIndexConfiguration>(mock => mock.Field)
                .Returns(new StrategyResolverFake<IFieldStrategy>(new FieldStrategy()));
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(new[] { "field1, field2, field3" });

            var queryParser = new MultiFieldQueryParser(mocker.GetMock<IStorageIndex>().Object, "");

            Assert.That(queryParser.Parse("[2014-09-10T11:00 TO 2014-09-10T13:00]").ToString(),
                Is.EqualTo("(field1, field2, field3:[0hzwfs800 TO 0hzxzie7z])"));
            //Note: The above is some sort of lucene supported "DateTime format"... 
            //      We would like to consider the implications of this aproach compared to our own.

            //Assert.That(queryParser.Parse("[2014-09-10T11:00 TO 2014-09-10T13:00]").ToString(),
            //    Is.EqualTo("(field1, field2, field3:[2014-09-10t11:00 TO 2014-09-10t13:00])"));
        }

        [Test]
        public void GetRangeQuery_ExtendedTypeHasOnlyDateFlagAndValidDates_ReturnCorrectBooleanQuery()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.Setup<IStorageIndex>(mock => mock.Configuration).Returns(mocker.GetMock<IIndexConfiguration>().Object);
            mocker.Setup<IIndexConfiguration>(mock => mock.Field)
                .Returns(new StrategyResolverFake<IFieldStrategy>(new FieldStrategy()));
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser(mocker.GetMock<IStorageIndex>().Object, "");

            string q = queryParser.Parse("Created: [2014-09-10T11:00 TO 2014-09-10T13:00]").ToString();
            Assert.That(queryParser.Parse("Created: [2014-09-10T11:00 TO 2014-09-10T13:00]").ToString(), 
                Is.EqualTo("+(" +
                           "+Created.@year:[2014 TO 2014] " +
                           "+Created.@month:[9 TO 9] " +
                           "+Created.@day:[10 TO 10] " +
                           "+Created.@ticks:[635459436000000000 TO 635459508000000000]" +
                           ")"));
        }

        [Test]
        public void GetRangeQuery_ExtendedTypeHasOnlyDateFlagAndInValidDates_ThrowParseException()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.Setup<IStorageIndex>(mock => mock.Configuration).Returns(mocker.GetMock<IIndexConfiguration>().Object);
            mocker.Setup<IIndexConfiguration>(mock => mock.Field)
                .Returns(new StrategyResolverFake<IFieldStrategy>(new FieldStrategy()));
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser(mocker.GetMock<IStorageIndex>().Object, "");


            Assert.Throws<ParseException>(() => queryParser
                .Parse("Created: [2014-09-10T11:00 TO Hest]"));
        }

        [Test, Ignore("Fails on AppVeyor but not locally, this is due to the Lucene format")]
        public void GetRangeQuery_ExtendedTypeHasDateAndOtherFlagAndValidDates_ReturnCorrectBooleanQuery()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.Setup<IStorageIndex>(mock => mock.Configuration).Returns(mocker.GetMock<IIndexConfiguration>().Object);
            mocker.Setup<IIndexConfiguration>(mock => mock.Field)
                .Returns(new StrategyResolverFake<IFieldStrategy>(new FieldStrategy()));
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date | JsonSchemaExtendedType.String);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser(mocker.GetMock<IStorageIndex>().Object, "");

            Assert.That(queryParser.Parse("Created: [2014-09-10T11:00 TO 2014-09-10T13:00]").ToString(), 
                Is.EqualTo("Created:[635459436000000000 TO 635459508000000000] " +
                           "Created:[0hzwfs800 TO 0hzxzie7z]"));
            //Note: The above is some sort of lucene supported "DateTime format"... 
            //      We would like to consider the implications of this aproach compared to our own.

            //Assert.That(queryParser.Parse("Created: [2014-09-10T11:00 TO 2014-09-10T13:00]").ToString(),
            //    Is.EqualTo("Created:[635459436000000000 TO 635459508000000000] " +
            //       "Created:[2014-09-10t11:00 TO 2014-09-10t13:00]"));
        }

        [Test]
        public void GetRangeQuery_ExtendedTypeHasDateAndOtherFlagAndInValidDates_ReturnCorrectBooleanQuery()
        {
            var mocker = new AutoMocker();
            mocker.Setup<IStorageIndex>(mock => mock.Schemas).Returns(mocker.GetMock<ISchemaCollection>().Object);
            mocker.Setup<IStorageIndex>(mock => mock.Configuration).Returns(mocker.GetMock<IIndexConfiguration>().Object);
            mocker.Setup<IIndexConfiguration>(mock => mock.Field)
                .Returns(new StrategyResolverFake<IFieldStrategy>(new FieldStrategy()));
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.ExtendedType(It.IsAny<string>())).Returns(JsonSchemaExtendedType.Date | JsonSchemaExtendedType.String);
            mocker.GetMock<ISchemaCollection>().Setup(mock => mock.AllFields()).Returns(Enumerable.Empty<string>());

            var queryParser = new MultiFieldQueryParser(mocker.GetMock<IStorageIndex>().Object, "");
            
            Assert.That(queryParser.Parse("Created: [2014-09-10T11:00 TO Hest]").ToString(), 
                Is.EqualTo("Created:[2014-09-10t11:00 TO hest]"));
        }
    }

    public class StrategyResolverFake<TStrategy> : IStrategyResolver<TStrategy> where TStrategy : class
    {
        private readonly TStrategy strategy;

        public StrategyResolverFake(TStrategy strategy)
        {
            this.strategy = strategy;
        }

        public TStrategy Strategy(string contentType, string field)
        {
            return strategy;
        }

        public TStrategy Strategy(string field)
        {
            return strategy;
        }
    }
}