using Lucene.Net.QueryParsers;
using Moq.AutoMock;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit
{
    [TestFixture]
    public class MultiFieldQueryParserTest
    {
        [Test]
        public void GetRangeQuery_FieldIsNull_ReturnDefaultQuery()
        {
            var mocker = new AutoMocker();

            //mocker.Setup<QueryParser>(mock => mock.GetBooleanQuery).Returns("fubar\\bob");


            //var contentService = new Mock<IContentService>();
            //container.Setup<IServiceProvider<IContentService>>(provider => provider.Create(It.IsAny<string>(), It.IsAny<ApiController>())).Returns(contentService.Object);

            //container.CreateInstance<ContentController>().Get("myType");

            //container.Verify<IAccessControlService>(i => i.Demand(ContentAccess.Read, "myType"), Times.Once());

        }
    }
}