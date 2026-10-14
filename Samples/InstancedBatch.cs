using UnityEngine;
using UnityEngine.Rendering;

namespace Hexborne.Gameplay
{
    /// <summary>
    /// Collects matrices during the frame and draws them with GPU instancing.
    /// One batch = one mesh + one material. Material must have GPU Instancing enabled.
    /// <para>
    /// Every draw uses one of a few fixed instance counts (<see cref="BucketOf"/>: 1, 4, 16, 64, 256); the rest of
    /// the bucket is padded with hidden instances. The WebGL backend builds a new shader program for every distinct
    /// instance count it sees, which froze the first minute of play with 100–550 ms compiles; with fixed buckets a
    /// material needs only five programs, and <see cref="ShaderWarmup"/> compiles them all on the first frame.
    /// </para>
    /// </summary>
    public sealed class InstancedBatch
    {
        /// <summary>Largest bucket. Bigger batches are drawn in chunks of this size.</summary>
        public const int ChunkSize = 256;
        /// <summary>Ratio between neighbouring buckets.</summary>
        public const int BucketStep = 4;

        private static readonly Bounds HugeBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        // Far below the arena and a tenth of a millimetre wide: never visible, never in a shadow frustum, invertible.
        private static readonly Matrix4x4 Hidden =
            Matrix4x4.TRS(new Vector3(0f, -1000f, 0f), Quaternion.identity, new Vector3(0.0001f, 0.0001f, 0.0001f));

        private readonly Mesh mesh;
        private readonly Matrix4x4[] matrices;
        private readonly int capacity;
        private RenderParams renderParams;
        private int count;

        public InstancedBatch(Mesh mesh, Material material, int capacity, bool castShadows = false)
        {
            this.mesh = mesh;
            this.capacity = capacity;
            matrices = new Matrix4x4[PaddedLength(capacity)];
            renderParams = new RenderParams(material)
            {
                shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                receiveShadows = true,
                worldBounds = HugeBounds
            };
        }

        /// <summary>Instance count actually drawn for <paramref name="n"/> instances: the smallest of 1, 4, 16, 64, 256 that fits.</summary>
        public static int BucketOf(int n)
        {
            int bucket = 1;
            while (bucket < n && bucket < ChunkSize) bucket *= BucketStep;
            return bucket;
        }

        public void Add(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (count < capacity) matrices[count++] = Matrix4x4.TRS(position, rotation, scale);
        }

        /// <summary>Draws everything added this frame and clears the batch.</summary>
        public void Flush()
        {
            if (mesh == null || renderParams.material == null) { count = 0; return; }
            for (int start = 0; start < count; start += ChunkSize)
            {
                int n = Mathf.Min(ChunkSize, count - start);
                int drawn = BucketOf(n);
                for (int k = start + n; k < start + drawn; k++) matrices[k] = Hidden;
                Graphics.RenderMeshInstanced(renderParams, mesh, 0, matrices, drawn, start);
            }
            count = 0;
        }

        /// <summary>Array length that fits every chunk padded up to its bucket.</summary>
        private static int PaddedLength(int capacity)
        {
            if (capacity <= ChunkSize) return BucketOf(Mathf.Max(1, capacity));
            return (capacity + ChunkSize - 1) / ChunkSize * ChunkSize;
        }
    }
}
