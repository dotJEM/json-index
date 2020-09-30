using System;
using System.Collections.Generic;

namespace DotJEM.Json.Index.Snapshots
{
    public interface ISnapshotReader : IDisposable, IEnumerable<ILuceneFile>
    {
        long Generation { get; }
    }
}