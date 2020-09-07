using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Documents.Fields;
using DotJEM.Json.Index.Manager;
using DotJEM.Json.Storage;
using DotJEM.Json.Storage.Adapter;
using DotJEM.Json.Storage.Adapter.Materialize.ChanceLog;
using Newtonsoft.Json.Linq;

namespace Ingest
{
    class Program
    {
        static void Main(string[] args)
        {
            IStorageContext context = new SqlServerStorageContext("Data Source=.\\DEV;Initial Catalog=SSN3DB;Integrated Security=True");

            StorageAreaIngestDataSource[] sources = context
                .AreaInfos
                .Select(info => new StorageAreaIngestDataSource(context.Area(info.Name)))
                .ToArray();

            LuceneJsonIndexBuilder builder = new LuceneJsonIndexBuilder("main");
            builder.UseMemoryStorage();

            IndexIngestHandler handler = new IndexIngestHandler(builder.Build(), new CompositeIngestDataSource(sources));
            handler.Initialize();

            DateTime start = DateTime.Now;
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
                return "Q;QUIT;EXIT".Split(';').Any(s => s.Equals(str, StringComparison.OrdinalIgnoreCase));
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
        private IObserver<IJsonIndexWriterCommand> observer;

        public StorageAreaIngestDataSource(IStorageArea area)
        {
            this.area = area;
        }

        public IDisposable Subscribe(IObserver<IJsonIndexWriterCommand> observer)
        {
            this.observer = observer;
            return null;
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
            Console.WriteLine($"Starting Ingest from {area.Name}");

            IStorageAreaLog log = area.Log;
            long initialGeneration = log.LatestGeneration;
            while (true)
            {
                IStorageChangeCollection changes = log.Get(log.CurrentGeneration >= initialGeneration, 5000);
                if (changes.Count > 0)
                {
                    FaultyChange[] faults = changes.OfType<FaultyChange>().ToArray();
                    if (faults.Length > 0)
                    {
                        Console.WriteLine($"{faults.Length} Faults found in: {area.Name}");
                    }
                    if (changes.Count.Created > 0)
                    {
                        JObject[] created = changes.Created.AsParallel().Where(c => !(c is FaultyChange)).Select(c => c.Entity).ToArray();
                        observer.OnNext(new JsonIndexMultiCreate(created));
                    }

                    if (changes.Count.Updated > 0)
                    {
                        JObject[] updated = changes.Updated.AsParallel().Where(c => !(c is FaultyChange)).Select(c => c.Entity).ToArray();
                        observer.OnNext(new JsonIndexMultiUpdate(updated));
                    }

                    if (changes.Count.Deleted > 0)
                    {
                        JObject[] deleted = changes.Deleted.AsParallel().Where(c => !(c is FaultyChange)).Select(c => c.Entity).ToArray();
                        observer.OnNext(new JsonIndexMultiDelete(deleted));
                    }

                    ingestedCount += changes.Count;

                    Console.WriteLine($"Ingesting {changes} changes from {area.Name} [{ingestedCount:N}, {changes.Generation:N}/{initialGeneration:N}]");
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
}
