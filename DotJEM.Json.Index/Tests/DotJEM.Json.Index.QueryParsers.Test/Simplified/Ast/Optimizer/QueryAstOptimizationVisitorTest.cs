using DotJEM.Json.Index.QueryParsers.Ast;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer;
using DotJEM.Json.Index.TestUtil;
using NUnit.Framework;

namespace DotJEM.Json.Index.QueryParsers.Test.Simplified.Ast.Optimizer
{
    [TestFixture]
    public class QueryAstOptimizationVisitorTest
    {
        [Test]
        public void Optimize_NestedOrs_YieldsFlatQuery()
        {
            FieldQuery[] fields = new FieldQuery[6];
            OrQuery or = new OrQuery(new BaseQuery[]
            {
                fields[0] = new FieldQuery("foo1", FieldOperator.Equals, new StringValue("x")),
                new OrQuery(new BaseQuery[]
                {
                    fields[1] = new FieldQuery("foo2", FieldOperator.Equals, new StringValue("y")),
                    fields[2] = new FieldQuery("foo3", FieldOperator.Equals, new StringValue("z")),
                }),
                fields[3] = new FieldQuery("foo4", FieldOperator.Equals, new StringValue("a")),
                new OrQuery(new BaseQuery[]
                {
                    fields[4] = new FieldQuery("foo5", FieldOperator.Equals, new StringValue("b")),
                    fields[5] = new FieldQuery("foo6", FieldOperator.Equals, new StringValue("c")),
                }),
            });

            BaseQuery ast = or.Optimize();
            DebugOut.WriteJson(ast);
            Assert.That(ast, Is.TypeOf<OrQuery>().With.Property(nameof(OrQuery.Queries)).EquivalentTo(fields));
        }

        [Test]
        public void Optimize_OrQuerySameFieldsWithEquals_YieldsInQuery()
        {
            OrQuery or = new OrQuery(new BaseQuery[]
            {
                new FieldQuery("foo", FieldOperator.Equals, new StringValue("x")),
                new FieldQuery("foo", FieldOperator.Equals, new StringValue("y")),
                new FieldQuery("foo", FieldOperator.Equals, new StringValue("z")),
            });

            BaseQuery ast = or.Optimize();
            DebugOut.WriteJson(ast);
            Assert.That(ast, Is.TypeOf<FieldQuery>().With.Property(nameof(FieldQuery.Operator)).EqualTo(FieldOperator.In));
        }

        [Test]
        public void Optimize_NestedOrsSameFields_YieldsFlatQuery()
        {
            FieldQuery[] fields = new FieldQuery[6];
            OrQuery or = new OrQuery(new BaseQuery[]
            {
                fields[0] = new FieldQuery("foo", FieldOperator.Equals, new StringValue("x")),
                new OrQuery(new BaseQuery[]
                {
                    fields[1] = new FieldQuery("foo", FieldOperator.Equals, new StringValue("y")),
                    fields[2] = new FieldQuery("foo", FieldOperator.Equals, new StringValue("z")),
                }),
                fields[3] = new FieldQuery("foo", FieldOperator.Equals, new StringValue("a")),
                new OrQuery(new BaseQuery[]
                {
                    fields[4] = new FieldQuery("foo", FieldOperator.Equals, new StringValue("b")),
                    fields[5] = new FieldQuery("foo", FieldOperator.Equals, new StringValue("c")),
                }),
            });

            BaseQuery ast = or.Optimize();
            DebugOut.WriteJson(ast);
            Assert.That(ast, Is.TypeOf<FieldQuery>().With.Property(nameof(FieldQuery.Operator)).EqualTo(FieldOperator.In));
        }
    }
}
