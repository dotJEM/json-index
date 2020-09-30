using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DotJEM.Json.Index.Snapshots;
using DotJEM.Json.Index.Util;
using Lucene.Net.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Directory = Lucene.Net.Store.Directory;

namespace DotJEM.Json.Index.Ingest
{
    public class IngestZipSnapshotTarget : ISnapshotTarget
    {
        private readonly string path;
        private readonly JToken properties;
        private readonly List<SingleFileSnapshot> snapShots = new List<SingleFileSnapshot>();

        public IReadOnlyCollection<ISnapshot> Snapshots => snapShots.AsReadOnly(); 

        public IngestZipSnapshotTarget(string path, JToken properties)
        {
            this.path = path;
            this.properties = properties;
        }

        public virtual ISnapshotWriter Open(long generation)
        {
            string snapshotPath = Path.Combine(path, $"{generation:x8}.zip");
            snapShots.Add(new SingleFileSnapshot(snapshotPath));
            return new Writer(snapshotPath, properties);
        }

        private class Writer : Disposable, ISnapshotWriter
        {
            private readonly ZipArchive archive;

            public Writer(string path, JToken properties)
            {
                this.archive = ZipFile.Open(path, ZipArchiveMode.Create);

                WriteProperties(properties);
            }

            private void WriteProperties(JToken token)
            {
                ZipArchiveEntry entry = archive.CreateEntry("_properties");
                using StreamWriter reader = new StreamWriter(entry.Open());
                token.WriteTo(new JsonTextWriter(reader));
            }

            public void WriteFile(string fileName, Directory dir)
            {
                using IndexInputStream source = new IndexInputStream(dir.OpenInput(fileName, IOContext.READ_ONCE));
                using Stream target = archive.CreateEntry(fileName).Open();
                source.CopyTo(target);
            }

            public void WriteSegmentsFile(string segmentsFile, Directory dir)
            {
                this.WriteFile(segmentsFile, dir);
            }

            //public void WriteProperties(ISnapshotProperties properties)
            //{
            //    using Stream target = archive.CreateEntry("_properties").Open();
            //    properties.WriteTo(target);
            //}

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    archive?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }

    public class IngestIndexZipSnapshotSource : IIndexSnapshotSource
    {
        private readonly string path;
        private readonly long? generation;
        public JToken RecentProperties { get; private set; }

        public IngestIndexZipSnapshotSource(string path, long? generation = null)
        {
            this.path = path;
            this.generation = generation;
        }

        public ISnapshotReader Open()
        {
            Reader reader = ResolveReader();
            this.RecentProperties = reader.Properties;
            return reader;
        }

        private Reader ResolveReader()
        {
            if (generation != null) return new Reader(Path.Combine(path, $"{generation:x8}.zip"));

            string file = System.IO.Directory.GetFiles(path, "*.zip")
                .OrderByDescending(f => f)
                .FirstOrDefault();
            return new Reader(file);
        }

        public class Reader : Disposable, ISnapshotReader
        {
            private readonly ZipArchive archive;
            public long Generation { get; }
            public JToken Properties { get; }

            public Reader(string path)
            {
                string generation = Path.GetFileNameWithoutExtension(path);
                this.Generation = long.Parse(generation, NumberStyles.AllowHexSpecifier);
                this.archive = ZipFile.Open(path, ZipArchiveMode.Read);

                this.Properties = ReadProperties();
            }

            private JToken ReadProperties()
            {
                ZipArchiveEntry entry = archive.GetEntry("_properties");
                using StreamReader reader = new StreamReader(entry.Open());
                return JToken.ReadFrom(new JsonTextReader(reader));
            }

            public IEnumerator<ILuceneFile> GetEnumerator()
            {
                return archive.Entries
                    .Where(entry => entry.Name != "_properties")
                    .Select(entry => {
                    using MemoryStream target = new MemoryStream();
                    using Stream source = entry.Open();
                    source.CopyTo(target);
                    return new LuceneFile(entry.Name, target.ToArray());
                }).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    archive.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}