using System;
using System.Collections.Generic;
using System.Threading;

namespace DotJEM.Json.Index.Inflow
{

    public interface IAsyncInflowJobQueue
    {
        bool IsEmpty { get; }
        void Enqueue(IInflowJob job, Priority priority);
        IInflowJob Dequeue();
    }

    public class AsyncInflowJobQueue : IAsyncInflowJobQueue
    {
        private readonly object padlock = new { };
        private readonly Queue<IInflowJob> highest = new Queue<IInflowJob>();
        private readonly Queue<IInflowJob> high = new Queue<IInflowJob>();
        private readonly Queue<IInflowJob> medium = new Queue<IInflowJob>();
        private readonly Queue<IInflowJob> low = new Queue<IInflowJob>();
        private readonly Queue<IInflowJob> lowest = new Queue<IInflowJob>();
        
        public bool IsEmpty => Count == 0;
        public int Count { get; private set; } = 0;

        public IInflowJob Dequeue()
        {
            lock (padlock)
            {
                while (IsEmpty) Monitor.Wait(padlock);
                if (InternalTryDequeue(out IInflowJob job))
                    Count--;
                return job;
            }
        }

        public void Enqueue(IInflowJob job, Priority priority)
        {
            Queue<IInflowJob> queue = priority switch
            {
                Priority.Highest => highest,
                Priority.High => high,
                Priority.Medium => medium,
                Priority.Low => low,
                Priority.Lowest => lowest,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
            };

            lock (padlock)
            {
                queue.Enqueue(job);
                Count++;
                Monitor.Pulse(padlock);
            }
        }
        
        private bool InternalTryDequeue(out IInflowJob job)
        {
            job = null;
            if (highest.Count > 0)
            {
                job = highest.Dequeue();
                return true;
            }

            if (high.Count > 0)
            {
                job = high.Dequeue();
                return true;
            }

            if (medium.Count > 0)
            {
                job = medium.Dequeue();
                return true;
            }

            if (low.Count > 0)
            {
                job =  low.Dequeue();
                return true;
            }

            if (lowest.Count > 0)
            {
                job =  lowest.Dequeue();
                return true;
            }

            return false;
        }
    }
}