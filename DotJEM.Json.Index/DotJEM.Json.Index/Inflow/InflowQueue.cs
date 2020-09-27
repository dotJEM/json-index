using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;

namespace DotJEM.Json.Index.Inflow
{
    public interface IInflowQueue
    {
        int Count { get; }
        IReservedSlot Reserve(string name = null, [CallerMemberName]string caller = null);
    }
    public class InflowQueue : IInflowQueue
    {
        private readonly object padlock = new object();
        private readonly Queue<IReservedSlot> queue = new Queue<IReservedSlot>();
        private readonly Thread thread;

        public int Count => queue.Count;

        public InflowQueue()
        {
            thread = new Thread(Drain);
            thread.Start();
        }

        public IReservedSlot Reserve(string name = null, [CallerMemberName] string caller = null)
        {
            IReservedSlot slot = new ReservedSlot(Pulse, caller);
            //Console.WriteLine($"InflowID: {id}");
            lock (padlock)
            {
                queue.Enqueue(slot);
                //Monitor.PulseAll(padlock);
            }
            return slot;
        }

        private void Pulse()
        {
            lock (padlock)
            {
                Monitor.PulseAll(padlock);
            }
        }


        public void Drain(object obj)
        {
            while (true)
            {
                try
                {
                    Queue<IReservedSlot> ready;
                    lock (padlock)
                    {
                        while (queue.Count < 1 || !queue.Peek().IsReady)
                        {
                            Monitor.Wait(padlock);
                        }
                        Console.WriteLine($"Draining inflow Queue of {queue.Count} objects...");
                        ready = Drain(queue);
                    }

                    // Note: We use a Queue and Dequing here so that each object can be collected right after complete is called.
                    while (ready.Count > 0)
                        ready.Dequeue().Complete();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

        private Queue<IReservedSlot> Drain(Queue<IReservedSlot> queue)
        {
            Queue<IReservedSlot> ready = new Queue<IReservedSlot>();
            while (queue.Count > 0 && queue.Peek().IsReady)
                ready.Enqueue(queue.Dequeue());
            return ready;
        }

        public override string ToString()
        {
            return $"Waiting slots in queue: {queue.Count}";
        }
    }
}