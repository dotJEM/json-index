using System.Collections.Generic;

namespace DotJEM.Json.Index.Snapshots
{
    public interface ISnapshotTarget
    {
        IReadOnlyCollection<ISnapshot> Snapshots { get; }
        ISnapshotWriter Open(long generation);
    }
}