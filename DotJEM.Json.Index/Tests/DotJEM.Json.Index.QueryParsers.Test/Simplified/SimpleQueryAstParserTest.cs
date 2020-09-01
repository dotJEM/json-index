using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.QueryParsers.Ast;
using DotJEM.Json.Index.QueryParsers.Simplified.Ast;
using DotJEM.Json.Index.TestUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.QueryParsers.Test.Simplified
{
    [TestFixture]
    public class SimpleQueryAstParserTest
    {
        [TestCaseSource(nameof(Transform)), Ignore("Need to verify the outcome of these after refactorings.")]
        public void Parse_RunTest(string query, string expectedJson)
        {
            BaseQuery ast = new SimplifiedQueryAstParser().Parse(query);
            string json = DebugOut.WriteJson(ast);
            Assert.That(json, Is.EqualTo(JToken.Parse(expectedJson).ToString(Formatting.Indented)));
        }

        public static IEnumerable<string[]> Transform => Cases().Select(@case => new[] { @case.Item1, @case.Item2 });
        public static IEnumerable<(string, string)> Cases()
        {
            yield return (
                "field1 = 2 AND ( field2 = foo OR field3 > 50 )",
                @"{
                  ""$type"": ""AndQuery"",
                  ""Count"": 2,
                  ""Queries"": [
                    {
                      ""$type"": ""FieldQuery"",
                      ""Name"": ""field1"",
                      ""Operator"": ""Equals"",
                      ""Value"": {
                        ""$type"": ""NumberValue"",
                        ""Value"": 2.0
                      }
                    },
                    {
                      ""$type"": ""OrQuery"",
                      ""Count"": 2,
                      ""Queries"": [
                        {
                          ""$type"": ""FieldQuery"",
                          ""Name"": ""field2"",
                          ""Operator"": ""Equals"",
                          ""Value"": {
                            ""$type"": ""StringValue"",
                            ""Value"": ""foo""
                          }
                        },
                        {
                          ""$type"": ""FieldQuery"",
                          ""Name"": ""field3"",
                          ""Operator"": ""GreaterThan"",
                          ""Value"": {
                            ""$type"": ""NumberValue"",
                            ""Value"": 50.0
                          }
                        }
                      ]
                    }
                  ]
                }"
                );

            yield return (
                "field1 = 2 OR field2 = foo OR field3 > 50 OR field4 = foo",
                @"{
                    ""$type"": ""OrQuery"",
                    ""Count"": 4,
                    ""Queries"": [
                      {
                        ""$type"": ""FieldQuery"",
                        ""Name"": ""field1"",
                        ""Operator"": ""Equals"",
                        ""Value"": {
                          ""$type"": ""NumberValue"",
                          ""Value"": 2.0
                        }
                      },
                      {
                        ""$type"": ""FieldQuery"",
                        ""Name"": ""field2"",
                        ""Operator"": ""Equals"",
                        ""Value"": {
                          ""$type"": ""StringValue"",
                          ""Value"": ""foo""
                        }
                      },
                      {
                        ""$type"": ""FieldQuery"",
                        ""Name"": ""field3"",
                        ""Operator"": ""GreaterThan"",
                        ""Value"": {
                          ""$type"": ""NumberValue"",
                          ""Value"": 50.0
                        }
                      },
                      {
                        ""$type"": ""FieldQuery"",
                        ""Name"": ""field4"",
                        ""Operator"": ""Equals"",
                        ""Value"": {
                          ""$type"": ""StringValue"",
                          ""Value"": ""foo""
                        }
                      }
                    ]
                  }"
                );

            yield return (
                "field1 = 2 OR field2 = foo AND field3 > 50 OR field4 = foo",
                @"{
                  ""$type"": ""OrQuery"",
                  ""Count"": 3,
                  ""Queries"": [
                    {
                      ""$type"": ""FieldQuery"",
                      ""Name"": ""field1"",
                      ""Operator"": ""Equals"",
                      ""Value"": {
                        ""$type"": ""NumberValue"",
                        ""Value"": 2.0
                      }
                    },
                    {
                      ""$type"": ""AndQuery"",
                      ""Count"": 2,
                      ""Queries"": [
                        {
                          ""$type"": ""FieldQuery"",
                          ""Name"": ""field2"",
                          ""Operator"": ""Equals"",
                          ""Value"": {
                            ""$type"": ""StringValue"",
                            ""Value"": ""foo""
                          }
                        },
                        {
                          ""$type"": ""FieldQuery"",
                          ""Name"": ""field3"",
                          ""Operator"": ""GreaterThan"",
                          ""Value"": {
                            ""$type"": ""NumberValue"",
                            ""Value"": 50.0
                          }
                        }
                      ]
                    },
                    {
                      ""$type"": ""FieldQuery"",
                      ""Name"": ""field4"",
                      ""Operator"": ""Equals"",
                      ""Value"": {
                        ""$type"": ""StringValue"",
                        ""Value"": ""foo""
                      }
                    }
                  ]
                }"
                );

            yield return (
                "field1 = *",
                @"{
                  ""$type"": ""FieldQuery"",
                  ""Name"": ""field1"",
                  ""Operator"": ""Equals"",
                  ""Value"": {
                    ""$type"": ""MatchAllValue""
                  }
                }"
                );

            yield return (
                "field1 = \"Some quoted value * here\"",
                @"{
                    ""$type"": ""FieldQuery"",
                    ""Name"": ""field1"",
                    ""Operator"": ""Equals"",
                    ""Value"": {
                      ""$type"": ""PhraseValue"",
                      ""Value"": ""\""Some quoted value * here\""""
                    }
                  }"
                );

            yield return (
                "field1 IN (One, Two, Three)",
                @"{
                  ""$type"": ""FieldQuery"",
                  ""Name"": ""field1"",
                  ""Operator"": ""In"",
                  ""Value"": {
                    ""$type"": ""ListValue"",
                    ""Count"": 3,
                    ""Values"": [
                      {
                        ""$type"": ""StringValue"",
                        ""Value"": ""One""
                      },
                      {
                        ""$type"": ""StringValue"",
                        ""Value"": ""Two""
                      },
                      {
                        ""$type"": ""StringValue"",
                        ""Value"": ""Three""
                      }
                    ]
                  }
                }"
                );
        }

    }
}
