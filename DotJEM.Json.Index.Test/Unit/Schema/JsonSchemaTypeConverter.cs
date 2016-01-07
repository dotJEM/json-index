using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Configuration;
using DotJEM.Json.Index.Schema;
using DotJEM.Json.Index.Test.Constraints;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit.Schema
{
    [TestFixture]
    public class JsonSchemaTypeConverterTest
    {
        [TestCase(JsonSchemaType.Any, JsonSchemaExtendedType.Any)]
        [TestCase(JsonSchemaType.Array, JsonSchemaExtendedType.Array)]
        [TestCase(JsonSchemaType.Boolean | JsonSchemaType.String, JsonSchemaExtendedType.Boolean | JsonSchemaExtendedType.String)]
        [TestCase(JsonSchemaType.Integer | JsonSchemaType.Float, JsonSchemaExtendedType.Date | JsonSchemaExtendedType.Float)]
        public void SerializeDeserialize_SimpleJSceme_TypeAndExtentedTypeIsKeept(JsonSchemaType type, JsonSchemaExtendedType extended)
        {
            JSchema schema = new JSchema(type, extended);
            JSchema deserialized = JsonConvert.DeserializeObject<JSchema>(JsonConvert.SerializeObject(schema));

            Assert.That(deserialized,
                Has.Property("Type").EqualTo(schema.Type)
                & Has.Property("ExtendedType").EqualTo(schema.ExtendedType));
        }

        [Test]
        public void SerializeDeserialize_RootObject_RootIntact()
        {
            JSchema schema = new JSchema(JsonSchemaType.Any, JsonSchemaExtendedType.Any);
            schema.IsRoot = true;
            schema.Schema = "https://foobar.com/api/fogs/bugs";

            JSchema deserialized = JsonConvert.DeserializeObject<JSchema>(JsonConvert.SerializeObject(schema));

            Assert.That(deserialized,
                Has.Property("IsRoot").EqualTo(schema.IsRoot)
                & Has.Property("Schema").EqualTo(schema.Schema));
        }

        [TestCase("{ Foo: 42 }")]
        [TestCase("{ Foo: 42, Bar: { Foo: 'Test this' } }")]
        [TestCase("{ Foo: 42, Bar: { Foo: ['Test this', 42] } }")]
        [TestCase("{ Foo: [ 42 ], Bar: { Foo: ['Test this', 42] } }")]
        [TestCase("{ Foo: '42', Bar: { Foo: ['Test this', 42] } }")]
        [TestCase("{ Foo: null, Bar: { Foo: ['Test this', 42] } }")]
        [TestCase("{ Foo: undefined, Bar: { Foo: ['Test this', 42] } }")]
        [TestCase("{ Foo: '42', Bar: { Foo: ['Test this', 42, { Test: 'hest' }] } }")]
        [TestCase("{ Foo: '42', Bar: { Foo: [ ['Test this', 42], { Test: 'hest' }] } }")]
        public void SerializeDeserialize_FromGeneratedSchemas_SchemasAreKeept(string json)
        {
            JSchema schema = new JSchemaGenerator().Generate(JObject.Parse(json));
            JSchema deserialized = JsonConvert.DeserializeObject<JSchema>(JsonConvert.SerializeObject(schema));

            Assert.That(deserialized, HAS.Properties.EqualTo(schema));
        }


        [TestCaseSource(nameof(JsonObjectToSchemas)), Ignore("For build server")]
        public string SerializeDeserialize_FromGeneratedSchemas_GeneratesCorrectStructure(string json)
        {
            JSchema schema = new JSchemaGenerator().Generate(JObject.Parse(json));
            return JsonConvert.SerializeObject(schema);
        }

        public static IEnumerable JsonObjectToSchemas
        {
            get
            {
                yield return new TestCaseData("{ Foo: 42 }")
                    .Returns(JObject.FromObject(new
                    {
                        id = "",
                        type = "object",
                        extendedType = "object",
                        required = false,
                        field = "",
                        properties = new
                        {
                            Foo = new
                            {
                                id = "Foo",
                                type = "integer",
                                extendedType = "integer",
                                required = false,
                                field = "Foo"
                            }
                        }
                    }).ToString(Formatting.None));

                yield return new TestCaseData("{ Foo: 42, Bar: { Foo: 'Test this' } }")
                    .Returns(JObject.FromObject(new
                    {
                        id = "",
                        type = "object",
                        extendedType = "object",
                        required = false,
                        field = "",
                        properties = new
                        {
                            Foo = new
                            {
                                id = "Foo",
                                type = "integer",
                                extendedType = "integer",
                                required = false,
                                field = "Foo"
                            },
                            Bar = new
                            {
                                id = "Bar",
                                type = "object",
                                extendedType = "object",
                                required = false,
                                field = "Bar",
                                properties = new
                                {
                                    Foo = new
                                    {

                                        id = "Bar/Foo",
                                        type = "string",
                                        extendedType = "string",
                                        required = false,
                                        field = "Bar.Foo"
                                    }
                                }

                            }
                        }
                    }).ToString(Formatting.None));
                
                yield return new TestCaseData("{ Foo: 42, Bar: { Foo: ['Test this', 42] } }")
                    .Returns(JObject.FromObject(new
                    {
                        id = "",
                        type = "object",
                        extendedType = "object",
                        required = false,
                        field = "",
                        properties = new
                        {
                            Foo = new
                            {
                                id = "Foo",
                                type = "integer",
                                extendedType = "integer",
                                required = false,
                                field = "Foo"
                            },
                            Bar = new
                            {
                                id = "Bar",
                                type = "object",
                                extendedType = "object",
                                required = false,
                                field = "Bar",
                                properties = new
                                {
                                    Foo = new
                                    {

                                        id = "Bar/Foo",
                                        type = "array",
                                        extendedType = "array",
                                        required = false,
                                        field = "Bar.Foo",
                                        items = new
                                        {
                                            type = new[] { "string", "integer" },
                                            extendedType = new[] { "string", "integer" },
                                            required = false,
                                            field = "Bar.Foo",
                                        }
                                    }
                                }

                            }
                        }
                    }).ToString(Formatting.None));
                //[TestCase("{ Foo: [ 42 ], Bar: { Foo: ['Test this', 42] } }")]
                //[TestCase("{ Foo: '42', Bar: { Foo: ['Test this', 42] } }")]
                //[TestCase("{ Foo: null, Bar: { Foo: ['Test this', 42] } }")]
                //[TestCase("{ Foo: undefined, Bar: { Foo: ['Test this', 42] } }")]
                //[TestCase("{ Foo: '42', Bar: { Foo: ['Test this', 42, { Test: 'hest' }] } }")]
                //[TestCase("{ Foo: '42', Bar: { Foo: [ ['Test this', 42], { Test: 'hest' }] } }")]
            }
        }

        /*

         
         
         */

    }
}
