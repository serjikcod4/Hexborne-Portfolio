using System.Collections.Generic;
using Hexborne.Gameplay;

namespace Hexborne.Tests
{
    /// <summary>Deterministic random for tests: returns queued values, then falls back to a seeded generator.</summary>
    public sealed class QueueRandom : IRandomSource
    {
        private readonly Queue<float> values = new Queue<float>();
        private readonly Queue<int> ints = new Queue<int>();
        private readonly SystemRandomSource fallback = new SystemRandomSource(1234);

        public QueueRandom WithValues(params float[] v) { foreach (float x in v) values.Enqueue(x); return this; }
        public QueueRandom WithInts(params int[] v) { foreach (int x in v) ints.Enqueue(x); return this; }

        public float Value01() => values.Count > 0 ? values.Dequeue() : fallback.Value01();

        public int Range(int minInclusive, int maxExclusive)
        {
            if (ints.Count == 0) return fallback.Range(minInclusive, maxExclusive);
            int v = ints.Dequeue();
            return v < minInclusive ? minInclusive : v >= maxExclusive ? maxExclusive - 1 : v;
        }
    }
}
