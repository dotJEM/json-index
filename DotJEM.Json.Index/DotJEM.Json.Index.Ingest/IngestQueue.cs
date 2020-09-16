using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index.Ingest
{
    public class IngestQueue
    {
        private bool active;
        private readonly object padlock = new object();
        private readonly ICapacityControl flow;
        private readonly IIngestInload inLoad;
        private readonly IngestScheduler scheduler;

        private readonly Thread[] workers;

        public static bool pause = false;

        public IngestQueue(ICapacityControl flow, IIngestInload inLoad)
        {
            scheduler = new IngestScheduler(flow);
            this.flow = flow;
            this.inLoad = inLoad;
            this.workers = Enumerable.Repeat(0, Environment.ProcessorCount).Select(_ => new Thread(ConsumeLoop)).ToArray();
        }

        public void CheckCapacity()
        {
            flow.CheckCapacity(() =>
            {
                scheduler.Enqueue(inLoad.CreateInloadJob(), JobPriority.Medium);
            });
        }

        private void ConsumeLoop(object obj)
        {
            while (active || scheduler.Any)
            {
                if (pause)
                {
                    Thread.Sleep(5000);
                    continue;
                }

                CheckCapacity();
                IAsyncJob job = scheduler.Next();
                job.Execute(scheduler); //TODO: Execute should take the scheduller.
                scheduler.Decrement(job.Cost);
            }
        }

        public void Start()
        {
            active = true;
            foreach (Thread worker in workers)
                worker.Start();
        }
    }

    public interface IIngestInload
    {
        IAsyncJob CreateInloadJob();
    }

    public interface ICapacityControl
    {
        int ActiveJobs { get; }
        void Increment(long cost);
        void Decrement(long cost);
        void CheckCapacity(Action onHasCapacity);
    }

    public class SimpleCountingCapacityControl : ICapacityControl
    {
        private int count;

        public int ActiveJobs => count;

        public void Increment(long cost)
        {
            Interlocked.Increment(ref count);
        }

        public void Decrement(long cost)
        {
            Interlocked.Decrement(ref count);
        }

        public void CheckCapacity(Action onHasCapacity)
        {
            if (count >= 20) return;

            lock (this)
            {
                if (count >= 20) return;
                onHasCapacity();
            }
        }
    }


    public interface IAsyncJob
    {
        long Cost { get; }
        void Execute(IScheduler scheduler);
    }


    public enum JobPriority { High, Medium, Low }

    public interface IScheduler
    {
        void Enqueue(IAsyncJob job, JobPriority priority);
    }

    public class IngestScheduler : IScheduler
    {
        private readonly Queue<IAsyncJob> high = new Queue<IAsyncJob>();
        private readonly Queue<IAsyncJob> medium = new Queue<IAsyncJob>();
        private readonly Queue<IAsyncJob> low = new Queue<IAsyncJob>();
        private readonly ICapacityControl capacity;

        private object padlock = new { };

        public IngestScheduler(ICapacityControl capacity)
        {
            this.capacity = capacity;
        }

        public bool Any => !Empty;
        public bool Empty => Waiting == 0;
        public int Waiting => high.Count + medium.Count + low.Count;

        public int Count => capacity.ActiveJobs;

        public IAsyncJob Next()
        {
            lock (padlock)
            {
                while (Empty) Monitor.Wait(padlock);
                return InternalDequeue();
            }
        }

        public void Decrement(long jobCost)
        {
            capacity.Decrement(jobCost);
        }

        private IAsyncJob InternalDequeue()
        {
            if (high.Count > 0) return high.Dequeue();
            if (medium.Count > 0) return medium.Dequeue();
            if (low.Count > 0) return low.Dequeue();
            return null;
        }

        public void Enqueue(IAsyncJob job, JobPriority priority)
        {
            Queue<IAsyncJob> queue = priority switch
            {
                JobPriority.High => high,
                JobPriority.Medium => medium,
                JobPriority.Low => low,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
            };

            lock (padlock)
            {
                queue.Enqueue(job);
                capacity.Increment(job.Cost);
                Monitor.PulseAll(padlock);
            }
        }
    }
    //TODO
    // - Job Scheduler
    // - Priority Queue (Fixed, enum [High, Medium, Low])
    // - Bulking of changes - Order must be preserved.

}
