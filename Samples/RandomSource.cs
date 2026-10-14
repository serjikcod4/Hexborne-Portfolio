namespace Hexborne.Gameplay
{
    /// <summary>Injectable randomness so gameplay logic stays testable.</summary>
    public interface IRandomSource
    {
        /// <summary>Uniform float in [0, 1).</summary>
        float Value01();

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly System.Random random;

        public SystemRandomSource() : this(System.Environment.TickCount) { }

        public SystemRandomSource(int seed) { random = new System.Random(seed); }

        public float Value01() => (float)random.NextDouble();

        public int Range(int minInclusive, int maxExclusive) => random.Next(minInclusive, maxExclusive);
    }
}
