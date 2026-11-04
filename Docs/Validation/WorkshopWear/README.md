# Workshop wheel wear and service details — 2026-11-04

The broad workshop court now has six pairs of wheel impressions: the receiving circulation loop, two parking approaches, inspection pad access, material drop-off and cable preparation access. Smooth sampled curves, three shared material strengths, variable opacity and feathered ends replace the previous single almost-invisible path. The existing wheel-impression shader supplies subtle compacted tread depth; no terrain deformation or shader-wide changes were made. Each route owns a separate saved mesh, so regeneration does not overwrite another route.

The surface follows terrain and nearby drive colliders at approximately 18–23 mm above terrain. Added physical details stay inside the three existing work areas: a floor jack and wheel chocks, a loading handtruck, and a low strap/shackle rack. No interiors, lights, scripts or new runtime systems.

`OpenWorldGarageWear.cs` owns the traces. Both full workshop composition and `Tow Trouble > Visuals > Dress Workshop Activity and Edges` regenerate them. `OpenWorldGarageActivity.cs` owns the small props.

## Validation

- Full Editor C# source compilation passed; Unity imported and authored the scene without new errors.
- Static audit passed: six distinct trace meshes, 9,870 total vertices, three shared materials, no trace colliders; no missing geometry or material references.
- Existing activity/planting group plus the added details: 149 renderers, 26 colliders, 54 LOD groups, no scripts or lights. The traces add six renderers and six LOD groups separately.
- New solid props remain at least 9.88 m from the sampled previously-tested towing route and 22.92 m from the garage centre.
- Mission data and sampled local terrain heights match the pre-change snapshot exactly. All four review views were captured in Edit Mode; courtyard and overhead views inspected.
- Final scene saved in Edit Mode. No Play session, profile change, physics simulation, multiplayer test, performance benchmark or build in this visual follow-up. An Editor account API availability warning is unrelated to the scene changes.

![Workshop court with wheel traces](Courtyard.png)

![Trace paths from above](Overhead.png)
