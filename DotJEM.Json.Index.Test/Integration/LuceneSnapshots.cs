using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotJEM.Json.Index.Storage.Snapshot;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.Test.Integration
{
    public class LuceneSnapshots
    {
        private readonly IStorageIndex index = new LuceneStorageIndex();

        [Test]
        public async Task WriteContext_MakesDocumentsAvailable()
        {
            var config = index.Configuration;
            config
                .SetTypeResolver("Type")
                .SetAreaResolver("Area")
                .ForAll()
                .SetIdentity("Id");

            using (ILuceneWriteContext writer = index.Writer.WriteContext())
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

            FakeSnapshotTarget target = new FakeSnapshotTarget();
            index.Storage.Snapshot(target);

            ISnapshotSource source = target.LastCreatedWriter.GetSource();
            Assert.That(index.Storage.Restore(source));

            Assert.That(index.Search("*").Count(), Is.EqualTo(9));
        }
    }

    public class FakeSnapshotTarget : ISnapshotTarget
    {
        public FakeSnapshotWriter LastCreatedWriter { get; private set; }
        public ISnapshotWriter Open(IndexCommit commit)
        {
            return LastCreatedWriter= new FakeSnapshotWriter(commit.Generation);
        }
    }

    public class FakeSnapshotWriter : ISnapshotWriter
    {
        private readonly long generation;
        private ILuceneFile segmentsFile;
        private readonly List<ILuceneFile> files = new List<ILuceneFile>();

        public FakeSnapshotWriter(long generation)
        {
            this.generation = generation;
        }

        public void WriteFile(IndexInputStream stream)
        {
            FakeFile file = new FakeFile(stream.FileName);
            stream.CopyTo(file.Stream);
            file.Stream.Flush();
            files.Add(file);
        }

        public void WriteSegmentsFile(IndexInputStream stream)
        {
            FakeFile file = new FakeFile(stream.FileName);
            stream.CopyTo(file.Stream);
            file.Stream.Flush();
            segmentsFile = file;
        }

        public class FakeFile : ILuceneFile
        {

            public string Name { get; }
            public MemoryStream Stream { get; } = new MemoryStream();

            public FakeFile(string name)
            {
                Name = name;
            }
            public Stream Open()
            {
                Stream.Seek(0, SeekOrigin.Begin);
                return Stream;
            }

        }

        public ISnapshotSource GetSource()
        {
            return new FakeSnapshotSource(new FakeSnapshot(generation, segmentsFile, files));
        }

        public void Dispose()
        {
        }
    }

    public class FakeSnapshotSource : ISnapshotSource
    {
        private readonly FakeSnapshot snapshot;

        public FakeSnapshotSource(FakeSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public ISnapshot Open()
        {
            return snapshot;
        }
    }

    public class FakeSnapshot : ISnapshot
    {
        public long Generation { get; }
        public ILuceneFile SegmentsFile { get; }
        public IEnumerable<ILuceneFile> Files { get; }

        public FakeSnapshot(long generation, ILuceneFile segmentsFile, IEnumerable<ILuceneFile> files)
        {
            Generation = generation;
            SegmentsFile = segmentsFile;
            Files = files;
        }

        public void Dispose()
        {
        }
    }


}
