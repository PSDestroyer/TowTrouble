# Garage zoom regression — 2026-11-01

## Report and baseline

User screenshots show Speedster Electron fitted, with the Body view extremely far from the car; switching to the Stock Front Bumper remains too far away. Expected: a readable body overview and a close front/rear detail view after fitting or previewing parts.

Unity 6000.3.19f1, OpenWorldRecovery scene, Editor initially outside Play Mode; no compilation errors. Prior static fixtures stripped RecoveryVehicleEffects and checked upper size/viewport bounds only, so they missed an interaction with runtime-created effect renderers and did not reject a tiny car.

## Investigation and reproduction

- Scale/Camera configuration: actual scene drivers have unit scale, Main Camera FOV57/aspect16:9. These do not explain the zoom.
- Effects hypothesis: GarageBounds enumerated every Renderer subclass under the current assembly. RecoveryVehicleEffects creates world-space Exhaust vapor ParticleSystemRenderers below visible exhaust meshes. Particle/trail/line bounds describe simulation content, not fitted car geometry.
- The actual Speedster Electron exhaust component was retained and rebound in a preview-scene fixture. An idle effect with zero particles has zero bounds and does **not** reproduce the problem by itself.
- A frozen conservative 24×24×24 local effect bound reproduces the aggregation error without emission or simulation. The body extents expand from (1.117,1.174,2.159) to (20.420,15.298,20.289), changing the requested overview distance from14.953m to187.517m. `Before.json` records this pre-fix experiment.
- This confirms an erroneous framing mechanism and a regression that explains the screenshot's symptom. The exact live particle bounds from the user's run were not captured; no claim of an interactive Play Mode reproduction is made.

## Correction

RecoveryCustomizationGarageFocus.LocalBounds accepts MeshRenderer and SkinnedMeshRenderer geometry only. ParticleSystemRenderer, TrailRenderer and LineRenderer no longer resize the body or selected detail. No arbitrary maximum camera-distance cap is used. No VFX behavior, scene, prefab, catalogue, saved profile, economy or network data changes.

The same frozen effect bounds after the correction leave both the body and distance unchanged at14.953m (`After.json`).

## Focused regression coverage

Added permanent `RecoveryGarageCameraTests`:

1. All16 slots on the reported Speedster Electron keep identical body/detail bounds and requested distances after real exhaust rebinding plus oversized particle, line and trail bounds.
2. All38 body choices, repeated application of each fitted build, then compatible front and rear bumper replacement.114 camera trajectories,180 explicit unscaled mathematical steps each; actual production GarageCameraPose helper, no PlayerLoop. Checks convergence, camera stays outside body, focus within UI preview column, and car projection wider than150 reference pixels (underfill check missing previously). Four parked vehicle headings exercised.

Both test methods passed when invoked directly in Edit Mode. Test Runner was not used.

## Scene-backed visual checks

`WorldCapture.cs` renders16 category views with the actual county/garage environment, source Main Camera lens parameters, real Speedster geometry and retained exhaust effects. A temporary preview-scene clone occupies driver0's authored garage spawn. Native driver renderers are temporarily forced off during captures and restored in finally, avoiding duplicate cars. A separate3-view sequence (`StockCapture.cs`) checks Body → Stock Front Bumper → Stock Rear Bumper from the report. Camera movement uses the production helper and rotation interpolation, with environment linecasts retained; all requested distances converge without environment clipping at this spawn. Body overview14.95m, stock front4.64m, stock rear4.64m; fitted bumper detail3.2m. See WorldChecks/StockWorldChecks and JPGs.

No Play Mode, physics/particle simulation or emission, networking, builds, profile loads/saves or persisted scene mutations were performed. These are controlled Edit Mode checks; live input, network acknowledgement timing and real running particle bounds remain outside this validation. Existing user instruction reserves Play Mode testing to the user.

Backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/GarageCameraBoundsRegression-20261101`.
