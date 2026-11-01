# Roadside accident cues and tier severity — 2026-11-01

R10/R12/R23 now have paired curved accident skids connecting the road to the final vehicle position, continuing beneath the four tires and widening into churned wheel ruts. Static meshes follow terrain and use existing wheel-impression shader/materials. Biome response: R10 earthy mud, R12 compacted snow, R23 churned sand. Width, raised shoulder relief, and curve spread increase with contract tier2/4/3. Natural bank selection now scores desired slope, road distance and height drop by tier; R12 is farther below the road and tilted on both axes. Terrain heights unchanged by this pass.

Six static mesh colliders expose actual ground contact types:2MudTerrain(OwnsTracks=false),2SnowGroundSurface,2SandGroundSurface. Existing vehicle grip/rolling resistance, per-wheel coating/spray and bounded tracks apply through these components. No new runtime controller, emitters or dynamic mud render textures. Three LOD groups; six meshes,392–427vertices each, shared3materials. Ordinary generic approach tracks are suppressed for these three jobs to avoid overlapping trails. Reconciliation replaces owned meshes deterministically without duplicate trails.

Final poses approximately R10(187.87,72.22,209.10),pitch-23.30,roll0.34; R12(178.53,134.20,312.39),pitch-21.36,roll-24.10; R23(396.34,23.37,-172.11),pitch-5.80,roll-11.78. Difficulty scaling is a prototype and not live-balanced.

Validation: clean compile; zero current Console errors (existing account warning), all96 wheel contacts supported, all12 wheels of these3cars specifically hit authored rut colliders (ContactSurfaces.txt). IDs/tiers/rewards/roads/deliveries and R01 preserved; five mission obstacles valid, no missing scripts. Six trails after repeated authoring. Static photos inspected and updated; no Play Mode, simulation, Test Runner, build or network session. User must test initial settling, extraction effort, grip transitions and snow/sand/mud coating live.

Files: OpenWorldRoadsideTrails.cs, OpenWorldRoadsideRecoveries.cs, OpenWorldRecoveryReconciliation.cs, OpenWorldLandmarks.cs. Backup: Desktop/TowTrublw/Backups/RoadsideAccidentCues-20261101.
