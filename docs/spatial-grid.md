# Spatial queries without per-enemy physics

Read [SpatialGrid.cs](../Samples/SpatialGrid.cs), then [SpatialGridTests.cs](../Tests/SpatialGridTests.cs).

Enemies live in manager-owned arrays. A grid centered on the player maps each position on the XZ plane into a cell, counts the occupants, builds prefix offsets and fills a sorted index buffer. Construction allocates the working arrays; rebuilds and queries reuse them.

Rebuilding costs O(N + C), where N is the supplied entity count and C the cell count. Radius queries visit the overlapping cell rectangle and then check squared center distances. `Query` writes into the caller's buffer and stops when it fills. `FindNearest` returns an array index or -1.

`DensestPoint` scores a nonempty cell by the population of its 3×3 neighborhood and returns that block's centroid. This is a practical targeting heuristic for area spells, rather than an exact maximum-density circle solver. The selected cell center must be in range; some members of its neighborhood can lie outside that radius.

The grid deliberately ignores entities outside its finite area. Query distances ignore height. Indices are valid for the current rebuild and must not be kept across enemy swap-removal; the full game uses stable enemy IDs for targets that survive across frames. Input capacity and valid cell-size configuration are caller responsibilities.

The existing tests cover radius filtering, ignored height, nearest targets, out-of-grid entities, a moving grid center, bounded result buffers and dense-cluster targeting.
