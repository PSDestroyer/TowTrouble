# Tow County — mountain approaches and refuges

Implemented in `Assets/TowTrouble/Scenes/OpenWorldRecovery.unity` on 2026-10-31, from baseline commit `15fbc8e`.

The four supplied locations now have a continuous graded upper junction, an off-road alpine delivery apron with a timber refuge, grouped native lower mountain formations, and a meadow composed around tree islands, undergrowth, stones and a second refuge. The refuges are scenic landmarks beside existing deliveries; the central customization garage remains the gameplay garage.

## Review locations

| Location | Approximate world position | Preview |
| --- | --- | --- |
| Upper junction / turn3 | (99, 126, 279) | [Current](Junction.png), [before](Junction-Before.png) |
| Delivery D / alpine refuge | (-11, 114, 254) | [Current](AlpineDelivery.png), [before](AlpineDelivery-Before.png) |
| Lower mountain formations | (-66…95, 78…100, 202…217) | [Overview](Escarpment.png), [road-level view](RoadsideCliffs.png) |
| Meadow and delivery C | (54, 66, 144) | [Meadow](Meadow.png), [refuge close-up](Refuge.png) |

The visible D bay and all contracts routed to D now share the same position. All 24 recovery IDs and eight delivery IDs are retained. Wheel wear follows the reshaped terrain; the ground-impression mask includes painted delivery aprons. Four affected contract photos changed. Native prefabs/material sources, the tutorial and original Mega/Desert scenes were not edited.

## Verification

- C# compilation: zero errors or warnings.
- [SceneChecks.json](SceneChecks.json): 18,213 sampled road lanes supported and clear, zero buried roads or obstacles; maximum sampled longitudinal grade 19.35°. All 363 reset points supported. All 440 river samples wet/downhill; four bridge approaches connected; no river trees, missing scripts or shader errors.
- [ApproachChecks.json](ApproachChecks.json): 99 entrance samples clear; D entrance maximum grade 4.08°; C/D parking pads clear; C pad elevation variation under 3 cm in the sampled parking footprint. All 24 contract destinations match their delivery records.
- [PreservedSources.json](PreservedSources.json): SHA-256 matches for CanyonRecovery, both vendor demo scenes and OpenWorldPlan.
- [AuthoringUndo.json](AuthoringUndo.json): repeatability and Undo/Redo checks for the authored terrain/paint and generated hierarchy.
- Captures are direct Edit Mode `Camera.Render` results. No Play Mode, runtime simulation, Test Runner, network session or build was started. Driving feel, delivery completion, animated effects, performance and multiplayer still require the user's Editor play test.
- Source whitespace check passed. Unity's generated scene YAML retains Unity's own empty-value trailing spaces.

## Authoring and recovery

`Tow Trouble > Art Integration > 9 - Compose Mountain Approaches and Refuges` reapplies the local composition. The complete art pass and gorge composition also invoke this pass after the gorge, preserving the shared junction and delivery changes.

`OpenWorldMountainApproaches.cs` owns local height/paint baselines, grading, native rock/foliage dressing and wheel-wear fitting. `OpenWorldMountainRefuges.cs` owns the two timber landmarks and delivery placement. Baselines and derivative meshes/materials live under `OpenWorld/Visuals`; vendor assets are referenced, not modified. Material properties are applied after persistent material creation so initial URP import does not replace authored rock textures with defaults.

Before-change backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/TowCounty-before-mountain-zones-20261031.tar.gz`. No commit or push was made.
