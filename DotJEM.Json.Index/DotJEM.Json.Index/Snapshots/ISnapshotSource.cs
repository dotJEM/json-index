using System.Collections.Generic;

namespace DotJEM.Json.Index.Snapshots
{
    public interface ISnapshotSource
    {
        IReadOnlyCollection<ISnapshot> Snapshots { get; }
        ISnapshotReader Open();
    }
}