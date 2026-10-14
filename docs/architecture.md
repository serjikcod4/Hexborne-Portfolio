# Architecture overview

The game separates scene composition, gameplay simulation and presentation. Confirmed environment: Unity 6000.6.0f1, URP 17.7.0, Input System 1.20.0, Extenject 9.2.0-stcf3, UniTask and Unity Test Framework 1.8.0. DOTween is used by the presentation layer.

```text
App / installers
  ├─ Gameplay → Core
  ├─ UI → Gameplay, Core
  ├─ Presentation → Gameplay, Core
  ├─ Meta → Core
  └─ Platform → Core
```

App binds dependencies through Extenject. Constructor-injected C# systems hold run logic; MonoBehaviour views handle Unity presentation. Menu is the first enabled build scene, followed by Run. A third SampleScene is also enabled in the inspected settings; this extract makes no claim that it participates in the release flow.

The hot layer stores enemies, projectiles, pickups and effects in preallocated arrays rather than one GameObject per entity. `EnemyManager` uses dense arrays and swap-removal. `SpatialGrid` supplies gameplay overlap queries. The central run loop orders movement, grid rebuilding, combat, statuses, reactions, charges, death resolution and rendering so consumers read valid indices and per-frame buffers before they are cleared.

Gameplay configuration lives in ScriptableObjects. Card definitions, build eligibility, offer generation and power application are separate responsibilities. The published sample stops before UI orchestration and card application.

Rendering batches repeated meshes and materials through `InstancedBatch`. Shader warmup in the full game enumerates materials and exercises fixed instance buckets behind a loading view. Unit motion is largely animated in vertex shaders; repeated units do not each carry an Animator.

Platform interfaces isolate saves, ads and portal lifecycle behavior. Presentation reads models and uses tweens for infrequent interface transitions. Hits and routine kills are processed through direct calls and per-frame buffers rather than a high-frequency signal bus.

This document summarizes the inspected source and project rules. The full installers, managers, platform services and scenes are intentionally not published. It is an architectural orientation, not a dependency-complete diagram of this extract.
