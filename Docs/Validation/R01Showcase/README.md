# R01 forest showcase — 2026-11-11

Local polish of Workshop meadow beside the garage. Applied and saved in OpenWorldRecovery.unity. No builds.

- Blended woodland soil within a local terrain-paint patch; terrain heights and serialized recovery map were compared before/after and remained unchanged. Road paint is excluded.
- R01-only copies of rut materials and meshes: reduced yellow raised lip, darker exposed soil, softer irregular edges. No collision geometry changes.
- Replaced scattered small forest plants with five grouped understory islands, medium plant meshes and distance culling. Existing trees, fence and log retained; two canopy trees repositioned to frame the incident. Small stones and two rock clusters added outside the road/towing corridor.
- Intro camera frames the authored wheel-rut recovery; soil spray comes only from its designated blocked wheel, using the existing Switch particle budget. Other unprofiled ditch incidents retain their previous behavior.

## Validation

- Editor compilation passed. Eight existing cinematic tests passed (Tests.json).
- R01 preview sampled 50 native camera poses, four rendered frames: finite positions, minimum sampled ground clearance 3.62 m, final actor position matches the contract; source casualty and map untouched.
- New dressing: 81 enabled renderers / 171 renderer components, 16,973 mesh triangles at inspected enabled levels, 16 LODGroups, no nested LODGroups, no new lights/colliders/missing materials. These are asset counters, not draw calls or measured frame time.
- Existing forest subtree active renderer-component count reduced from 414 to 92 before adding the new dressing. Disabled old plants are retained for reversibility.
- Scene saved after checks. Pre-existing dirty scene was copied to /private/tmp/TowR01ShowcaseBackup/Before.unity before editing; terrain backup alongside it.
- Before.png and After.png are actual Editor preview captures with different cinematic framing, not same-camera pixel comparisons.
- Switch hardware frame time and final subjective in-game appearance still require device validation. No build or full gameplay session was run.

## Re-authoring

Menu: Tow Trouble / Art Integration / Polish R01 Showcase Only. The tool leaves the scene dirty for review and saves local generated assets. GroundBaseline.json preserves the original patch for repeatable painting; do not delete it casually or rerun after unrelated manual edits to that patch without reviewing the baseline. Existing shared nature assets and materials are untouched.
