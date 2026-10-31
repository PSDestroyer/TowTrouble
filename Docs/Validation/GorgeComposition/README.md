# Mountain gorge composition — 2026-10-31

The straight upper water chute and repeated mountain boulder rows were replaced in `OpenWorldRecovery.unity`. The current game scene remains saved in Edit Mode. No Play Mode, builds, Test Runner, physics simulation or network startup were used.

## Result

- Three water drops (18 m, 15.7 m and 20.9 m) connect curved plunge pools. The downstream channel, lake clipping and four surveyed bridges remain connected.
- Sixteen scene-owned cliff/shelf meshes derived from SoStylized Classic prefabs. Their feet are recessed into terrain, with the driving corridor protected. Native triplanar rock materials have dry, wet/moss and snow variants; vendor material inheritance is flattened in the owned copies.
- Local landform bulges and erosion gullies, snow/soil/rock transitions, talus clusters, ferns, grass and 16 trees around stable ledges. The authored group contains 298 direct children, including the 16 massifs.
- Three game-owned URP flowing curtains (1,683 total vertices), nine impact foam patches and three bounded mist emitters (72 particles maximum combined). Water and foam motion use shader time. The native flat river/lake material remains. Mist is configured for runtime; its live appearance was not simulated in the Editor.
- The six affected northern contract photos were refreshed. UI layout, economy, contract graph and FishNet authority are unchanged.

## Validation

`SceneChecks.json` and `GorgeChecks.json` were produced by direct static Edit Mode inspection:

- 18,213 road-lane samples: zero missing supports, obstacles or buried samples; max sampled grade 22.05°.
- 363 supported reset points; all 24 recoveries and eight deliveries retained.
- 440 river samples: none dry or uphill; no trees in the river corridor. No authored massif covers the sampled upper river center.
- Four supported bridge crossings; maximum ramp end mismatch approximately 0.10 m.
- Zero missing scripts or shader errors in inspected scene materials. C# compilation completed without errors or warnings.

`Cascade-After.png`, `Mountains-After.png`, `Pools-After.png`, `Cascade-FromRoad.png` and `Cliffs-FromRoad.png` are static Camera.Render images of the actual scene. Before images use the same first two camera poses but were captured without the additional URP review camera settings. These images are visual review evidence, not proof of runtime physics, frame rate or multiplayer behavior. User owns driving, live VFX/performance and multiplayer testing.

## Authoring and preservation

`Tow Trouble > Art Integration > 8 - Compose Mountain Gorge` is the focused entry point. `Apply Complete Art Pass` includes the geology stage before the ground-map bake and photos. Avoid running individual legacy scenery generators afterward.

The focused pass restores local height/splat baselines from the owned `Gorge original heights.bytes` and `Gorge original splats.bytes` before carving. The lower river always uses the graded road baseline, so repeated passes do not accumulate a bridge ramp lip. Fixed placements and seeded dressing make repeated authoring deterministic for this map. These baselines belong to this version of the map; recapture them deliberately if the underlying road/terrain design changes.

Backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/TowCounty-before-geology-20261031-211003.tar.gz`. `Preservation.json` confirms unchanged tutorial, original Mega/Desert scenes and OpenWorldPlan; no vendor assets changed. No commit or push was created.
