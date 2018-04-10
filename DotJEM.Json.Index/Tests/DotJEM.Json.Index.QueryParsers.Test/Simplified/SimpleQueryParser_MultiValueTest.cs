using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using DotJEM.Json.Index.TestUtil;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace DotJEM.Json.Index.QueryParsers.Test.Simplified
{
    [TestFixture]
    public class SimplifiedParser_WithoutFields
    {
        [TestCase("left right")]
        [TestCase("left or right")]
        [TestCase("left and right")]
        [TestCase("field: left and field: right")]
        public void Parse_RunTest(string query)
        {
            BaseQuery ast = new SimplifiedQueryAstParser().Parse(query);
            DebugOut.WriteJson(ast);
            Assert.That(ast, Is.Not.Null);
        }
    }

    [TestFixture]
    public class SimplifiedParser_MultiValueTest
    {
        [TestCase("field: (A OR B)")]
        [TestCase("field: (A OR B NOT C)")]
        public void Parse_RunTest(string query)
        {
            BaseQuery ast = new SimplifiedQueryAstParser().Parse(query);
            DebugOut.WriteJson(ast);
            Assert.That(ast, Is.Not.Null);
        }
    }

    [TestFixture]
    public class SimplifiedParser_EqualsTest
    {
        [TestCase("field = value")]
        [TestCase("field = \"some phrase\"")]
        [TestCase("field = *")]
        public void Parse_RunTest(string query)
        {
            BaseQuery ast = new SimplifiedQueryAstParser().Parse(query);
            DebugOut.WriteJson(ast);
            Assert.That(ast, Is.Not.Null);
        }
    }

    [TestFixture]
    public class SimplifiedParser_NotEqualsTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_GreaterThanTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_GreaterThanOrEqualsTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_LessThanTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_LessThanOrEqualsTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_OrClauseTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_AndClauseTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_NotClauseTest
    {

    }

    [TestFixture]
    public class SimplifiedParser_DefaultClauseTest
    {

    }
    
    [TestFixture]
    public class SimplifiedParser_OrderClause
    {
        [TestCase("field = value ORDER BY foo")]
        [TestCase("field = value ORDER BY foo ASC")]
        [TestCase("field = value ORDER BY foo DESC")]
        [TestCase("field = value ORDER BY foo, other ASC")]
        public void Parse_RunTest(string query)
        {
            OrderedQuery ast = new SimplifiedQueryAstParser().Parse(query) as OrderedQuery;
            Assert.That(ast?.Ordering, Is.Not.Null);
            DebugOut.WriteJson(ast);
        }

    }
}
