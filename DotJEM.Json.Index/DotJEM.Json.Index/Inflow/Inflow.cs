﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace DotJEM.Json.Index.Inflow
{

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

    public class NullInflowCapacity : IInflowCapacity
    {
        public void Free(int estimatedCost)
        {
        }

        public void Allocate(int estimatedCost)
        {
        }
    }

    public class InflowManager : IInflowManager
    {
        private readonly IAsyncInflowJobQueue jobQueue;
        private readonly IInflowCapacity capacity;

        private bool active;
        private Thread[] workers;

        public IInflowQueue Queue { get; }
        public IInflowScheduler Scheduler { get; }

        public InflowManager(IIndexWriterManager manager, IInflowCapacity capacity)
        {
            jobQueue = new AsyncInflowJobQueue();
            this.capacity = capacity ?? new NullInflowCapacity();

            Queue = new InflowQueue(manager);
            Scheduler = new InflowScheduler(jobQueue, this.capacity);

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

    public enum Priority { Highest, High, Medium, Low, Lowest }
}
