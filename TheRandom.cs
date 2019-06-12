using System;
using System.Threading;

namespace ManualPump
{
    public static class TheRandom
    {
        private static int _seed
            = Environment.TickCount;

        private static readonly ThreadLocal<Random> RandomWrapper
            = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        private static Random Instance
            => RandomWrapper.Value;

        public static decimal Next(decimal val)
            => (decimal) Instance.NextDouble() * val;
        
        public static decimal Less(int than)
            => Instance.Next(than);

        public static (int, int) Pair(int max)
            => (RandomWrapper.Value.Next(max), Instance.Next(max));

        public static decimal In(decimal from, decimal to)
            => from + (decimal)Instance.NextDouble() * (to - from);

        public static decimal Range(decimal mid, decimal dev)
            => mid - dev + 2 * Next(dev);
    }
}