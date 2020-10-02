using System;
using System.Collections.Generic;

namespace DotJEM.Json.Index.Util
{
    public static class EnumerablePartitionExtensions
    {
        public static IEnumerable<T[]> Partition<T>(this IEnumerable<T> self, int size)
        {
            int i = 0;
            T[] partition = new T[size];
            foreach (T item in self)
            {
                if (i == size)
                {
                    yield return partition;
                    partition = new T[size];
                    i = 0;
                }

                partition[i++] = item;
            }

            if (i <= 0) yield break;

            Array.Resize(ref partition, i);
            yield return partition;
        }
    }
}