using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Ingest;
using DotJEM.Json.Index.IO;
using DotJEM.Json.Index.Manager;
using DotJEM.Json.Index.QueryParsers;
using DotJEM.Json.Index.Results;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog.ChangeObjects;
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using Lucene.Net.Util;
using Newtonsoft.Json.Linq;

namespace Ingest
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "M:\\INDEX";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path))
                File.Delete(file);

            //IndexWriterManager.DEFAULT_RAM_BUFFER_SIZE_MB = 1024 * 8; // 8GB.

            IStorageContext context = new SqlServerStorageContext("Data Source=.\\DEV;Initial Catalog=SSN3DB;Integrated Security=True");

            IStorageArea[] areas = context
                .AreaInfos
                .Where(info => info.Name != "audit")
                .Select(info => context.Area(info.Name))
                .ToArray();

            LuceneJsonIndexBuilder builder = new LuceneJsonIndexBuilder("main");
            builder.UseSimpleFileStorage(path);
            builder.Services.Use<ILuceneQueryParser, SimplifiedLuceneQueryParser>();

            ILuceneJsonIndex index = builder.Build();



            IngestQueue ingest = new IngestQueue(new SimpleCountingCapacityControl(), new StorageAreaInloadSource(areas, index));
            ingest.Start();


            Stopwatch timer = Stopwatch.StartNew();

            while (!IsExit(Console.ReadLine()))
            {
                CheckSources(null, null);
            }


            bool IsExit(string str)
            {
                if ("Q;QUIT;EXIT".Split(';').Any(s => s.Equals(str, StringComparison.OrdinalIgnoreCase)))
                    return true;

                if (string.IsNullOrEmpty(str))
                    return false;


                try
                {
                    Stopwatch searchtimer = Stopwatch.StartNew();
                    SearchResults results = index.Search(str).Take(25).Execute().Result;
                    Console.WriteLine($"Search for '{str}' resulted in {results.TotalHits} results in {searchtimer.ElapsedMilliseconds} ms...");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
                return false;
            }

            void CheckSources(object sender, EventArgs eventArgs)
            {
                TimeSpan elapsed = timer.Elapsed;
                //int ready = sources.Count(s => s.Ready);
                //Console.WriteLine($"{ready} Sources Ready of {sources.Length}");
                Console.WriteLine($"Elapsed time: {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s {elapsed.Milliseconds}f");
                JobMetrics.Print();
            }
        }
    }

    public class StorageAreaInloadSource : IIngestInload
    {
        private int i = 0;
        private readonly IStorageArea[] areas;
        private readonly ILuceneJsonIndex index;

        public StorageAreaInloadSource(IStorageArea[] areas, ILuceneJsonIndex index)
        {
            this.areas = areas;
            this.index = index;
        }

        public IAsyncJob CreateInloadJob()
        {
            Console.WriteLine("Creating a new Inload job");
            IStorageArea area = areas[i++];
            i %= areas.Length;
            return new StorageAreaLoad(area, index);
        }
    }

    public static class JobMetrics
    {
        private static Dictionary<string, Stats> statisics = new Dictionary<string, Stats>();

        public static void Initiate(string name)
        {
            Enshure(name).Initiated++;
        }

        public static void Complete(string name)
        {
            Enshure(name).Completed++;

        }

        public static void Finalize(string name)
        {
            Enshure(name).Finalized++;
        }

        private static Stats Enshure(string name)
        {
            lock (statisics)
            {
                if (!statisics.TryGetValue(name, out Stats stats))
                    statisics.Add(name, stats = new Stats(name));
                return stats;
            }
        }

        public static void Print()
        {
            Console.WriteLine("JobStatistics:");
            foreach (Stats stats in statisics.Values)
                Console.WriteLine($" - {stats}");
        }

        public class Stats
        {
            private readonly string name;

            public Stats(string name)
            {
                this.name = name;
            }

            public int Initiated { get; set; }
            public int Completed { get; set; }
            public int Finalized { get; set; }

            public override string ToString()
            {
                return $"{name}: {Initiated} Initiated, {Completed} Completed, {Finalized} Finalized...";
            }
        }
    }

    public class StorageAreaLoad : IAsyncJob
    {
        private readonly IStorageArea area;
        private readonly ILuceneJsonIndex index;

        public long Cost { get; } = 1;

        public StorageAreaLoad(IStorageArea area, ILuceneJsonIndex index)
        {
            this.area = area;
            this.index = index;
            JobMetrics.Initiate(nameof(StorageAreaLoad));
        }

        public IAsyncJob[] Execute()
        {
            IStorageChangeCollection changes = area.Log.Get(includeDeletes: false, count: 10000);
            if (changes.Count < 1)
                return null;

            JobMetrics.Complete(nameof(StorageAreaLoad));
            Console.WriteLine($"[{area.Name}] Loading {changes} changes ({changes.Generation}/{area.Log.LatestGeneration})");
            return new IAsyncJob[]
            {
                new Materialize(index, ChangeType.Create, changes.Created),
                new Materialize(index, ChangeType.Update, changes.Updated),
                new Materialize(index, ChangeType.Delete, changes.Deleted)
            };
        }

        ~StorageAreaLoad()
        {
            JobMetrics.Finalize(nameof(StorageAreaLoad));
        }
    }

    public class Materialize : IAsyncJob
    {
        public static JObject DUMMY;
        private readonly ChangeType type;
        private readonly ILuceneJsonIndex index;
        private readonly List<IChangeLogRow> changes;

        public long Cost => changes.Count;

        public Materialize(ILuceneJsonIndex index, ChangeType type, IEnumerable<IChangeLogRow> changes)
        {
            this.index = index;
            this.type = type;
            this.changes = changes.ToList();
            JobMetrics.Initiate(nameof(Materialize));
        }

        public IAsyncJob[] Execute()
        {
            Console.WriteLine($"Executing {GetType()} materializing {changes.Count} objects...");
            JObject[] objects = changes
                .Select(change => DUMMY ?? change.CreateEntity())
                .ToArray();
            JobMetrics.Complete(nameof(Materialize));
            return new[] { CreateJob(objects) };
        }

        private IAsyncJob CreateJob(JObject[] objects)
        {
            switch (type)
            {
                case ChangeType.Create:
                    return new CreateWriteJob(index, objects);
                case ChangeType.Update:
                    return new UpdateWriteJob(index, objects);
                case ChangeType.Delete:
                    return new DeleteWriteJob(index, objects);
            }
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        ~Materialize()
        {
            JobMetrics.Finalize(nameof(Materialize));
        }
    }

    public abstract class WriteJob : IAsyncJob
    {
        private static Random rand = new Random();
        private readonly ILuceneJsonIndex index;
        private readonly JObject[] objects;
        private readonly string name;

        public long Cost => objects.LongLength;

        public WriteJob(ILuceneJsonIndex index, JObject[] objects)
        {
            this.index = index;
            this.objects = objects;
            this.name = this.GetType().Name;
            JobMetrics.Initiate(name);
        }

        public IAsyncJob[] Execute()
        {
            try
            {
                Console.WriteLine($"Executing {GetType()} writing {objects.Length} objects...");
                //Write(objects, index.CreateWriter());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            JobMetrics.Complete(name);
            return rand.Next(10) >= 9 ? new IAsyncJob[] { new CommitJob(index) } : null;
        }

        protected abstract void Write(JObject[] objects, IJsonIndexWriter writer);

        ~WriteJob()
        {
            JobMetrics.Finalize(name);
        }
    }

    public class CommitJob : IAsyncJob
    {
        private readonly ILuceneJsonIndex index;

        public long Cost { get; } = 1;

        public CommitJob(ILuceneJsonIndex index)
        {
            this.index = index;
            JobMetrics.Initiate(nameof(CommitJob));
        }

        public IAsyncJob[] Execute()
        {
            Console.WriteLine($"Executing {GetType()} committing objects...");
            index.CreateWriter().Commit();
            JobMetrics.Complete(nameof(CommitJob));
            return null;
        }

        ~CommitJob()
        {
            JobMetrics.Finalize(nameof(CommitJob));
        }
    }

    public class CreateWriteJob : WriteJob
    {
        public CreateWriteJob(ILuceneJsonIndex index, JObject[] objects) : base(index, objects)
        {
        }

        protected override void Write(JObject[] objects, IJsonIndexWriter writer)
        {
            writer.Create(objects);
        }
    }

    public class UpdateWriteJob : WriteJob
    {
        public UpdateWriteJob(ILuceneJsonIndex index, JObject[] objects) : base(index, objects)
        {
        }

        protected override void Write(JObject[] objects, IJsonIndexWriter writer)
        {
            writer.Update(objects);
        }
    }

    public class DeleteWriteJob : WriteJob
    {
        public DeleteWriteJob(ILuceneJsonIndex index, JObject[] objects) : base(index, objects)
        {
        }

        protected override void Write(JObject[] objects, IJsonIndexWriter writer)
        {
            writer.Delete(objects);
        }
    }


    //public class StorageAreaIngestDataSource : IIngestDataSource
    //{
    //    private readonly IStorageArea area;
    //    private AsyncIngestQueue queue = new AsyncIngestQueue();

    //    public StorageAreaIngestDataSource(IStorageArea area)
    //    {
    //        this.area = area;
    //    }

    //    public IDisposable Subscribe(IObserver<IJsonIndexWriterCommand> observer)
    //    {
    //        return queue.Subscribe(observer);
    //    }

    //    public void SaveState(IIngestDataSourceState state)
    //    {
    //    }

    //    public void RestoreState(IIngestDataSourceState state)
    //    {
    //    }

    //    public void Start()
    //    {
    //        Task.Factory.StartNew(Ingest, TaskCreationOptions.LongRunning);
    //    }

    //    private async Task Ingest()
    //    {
    //        IStorageAreaLog log = area.Log;
    //        long initialGeneration = log.LatestGeneration;
    //        while (true)
    //        {
    //            IStorageChangeCollection changes = log.Get(log.CurrentGeneration >= initialGeneration, 5000);
    //            if (changes.Count > 0)
    //            {
    //                queue.Enqueue(changes);
    //                ingestedCount += changes.Count;

    //                Console.WriteLine($"Ingesting {changes} changes from {area.Name} [{ingestedCount:N}, {changes.Generation:N}/{initialGeneration:N}]");
    //                await Task.Delay(10000);
    //                if (changes.Count == 5000)
    //                    continue;
    //            }
    //            else
    //            {
    //                if (!Ready)
    //                {
    //                    Console.WriteLine($"No changes from {area.Name}");
    //                    Ready = true;
    //                    OnReady?.Invoke(this, EventArgs.Empty);
    //                }
    //            }
    //            await Task.Delay(10000);
    //        }
    //    }

    //    private long ingestedCount = 0;
    //    public bool Ready { get; private set; } = false;
    //    public event EventHandler<EventArgs> OnReady;
    //}

    //public class AsyncIngestQueue : IObservable<IJsonIndexWriterCommand>
    //{
    //    private readonly Queue<Task<DbIngestCommand>> commands = new Queue<Task<DbIngestCommand>>();

    //    public void Enqueue(IStorageChangeCollection changes)
    //    {
    //        Drain();
    //        commands.Enqueue(Task.Run(() => Materialize(changes)));
    //    }

    //    private Task asyncDrain;
    //    private IObserver<IJsonIndexWriterCommand> observer;

    //    private void Drain()
    //    {
    //        if (asyncDrain == null || asyncDrain.IsCompleted)
    //            asyncDrain = InternalDrain();

    //        Task InternalDrain()
    //        {
    //            return Task.Factory.StartNew(async () =>
    //            {
    //                while (true)
    //                {
    //                    while (commands.Count > 0)
    //                    {
    //                        try
    //                        {
    //                            DbIngestCommand command = await commands.Dequeue();
    //                            Console.WriteLine($"Draining {command.ChangesCount}");
    //                            observer.OnNext(command);
    //                            Console.WriteLine($"Drained {command.ChangesCount}");
    //                        }
    //                        catch (Exception e)
    //                        {
    //                            Console.WriteLine(e);
    //                        }
    //                    }
    //                    await Task.Delay(1000);
    //                }
    //            }, TaskCreationOptions.LongRunning);
    //        }
    //    }
    //    private DbIngestCommand Materialize(IStorageChangeCollection changes)
    //    {
    //        var cmd = changes.Partitioned.Aggregate(new DbIngestCommand(changes.Count), (command, change) => command.Enqueue(change));
    //        Console.WriteLine($"Materialized {changes} changes from {changes.StorageArea}");
    //        return cmd;
    //    }

    //    public IDisposable Subscribe(IObserver<IJsonIndexWriterCommand> observer)
    //    {
    //        this.observer = observer;
    //        return null;
    //    }

    //}

    //class DbIngestCommand : IJsonIndexWriterCommand
    //{
    //    public ChangeCount ChangesCount { get; }

    //    private readonly JObject[] created;
    //    private readonly JObject[] updated;
    //    private readonly JObject[] deleted;

    //    private int createOffset;
    //    private int updateOffset;
    //    private int deleteOffset;


    //    public DbIngestCommand(ChangeCount changesCount)
    //    {
    //        ChangesCount = changesCount;
    //        created = new JObject[changesCount.Created];
    //        updated = new JObject[changesCount.Updated];
    //        deleted = new JObject[changesCount.Deleted];
    //    }

    //    public DbIngestCommand Enqueue(Change change)
    //    {
    //        switch (change)
    //        {
    //            case SqlServerInsertedChange _:
    //                created[createOffset++] = change.CreateEntity();
    //                break;
    //            case SqlServerEntityChange regularChange:
    //                switch (regularChange.Type)
    //                {
    //                    case ChangeType.Create:
    //                        created[createOffset++] = change.CreateEntity();
    //                        break;
    //                    case ChangeType.Update:
    //                        updated[updateOffset++] = change.CreateEntity();
    //                        break;
    //                    case ChangeType.Delete:
    //                        deleted[deleteOffset++] = change.CreateEntity();
    //                        break;
    //                }
    //                break;
    //            case SqlServerDeleteChange _:
    //                deleted[deleteOffset++] = change.CreateEntity();
    //                break;
    //        }

    //        return this;
    //    }

    //    public void Execute(IJsonIndexWriter writer)
    //    {
    //        try
    //        {
    //            writer.Create(created);
    //            writer.Update(updated);
    //            writer.Delete(deleted);
    //            //writer.Commit();
    //            //writer.Flush(true, true);
    //        }
    //        catch (Exception e)
    //        {
    //            Console.WriteLine(e);
    //            throw;
    //        }
    //    }
    //}

    //internal class JsonIndexNullCommand : IJsonIndexWriterCommand
    //{
    //    public void Execute(IJsonIndexWriter writer)
    //    {
    //    }
    //}



}
