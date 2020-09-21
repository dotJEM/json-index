using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Inflow
{
    public interface IInflowQueue
    {
        IReservedSlot Reserve(Action<IIndexWriterManager, IEnumerable<LuceneDocumentEntry>> write, string name = null);
    }

    public class InflowQueue : IInflowQueue
    {
        private readonly object padlock = new object();
        private readonly object drainlock = new object();
        private readonly IIndexWriterManager manager;
        private readonly Queue<IReservedSlot> defaultQueue = new Queue<IReservedSlot>();
        private readonly Dictionary<string, Queue<IReservedSlot>> namedQueues = new Dictionary<string, Queue<IReservedSlot>>();

        private bool draining;

        public InflowQueue(IIndexWriterManager manager)
        {
            this.manager = manager;
        }

        public IReservedSlot Reserve(Action<IIndexWriterManager, IEnumerable<LuceneDocumentEntry>> write, string name = null)
        {
            if (write == null) throw new ArgumentNullException(nameof(write));
            IReservedSlot slow = new ReservedSlot(write, this);
            lock (padlock)
            {
                Queue<IReservedSlot> queue = SelectQueue(name);
                queue.Enqueue(slow);
            }
            return slow;
        }

        private Queue<IReservedSlot> SelectQueue(string name)
        {
            if (name == null) return defaultQueue;
            if (namedQueues.TryGetValue(name, out var queue)) return queue;
            
            queue = new Queue<IReservedSlot>();
            namedQueues.Add(name, queue);
            return queue;
        }

        public void Drain()
        {
            if(draining)
                return;

            bool drained;
            lock (drainlock)
            {
                if(draining)
                    return;

                draining = true;
                drained = namedQueues
                    .Values
                    .Aggregate(Drain(defaultQueue), (current, queue) => current | Drain(queue));
                draining = false;
            }

            //NOTE: Keep draining as long as we drained in any given loop.
            if (drained) Drain();
        }

        private bool Drain(Queue<IReservedSlot> queue)
        {
            if (queue.Count < 1)
                return false;

            while (queue.Count > 0 && queue.Peek().IsReady)
            {
                IReservedSlot slot = queue.Dequeue();
                slot.Complete(manager);
            }
            return true;
        }
    }

    public interface IReservedSlot
    {
        bool IsReady { get; }
        void Ready(IEnumerable<LuceneDocumentEntry> documents);
        void Complete(IIndexWriterManager writer);
    }

    public class ReservedSlot : IReservedSlot
    {
        private readonly Action<IIndexWriterManager, IEnumerable<LuceneDocumentEntry>> write;
        private readonly InflowQueue queue;
        private IEnumerable<LuceneDocumentEntry> documents;

        public bool IsReady { get; private set; }

        public ReservedSlot(Action<IIndexWriterManager, IEnumerable<LuceneDocumentEntry>> write, InflowQueue queue)
        {
            this.write = write;
            this.queue = queue;
        }

        public void Ready(IEnumerable<LuceneDocumentEntry> documents)
        {
            this.documents = documents;
            this.IsReady = true;
            this.queue.Drain();
        }

        public void Complete(IIndexWriterManager writer)
        {
            write(writer, documents);
        }
    }

    public interface IInflowManager
    {
        IInflowScheduler Scheduler { get; }
        IInflowQueue Queue { get; }
    }

    public interface IInflowCapacity
    {
        void Free(int estimatedCost);
        void Allocate(int estimatedCost);
    }

    public class InflowManager : IInflowManager
    {
        private readonly IAsyncInflowJobQueue jobQueue;
        private readonly IInflowCapacity capacity;

        private bool active;
        private Thread[] workers;

        public IInflowQueue Queue { get; }
        public IInflowScheduler Scheduler { get; }

        public InflowManager(IIndexWriterManager manager)
        {
            jobQueue = new AsyncInflowJobQueue();
            
            Queue = new InflowQueue(manager);
            Scheduler = new InflowScheduler(jobQueue, capacity);

            //TODO: Start on scheduler enqueue.
            Start();
        }
        
        private void ConsumeLoop(object obj)
        {
            while (active)
            {
                IInflowJob job = jobQueue.Dequeue();
                job.Execute(Scheduler);
                capacity.Free(job.EstimatedCost);
            }
        }

        public void Start()
        {
            active = true;
            workers = Enumerable.Repeat(0, Environment.ProcessorCount).Select(_ => new Thread(ConsumeLoop)).ToArray();
            foreach (Thread worker in workers)
                worker.Start();
        }

    }

    public interface IInflowJob
    {
        int EstimatedCost { get; }
        void Execute(IInflowScheduler scheduler);
    }

    public interface IInflowScheduler
    {
        void Enqueue(IInflowJob job, Priority priority);
    }

    public class InflowScheduler : IInflowScheduler
    {
        private readonly IInflowCapacity capacity;
        private readonly IAsyncInflowJobQueue queue;

        public InflowScheduler(IAsyncInflowJobQueue queue, IInflowCapacity capacity)
        {
            this.queue = queue;
            this.capacity = capacity;
        }

        public void Enqueue(IInflowJob job, Priority priority)
        {
            capacity.Allocate(job.EstimatedCost);
            queue.Enqueue(job, priority);
        }
    }
    public class ConvertDocuments : IInflowJob
    {
        public int EstimatedCost { get; } = 1;
     
        private readonly IReservedSlot slot;
        private readonly IEnumerable<JObject> docs;
        private readonly ILuceneDocumentFactory factory;

        public ConvertDocuments(IReservedSlot slot, IEnumerable<JObject> docs, ILuceneDocumentFactory factory)
        {
            this.slot = slot;
            this.docs = docs;
            this.factory = factory;
        }

        public void Execute(IInflowScheduler scheduler)
        {
            List<LuceneDocumentEntry> documents = factory
                .Create(docs)
                .ToList();
            scheduler.Enqueue(new WriteDocuments(slot, documents), Priority.Highest);
        }
    }

    public class WriteDocuments : IInflowJob
    {
        public int EstimatedCost { get; } = 1;
      
        private readonly IReservedSlot slot;
        private readonly IEnumerable<LuceneDocumentEntry> documents;

        public WriteDocuments(IReservedSlot slot, IEnumerable<LuceneDocumentEntry> documents)
        {
            this.slot = slot;
            this.documents = documents;
        }

        public void Execute(IInflowScheduler scheduler) => slot.Ready(documents);
    }

    public enum Priority { Highest, High, Medium, Low, Lowest }
}
