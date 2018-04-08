using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Json.Index.Util
{
    public static class  TaskExtensions
    {
        public static async Task Join(this Task self, Task next)
        {
            await self.ConfigureAwait(false);
            await next.ConfigureAwait(false);
        }

        public static async Task<TResult> Then<TInput, TResult>(this Task<TInput> self, Func<TInput, TResult> success, Func<AggregateException, TResult> failure = null)
        {
            return await await self.ContinueWith(task =>
            {
                if (!task.IsFaulted)
                    return Task.FromResult(success(task.Result));

                if (failure != null)
                    return Task.FromResult(failure(task.Exception));

                return Task.FromException<TResult>(task.Exception);
            });
        }

        public static async Task<T> ThenReturn<T>(this Task self, T value)
        {
            await self.ConfigureAwait(false);
            return value;
        }
    }
}
