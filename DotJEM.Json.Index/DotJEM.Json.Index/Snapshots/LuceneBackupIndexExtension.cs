using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using DotJEM.Json.Index.Util;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;


namespace DotJEM.Json.Index.Snapshots
{
    public static class LuceneBackupIndexExtension
    {

        public static ISnapshot Snapshot(this ILuceneJsonIndex self, IIndexSnapshotTarget target, ISnapshotProperties properties = null)
        {
            IndexWriter writer = self.WriterManager.Writer;
            SnapshotDeletionPolicy sdp = writer.Config.IndexDeletionPolicy as SnapshotDeletionPolicy;
            if (sdp == null)
            {
                throw new InvalidOperationException("Index must use an implementation of the SnapshotDeletionPolicy.");
            }

            IndexCommit commit = null;
            try
            {
                commit = sdp.Snapshot();
                LuceneDirectory dir = commit.Directory;
                string segmentsFile = commit.SegmentsFileName;

                using IIndexSnapshotWriter snapshotWriter = target.Open(commit.Generation);

                if(properties != null) snapshotWriter.WriteProperties(properties);
                foreach (string fileName in commit.FileNames)
                {
                    if (!fileName.Equals(segmentsFile, StringComparison.Ordinal))
                        snapshotWriter.WriteFile(fileName, dir);
                }
                snapshotWriter.WriteSegmentsFile(segmentsFile, dir);
            }
            finally
            {
                if (commit != null)
                {
                    sdp.Release(commit);
                }
            }

            return target.Snapshots.Last();
        }

        public static ISnapshot Restore(this ILuceneJsonIndex self, IIndexSnapshotSource source, ISnapshotProperties properties = null)
        {
            self.Storage.Delete();
            LuceneDirectory dir = self.Storage.Directory;
            using (IIndexSnapshotReader reader = source.Open())
            {
                ILuceneFile sementsFile = null;
                List<string> files = new List<string>();
                foreach (ILuceneFile file in reader)
                {
                    if (Regex.IsMatch(file.Name, "^" + IndexFileNames.SEGMENTS + "_.*$"))
                    {
                        sementsFile = file;
                        continue;
                        
                    }
                    IndexOutput output = dir.CreateOutput(file.Name, IOContext.DEFAULT);
                    output.WriteBytes(file.Bytes, 0, file.Length);
                    output.Flush();
                    output.Dispose();

                    files.Add(file.Name);
                }
                dir.Sync(files);

                if (sementsFile == null)
                    throw new ArgumentException();

                IndexOutput segOutput = dir.CreateOutput(sementsFile.Name, IOContext.DEFAULT);
                segOutput.WriteBytes(sementsFile.Bytes, 0, sementsFile.Length);
                segOutput.Flush();
                segOutput.Dispose();

                dir.Sync(new [] { sementsFile.Name });

                SegmentInfos.WriteSegmentsGen(dir, reader.Generation);
                //var last = DirectoryReader.ListCommits(dir).Last();
                //if (last != null)
                //{
                //    ISet<string> commitFiles = new HashSet<string>(last.FileNames);
                //    commitFiles.Add(IndexFileNames.SEGMENTS_GEN);
                //}

            }

            self.WriterManager.Close();
            return null;
        }
    }

    public interface ISnapshotProperties
    {
        void WriteTo(Stream target);
    }

    public interface IIndexSnapshotSource
    {
        IIndexSnapshotReader Open();
    }

    public class IndexZipSnapshotSource : IIndexSnapshotSource
    {
        private readonly string path;
        private readonly long? generation;

        public IndexZipSnapshotSource(string path, long? generation = null)
        {
            this.path = path;
            this.generation = generation;
        }

        public IIndexSnapshotReader Open()
        {
            //TODO: Verify generation exist!!
            if (generation != null) return new IndexZipSnapshotReader(Path.Combine(path, $"{generation:x8}.zip"));
            
            string file = Directory.GetFiles(path, "*.zip")
                .OrderByDescending(f => f)
                .FirstOrDefault();
            return new IndexZipSnapshotReader(file);
        }
    }

    public interface IIndexSnapshotReader : IDisposable, IEnumerable<ILuceneFile>
    {
        long Generation { get; }
    }

    public interface ILuceneFile
    {
        string Name { get; }
        byte[] Bytes { get; }
        int Length { get; }
    }

    public class LuceneFile : ILuceneFile
    {
        public byte[] Bytes { get; }
        public int Length { get; }
        public string Name { get; }

        public LuceneFile(string name, byte[] bytes)
        {
            Bytes = bytes;
            Name = name;
            Length = bytes.Length;
        }
    }

    public interface IIndexSnapshotTarget
    {
        IReadOnlyCollection<ISnapshot> Snapshots { get; }
        IIndexSnapshotWriter Open(long generation);
    }

    public interface ISnapshot
    {
    }

    public class SingleFileSnapshot : ISnapshot
    {
        public string Path { get; }

        public SingleFileSnapshot(string path)
        {
            Path = path;
        }
    }

    public interface IIndexSnapshotWriter : IDisposable
    {
        void WriteFile(string fileName, LuceneDirectory dir);
        void WriteSegmentsFile(string segmentsFile, LuceneDirectory dir);
        void WriteProperties(ISnapshotProperties properties);
    }


    public class IndexZipSnapshotReader : Disposable, IIndexSnapshotReader
    {
        private readonly ZipArchive archive;
        public long Generation { get; }

        public IndexZipSnapshotReader(string path)
        {
            string generation = Path.GetFileNameWithoutExtension(path);
            this.Generation = long.Parse(generation, NumberStyles.AllowHexSpecifier);
            this.archive = ZipFile.Open(path, ZipArchiveMode.Read);
        }
        
        public IEnumerator<ILuceneFile> GetEnumerator()
        {
            return archive.Entries.Select(entry =>
            {
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

    public class IndexZipSnapshotWriter : Disposable, IIndexSnapshotWriter
    {
        private readonly ZipArchive archive;

        public IndexZipSnapshotWriter(string path)
        {
            this.archive = ZipFile.Open(path, ZipArchiveMode.Create);
        }

        public void WriteFile(string fileName, LuceneDirectory dir)
        {
            using IndexInputStream source = new IndexInputStream(dir.OpenInput(fileName, IOContext.READ_ONCE));
            using Stream target = archive.CreateEntry(fileName).Open();
            source.CopyTo(target);
        }

        public void WriteSegmentsFile(string segmentsFile, LuceneDirectory dir)
        {
            this.WriteFile(segmentsFile, dir);
        }

        public void WriteProperties(ISnapshotProperties properties)
        {
            using Stream target = archive.CreateEntry("_properties").Open();
            properties.WriteTo(target);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                archive?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class IndexZipSnapshotTarget : IIndexSnapshotTarget
    {
        private readonly string path;
        private readonly List<SingleFileSnapshot> snapShots = new List<SingleFileSnapshot>();

        public IReadOnlyCollection<ISnapshot> Snapshots => snapShots.AsReadOnly(); 

        public IndexZipSnapshotTarget(string path)
        {
            this.path = path;
        }

        public IIndexSnapshotWriter Open(long generation)
        {
            string snapshotPath = Path.Combine(path, $"{generation:x8}.zip");
            snapShots.Add(new SingleFileSnapshot(snapshotPath));
            return new IndexZipSnapshotWriter(snapshotPath);
        }
    }
}
