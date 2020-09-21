using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;

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
            while (true)
            {
                if (draining) return;

                bool drained;
                lock (drainlock)
                {
                    if (draining) return;

                    draining = true;
                    lock (padlock)
                    {
                        drained = namedQueues.Values.Aggregate(Drain(defaultQueue), (current, queue) => current | Drain(queue));
                    }
                    draining = false;
                }

                //NOTE: Keep draining as long as we drained in any given loop.
                if (drained)
                    continue;
                break;
            }
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
}