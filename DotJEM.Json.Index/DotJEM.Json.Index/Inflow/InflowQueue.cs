using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;

namespace DotJEM.Json.Index.Inflow
{
    public interface IInflowQueue
    {
        IReservedSlot Reserve(Action<IIndexWriterManager, IEnumerable<LuceneDocumentEntry>> write, string name = null, int id = -1);
    }
    public class InflowQueue : IInflowQueue
    {
        private readonly object padlock = new object();
        private readonly object drainlock = new object();
        private readonly IIndexWriterManager manager;
        private readonly Queue<IReservedSlot> defaultQueue = new Queue<IReservedSlot>();
        public readonly Queue<IReservedSlot> copy = new Queue<IReservedSlot>();
        private readonly Dictionary<string, Queue<IReservedSlot>> namedQueues = new Dictionary<string, Queue<IReservedSlot>>();

        private bool draining;

        public InflowQueue(IIndexWriterManager manager)
        {
            this.manager = manager;
        }

        public IReservedSlot Reserve(Action<IIndexWriterManager, IEnumerable<LuceneDocumentEntry>> write, string name = null, int id = -1)
        {
            if (write == null) throw new ArgumentNullException(nameof(write));
            IReservedSlot slot = new ReservedSlot(manager, write, this, id);
            lock (padlock)
            {
                Console.WriteLine($"InflowID: {id}");
                Queue<IReservedSlot> queue = SelectQueue(name);
                queue.Enqueue(slot);
                copy.Enqueue(slot);
            }
            return slot;
        }

        private Queue<IReservedSlot> SelectQueue(string name)
        {
            return defaultQueue;
            // Multi Queues may cause trouble, we have to rethink that and until then go back to a single Queue.
            // While the idea was that the order between the named Queues would not matter as the source different, when we try to commit we need to consider what this should entail. 
            // And if there is a chance that unexpected behavioir would occur. Like a commit happening before a completed task even though we anticipated it to happen after.

            //if (name == null) return defaultQueue;
            //if (namedQueues.TryGetValue(name, out var queue)) return queue;
            
            //queue = new Queue<IReservedSlot>();
            //namedQueues.Add(name, queue);
            //return queue;
        }

        public void Drain()
        {
            if (draining) 
                return;

            while (true)
            {
                bool drained;
                lock (drainlock)
                {
                    if (draining) return;

                    draining = true;
                    lock (padlock)
                    {
                        drained = Drain(defaultQueue);
                        //drained = namedQueues.Values.Aggregate(Drain(defaultQueue), (current, queue) => current | Drain(queue));
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
                slot.Complete();
            }
            return true;
        }
    }
}