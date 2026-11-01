# R05/R08 natural lakeshore — 2026-11-01

User screenshots showed disconnected circular water-filled excavations in the beach. ShapeSituationPocket flattened the water jobs to a fixed elevation, creating artificial pools separated from the main lake by a sandy ridge.

Reconciliation now restores the original terrain samples in both owned water-site patches and places R05/R08 at naturally shallow points of the main lake. Placement searches inside the real water mesh polygon and checks a20m submerged path toward the main lake; no new pool is excavated. Landward exit is selected from nearby firm terrain. Props, signs, approaches, headings and wheel seating are reconciled. Existing lake water material and geometry are preserved. Standalone broad-ground authoring does not alter these water patches.

New positions: R05 approx(-229.14,10.35,-189.52), R08(-364.38,10.35,-194.37). Existing contract metadata and R01 preserved. Static R05/R08 photos refreshed; all96 wheel contacts, five obstacles and no missing scripts validated. Compile successful; zero current Console errors, one existing account warning. No Play Mode/simulation/build. Live tow extraction remains user's test.

Source: OpenWorldNaturalLakeRecoveries.cs, invoked by ReconcileRecoveryLocations. Backup: Desktop/TowTrublw/Backups/NaturalLakeShore-20261101. Terrain baselines remain the fixed RecoverySituations/TerrainPatchesBaseline.json. Re-running complete situation authoring eventually invokes reconciliation and repairs the water pockets again.
