using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.Storage.Snapshot;


public interface ISnapshotTarget
{
    ISnapshotWriter Open(IndexCommit commit);
}

public interface ISnapshotWriter : IDisposable
{
    void WriteFile(IndexInputStream stream);
    void WriteSegmentsFile(IndexInputStream stream);
    void WriteSegmentsGenFile(IndexInputStream stream);
}

public interface ISnapshotSource
{
    ISnapshot Open();
}


public interface ISnapshot : IDisposable
{
    long Generation { get; }
    ILuceneFile SegmentsFile { get; }
    ILuceneFile SegmentsGenFile { get; }
    IEnumerable<ILuceneFile> Files { get; }
}

public interface ILuceneFile
{
    string Name { get; }
    Stream Open();
}