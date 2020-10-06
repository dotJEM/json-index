using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using DotJEM.Json.Index.Documents;
using DotJEM.Json.Index.IO;
using J2N.Text;

namespace DotJEM.Json.Index.Inflow
{
    public interface IInflowQueue
    {
        int Count { get; }
        IReservedSlot Reserve(string name = null, [CallerMemberName]string caller = null);
        IReservedSlot Split(IReservedSlot slot, [CallerMemberName] string caller = null);
    }

    public class InflowQueue : IInflowQueue
    {
        private const int SHRINK_THRESHOLD = 32;
        private const double GROW_FACTOR = 2;
        private const int CAPACITY = 32;

        private readonly object padlock = new object();
        private readonly object readlock = new object();
        private readonly object writelock = new object();

        //private readonly Queue<IReservedSlot> queue = new Queue<IReservedSlot>();
        private IReservedSlot[] array;
        private int head = 0;
        private int tail = 0;
        private int size = 0;

        private readonly Thread thread;

        public int Count => size;

        public InflowQueue()
        {
            array = new IReservedSlot[CAPACITY];

            thread = new Thread(Drain);
            thread.Start();
        }

        private int Enqueue(IReservedSlot slot)
        {
            lock (writelock)
            {
                if (size == array.Length) SetCapacity((int)(array.Length * GROW_FACTOR));

                int index = tail;
                array[tail] = slot;
                tail = (tail + 1) % array.Length;
                size++;
                return index;
            }
        }
        private IReservedSlot Dequeue()
        {
            if(size == 0) throw new InvalidOperationException("Queue was empty.");

            lock (readlock)
            {
                IReservedSlot removed = array[head];
                array[head] = null;
                head = (head + 1) % array.Length;
                size--;
                return removed;
            }
        }
        private IReservedSlot Peek()
        {
            if(size == 0) throw new InvalidOperationException("Queue was empty.");
            return array[head];
        }
        private void SetCapacity(int capacity)
        {
            lock (readlock)
            {
                array = CloneArray(capacity);
                head = 0;
                tail = (size == capacity) ? 0 : size;
            }
        }
        private IReservedSlot[] CloneArray() => CloneArray(size);
        private IReservedSlot[] CloneArray(int newSize)
        {
            ReservedSlot[] newarray = new ReservedSlot[newSize];
            if (size == 0)
                return newarray;

            if (head < tail) {
                Array.Copy(array, head, newarray, 0, size);
            } else {
                Array.Copy(array, head, newarray, 0, array.Length - head);
                Array.Copy(array, 0, newarray, array.Length - head, tail);
            }

            return newarray;
        }

        public IReservedSlot Split(IReservedSlot slot, [CallerMemberName] string caller = null)
        {
            
            int start = (((ReservedSlot) slot).Index + 1) % array.Length;
            IReservedSlot insert = new ReservedSlot(Pulse, caller) { Index = start};
            IReservedSlot move = insert;
            lock (writelock)
            {
                if (size == array.Length) SetCapacity((int)(array.Length * GROW_FACTOR));

                tail = (tail + 1) % array.Length;
                lock (readlock)
                {
                    while (start != tail)
                    {
                        IReservedSlot forward = array[start];
                        array[start] = move;
                        move = forward;
                        start = (start + 1) % array.Length;
                    }
                }
                size++;
            }
            return insert;
        }

        public IReservedSlot Reserve(string name = null, [CallerMemberName] string caller = null)
        {
            ReservedSlot slot = new ReservedSlot(Pulse, caller);
            slot.Index = Enqueue(slot);
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
                        try
                        {
                            while (size == 0 || !Peek().IsReady) Monitor.Wait(padlock);
                            Console.WriteLine($"Draining inflow Queue of {Count} objects...");
                            ready = GetReadySlots();

                        }
                        catch (Exception e)
                        {
                            // TODO: A nullreference exception was seen here, but why?
                            Console.WriteLine(e);
                            throw;
                        }
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

        private Queue<IReservedSlot> GetReadySlots()
        {
            Queue<IReservedSlot> ready = new Queue<IReservedSlot>();
            while (Count > 0 && Peek().IsReady)
                ready.Enqueue(Dequeue());
            return ready;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Waiting slots in queue: {Count}");
            foreach (IReservedSlot slot in CloneArray())
                builder.AppendLine($"  - {slot}");
            return builder.ToString();
        }
    }
}