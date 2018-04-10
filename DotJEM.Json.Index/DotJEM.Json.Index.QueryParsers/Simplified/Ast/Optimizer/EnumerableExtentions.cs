using System;
using System.Collections.Generic;

namespace DotJEM.Json.Index.QueryParsers.Simplified.Ast.Optimizer
{
    public static class EnumerableExtentions
    {
        public static (IEnumerable<TOut1>, IEnumerable<TOut2>)  Split<TIn, TOut1, TOut2>(this IEnumerable<TIn> source, Func<TIn, (TOut1, TOut2)> splitter)
        {
            TwinConsumerCollection<TIn, TOut1, TOut2> twin = new TwinConsumerCollection<TIn, TOut1, TOut2>(source, splitter);
            return (twin.Out1, twin.Out2);
        }
    }
}