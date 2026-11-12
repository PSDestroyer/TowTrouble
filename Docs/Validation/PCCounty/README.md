> Superseded visually by the vegetation-scale correction on 2026-11-11. The table below records the rejected miniature-detail pass. Current Authoring/Checks/Clearance JSON reflects the corrected pass: 11,244 placements, 1,451 spatial renderers, 2,882,583 triangles. See ../VegetationScale/README.md.

# PC county art extension

Six-biome detail pass, preserving the approved workshop/R01 study, terrain and gameplay.

Small native decorative instances were counted by biome before this pass (active outermost SoStylized prefab instances, excluding trees/collision-only objects, maximum 4m width and 2.5m height). Added four times that count per biome: total small-detail count is now 5x. This is a count across each biome, not five times the density at every point or five times all scene geometry.

| Biome | Baseline | Added | Total ratio |
|---|---:|---:|---:|
| Forest | 2836 | 11344 | 5x |
| Lake | 742 | 2968 | 5x |
| Mountain | 1469 | 5876 | 5x |
| Snow | 453 | 1812 | 5x |
| Beach | 567 | 2268 | 5x |
| Desert | 1065 | 4260 | 5x |

Additional perimeter composition: 384 rock formations. All 28,912 placements use native source geometry, merged by material into spatial 48m cells. 1,535 renderers, 6,809,578 triangles across the whole addition, 25 shared material copies, shadow casting, distance LOD. These are authored totals, not visible-frame counts or measured GPU performance. Mesh assets add about 1.2GB of text-serialized source data; this is not runtime memory consumption.

Five additional 256 HDR reflection cubemaps at lake/mountain/snow/beach/desert recovery locations. Custom static mode; no realtime capture. Existing forest/workshop probes remain.

Native checks:
- 2,870 existing collider serializations and RecoveryMap JSON unchanged on authoring.
- No added colliders or terrain changes.
- All 28,912 placements independently checked against the sampled road curves: minimum bounding-radius clearance 2.53m beyond road edge; ground anchors within 0.000002m of terrain.
- Road exclusion also includes predelivery bays, house foundations, workshop/R01 reference zones, extraction corridors, water surface bounds and steep ground.
- No missing scripts, invalid mesh bindings or shader errors. Five reflection textures bound.
- Eight matching Before/After Editor images: View0 forest, View1 lake, View2 mountain, View3 snow, View4 beach, View5 desert, View6 east overview, View7 west woodland. These are Edit Mode landscape views, not gameplay timing evidence.

No builds or target-PC profiling. New layer uses RecoveryDesktopScenery, so Switch skips it. Backup: /private/tmp/TowDesktopCounty/Before.unity.

Authoring: Tow Trouble > Visuals > Refine Desktop County Biomes and Borders. Regeneration replaces only the desktop county root and its generated assets, preserving prior scene work. Re-bake local reflection assets using ReflectionBake.cs.txt when surrounding geometry changes. Bulk mesh imports are batched for subsequent authoring; the first run completed after the CLI's response timeout, and was verified before any retry (no duplicate pass).
