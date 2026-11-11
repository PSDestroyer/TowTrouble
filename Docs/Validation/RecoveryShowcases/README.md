# Biome recovery showcases — 2026-11-11

R02–R24 receive local biome compositions in OpenWorldRecovery. R01 is preserved. No builds.

## Direction

- Forest: logging remnants, fallen timber, fern/grass islands and a canopy backdrop.
- Lake: reeds on dry banks, driftwood and shoreline stone clusters. Water recoveries keep the lake bed clear.
- Mountain: asymmetric scree clusters, rock outcrops and sparse low vegetation beside the ramp.
- Snow: drifts, snow-covered timber and sparse dead/snowy shrubs.
- Beach: dune grass, palm driftwood/stumps and isolated palms, keeping the sand ramp clear.
- Desert: warm rock clusters, cactus and dry scrub; site-specific arrangements for wash, dunes, canyon bank and breakdown.

Every site has a named composition and deterministic variation. Details sit outside a central 8.4m-wide towing corridor (expanded for prop radius), the nearby road margin and neighboring recovery centers. Placement excludes slopes above48 degrees and terrestrial plants below the lake-shore threshold.

## Scope and safety

Only decorative instances and local terrain splat weights change. Existing recovery map, difficulty, vehicle pose, extraction direction, obstacles, mud surfaces and terrain height/collision are preserved. Initial application compared map serialization, all terrain height samples and collider instance IDs before/after successfully. Scene/terrain copies are in /private/tmp/TowRecoveryShowcasesBackup.

The tool stores original local paint patches in RxxGroundBaseline.json. Do not delete these or rerun after hand painting those patches without reviewing the baseline. No shared nature prefabs/materials, runtime systems, global PC/Switch settings or player saves are edited.

## Switch budget

Native medium mesh sets, shared materials, distance culling, no new lights/colliders/physics or permanent particle systems. New scenery does not cast shadows. Validation.json contains final whole-map counters; these are not simultaneous draw calls or measured Switch frame times. Small native rock prefabs with disabled renderers are enabled only on new scene instances.

## Verification

Editor compilation clean; eight cinematic tests pass. Native preview sampled24 contracts ×50 camera poses in Switch-preview mode: finite camera positions, correct final actor positions, source casualty/map unchanged. CameraValidation.json records sampled clearances and particle budgets. Photos R02–R24 were regenerated using stable existing references and Switch compression; R01 photo remains unchanged.

Scene saved after final reference/LOD checks. No build or hardware FPS measurement. The scene should be driven on Switch to assess actual frame pacing and LOD transitions. Unity emits whitespace on empty prefab override values in the scene YAML; source-code whitespace is checked separately.
