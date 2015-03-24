using System;
using DotJEM.Json.Index.Schema;
using Lucene.Net.QueryParsers;
using Newtonsoft.Json.Schema;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit.Schema
{
    [TestFixture]
    public class SchemaCollectionTest
    {

        [Test]
        public void Add_NullSchema_ShouldThrowException()
        {
            var schemaCollection = new SchemaCollection();
            Assert.Throws<ArgumentNullException>(() => schemaCollection.Add("contentType", null));
        }

        [Test]
        public void Add_NullContentType_ShouldThrowException()
        {
            var schemaCollection = new SchemaCollection();
            var schema = new JSchema(JsonSchemaType.Any, new JsonSchemaExtendedType());
            Assert.Throws<ArgumentNullException>(() => schemaCollection.Add(null, schema));
        }

        [Test]
        public void Indexer_NullSchema_ShouldThrowException()
        {
            var schemaCollection = new SchemaCollection();
            Assert.Throws<ArgumentNullException>(() => schemaCollection["contentType"] = null);
        }

        [Test]
        public void Indexer_NullContentType_ShouldThrowException()
        {
            var schemaCollection = new SchemaCollection();
            var schema = new JSchema(JsonSchemaType.Any, new JsonSchemaExtendedType());
            Assert.Throws<ArgumentNullException>(() => schemaCollection[null] = schema);
        }
    }
}