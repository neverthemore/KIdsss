// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Utils
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for <see cref="TimeSpan"/>.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Suspend execution of the calling async method for the specified amount of time.
        /// <remarks>
        /// This method is safe to use on WebGL platforms (unlike <see cref="Task.Delay(int)"/>).
        /// </remarks>
        /// </summary>
        /// <param name="timeSpan"> The amount of time to wait. </param>
        /// <returns> An awaiter. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static
#if UNITY_6000_0_OR_NEWER
        UnityEngine.Awaitable.Awaiter GetAwaiter(this TimeSpan timeSpan) => UnityEngine.Awaitable.WaitForSecondsAsync((float)timeSpan.TotalSeconds).GetAwaiter();
#else
        TaskAwaiter<bool> GetAwaiter(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalMilliseconds <= 0)
            {
                return Task.FromResult(true).GetAwaiter();
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();
            Await(timeSpan).ContinueWith(_ => { });
            return taskCompletionSource.Task.GetAwaiter();

            static async Task Await(TimeSpan timeSpan)
            {
                var waitUntil = DateTime.Now + timeSpan;
                do
                {
                    await Task.Yield();
                }
                while (DateTime.Now < waitUntil);
            }
        }
#endif
    }
}
