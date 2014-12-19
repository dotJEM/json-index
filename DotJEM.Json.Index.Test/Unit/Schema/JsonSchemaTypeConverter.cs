using System;
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

    }
}
