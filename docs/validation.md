# Validation and integration boundaries

## What was checked for publication

- Every copied source, test and screenshot matches its original SHA-256 hash.
- Publication uses an explicit file allowlist, excluding scenes, assets, builds, SDK code, secrets and private Git history.
- Local Markdown links and image references resolve inside the extract.
- The public itch.io page exposes HEXBORNE and its Run game entry point. The playable build was not replayed during publication.
- A publication scan checks credential patterns and private machine paths without printing any matched values.

Unity EditMode tests and a fresh WebGL build were **not run during this publication task**. The copied tests are existing project tests; their presence is not a new passing test result.

## Run the independent spatial-grid tests

1. Create a fresh project with Unity 6000.6.0f1 and enable Unity Test Framework.
2. Copy **only** `Samples/SpatialGrid.cs` into `Assets/Portfolio/Runtime`.
3. Add a runtime assembly definition named `Hexborne.Portfolio.Spatial` to that folder with no additional references.
4. Copy **only** `Tests/SpatialGridTests.cs` into `Assets/Portfolio/Tests/Editor`.
5. Create an Editor test assembly definition referencing `Hexborne.Portfolio.Spatial`; enable Test Assemblies so NUnit and the Unity test runner are referenced.
6. Open Test Runner → EditMode and run `Hexborne.Tests.SpatialGridTests`.

The source uses UnityEngine math types but no private game dependencies. The existing tests exercise eight scenarios. No production assembly definitions are copied, avoiding references to omitted game layers.

## Rendering sample

`InstancedBatch.cs` can be imported separately into a Unity 6 runtime assembly. It uses UnityEngine and UnityEngine.Rendering. Its XML `ShaderWarmup` reference is documentation; that class is not required by the implementation. To draw, supply a mesh and a material supporting GPU instancing. The original bucket cases are 1→1, 2→4, 4→4, 5→16, 17→64, 65→256, 256→256 and 400→256 per chunk. GPU behavior requires actual rendering and browser validation, not just these arithmetic cases.

## Card subsystem

The unchanged card files are supplied for reading with their existing project tests. They are not a ready-to-import standalone package. Production integration references **Zenject**, **PlayerConfig** (the injection constructor reads `SkillSlots`) and **SoundId** (skill audio fields). These definitions, their dependencies and the offer UI/controller remain outside this extract.

`BuildState(int skillSlots)` and the direct `CardRoller` constructor show the logic's narrow inputs. To execute the card tests, use the owning game project with its actual dependencies. Do not copy all `Samples` and `Tests` into a new project and assume the entire extract compiles. No substitutes for omitted production types are represented as original game code.
