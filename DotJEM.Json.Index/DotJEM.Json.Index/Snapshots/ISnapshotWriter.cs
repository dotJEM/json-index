using System;
using Lucene.Net.Store;

namespace DotJEM.Json.Index.Snapshots
{
    public interface ISnapshotWriter : IDisposable
    {
        void WriteFile(string fileName, Directory dir);
        void WriteSegmentsFile(string segmentsFile, Directory dir);
    }
}