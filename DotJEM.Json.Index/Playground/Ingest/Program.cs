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
using DotJEM.Json.Storage.Adapter.Materialize.Log;
using Newtonsoft.Json.Linq;

namespace Ingest
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = "G:\\INDEX";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path))
                File.Delete(file);

            //IndexWriterManager.DEFAULT_RAM_BUFFER_SIZE_MB = 1024 * 8; // 8GB.

            IStorageContext context = new SqlServerStorageContext("Data Source=.\\DEV;Initial Catalog=SSN3DB;Integrated Security=True");

            StorageAreaIngestDataSource[] sources = context
                .AreaInfos
                .Where(info => info.Name != "audit")
                .Select(info => new StorageAreaIngestDataSource(context.Area(info.Name)))
                .ToArray();

            LuceneJsonIndexBuilder builder = new LuceneJsonIndexBuilder("main");
            builder.UseSimpleFileStorage(path);
            builder.Services.Use<ILuceneQueryParser, SimplifiedLuceneQueryParser>();

            ILuceneJsonIndex index = builder.Build();

            IndexIngestHandler handler = new IndexIngestHandler(index, new CompositeIngestDataSource(sources));
            handler.Initialize();

            Stopwatch timer = Stopwatch.StartNew();
            
            foreach (StorageAreaIngestDataSource source in sources)
            {
                source.Start();
                source.OnReady += CheckSources;
            }

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
                int ready = sources.Count(s => s.Ready);
                Console.WriteLine($"{ready} Sources Ready of {sources.Length}");
                Console.WriteLine($"Elapsed time: {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s {elapsed.Milliseconds}f");
            }
        }
    }

    public class StorageAreaIngestDataSource : IIngestDataSource
    {
        private readonly IStorageArea area;
        private AsyncIngestQueue queue = new AsyncIngestQueue();

        public StorageAreaIngestDataSource(IStorageArea area)
        {
            this.area = area;
        }

        public IDisposable Subscribe(IObserver<IJsonIndexWriterCommand> observer)
        {
            return queue.Subscribe(observer);
        }

        public void SaveState(IIngestDataSourceState state)
        {
        }

        public void RestoreState(IIngestDataSourceState state)
        {
        }

        public void Start()
        {
            Task.Factory.StartNew(Ingest, TaskCreationOptions.LongRunning);
        }

        private async Task Ingest()
        {
            IStorageAreaLog log = area.Log;
            long initialGeneration = log.LatestGeneration;
            while (true)
            {
                IStorageChangeCollection changes = log.Get(log.CurrentGeneration >= initialGeneration, 5000);
                if (changes.Count > 0)
                {
                    queue.Enqueue(changes);
                    ingestedCount += changes.Count;

                    Console.WriteLine($"Ingesting {changes} changes from {area.Name} [{ingestedCount:N}, {changes.Generation:N}/{initialGeneration:N}]");
                    await Task.Delay(10000);
                    if (changes.Count == 5000)
                        continue;
                }
                else
                {
                    if (!Ready)
                    {
                        Console.WriteLine($"No changes from {area.Name}");
                        Ready = true;
                        OnReady?.Invoke(this, EventArgs.Empty);
                    }
                }
                await Task.Delay(10000);
            }
        }

        private long ingestedCount = 0;
        public bool Ready { get; private set; } = false;
        public event EventHandler<EventArgs> OnReady;
    }

    public class AsyncIngestQueue : IObservable<IJsonIndexWriterCommand>
    {
        private readonly Queue<Task<DbIngestCommand>> commands = new Queue<Task<DbIngestCommand>>();

        public void Enqueue(IStorageChangeCollection changes)
        {
            Drain();
            commands.Enqueue(Task.Run(() => Materialize(changes)));
        }

        private Task asyncDrain;
        private IObserver<IJsonIndexWriterCommand> observer;

        private void Drain()
        {
            if (asyncDrain == null || asyncDrain.IsCompleted)
                asyncDrain = InternalDrain();

            Task InternalDrain()
            {
                return Task.Factory.StartNew(async () =>
                {
                    while (true)
                    {
                        while (commands.Count > 0)
                        {
                            try
                            {
                                DbIngestCommand command = await commands.Dequeue();
                                Console.WriteLine($"Draining {command.ChangesCount}");
                                observer.OnNext(command);
                                Console.WriteLine($"Drained {command.ChangesCount}");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        }
                        await Task.Delay(1000);
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }
        private DbIngestCommand Materialize(IStorageChangeCollection changes)
        {
            var cmd =  changes.Partitioned.Aggregate(new DbIngestCommand(changes.Count), (command, change) => command.Enqueue(change));
            Console.WriteLine($"Materialized {changes} changes from {changes.StorageArea}");
            return cmd;
        }

        public IDisposable Subscribe(IObserver<IJsonIndexWriterCommand> observer)
        {
            this.observer = observer;
            return null;
        }

    }

    class DbIngestCommand : IJsonIndexWriterCommand
    {
        public ChangeCount ChangesCount { get; }

        private readonly JObject[] created;
        private readonly JObject[] updated;
        private readonly JObject[] deleted;

        private int createOffset;
        private int updateOffset;
        private int deleteOffset;


        public DbIngestCommand(ChangeCount changesCount)
        {
            ChangesCount = changesCount;
            created = new JObject[changesCount.Created];
            updated = new JObject[changesCount.Updated];
            deleted = new JObject[changesCount.Deleted];
        }

        public DbIngestCommand Enqueue(Change change)
        {
            switch (change)
            {
                case SqlServerInsertedChange _:
                    created[createOffset++] = change.CreateEntity();
                    break;
                case SqlServerEntityChange regularChange:
                    switch (regularChange.Type)
                    {
                        case ChangeType.Create:
                            created[createOffset++] = change.CreateEntity();
                            break;
                        case ChangeType.Update:
                            updated[updateOffset++] = change.CreateEntity();
                            break;
                        case ChangeType.Delete:
                            deleted[deleteOffset++] = change.CreateEntity();
                            break;
                    }
                    break;
                case SqlServerDeleteChange _:
                    deleted[deleteOffset++] = change.CreateEntity();
                    break;
            }
            
            return this;
        }

        public void Execute(IJsonIndexWriter writer)
        {
            try
            {
                writer.Create(created);
                writer.Update(updated);
                writer.Delete(deleted);
                //writer.Commit();
                //writer.Flush(true, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    internal class JsonIndexNullCommand : IJsonIndexWriterCommand
    {
        public void Execute(IJsonIndexWriter writer)
        {
        }
    }



}
