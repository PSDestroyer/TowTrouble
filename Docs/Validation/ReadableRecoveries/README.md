# Readable wheel blockages and layered map detail — 2026-11-01

User requested clearer ditch/sunk-wheel causes, comparable to R01, and richer map detail that can be optimized later.

## Implementation
- Fifteen existing ditch, embankment, mud, snow, sand and dry-wash sites receive terrain-following raised wheel shoulders, a short exit lip and darker pocket bottoms. A combined shoulder mesh per site uses shared materials per ground type; wet mud uses the existing Living Mud albedo. Only the caught side is emphasized for one-sided ditches. The existing terrain supplies physical contact; these shallow cosmetic shoulders have no additional colliders or extraction force changes. R01 visuals/physics/settings and its surrounding 35 m exclusion remain intact. Water/engine/rock sites retain their distinct earlier evidence.
- `OpenWorldReadableDressing` owns exactly two scene groups: `Readable recovery wheel pockets` and `Layered county details`. Reapplication replaces only these groups. Existing landscape and recovery scenery stay intact. Existing reconciliation refreshes the wheel pockets when already installed, without regenerating ambient decoration.
- Current ambient output: 1,352 native prefab instances, 207 clusters, 107 spatial cells (64 m). Category subgroups separate understory, small stones and local landscape vignettes (wood, roots, snow). Biome selection, seeded placement, road/river/garage/delivery clearance and explicit recovery pull-out corridors protect gameplay space.

## Optimization controls
Editor recipe: `Assets/TowTrouble/Editor/RecoveryDressingSettings.asset`. Regenerate using `Tow Trouble > Recovery > Readable Wheel Blockages and Layered Map Details`.

- Seed and Density control repeatable distribution; MaximumProps is a hard cap (default 1,400).
- RoadsideClusters, LandscapeClusters and StoryClusters independently enable categories.
- FoliageDistance (110) and StoneDistance (160) set nominal screen-size LOD/culling distances; actual transitions depend on FOV, object size and quality LOD bias. Pocket presentation has nominal 170 m culling.
- Native prefab meshes and materials remain shared. Current ambient renderers use 18 materials; 1,670 renderers include all LOD levels, **not** simultaneous draw calls. Every ambient renderer belongs to a native LODGroup. Native lower LOD meshes are retained when provided.
- No new ambient colliders, rigidbody simulation, per-object MonoBehaviours, particle emitters or shadow casters. Small decorative props are deliberately nonblocking. Important existing mission obstacles are untouched.
- Cells/category roots support later selective disabling, mesh combination, instancing or streaming without dismantling recovery presentation. Do not claim these have already been benchmarked or streamed: this is an authoring structure and bounded initial content pass.

## Verification
- Unity compile succeeds; Console ground truth has zero errors, with the existing account API/network warning.
- Direct Edit Mode validation confirms repeatable map/object transforms, finite nonempty generated meshes, zero new colliders/scripts, LOD ownership and all generated prop positions passing protected-area checks.
- Existing 96 wheel support rays pass; five mission obstacles active; R01 settings/terrain, all24 IDs/rewards/deliveries/checkpoints preserved; no missing scripts. Scene saved clean.
- All24 contract photos and regional context previews inspected. 23 photos refreshed; R01 photo retained. Photos use posed models, not simulated gameplay.
- No Play Mode, Test Runner, physics/particle simulation, build or network startup. Extraction feel, view-dependent popping, GPU/CPU/frame-time cost and live multiplayer remain user tests. Recommended first locations: R04, R03, R14, R17, then a normal road circuit through forest/lake/desert.

Backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/ReadableRecoveries-20261101`. Source scene/photos and intermediate full-size previews retained there. No commits/pushes.
