# Workshop building polish — 2026-11-04

The main garage now has a closed terracotta/timber exterior, two framed petrol-blue doors, a clean slate-metal roof, a shorter wall-supported canopy, and the original Tow Trouble logo mounted on a physical plaque. Side windows and a service door are opaque. Gutters, downpipes, handles, lamp housings, thresholds and drainage grates finish the exterior.

The new authoring replaces the old building shell and detached canopy supports while retaining the existing court, slab, workshop activity, vehicle spawns and road wear. Dedicated materials avoid recolouring shared county props. Low surface emission keeps the shaded painted facade readable; only lamp lenses have a stronger accent. No real-time Light components, interiors or new runtime behaviours were added, and no lighting bake was run.

## Regeneration

Use **Tow Trouble / Visuals / Polish Workshop Building** in Edit Mode. Full **Compose Workshop Service Base** also builds this version. The narrow command updates the building within `Workshop service base/Workshop exterior service frontage` and removes only named obsolete building pieces from `Central garage - Tow County`.

Authoring is in `OpenWorldGarageFacade.cs`, `OpenWorldWorkshopPolish.cs`, `OpenWorldWorkshopShell.cs` and `OpenWorldWorkshopIdentity.cs`. Combined mesh assets and dedicated shared materials live in `Assets/TowTrouble/OpenWorld/Visuals/GarageServiceBase`. The updated scene is `Assets/TowTrouble/Scenes/OpenWorldRecovery.unity`.

## Verification

- Full Editor C# source compilation passed. Unity completed the final import without a compilation failure. The Console initially had zero errors after import; subsequent inspection produced three Unity Pipeline transport errors, described below.
- `Building.json`: unchanged map data and sampled terrain heights; no duplicate facade, missing meshes/components, new scripts or lights. Generated frontage has 51 renderers, 16 colliders and 11 LOD groups. These are scene counts, not measured GPU draw calls.
- New building colliders remain at least 13.34 m from the previously recorded towing route samples. This is a static clearance check, not a new driving test.
- `GarageFrames.json`: 128 settled camera samples passed using the actual Scout scene geometry and production garage framing rules. Coverage: Body, front bumper, rear bumper and wheels; eight headings; two garage positions; 16:9 and 21:9. No blocked workpiece sightlines or near-plane clips. Minimum workshop clearance was 0.349 m.
- Camera tests used actual collider raycasts; the non-convex gable uses conservative AABB distance only for the clearance calculation. They do not cover every purchasable body, live camera transitions or input timing.
- Six temporary Edit Mode camera views were captured and inspected; representative images are included here. Temporary cameras were removed and the final scene remained saved and clean. The final imported door material retained its emission keyword and intended colour.
- No new Play session, Test Runner execution, physics simulation, profile writes, multiplayer test, build or performance benchmark was performed for this visual refinement.

Unity CLI briefly reported transport timeouts during authoring; readback confirmed the commands completed and the scene saved before further steps. Those transport errors were not project compilation errors. `git diff --check` reports one Unity-generated empty prefab override (`value: `) in the saved scene; authored C# and documentation have no whitespace errors.

## Captures

![From the court](Frontage.png)

![Front view](Straight.png)

![Roof and facade construction](Roof.png)

![Side exterior](Side.png)
