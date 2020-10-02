using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotJEM.Json.Index.Snapshots.Zip
{
    public class ZipSnapshotSource : ISnapshotSource
    {
        private readonly string path;
        private readonly long? generation;

        public ZipSnapshotSource(string path, long? generation = null)
        {
            this.path = path;
            this.generation = generation;
        }

        public IReadOnlyCollection<ISnapshot> Snapshots => InternalGetSnapshots().ToList().AsReadOnly();

        public ISnapshotReader Open()
        {
            //TODO: Verify generation exist!!
            if (generation != null) return new ZipSnapshotReader(Path.Combine(path, $"{generation:x8}.zip"));
       
            ZipFileSnapshot latest = InternalGetSnapshots()
                .FirstOrDefault();

            if(latest == null)
                throw new InvalidOperationException($"There was no snapsots found in the directory: {path}");
            return new ZipSnapshotReader(latest);
        }


        private IEnumerable<ZipFileSnapshot> InternalGetSnapshots()
        {
            return Directory.GetFiles(path, "*.zip")
                .Select(file => new ZipFileSnapshot(file))
                .OrderByDescending(f => f.Generation);
        }
    }
}