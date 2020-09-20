using System;
using System.Collections.Generic;

namespace Ingest
{
    public static class Metrics
    {
        private static readonly Dictionary<string, Stats> statisics = new Dictionary<string, Stats>();

        public static void Initiate(string name)
        {
            Enshure(name).Initiated++;
        }

        public static void Complete(string name)
        {
            Enshure(name).Completed++;

        }

        public static void Finalize(string name)
        {
            Enshure(name).Finalized++;
        }

        private static Stats Enshure(string name)
        {
            lock (statisics)
            {
                if (!statisics.TryGetValue(name, out Stats stats))
                    statisics.Add(name, stats = new Stats(name));
                return stats;
            }
        }

        public static void Print()
        {
            Console.WriteLine("JobStatistics:");
            foreach (Stats stats in statisics.Values)
                Console.WriteLine($" - {stats}");
        }

        public class Stats
        {
            private readonly string name;

            public Stats(string name)
            {
                this.name = name;
            }

            public int Initiated { get; set; }
            public int Completed { get; set; }
            public int Finalized { get; set; }

            public override string ToString()
            {
                return $"{name}: {Initiated} Initiated, {Completed} Completed, {Finalized} Finalized...";
            }
        }
    }
}