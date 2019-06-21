using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Schema;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit.Schema
{
    [TestFixture]
    public class JSchemaGeneratorTest
    {
        [Test]
        public void typeForDate_Expected_String()
        {
            JSchemaGenerator generator = new JSchemaGenerator();

            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeDate = DateTime.UtcNow,
            }), "contentType", "area");
            var other = mySchema.Properties["SomeDate"];
            Assert.That(other.Type.ToString(), Is.EqualTo("String"));
            Assert.That(other.ExtendedType.ToString(), Is.EqualTo("Date"));
        }

        [Test]
        public void extendedTypeForDate_Expected_Date()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeDate = DateTime.UtcNow,
            }), "contentType", "area");
            var other = mySchema.Properties["SomeDate"];
            Assert.That(other.ExtendedType.ToString(), Is.EqualTo("Date"));
        }
        [Test]
        public void extendedTypeForGuid_Expected_Guid()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeDate = DateTime.UtcNow,
                SomeGuid = Guid.NewGuid()
            }), "contentType", "area");
            var guidType = mySchema.Properties["SomeGuid"];
            Assert.That(guidType.ExtendedType.ToString(), Is.EqualTo("Guid"));
        }

        [Test]
        public void typeForGuid_Expected_String()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeDate = DateTime.UtcNow,
                SomeGuid = Guid.NewGuid()
            }), "contentType", "area");
            var guidType = mySchema.Properties["SomeGuid"];
            Assert.That(guidType.Type.ToString(), Is.EqualTo("String"));
        }

        [Test]
        public void typeForUri_Expected_String()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeUri =Uri.UriSchemeHttp
            }), "contentType", "area");
            var uri = mySchema.Properties["SomeUri"];
            Assert.That(uri.Type.ToString(), Is.EqualTo("String"));

        }
        [Test]
        public void extendedTypeForUri_Expected_String()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeUri = Uri.UriSchemeHttp
            }), "contentType", "area");
            var uri = mySchema.Properties["SomeUri"];
            Assert.That(uri.ExtendedType.ToString(), Is.EqualTo("String"));

        }
        [Test]
        public void typeForTimeSpan_Expected_String()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeTimeSpan =TimeSpan.MaxValue
            }), "contentType", "area");
            var timeSpan = mySchema.Properties["SomeTimeSpan"];
            Console.WriteLine(timeSpan);
            Assert.That(timeSpan.Type.ToString(), Is.EqualTo("String"));

        }

        [Test]
        public void extendedTypeForTimeSpan_Expected_TimeSpan()
        {
            JSchemaGenerator generator = new JSchemaGenerator();
            JSchema mySchema = generator.Generate(JObject.FromObject(new
            {
                SomeString = "Foo",
                SomeInt = 43,
                SomeTimeSpan = TimeSpan.FromDays(10)
            }), "contentType", "area");
            var timeSpan = mySchema.Properties["SomeTimeSpan"];
            Console.WriteLine(timeSpan);
            Assert.That(timeSpan.ExtendedType.ToString(), Is.EqualTo("TimeSpan"));

        }
    }
}
