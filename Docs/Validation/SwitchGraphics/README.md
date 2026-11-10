# Initial Nintendo Switch graphics preset — 2026-11-09

The first optimization step selects a dedicated URP quality level for UNITY_SWITCH and targets 30 rendered frames/second. These are initial configuration values, not measured console performance.

| Setting | Switch Quality | Switch Performance |
|---|---:|---:|
| Internal render scale | 0.80 | 0.75 |
| Main shadow distance | 35 m | 25 m |
| Shadow cascades | 1 | 1 |
| Main shadow map | 1024 | 1024 |
| MSAA | 1 (off) | 1 (off) |
| Additional-light / soft shadows | off | off |
| LOD bias | 1 | 1 |
| Frame cap | 30 | 30 |

Switch_Renderer uses Forward rendering, retaining the existing Better Fog feature and depth texture dependency. HDR, volume/color art controls, opaque texture behavior and shader integrations inherit the existing Mobile renderer/pipeline configuration. Weather, vegetation budgets, mud resolution, physics, simulation ticks and networking payloads are outside this step.

QualitySettings.asset adds Switch at index 2, makes it the Nintendo Switch default, and excludes PC/Mobile from that target. Switch is excluded from the other currently registered targets. Nintendo Switch 2 is unchanged. Runtime selects a named level rather than the last index: PC for standalone/desktop Editor, Switch under UNITY_SWITCH, native configured defaults for other player targets. Both in-game quality choices remain within that platform baseline. FishNet's client/server frame caps match Application.targetFrameRate so connection startup cannot restore 60 FPS on Switch; TimeManager tick/physics settings are unchanged.

RecoveryGraphicsPreference owns the temporary Performance URP clone, reuses it across menu changes and restores both the platform slot and entry quality/pipeline/LOD before releasing it. This prevents leaving a destroyed runtime asset in a quality slot between sessions. No profile schema/save migration or package changes.

## Validation

- Unity 6000.3.19f1 compiled successfully: no compilation errors.
- Eight directly invoked NUnit Edit Mode checks passed: five graphics cases and three existing solo-platform cases. Includes 8 repeated quality cycles, reused/released temporary pipeline, full QualitySettings serialized-state restoration across three session scopes, PC selection despite appended Switch level, quality inclusion/default mapping and native renderer/depth/feature references.
- Actual conditional source compiled under UNITY_SWITCH, desktop Editor symbols, and other-player/no-symbol path. This is policy-only C# compilation, not a Unity player build.
- Current Editor remains StandaloneOSX, PC quality, stopped. OpenWorldRecovery was already dirty at first inspection; its dirty state and active scene were preserved. No scene save, Play, Test Runner or live network startup.
- Pipeline switching/recompilation produced stale Volume inspector SerializedObjectNotCreatableException records. Shared Inspector tracker was rebuilt at 17:15:10 UTC. Latest read at 17:15:41 UTC still had the earlier 17:13:55 last error; no new errors after rebuild. Six historical Console records were retained, not cleared. Do not describe Console as empty.
- Existing .DS_Store changes preserved. Font atlas and manual Volume contrast/saturation changes appeared during Editor work and were left intact; they are not part of this preset change.
- No build, commit/push, real console render test or CPU/GPU/memory/FPS measurement. Device profiling remains necessary to establish sustained 30 FPS.

## Editing the preset

Open Assets/Settings/Switch_RPAsset.asset and Project Settings > Quality > Switch to adjust Quality defaults. Runtime Performance limits are in RecoveryGraphicsPreference.cs; frame cap and build-target selection are in RecoveryPlatform.cs. A normal desktop Editor Play session continues using PC; controller family and PreviewSinglePlayerOnly do not activate Switch graphics. Rollback consists of reverting this task's runtime/tests/QualitySettings changes and removing the new Switch assets with their metadata.
