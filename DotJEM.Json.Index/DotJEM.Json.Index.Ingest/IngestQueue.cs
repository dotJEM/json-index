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
        private readonly IngestScheduler scheduler = new IngestScheduler();

        private readonly Thread[] workers;

        public IngestQueue(ICapacityControl flow, IIngestInload inLoad)
        {
            this.flow = flow;
            this.inLoad = inLoad;
            this.workers = Enumerable.Repeat(0, Environment.ProcessorCount).Select(_ => new Thread(ConsumeLoop)).ToArray();
        }

        private void ConsumeLoop(object obj)
        {
            while (active || scheduler.Any)
            {
                flow.CheckCapacity(() =>
                {
                    scheduler.Enqueue(inLoad.CreateInloadJob(), JobPriority.High);
                });
                IAsyncJob job = scheduler.Next();
                IAsyncJob[] next = job.Execute(); //TODO: Execute should take the scheduller.
                if (next != null && next.Any())
                {
                    foreach (IAsyncJob asyncJob in next)
                    {
                        flow.Increment(asyncJob.Cost);
                        scheduler.Enqueue(asyncJob, JobPriority.Medium);
                    }
                }
                flow.Decrement(job.Cost);
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
        void Increment(long cost);
        void Decrement(long cost);
        void CheckCapacity(Action onHasCapacity);
    }

    public class SimpleCountingCapacityControl : ICapacityControl
    {
        private int count;

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
                Increment(1);
                onHasCapacity();
            }
        }
    }


    public interface IAsyncJob
    {
        long Cost { get; }
        IAsyncJob[] Execute();
    }


    public enum JobPriority { High, Medium, Low }

    public class IngestScheduler
    {
        private int count = 0;
        private Queue<IAsyncJob> high = new Queue<IAsyncJob>();
        private Queue<IAsyncJob> medium = new Queue<IAsyncJob>();
        private Queue<IAsyncJob> low = new Queue<IAsyncJob>();
        private object padlock = new { };

        public IngestScheduler()
        {
        }

        public bool Any => count > 0;
        public bool Empty => count == 0;
        public int Count => count;

        public IAsyncJob Next()
        {
            lock (padlock)
            {
                while (Empty) Monitor.Wait(padlock);
                count--;
                if (high.Count > 0) return high.Dequeue();
                if (medium.Count > 0) return medium.Dequeue();
                if (low.Count > 0) return low.Dequeue();
            }

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
                count++;
                queue.Enqueue(job);
                Monitor.Pulse(padlock);
            }
        }
    }
    //TODO
    // - Job Scheduler
    // - Priority Queue (Fixed, enum [High, Medium, Low])
    // - Bulking of changes - Order must be preserved.

}
