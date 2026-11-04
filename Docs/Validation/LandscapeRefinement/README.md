# County landscape refinement — 2026-11-04

Implemented and saved in `OpenWorldRecovery.unity`. The user authorized map refinement, use of the newly imported Suntail assets without interiors, and Play/Solo verification. Existing uncommitted environment work was retained.

## Authoring

- `OpenWorldLandscapeComposition.cs` orchestrates the existing `Tow Trouble/Visuals/Compose County Landscape` command. Terrain regeneration starts from the preserved `LandscapeComposition/Terrain baseline.asset`; do not replace that baseline with the generated terrain.
- Mountain relief adds gullies/spurs. Sixteen pairs of native, embedded cliff fragments replace the round slope patches and separate summit caps. Generated geometry has shared materials, LOD groups and environment collision.
- River/cove banks and vegetation use irregular clusters. Existing bridge ramps receive combined timber skins; their driving colliders remain the same. Lowland crossing boulders are reduced and embedded.
- Broad wet-soil surfaces use a project-owned material with corrected tint. Existing Living Mud simulation is retained.
- `OpenWorldLandscapeSuntail.cs` creates two closed exterior buildings and one open canopy, with compact feathered courts and access paths painted into the terrain.

## Suntail exterior boundary

The furnished vendor house prefabs are never instantiated in the gameplay scene. A positive whitelist copies only exterior module meshes into project-owned, material-combined prefabs. Interior hierarchy, furniture, internal stairs/platforms, lights and scripts are excluded. Doors/windows are closed scenery.

The three generated prefabs contain two simple building colliders, three LOD groups and 18 rendering batches at either active LOD: 42,362 triangles near / 18,386 far in total. Source assets are kept under `Assets/Suntail Village`; derivatives are under `Assets/TowTrouble/OpenWorld/Visuals/LandscapeComposition/SuntailExteriors`.

## Verification

See `Results.json` for measured results.

- Static clearance/ground checks passed across 70 existing road meshes, 73,712 terrain samples, all 24 recovery sites and eight delivery areas. No new collider encroachments; serialized recovery map unchanged.
- Current strong-snow paint agrees with Snow grip, Snow tracks and wheel impressions in 3,700 unprotected cells. Protected authored areas were reported separately. Earlier fixed coordinates became exposed rock after reshaping and are not valid permanent snow fixtures.
- Play/Solo: both bridges crossed alone and while towing; a 66 m physical uphill traversal completed. The actual R09 target was extracted with winching followed by low-throttle driving; no cable break. Winching alone reached its minimum length before release.
- Camera occlusion faded and restored a roadside tree through the real camera update; renderer enable flags and collision remained intact. Driving tests also observed trees hide and restore.
- No gameplay errors were logged during the measured tests. Pipeline transport errors and Unity Account API warnings occurred around editor reloads. The first cold Solo request timed out; retry connected successfully.
- All four protected game PlayerPrefs keys remained identical throughout. Only Unity's own session metadata changed on disk. Test callbacks were removed and Play Mode stopped; scene saved cleanly.

Traversal fixtures place vehicles at the start, then use normal session input and host physics. They do not certify complete contract delivery, multiplayer or standalone performance. No build was run. Test-only scripts and private profile backup remained outside the project in `/private/tmp`.
