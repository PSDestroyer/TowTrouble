# R01 visible wheel blockage and release — 2026-11-01

User requested a visually obvious trapped wheel and a clear release moment. This extends the existing R01 extraction prototype without changing its terrain heights, extraction forces, distance/hold rules or other site records.

## Implemented

- Scene-owned exposed-earth bed, irregular feathered cut banks, small loose soil clods and a light exit lip, aligned with R01's blocked front-left wheel and the actual 2.2 m exit plane. The meshes follow the existing terrain. Loose-earth shoulders are cosmetic and at most 12 cm high; the dressing adds no colliders.
- Owned meshes/materials under `Assets/TowTrouble/OpenWorld/Visuals/Extraction`, using existing native terrain texture and project URP shaders. Source assets and shared wheel-track shader/materials are untouched.
- A single existing R01 birch is moved 6 m outward and 2 m forward from its saved baseline to clear the original recovery sightline. Its native art, scale, rotation and ground embed are preserved. No forest regeneration runs. `TreeBaseline.json` makes reapplication repeatable.
- R01's camera framing shifts smoothly to the left side and centers both vehicles, with separation-scaled offset and slightly higher elevation. Manual orbit, elevation controls and C reset remain available. The framing remains through the active R01 contract to avoid flipping sides at release; other contracts and solo driving retain their previous settings.
- Existing world marker points directly to the wheel: caught / climbing out / green wheel-freed confirmation. After the release confirmation it returns to the delivery marker.
- `RecoveryExtractionVisuals` has scene-authored, manually emitted world-space soil dust (max 18) and clods (max 14). Native soft-particle shader and an owned low-poly clod mesh; no automatic emission, looping or collision. Effects gain controls the count, including zero. Existing authoritative stage transition triggers the burst once per run on each peer. Observer presentation has a longer ground-ray fallback if the short suspension probe briefly misses while interpolating. No new network message or physics force.
- Stopping/restarting a contract clears the effect. Late-joining an already-released recovery does not replay its burst.

## Files

- `Runtime/RecoveryExtractionVisuals.cs`: bounded native release particles and cleanup.
- `Runtime/RecoveryWorldExtraction.cs`: serialized binding and existing once-per-release presentation trigger.
- `Runtime/RecoveryHudExtraction.cs`, `RecoveryHud.cs`: wheel marker and clean color reset.
- `Runtime/RecoveryCamera.cs`: scoped recovery framing.
- `Editor/ExtractionVisualAuthoring.cs`: repeatable visual authoring, one-tree sightline correction and native asset bindings.
- `Editor/ExtractionPrototypeAuthoring.cs`: invokes the visual pass after applying the prototype.
- `Tests/Editor/RecoveryExtractionTests.cs`: disabled-effects/budget regression alongside existing extraction checks.

Menu: **Tow Trouble > Recovery > Refresh R01 Extraction Visuals**. Open `OpenWorldRecovery` in Edit Mode first. No manual component wiring is required.

## Verification

- Unity Editor compilation passed; no feature or shader errors.
- 24 direct Edit Mode checks passed, including existing extraction/scenario/cable/delivery rules. No Test Runner was started.
- All 24 serialized site records stayed identical to the pre-task snapshot. Terrain asset hash and runtime force/state model are unchanged.
- World visual binding valid, missing scripts zero, added visual colliders zero. Materials use valid shaders. Particle budgets/configuration inspected without running particles.
- Reapplication preserved child count, mesh topology signatures and birch position; no duplicate dressing or accumulating tree offset.
- Static close-up and representative recovery-frame renders inspected, including original camera occlusion, the blocked wheel and a posed exit state. `ReleasedGameplay-posed.png` moves visual clones and arranges clod proxies; it is not a live particle capture or proof of animation timing.
- Editor remained stopped. No Play Mode, particle simulation, physics simulation, network startup, player build, commit or push.

Reports: `Audit.json`, `Rules.json`, `Preservation.json`. Backups and static preview script: `/Users/gheorghe/Desktop/TowTrublw/Backups/ExtractionVisuals-20261101`.

## User test

1. Play manually, Explore, accept R01. Approach the car and reset camera with C if needed. Check that the wheel, dark rut and exit lip are legible before connecting.
2. Connect and pull along the trail. Watch the wheel marker change as the car physically moves, followed by the short contact-point dust/clod burst and green confirmation at release.
3. Verify the camera frames both cars through turning and changing cable length; manual orbit must still work.
4. Cancel/reaccept and recover again. Expect one release burst each run, no idle or repeated emissions.
5. Try effects gain zero and retest co-op with matching scripts when ready. Live particle motion, feel, camera transitions, controller interaction and multiplayer are still for user validation.

Unity AI Assistant's account-service/network warning is unrelated to this feature.
