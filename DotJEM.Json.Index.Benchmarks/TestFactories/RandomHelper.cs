using System;
using System.Collections.Generic;
using System.Linq;

namespace DotJEM.Json.Index.Benchmarks.TestFactories
{
    public static class RandomHelper
    {
        private static readonly Random rand = new Random();

        public static T RandomItem<T>(this IEnumerable<T> items)
        {
            ICollection<T> list = items as ICollection<T> ?? items.ToArray();
            return list.ElementAt(rand.Next(0, list.Count()));
        }

        public static T[] RandomItems<T>(this IEnumerable<T> items, int take)
        {
            ICollection<T> list = items as ICollection<T> ?? items.ToArray();
            return list.Skip(rand.Next(list.Count-take)).Take(take).ToArray();
        }

        public static IEnumerable<int> RandomSequence(int lenght, int maxValue, bool allowRepeats)
        {
            var sequence = RandomSequence(maxValue);
            if (!allowRepeats)
                sequence = sequence.Distinct();
            return sequence.Take(lenght);
        }

        private static IEnumerable<int> RandomSequence(int maxValue)
        {
            while (true) yield return rand.Next(maxValue);
        }
    }
}