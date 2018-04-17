using System;
using System.Collections.Generic;

namespace DotJEM.Json.Index.TestUtil
{
    public static class FluentTimeExtensions
    {
        public static TimeSpan Millisecond(this int self) => TimeSpan.FromMilliseconds(self);
        public static TimeSpan Milliseconds(this int self) => TimeSpan.FromMilliseconds(self);
        public static TimeSpan Second(this int self) => TimeSpan.FromSeconds(self);
        public static TimeSpan Seconds(this int self) => TimeSpan.FromSeconds(self);
        public static TimeSpan Minute(this int self) => TimeSpan.FromMinutes(self);
        public static TimeSpan Minutes(this int self) => TimeSpan.FromMinutes(self);
        public static TimeSpan Hour(this int self) => TimeSpan.FromHours(self);
        public static TimeSpan Hours(this int self) => TimeSpan.FromHours(self);
        public static TimeSpan Day(this int self) => TimeSpan.FromDays(self);
        public static TimeSpan Days(this int self) => TimeSpan.FromDays(self);

        public static DateTime Ago(this TimeSpan self) => DateTime.Now.Subtract(self);
        public static DateTime Ahead(this TimeSpan self) => DateTime.Now.Add(self);

    }

    public static class EnumerableExtensions
    {
        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func, TSource @ifEmpty)
        {
            if (source == null) throw new ArgumentException(nameof(source));
            if (func == null) throw new ArgumentException(nameof(func));
            if (ifEmpty == null) throw new ArgumentException(nameof(ifEmpty));
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    return ifEmpty;

                TSource result = e.Current;
                while (e.MoveNext()) result = func(result, e.Current);

                return result;
            }
        }
    }
}