# Recovery map reconciliation — 2026-11-01

User reported the previous situation pass left the former locations/scenery inconsistent and made the map look worse. Reviewed 24 posed recovery views, 24 wide site/approach views and seven regional overview captures in Edit Mode.

Confirmed problems: fitting Living Mud raised the formerly submerged edge of every mesh, exposing huge circular patches; R07 retained mud despite becoming an embankment recovery; signs/stakes still described earlier locations; generated black tracks ignored the actual terrain layer (notably snow at R12); repeated tiny stump rows looked synthetic. The R20 sand recovery was still mostly on green ground. Eleven existing lakeshore props also retained the previous terrain height (up to 3.03 m above the changed bed), producing floating plants/rocks. They are now seated using fixed original transforms and local terrain-height deltas. Existing authored surrounds are retained as landscape, with stale approach markers reconciled, rather than another layer of new recovery sites.

Changes:
- New focused `ReconcileRecoveryLocations` authoring pass, also called by `DressDistinctRecoverySituations`, updates the existing scene. No whole-world/road/forest regeneration.
- Three smaller irregular Living Mud footprints, sunk edges plus terrain-texture/alphamap-matched visual transition. Existing DMS shader, runtime stamp component, collider UVs and coating stay intact. UV frame aligned with mesh orientation. R05/R07/R08 old mud surfaces inactive.
- R02–R24 signs and stakes aligned with current extraction approach; signs readable from both sides. Twenty-three subtle track connections use each existing recovery road spur, not a distant main-road checkpoint. Shorter, narrower, low-contrast local marks match the actual ground layer.
- Removed only synthetic stump rows in new ditch/embankment/wash groups; retained native landscape, rocks and five mission trees. Shore props seated and kept out of car footprint. Terrain detail grass/reeds cleared only under R02–R24 cars.
- Small R20 sand paint patch with smooth transition and fixed baseline for repeatability. No new terrain height changes in this pass. R01 terrain, settings, photo and scenery preserved.
- Refreshed 23 contract photos (static posed smoke only). Before/after images and full-site sheets retained here. Full raw context captures and pre-change assets are under Desktop/TowTrublw/Backups/MapReconciliation-20261101.

Validation: successful compile; custom ground-blend shader has no compiler messages; all 96 wheel-support raycasts pass; all five mission obstacles active; zero missing scripts; IDs/rewards/deliveries/checkpoints/R01 settings preserved. Reapplication produces identical map/terrain heights/paint and stable object transforms, with one set of 23 approach tracks/23 reverse labels/3 mud margins. The earlier Repeatability.json precedes the final road-spur targeting refinement; FinalValidation.json records final approach count. Final scene saved clean. Console has zero errors and the existing Unity account API/network warning.

No Play Mode, physics/particle simulation, Test Runner, build or network startup. Static capture coverage establishes composition and placement, not live extraction difficulty, animated mud/smoke, performance or multiplayer. User owns driving tests. Wider mountain forms/horizon were reviewed but not redesigned; changes address recovery-site integration and their existing approaches.

Suggested live check: R02/R03 mud and approach; R05/R08 water approach; R07 former mud removal; R12 snow tracks; R20 sand transition; R01 unchanged regression. Then follow the normal circuit past all 24 signs.
