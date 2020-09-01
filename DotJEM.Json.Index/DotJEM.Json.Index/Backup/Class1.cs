using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Index.Searching;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;


namespace DotJEM.Json.Index.Backup
{
    public static class LuceneBackupIndexExtension
    {

        public static void Backup(this ILuceneJsonIndex self, IIndexBackupTarget target)
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

                using (IIndexBackupWriter backupWriter = target.Open(commit.Generation))
                {
                    foreach (string fileName in commit.FileNames)
                    {
                        if (!fileName.Equals(segmentsFile, StringComparison.Ordinal))
                            backupWriter.AddFile(fileName, dir);
                    }

                    backupWriter.AddSegmentsFile(segmentsFile, dir);
                }
            }
            finally
            {
                if (commit != null)
                {
                    sdp.Release(commit);
                }
            }
        }

        public static void Restore(this ILuceneJsonIndex self, IIndexBackupSource source)
        {
            self.Storage.Delete();
            LuceneDirectory dir = self.Storage.Directory;
            using (IIndexBackupReader reader = source.Open())
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


                var last = DirectoryReader.ListCommits(dir).Last();

                if (last != null)
                {
                    ISet<string> commitFiles = new HashSet<string>(last.FileNames);
                    commitFiles.Add(IndexFileNames.SEGMENTS_GEN);



                }

            }

            self.WriterManager.Close();
        }
    }

    public interface IIndexBackupSource
    {
        IIndexBackupReader Open();
    }

    public class IndexZipBackupSource : IIndexBackupSource
    {
        private readonly string path;
        private readonly long? generation;

        public IndexZipBackupSource(string path, long? generation = null)
        {
            this.path = path;
            this.generation = generation;
        }

        public IIndexBackupReader Open()
        {
            if (generation == null)
            {
                string file = Directory.GetFiles(path, "*.zip")
                    .OrderByDescending(f => f)
                    .FirstOrDefault();
                //TODO: Verify generation exist!!
                return new IndexZipBackupReader(file);
            }
            //TODO: Verify generation exist!!
            return new IndexZipBackupReader(Path.Combine(path, $"{generation:x8}.zip"));
        }
    }

    public interface IIndexBackupReader : IDisposable, IEnumerable<ILuceneFile>
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

    public interface IIndexBackupTarget
    {
        IIndexBackupWriter Open(long generation);
    }

    public interface IIndexBackupWriter : IDisposable
    {
        void AddFile(string fileName, LuceneDirectory dir);
        void AddSegmentsFile(string segmentsFile, LuceneDirectory dir);
    }

    public class IndexZipBackupReader : IIndexBackupReader
    {
        private readonly ZipArchive archive;
        public long Generation { get; }

        public IndexZipBackupReader(string path)
        {
            string generation = Path.GetFileNameWithoutExtension(path);
            this.Generation = long.Parse(generation, NumberStyles.AllowHexSpecifier);
            this.archive = ZipFile.Open(path, ZipArchiveMode.Read);
        }


        public IEnumerator<ILuceneFile> GetEnumerator()
        {
            return archive.Entries.Select(entry =>
            {
                using (MemoryStream target = new MemoryStream())
                {
                    using (Stream source = entry.Open())
                    {
                        source.CopyTo(target);
                    }
                    return new LuceneFile(entry.Name, target.ToArray());
                }
            }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            this.archive.Dispose();
        }
    }

    public class IndexZipBackupWriter : IIndexBackupWriter
    {
        private readonly ZipArchive archive;
        private readonly List<string> fileListings = new List<string>();

        public IndexZipBackupWriter(string path)
        {
            this.archive = ZipFile.Open(path, ZipArchiveMode.Create);
        }

        public void AddFile(string fileName, LuceneDirectory dir)
        {
            using (IndexInputStream source = new IndexInputStream(dir.OpenInput(fileName, IOContext.READ_ONCE)))
            {
                using (Stream target = archive.CreateEntry(fileName).Open())
                {
                    source.CopyTo(target);
                }
            }
        }

        public void AddSegmentsFile(string segmentsFile, LuceneDirectory dir)
        {
            this.AddFile(segmentsFile, dir);
        }

        public void Dispose()
        {
            archive?.Dispose();
        }
    }

    public class IndexZipBackupTarget : IIndexBackupTarget
    {
        private readonly string path;

        public IndexZipBackupTarget(string path)
        {
            this.path = path;
        }

        public IIndexBackupWriter Open(long generation)
        {
            return new IndexZipBackupWriter(Path.Combine(path, $"{generation:x8}.zip"));
        }
    }
}
