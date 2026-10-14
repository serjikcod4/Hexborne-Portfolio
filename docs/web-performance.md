# Fixed instance buckets on WebGL

Read [InstancedBatch.cs](../Samples/InstancedBatch.cs).

The project's WebGL investigation found first-use shader compilation stalls when instance counts varied freely. The solution was to draw only fixed bucket sizes: **1, 4, 16, 64 and 256**. A draw with five real instances pads to sixteen hidden instances. Four hundred real instances use a 256-instance chunk followed by a 256-instance padded chunk.

Each batch owns one mesh/material pair and a preallocated matrix array. Padding uses tiny invertible transforms far below the arena; materials require GPU instancing. `Flush` emits each chunk and clears the count. The array length accommodates the last padded chunk. `Add` drops instances beyond capacity; missing mesh/material clears without drawing. Huge fixed bounds avoid fine-grained culling and make the padding/culling tradeoff explicit.

The full game's `ShaderWarmup` enumerates relevant materials and exercises all buckets behind the loading screen, delaying simulation until warmup completes. It is outside this extract because its material enumeration depends on most game configs. The source XML comment says "first frame"; current project notes describe staged warmup: first display the loading screen, then process a few materials per frame.

## Recorded project measurements

These are historical measurements recorded in the local project's engineering notes (`CLAUDE.md`, performance section), not benchmarks rerun for this portfolio or guarantees for every browser/device.

| Recorded scenario | Result | Limits |
| --- | --- | --- |
| Earlier cold-cache all-skills run before bucket/warmup changes | About 20 frames above 100 ms; worst 513 ms | Earlier content snapshot, desktop browser |
| Cold cache after bucket/warmup changes | No frames above 100 ms after loading; worst 3–11 ms; first-visit load about 3.7 s | Earlier content snapshot, same investigation |
| October 12 content build, Fireball, 400 enemies, no HUD | 780–810 FPS; gameplay about 0.20 ms/frame | Release WebGL, vsync disabled, 960×600, warm profile |
| October 12 richer spell/Heart setup | 430–600 FPS; gameplay about 0.33–0.48 ms/frame | Back-to-back runs varied about 30%; desktop only |

The October 12 notes report a 16.5 MB build, no GC collections in those measured windows, and known heap growth around 33 bytes/frame. This is evidence of a bounded, allocation-conscious hot layer, not a claim of zero allocations across the entire application. Current release size and mobile performance were not independently measured for this publication.

The mechanism trades extra hidden GPU instances and a loading-time warmup for fewer distinct shader programs during play. Bucket sizing and warmup coverage are review topics; the specific observed behavior is scoped to this project's tested WebGL backend.
