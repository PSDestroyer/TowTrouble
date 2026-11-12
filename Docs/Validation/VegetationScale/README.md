# Vegetation proportions correction

User rejected miniature duplicates of existing grass and sapling-like bushes, and excessive small meadow trees.

Measured existing active P_Grass2 median width2.88m, height.49m; previous new grass clumps were only.38–.78m wide. Corrected desktop county grass to2.4–3.3m wide, dry grass2–2.8m, ferns1.4–2.1m, reeds1.3–2m, real shrub masses2.6–4.2m. Forest P_BushLeafy01 replaced with full P_Bush2_Light; desert uses P_Bush2_Dry. Added spacing and larger islands. Small-count quota reduced from4x additions to1.6x, prioritizing proportions over the earlier rejected numeric target. The six-biome density is no longer claimed as5x.

Current county layer:11,244 placements including384 retained perimeter formations;1451 renderers,2882583triangles. Workshop/R01 desktop layer also regenerated with full-size grass, ferns and bush masses and fewer plants per island. Older authored vegetation retained for natural variation.

Tree pass:223 small trees increased to11.5m broadleaf /12.5m conifer,17 repeated meadow trees deactivated with their child colliders. Grounded bases retained. Trees within27m of recovery sites, within5m of road edge, near delivery bays and high alpine trees excluded. Existing trunk colliders scale with their tree; no new colliders. See Trees.json.

Seven reflection cubemaps refreshed. Compile clean; native mesh/shader/reference checks passed. Independent check of11244 ground anchors and road clearances passed (minimum2.3155m beyond road edge using bounding radius). Scene saved, no builds. Eight final landscape captures in After. No target-PC FPS measurement.

PreviousPlacements.json records the rejected county distribution. Scene checkpoint /private/tmp/TowVegetationScale/Before.unity; generated meshes were updated in place, so the scene checkpoint alone is not a full asset rollback.
