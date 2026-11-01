# County detail expansion — 2026-11-01

User requested more detail across the empty map. Expanded the existing Layered county details recipe instead of adding another runtime system. Native SoStylized prefab instances now include medium bushes, birch saplings, fallen fir/pine logs, driftwood, cactus groups, rock clumps, grasses and ferns, chosen by region. Landscape clusters use a30m jittered grid with wider6.5m local spread; road and landscape centres are shuffled before the cap so early roads cannot exhaust the budget. Accent height ranges1–4.3m, small detail0.25–1.1m. Existing recovery vignettes retained.

Settings asset updated through Editor API:Density1.55,MaximumProps3000,FoliageDistance120,StoneDistance175. Final2934props in568clusters/183cells64m,including490medium accents. Previous1352props in207clusters/107cells.21sharedmaterials,zero added colliders/MonoBehaviours,every prop hasLOD; ambient shadows off. Performance has not been measured in Play Mode.

Protection:garage/R01/delivery clearances, current recovery positions and uphill tow corridors, roads/rivers and steep slopes. Added full-footprint exclusion for all broad soft-ground areas, keeping the larger marsh/snow/sand zones readable. Authoring replaces only the owned Layered county details root. Wheel shoulder generation remains disabled while broad zones exist.

Validation:clean compilation,zero current Console errors(existing account warning),all96wheel contacts and five mission obstacles preserved,R01/settings/roads/progression/deliveries unchanged,no missing scripts. Four static Camera.Render regional views inspected. No Play Mode,simulation,Test Runner,build or network. User should assess density and FPS while driving.

Files:OpenWorldReadableDressing.cs,RecoveryDressingSettings.asset,OpenWorldRecovery.unity. Menu:Tow Trouble / Recovery / Readable Wheel Blockages and Layered Map Details. Backup:Desktop/TowTrublw/Backups/CountyDetailExpansion-20261101.
