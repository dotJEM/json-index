using System.Collections.Generic;

namespace DotJEM.Json.Index.Snapshots
{
    public interface IIndexSnapshotSource
    {
        IReadOnlyCollection<ISnapshot> Snapshots { get; }
        ISnapshotReader Open();
    }
}