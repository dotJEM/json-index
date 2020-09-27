using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Json.Index;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.Inflow;
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
using Lucene.Net.Index;
using Lucene.Net.Util;
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

            IStorageArea[] areas = context
                .AreaInfos
                .Where(info => info.Name != "audit")
                .Select(info => context.Area(info.Name))
                .ToArray();

            DbInloadingInflowCapacity cap  = null;
            LuceneJsonIndexBuilder builder = new LuceneJsonIndexBuilder("main");
            builder.UseSimpleFileStorage(path);
            builder.Services.Use<ILuceneQueryParser, SimplifiedLuceneQueryParser>();
            builder.Services.Use<IInflowCapacity>(
                resolver =>
                {
                    ILuceneJsonIndex idx = resolver.Resolve<ILuceneJsonIndex>();
                    return cap = new DbInloadingInflowCapacity(areas, idx);
                });
            ILuceneJsonIndex index = builder.Build();

            index.CreateWriter();
            cap.Initialize(10);


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

                if (str == "p")
                {
                    cap.Pause();
                    return false;
                }

                if (str == "r")
                {
                    cap.Resume();
                    return false;
                }

                if (str == "c")
                {
                    Console.WriteLine("Forcing Garbage Collection...");
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                    return false;
                }

                if (str == "f")
                {
                    InflowManager.pause = true;
                    Console.WriteLine("Forcing Flush...");
                    index.WriterManager.Writer.Flush(true, true);
                    index.WriterManager.Writer.Commit();
                    InflowManager.pause = false;
                    return false;
                }

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

                return false;
            }

            void CheckSources(object sender, EventArgs eventArgs)
            {
                TimeSpan elapsed = timer.Elapsed;
                Console.WriteLine($"Elapsed time: {elapsed.Hours}h {elapsed.Minutes}m {elapsed.Seconds}s {elapsed.Milliseconds}f");
                Console.WriteLine($"Index cached ram: {index.WriterManager.Writer.RamSizeInBytes()}");
                Console.WriteLine(InflowManager.Instance.ToString());
            }
        }
    }


    public class DbInloadingInflowCapacity : IInflowCapacity
    {
        private readonly ILuceneJsonIndex index;
        private int count;

        private readonly StorageAreaInloadSource source;
        private bool pause;

        public DbInloadingInflowCapacity(IStorageArea[] areas, ILuceneJsonIndex index)
        {
            this.index = index;
            this.source = new StorageAreaInloadSource(areas, index);
        }

        public DbInloadingInflowCapacity Initialize(int count)
        {
            for (int i = 0; i < count; i++)
                source.LoadData();
            return this;
        }

        public void Free(int estimatedCost)
        {
            if (Interlocked.Decrement(ref this.count) < 20)
            {
                CheckLoadData();
            }
        }

        private void CheckLoadData()
        {
            if(pause) return;
            
            if (count < 20 && index.CreateWriter().Inflow.Queue.Count < 50)
                source.LoadData();
            else if (count < 2)
                ThreadPool.RegisterWaitForSingleObject(new AutoResetEvent(false), (state, signaled) => CheckLoadData(), null, 5000, true);
        }

        public void Allocate(int estimatedCost)
        {
            Interlocked.Increment(ref this.count);
        }

        public override string ToString()
        {
            return count.ToString();
        }

        public void Pause()
        {
            this.pause = true;
        }

        public void Resume()
        {
            this.pause = false;
            if (count < 1)
            {
                Initialize(10);
            }
        }
    }

    public class StorageAreaInloadSource 
    {
        private int i = 0;
        private readonly IStorageArea[] areas;
        private readonly ILuceneJsonIndex index;

        public StorageAreaInloadSource(IStorageArea[] areas, ILuceneJsonIndex index)
        {
            this.areas = areas;
            this.index = index;
        }

        public void LoadData()
        {
            index.CreateWriter().Inflow.Scheduler.Enqueue(new LoadInflowJob(index, FlipArea()), Priority.Highest);
        }

        private IStorageArea FlipArea()
        {
            IStorageArea area = areas[i++];
            i %= areas.Length;
            return area;
        }
    }

    public class LoadInflowJob : IInflowJob
    {
        private readonly IStorageArea area;
        private readonly IInflowManager inflow;
        private readonly ILuceneJsonIndex index;

        public LoadInflowJob(ILuceneJsonIndex index, IStorageArea area)
        {
            this.index = index;
            this.area = area;
            this.inflow = index.CreateWriter().Inflow;
        }

        public int EstimatedCost { get; } = 1;

        public void Execute(IInflowScheduler scheduler)
        {
            IStorageChangeCollection changes = area.Log.Get(includeDeletes: false, count: 10000);
            if (changes.Count < 1)
                return;

            if (changes.Count.Created > 0) scheduler.Enqueue(new CreateDeserializeInflowJob(inflow.Queue.Reserve(area.Name), index, changes.Created), Priority.High);
            if (changes.Count.Updated > 0) scheduler.Enqueue(new UpdateDeserializeInflowJob(inflow.Queue.Reserve(area.Name), index, changes.Created), Priority.High);
            if (changes.Count.Deleted > 0) scheduler.Enqueue(new DeleteDeserializeInflowJob(inflow.Queue.Reserve(area.Name), index, changes.Created), Priority.High);
        }
    }

    public abstract class DeserializeInflowJob : IInflowJob
    {
        protected IReservedSlot Slot { get; }
        
        private readonly IEnumerable<IChangeLogRow> changes;
        private readonly ILuceneJsonIndex index;

        public int EstimatedCost { get; } = 1;

        protected DeserializeInflowJob(IReservedSlot slot, ILuceneJsonIndex index, IEnumerable<IChangeLogRow> changes)
        {
            this.Slot = slot;
            this.changes = changes;
            this.index = index;
        }

        public void Execute(IInflowScheduler scheduler)
        {
            JObject[] objects = changes
                .Select(change => change.CreateEntity())
                .ToArray();

            Write(index.CreateWriter(), objects);
            if (rand.Next(10) > 8)
            {
                index.CreateWriter().Commit();
            }
        }

        protected abstract void Write(IJsonIndexWriter writer, JObject[] objects);

        private static readonly Random rand = new Random();
    }

    public class CreateDeserializeInflowJob : DeserializeInflowJob
    {
        public CreateDeserializeInflowJob(IReservedSlot slot, ILuceneJsonIndex index, IEnumerable<IChangeLogRow> changes) : base(slot, index, changes) { }
        protected override void Write(IJsonIndexWriter writer, JObject[] objects) => writer.Create(objects, Slot);
    }

    public class UpdateDeserializeInflowJob : DeserializeInflowJob
    {
        public UpdateDeserializeInflowJob(IReservedSlot slot, ILuceneJsonIndex index, IEnumerable<IChangeLogRow> changes) : base(slot, index, changes) { }
        protected override void Write(IJsonIndexWriter writer, JObject[] objects) => writer.Update(objects, Slot);
    }

    public class DeleteDeserializeInflowJob : DeserializeInflowJob
    {
        public DeleteDeserializeInflowJob(IReservedSlot slot, ILuceneJsonIndex index, IEnumerable<IChangeLogRow> changes) : base(slot, index, changes) { }
        protected override void Write(IJsonIndexWriter writer, JObject[] objects) => writer.Delete(objects, Slot);
    }





}
