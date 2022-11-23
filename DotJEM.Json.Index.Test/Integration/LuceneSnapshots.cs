using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Storage.Snapshot;

namespace DotJEM.Json.Index.Test.Integration
{
    public class LuceneSnapshots
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

            ISnapshot snapshot = new TestSnapshot();
            index.Storage.Snapshot(snapshot);
            index.Storage.Restore(snapshot);

            Assert.That(index.Search("*").Count(), Is.EqualTo(9));
        }
    }

    public class TestSnapshot : ISnapshot
    {
        private readonly List<File> files = new List<File>();

        public long Generation { get; set; }
        public ILuceneFile SegmentsFile { get; private set;  }
        public IEnumerable<ILuceneFile> Files => files;

        public void WriteFile(IndexInputStream stream)
        {
            File file = new File(stream.FileName);
            stream.CopyTo(file.Stream);
            file.Stream.Flush();
            files.Add(file);
        }

        public void WriteSegmentsFile(IndexInputStream stream)
        {
            File file = new File(stream.FileName);
            stream.CopyTo(file.Stream);
            file.Stream.Flush();

            SegmentsFile = file;
        }

        public void WriteGeneration(long generation)
        {
            this.Generation = generation;
        }


        public class File : ILuceneFile
        {

            public string Name { get; }
            public MemoryStream Stream { get; } = new MemoryStream();
            
            public File(string name)
            {
                Name = name;
            }
            public Stream Open()
            {
                Stream.Seek(0, SeekOrigin.Begin);
                return Stream;
            }

        }
    }


}
