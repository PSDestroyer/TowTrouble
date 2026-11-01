# Roadside recovery replacements — 2026-11-01

Replaced decorative rock-circle jobs R10/R12/R23 with cars slipped below actual road edges. Existing natural slopes only; no new terrain excavation or colliders. Scene road mesh centre samples are used because the legacy checkpoint array omits newer mountain roads. All three sites retain contract IDs, tiers, resistance, deliveries and progression; titles and hints now describe roadside banks. Existing slope gravity plus contract breakout resistance provide extraction load. No live force tuning claimed.

R10: approx(186.43,73.37,211.47),pitch-21.3°,roll-3.5°. R12:(185.27,138.30,317.82),pitch-5.0°,roll-25.1°. R23:(395.91,23.27,-171.09),pitch-9.7°,roll-6.1°. Exit vectors point uphill to sampled road positions; headings rotated25° to show diagonal slipping. Rock props in owned situation groups removed; obsolete setting decor disabled. Signs/approach tracks and photos updated. These three sites are excluded from soft-ground reauthoring so it cannot create marsh/sand beds here. Reconciliation reapplies the final scene setup.

Validation: compilation successful, zero current Console errors/warnings, all96 wheel support raycasts passed, five mission obstacles present, R01 settings/terrain preserved, roads/IDs/rewards/deliveries preserved, no missing scripts. Validator permits only the three intentional title changes. Repeated authoring returned the same scene poses. No Play Mode, physics simulation, Test Runner, network or build. User must assess initial settling, uphill pulling/winch resistance and recovery completion in Play Mode.

Source: OpenWorldRoadsideRecoveries.cs, invoked by reconciliation. Roadside contract photographs have a dedicated camera angle. Backup: Desktop/TowTrublw/Backups/RoadsideRecoveries-20261101.
