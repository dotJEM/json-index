using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DotJEM.Json.Index.Ingest
{
    public class IngestQueue<TIn, TOut>
    {
        private readonly object padlock = new object();
        private readonly IFlowControl ctrl;
        private Func<int, TIn[]> onQueueReady;
        private Func<TOut[]> onItemsReady;

        private Queue<AsyncJob<TOut>> jobs = new Queue<AsyncJob<TOut>>();
        private Queue<TOut> ready = new Queue<TOut>();

        private Thread[] workers;

        public IngestQueue(IFlowControl ctrl, Func<int, TIn[]> onQueueReady, Func<TOut[]> onItemsReady)
        {
            this.ctrl = ctrl;
            this.onQueueReady = onQueueReady;
            this.onItemsReady = onItemsReady;
            this.workers = Enumerable.Repeat(0, Environment.ProcessorCount).Select(_ => new Thread(Consume)).ToArray();
        }

        private void Consume()
        {
            while (true)
            {
                AsyncJob<TOut> task;
                lock (padlock)
                {
                    while (jobs.Count == 0) Monitor.Wait(padlock);
                    task = jobs.Dequeue();
                }

                task.Execute(Enqueue, Ready);
            }
        }

        private void Ready(TOut obj)
        {
            ready.Enqueue(obj);
        }

        public void Enqueue(AsyncJob<TOut> task)
        {
            lock (padlock)
            {
                jobs.Enqueue(task);
                Monitor.PulseAll(padlock);
            }
        }

        private void CheckCapacity()
        {
        }

        public void Start()
        {
            foreach (Thread worker in workers)
                worker.Start();
        }
    }

    public interface IFlowControl
    {
        bool HasCapacity { get; }

        void Increment();
        void Decrement();
    }

    public class AsyncJob<T>
    {
        public void Execute(Action<AsyncJob<T>> enqueue, Action<T> ready)
        {
            throw new NotImplementedException();
        }
    }
}
