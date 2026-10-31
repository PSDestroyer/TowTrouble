# All-surface wheel tracks — 2026-10-31

Request: leave wheel marks everywhere the car drives, with a response appropriate to the contact surface. Integrated in OpenWorldRecovery. Baseline Git dd631ff was clean; the active scene contained the user's unsaved camera distance of 7. Saved a pre-change scene copy including that value to /Users/gheorghe/Desktop/TowTrublw/Backups/TracksSurfaces-20261031/OpenWorldRecovery-before.unity. No Play Mode, Test Runner, physics simulation, networking startup or player builds were used.

## Implemented
- Six visual responses: dirt, sand, snow, grass, hard surface and wood. Soft surfaces have shallow raised shoulders and darker tread centers; grass has a dark compressed appearance; hard/wood surfaces have zero rut relief and faint tire prints, intensified by existing wheel slip. This is visual relief, not permanent terrain excavation or actual grass-mesh deformation.
- A separate byte grid in RecoveryGroundMap classifies every terrain cell from the authored paint. It does not change the existing traction kinds/cells. Road-mask restrictions no longer suppress grass/forest/rock impressions.
- Contact geometry projects onto the actual collider. Terrain sampling is confined to TerrainCollider; bridge and rock contacts raycast the same collider, preventing marks from appearing on the terrain below a deck. Terrain holes, unsupported strip edges, teleports, changes of material and vehicle geometry break or clip marks.
- All four bridges (four driving decks/eight ramps) explicitly identify timber. Other solid ground colliders default to hard surface unless marked with RecoveryTrackSurface. Native Living Mud colliders remain owned by their original stamp/coating system and receive no duplicate strip.
- The original bounded pool remains 36 chunks x128 segments, shared across all material kinds and all three vehicles. Old chunks recycle as travel continues; these are local session effects, not saved or synchronized history. Physics/network authority and messages are unchanged.
- Editor entry: Tow Trouble/Art Integration/10 - Refresh Surface Wheel Tracks. Existing complete/gorge/mountain workflows already call ConfigureWheelTracks. BakeGroundSurfaceMap also rebuilds visual track classification after regenerating the grip asset.

## Evidence
- Connected Editor compiled runtime and tests with zero errors/warnings.
- Checks.json: ten direct Edit Mode checks passed. Includes paint selection without grip changes, projection onto tilted decks, edge/hole rejection, mud exclusivity, teleport/material breaks and existing map behavior. No Test Runner was launched.
- SceneIntegration.json: six materials, 262144 classified cells (117340 grass, 87743 sand, 37624 dirt, 9401 hard, 10036 snow), twelve wood markers, unchanged grip bytes and terrain asset SHA-256, camera distance 7, no shader errors.
- ActualSceneContacts.json: five representative terrain classes and all twelve actual deck/ramp colliders classified/projected successfully; greatest center discrepancy 0.0000153 m. GeometryPreview.json: 240 rendered strip segments; soft relief varies by surface, hard/wood remain flat, and the elevated wood fixture retains its own contact height.
- SurfaceComparison.png is a static Camera.Render from a disposable preview scene. It demonstrates generated mesh/material appearance, not driving validation. Preview objects and render textures were destroyed afterward.

## Remaining user validation
Drive straight and reverse through grass/soil/sand/snow, across a bridge and over a rock. Check transitions into/out of Living Mud and vehicle resets. Verify performance and remote-peer visuals when available. Driving and multiplayer were intentionally not tested. Tutorial and vendor source scenes were not modified.

Unity also normalized the existing Refuge deep blue windows material on import (URP metadata and matching _Color); no authored window tint was changed. The user's camera setting was preserved and saved with the scene.
