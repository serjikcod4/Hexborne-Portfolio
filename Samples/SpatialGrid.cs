using UnityEngine;

namespace Hexborne.Gameplay
{
    /// <summary>
    /// Uniform grid centred on the player, rebuilt every frame with a counting sort.
    /// Zero allocations after construction. All gameplay radius queries go through it.
    /// Entities outside the grid area are ignored by queries (they are far off-screen).
    /// </summary>
    public sealed class SpatialGrid
    {
        private readonly float cellSize;
        private readonly float inverseCellSize;
        private readonly int side;
        private readonly int[] cellStart;
        private readonly int[] cellCursor;
        private readonly int[] sorted;
        private readonly int[] entityCell;

        private Vector3[] positions;
        private int count;
        private float originX;
        private float originZ;

        public SpatialGrid(float cellSize, int cellsPerSide, int capacity)
        {
            this.cellSize = cellSize;
            inverseCellSize = 1f / cellSize;
            side = cellsPerSide;
            cellStart = new int[side * side + 1];
            cellCursor = new int[side * side];
            sorted = new int[capacity];
            entityCell = new int[capacity];
        }

        public int Count => count;

        public void Rebuild(Vector3 center, Vector3[] entityPositions, int entityCount)
        {
            positions = entityPositions;
            count = entityCount;
            float half = side * cellSize * 0.5f;
            originX = center.x - half;
            originZ = center.z - half;

            System.Array.Clear(cellCursor, 0, cellCursor.Length);
            for (int i = 0; i < count; i++)
            {
                int cell = CellOf(positions[i]);
                entityCell[i] = cell;
                if (cell >= 0) cellCursor[cell]++;
            }

            cellStart[0] = 0;
            for (int c = 0; c < cellCursor.Length; c++)
            {
                cellStart[c + 1] = cellStart[c] + cellCursor[c];
                cellCursor[c] = cellStart[c];
            }

            for (int i = 0; i < count; i++)
            {
                int cell = entityCell[i];
                if (cell >= 0) sorted[cellCursor[cell]++] = i;
            }
        }

        /// <summary>Writes indices of entities within radius (centre distance) into results. Returns how many were written.</summary>
        public int Query(Vector3 center, float radius, int[] results)
        {
            int written = 0;
            float radiusSq = radius * radius;
            CellRange(center, radius, out int minX, out int maxX, out int minZ, out int maxZ);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int cell = z * side + x;
                    for (int s = cellStart[cell]; s < cellStart[cell + 1]; s++)
                    {
                        int index = sorted[s];
                        float dx = positions[index].x - center.x;
                        float dz = positions[index].z - center.z;
                        if (dx * dx + dz * dz <= radiusSq)
                        {
                            if (written == results.Length) return written;
                            results[written++] = index;
                        }
                    }
                }
            }
            return written;
        }

        /// <summary>Index of the nearest entity within maxRadius, or -1.</summary>
        public int FindNearest(Vector3 center, float maxRadius)
        {
            int best = -1;
            float bestSq = maxRadius * maxRadius;
            CellRange(center, maxRadius, out int minX, out int maxX, out int minZ, out int maxZ);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int cell = z * side + x;
                    for (int s = cellStart[cell]; s < cellStart[cell + 1]; s++)
                    {
                        int index = sorted[s];
                        float dx = positions[index].x - center.x;
                        float dz = positions[index].z - center.z;
                        float distSq = dx * dx + dz * dz;
                        if (distSq <= bestSq)
                        {
                            bestSq = distSq;
                            best = index;
                        }
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// Finds the most crowded spot within <paramref name="radius"/>: the non-empty cell whose 3×3 block holds
        /// the most entities. <paramref name="point"/> is the centroid of that block. False if nothing is in range.
        /// </summary>
        public bool DensestPoint(Vector3 center, float radius, out Vector3 point)
        {
            point = center;
            int bestCell = -1;
            int bestScore = 0;
            float radiusSq = radius * radius;
            CellRange(center, radius, out int minX, out int maxX, out int minZ, out int maxZ);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int cell = z * side + x;
                    if (cellStart[cell] == cellStart[cell + 1]) continue;
                    float cx = originX + (x + 0.5f) * cellSize - center.x;
                    float cz = originZ + (z + 0.5f) * cellSize - center.z;
                    if (cx * cx + cz * cz > radiusSq) continue;

                    int score = 0;
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nz = z + dz;
                        if (nz < 0 || nz >= side) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= side) continue;
                            int n = nz * side + nx;
                            score += cellStart[n + 1] - cellStart[n];
                        }
                    }
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCell = cell;
                    }
                }
            }
            if (bestCell < 0) return false;

            int bx = bestCell % side;
            int bz = bestCell / side;
            Vector3 sum = Vector3.zero;
            int total = 0;
            for (int z = Mathf.Max(0, bz - 1); z <= Mathf.Min(side - 1, bz + 1); z++)
            {
                for (int x = Mathf.Max(0, bx - 1); x <= Mathf.Min(side - 1, bx + 1); x++)
                {
                    int cell = z * side + x;
                    for (int s = cellStart[cell]; s < cellStart[cell + 1]; s++)
                    {
                        sum += positions[sorted[s]];
                        total++;
                    }
                }
            }
            point = sum / total;
            point.y = 0f;
            return true;
        }

        private int CellOf(Vector3 p)
        {
            int x = (int)Mathf.Floor((p.x - originX) * inverseCellSize);
            int z = (int)Mathf.Floor((p.z - originZ) * inverseCellSize);
            if (x < 0 || z < 0 || x >= side || z >= side) return -1;
            return z * side + x;
        }

        private void CellRange(Vector3 center, float radius, out int minX, out int maxX, out int minZ, out int maxZ)
        {
            minX = Mathf.Max(0, (int)Mathf.Floor((center.x - radius - originX) * inverseCellSize));
            maxX = Mathf.Min(side - 1, (int)Mathf.Floor((center.x + radius - originX) * inverseCellSize));
            minZ = Mathf.Max(0, (int)Mathf.Floor((center.z - radius - originZ) * inverseCellSize));
            maxZ = Mathf.Min(side - 1, (int)Mathf.Floor((center.z + radius - originZ) * inverseCellSize));
        }
    }
}
