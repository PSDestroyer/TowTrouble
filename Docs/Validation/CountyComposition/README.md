# County composition and scale pass — 2026-11-11

Replaces the previous desktop county scatter with connected shrub masses, graded stone groups and lower vegetation edges. Native prefabs are scaled by measured mesh bounds: main shrubs 2.8–4 m wide, grass generally 2.4–3.3 m wide, decorative stones 0.65/1.05/1.75 m; alpine drifts use larger sizes. These are widths, not plant heights.

Placement follows existing mature tree/rock anchors, recovery banks and road verges. A terrain-aware coverage pass adds groups in safe meadow gaps. Water protection now samples actual transformed water triangles through a spatial index instead of rejecting entire river bounding rectangles, which previously left dry river-adjacent areas empty. Geometry remains combined by material into 48 m cells with distance culling. Existing gameplay and baseline props are retained.

Evidence: Before/View0–7 and After/View0–7 are matching native Unity camera captures across six biomes and two edge/meadow views. After/Meadow0–5 inspect the two houses, bridge vicinity and additional woodland/meadow approaches; these are representative views, not exact matches for every user screenshot.

Native compilation succeeded with no reported errors or warnings. Generation preserved 2,870 existing collider serializations and the RecoveryMap. Added layer contains no colliders. Exact road-path clearance checks on all 24,057 placements found minimum road-edge allowance 2.6547 m (including the placement footprint); terrain anchor error below 0.000002 m. Reflection probes refreshed separately. Final mesh/shader/reference checks are recorded in Checks.json.

This is an Editor visual/placement validation, not a gameplay traversal or PC performance benchmark. No player build was run. Current layer totals 1,705 spatial/material batches and 6,793,313 triangles over the full map; these totals are not simultaneous visible render cost. Existing Switch runtime gate remains in place.
