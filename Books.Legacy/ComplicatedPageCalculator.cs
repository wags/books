using System;
using System.Diagnostics;
using System.Threading;

namespace Books.Legacy
{
    /// <summary>
    /// Simulate long-running legacy, non-async code
    /// </summary>
    public class ComplicatedPageCalculator
    {
        /// <summary>
        /// Full CPU load for 5 seconds
        /// </summary>
        public int CalculateBookPages()
        {
            var watch = new Stopwatch();
            watch.Start();
            while (true)
            {
                if (watch.ElapsedMilliseconds > 5000)
                {
                    break;
                }
            }

            return 42;
        }
    }
}
