namespace DotJEM.Json.Index.Snapshots
{
    public static class LuceneSnapshotIndexExtension
    {
        public static ISnapshot Snapshot(this ILuceneJsonIndex self, ISnapshotTarget target)
        {
            IIndexSnapshotHandler handler = self.Services.Resolve<IIndexSnapshotHandler>() ?? new IndexSnapshotHandler();
            return handler.Snapshot(self, target);
        }

        public static ISnapshot Restore(this ILuceneJsonIndex self, ISnapshotSource source)
        {
            IIndexSnapshotHandler handler = self.Services.Resolve<IIndexSnapshotHandler>() ?? new IndexSnapshotHandler();
            return handler.Restore(self, source);
        }
    }
}
