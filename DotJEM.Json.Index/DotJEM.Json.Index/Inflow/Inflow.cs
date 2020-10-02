﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;
using J2N.Text;

namespace DotJEM.Json.Index.Inflow
{
  

    public interface IInflowManager
    {
        IInflowScheduler Scheduler { get; }
        IInflowQueue Queue { get; }
    }

    public class InflowManager : IInflowManager
    {
        public static InflowManager Instance { get; set; }

        private readonly IAsyncInflowJobQueue jobQueue;
        private readonly IInflowCapacity capacity;

        private bool active;
        private Thread[] workers;

        public IInflowQueue Queue { get; }
        public IInflowScheduler Scheduler { get; }

        public InflowManager(IInflowCapacity capacity)
        {
            if(Instance != null) throw new InvalidOperationException("Inflow manager already created...");

            Instance = this;

            jobQueue = new AsyncInflowJobQueue();
            this.capacity = capacity ?? new NullInflowCapacity();

            Queue = new InflowQueue();
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
                //Console.WriteLine($"Executed {job.GetType().Name}...");
                capacity.Free(job.EstimatedCost);
            }
        }

        public void Start()
        {
            active = true;
            workers = Enumerable.Repeat(0, Environment.ProcessorCount * 2).Select(_ => new Thread(ConsumeLoop)).ToArray();
            foreach (Thread worker in workers)
                worker.Start();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Job Queue:");
            builder.AppendLine("==============================================");
            builder.AppendLine(jobQueue.ToString());
            builder.AppendLine();
            builder.AppendLine();

            builder.AppendLine("Capacity:");
            builder.AppendLine("==============================================");
            builder.AppendLine(capacity.ToString());
            builder.AppendLine();
            builder.AppendLine();

            builder.AppendLine("Inflow Queue:");
            builder.AppendLine("==============================================");
            builder.AppendLine(Queue.ToString());
            builder.AppendLine();
            builder.AppendLine();

            return builder.ToString();
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
            //Console.WriteLine($"Scheduling inflow job: {job.GetType()}, {priority}: count {queue.Count}, capacity: {capacity}");
            capacity.Allocate(job.EstimatedCost);
            queue.Enqueue(job, priority);
        }
    }

    public enum Priority { Highest, High, Medium, Low, Lowest }
}
