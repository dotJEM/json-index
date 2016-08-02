using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.NUnit.Json;
using DotJEM.NUnit.Json.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Integration
{
    [TestFixture]
    public class LuceneWriterContextTest
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();

        [Test]
        public async void WriteContext_MakesDocumentsAvailable()
        {
            var config = index.Configuration;
            config
                .SetTypeResolver("Type")
                .SetAreaResolver("Area")
                .ForAll()
                .SetIdentity("Id");

            using (ILuceneWriteContext writer = index.Writer.WriteContext(1024))
            {
                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000001"), Type = "Person", Name = "John", LastName = "Doe", Area = "Foo" }));
                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000002"), Type = "Person", Name = "Peter", LastName = "Pan", Area = "Foo" }));
                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000003"), Type = "Person", Name = "Alice", Area = "Foo" }));

                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000004"), Type = "Car", Brand = "Ford", Model = "Mustang", Number = 5, Area = "Foo" }));
                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000005"), Type = "Car", Brand = "Dodge", Model = "Charger", Number = 10, Area = "Foo" }));
                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000006"), Type = "Car", Brand = "Chevrolet", Model = "Camaro", Number = 15, Area = "Foo" }));

                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000007"), Type = "Flower", Name = "Lilly", Meaning = "Majesty", Number = 5, Area = "Foo" }));
                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000008"), Type = "Flower", Name = "Freesia", Meaning = "Innocence", Number = 10, Area = "Foo" }));
                await writer.Write(JObject.FromObject(new { Id = new Guid("00000000-0000-0000-0000-000000000009"), Type = "Flower", Name = "Aster", Meaning = "Patience", Number = 15, Area = "Foo" }));
            }

            Assert.That(index.Search("*").Count(), Is.EqualTo(9));
        }

    }
}
