# Wheel dust visibility — 2026-11-10

The user reported that wheel particles were effectively invisible and the game lacked a readable dust trail. Native inspection of OpenWorldRecovery confirmed all three vehicles have four valid coarse/fine emitter pairs, a track resolver, enabled billboard renderers and a supported powder shader. This was not a missing-material or missing-binding problem in the inspected scene.

The previous fine emission was only 32% of the coarse rate, with 0.12–0.24 m puffs lasting 0.65 s on dirt/sand, alpha 0.24 and a 14-particle limit per wheel. Grass emitted no fines. At 5 m/s without slip, a representative steady snapshot had only two fine particles per wheel on dirt/sand, which barely read from the normal follow-camera distance. The old chassis dust is intentionally suppressed when the wheel system owns VFX.

## Change

- Fine emission has a separate surface budget, reaching 16 puffs/s per wheel on dirt/sand/snow. Ordinary grounded rolling produces a trail without requiring wheelspin. Grass gets a modest soil haze; wet mud and stone remain sparse, and wood/airborne/rest remain silent. Existing coarse debris rules and trajectories are unchanged.
- Dry-ground start size grows from 0.48 to 0.78 m with speed, expands over life, and lasts approximately 1.55 s. Dust tint/opacity now differs from the coarse grains. Each puff receives independent size/lifetime/position variation and follows the existing normal and rolling-direction calculation.
- Shared native ConfigurePowder applies world space, manual emission, expansion/fade and a 28-particle cap per wheel. RecoveryWheelMud.OnEnable applies it at runtime, including to older serialized scene settings; the authoring menu uses the same helper. No scene save is required to receive the fix.
- Maximum is 112 fine +112 coarse particles per vehicle, 672 total across the three authored vehicles. No new material, shader, texture, particle collision, runtime manager or network message. Increased transparent coverage has not been profiled on Switch.

## Validation

- Connected Unity 6000.3.19f1, URP 17.3.0, CLI 1.0.0-beta.17 (current version checked). OpenWorldRecovery was already dirty and was preserved without saving.
- Compilation succeeds with zero compiler errors/warnings: Compile.json.
- Six NUnit methods invoked directly in Edit Mode pass, including two new regression methods for grounded activity, no phantom dust, ordinary rolling and the bounded fine budget: Tests.json. No Test Runner/Play Mode run.
- Twelve native checks on disposable copies of the actual scene powder systems pass: four wheels on Scout, Hauler and Stranded Classic. Verified cap, world space, manual-only/stopped initialization, expansion/fade, renderer support and no shader errors: NativeChecks.json.
- Six native 1280×720 static renders use actual map locations/models, the production shader and the main camera's 57° FOV at the regular (6,5.6,-9) follow offset. Before/After pairs show representative steady dust ages at 5 m/s: Sand 8→84, Snow 12→68, Dirt 8→96 actual particles across four wheels. Particle counts and baked geometry were read back, not inferred from emitter configuration. Images were visually inspected and the new plumes are readable.
- These are controlled presentation snapshots, not a recorded drive or a measurement of surface classification along a route. R20 and R13 provide sand/snow backgrounds; the Dirt pair deliberately illustrates the dirt style against the R24 background. Temporary systems were initialized with Play and a zero-time Simulate before SetParticles/Pause; the game never entered Play Mode and no physical simulation ran.
- Source vehicle/world JSON, active scene and initial dirty state were preserved. All 21 protected files (both scenes, existing package manifest/lock changes, quality settings and vehicle-effect assets) remain hash-identical. Temporary preview objects were destroyed. No save-system execution or user-profile writes.
- Existing Console history was retained: UV errors from RecoveryMudSurface, stale Better Fog/URP Inspector exceptions and the AI Account timeout predated this task. Inspector exceptions recurred during recompilation; do not interpret the successful compile as an empty historical Console.

## In-game acceptance still required

Drive normally on dirt and sand, reverse, and briefly spin grounded wheels. A fading trail should remain behind each contact; grass should be lighter, snow white, mud sparse. Stop or lift the wheels: new emission should cease while existing puffs finish fading. Pause/resume, return to the menu, swap body/wheels and retry. Confirm visibility and frame time on Switch and with a second real peer; neither hardware nor multiplayer was tested here. No build, commit or push was made.
