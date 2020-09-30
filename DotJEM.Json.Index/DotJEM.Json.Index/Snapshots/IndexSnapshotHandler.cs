using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Snapshots
{
    
    public interface IIndexSnapshotHandler
    {
        ISnapshot Snapshot(ILuceneJsonIndex index, ISnapshotTarget target);
        ISnapshot Restore(ILuceneJsonIndex index, IIndexSnapshotSource source);
    }

    public class IndexSnapshotHandler : IIndexSnapshotHandler
    {
        public ISnapshot Snapshot(ILuceneJsonIndex index, ISnapshotTarget target)
        {
            IndexWriter writer = index.WriterManager.Writer;
            SnapshotDeletionPolicy sdp = writer.Config.IndexDeletionPolicy as SnapshotDeletionPolicy;
            if (sdp == null)
            {
                throw new InvalidOperationException("Index must use an implementation of the SnapshotDeletionPolicy.");
            }

            IndexCommit commit = null;
            try
            {
                commit = sdp.Snapshot();
                Directory dir = commit.Directory;
                string segmentsFile = commit.SegmentsFileName;

                using ISnapshotWriter snapshotWriter = target.Open(commit.Generation);
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

        public ISnapshot Restore(ILuceneJsonIndex index, IIndexSnapshotSource source)
        {
            
            index.Storage.Delete();
            Directory dir = index.Storage.Directory;
            using (ISnapshotReader reader = source.Open())
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

                //NOTE: (jmd 2020-09-30) Not quite sure what this does at this time, but the Lucene Replicator does it so better keep it for now.
                IndexCommit last = DirectoryReader.ListCommits(dir).Last();
                if (last != null)
                {
                    ISet<string> commitFiles = new HashSet<string>(last.FileNames);
                    commitFiles.Add(IndexFileNames.SEGMENTS_GEN);
                }
            }
            index.WriterManager.Close();
            return null;
        }
    }
}