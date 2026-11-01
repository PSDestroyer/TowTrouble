# Weaker stock cable and early upgrades — 2026-11-01

User requested a weaker starting cable and then an additional 11 kN starting tier. Current verification for the latest revision is in `../Cable11kN`.

For the later magnitude-sensitive rupture/impulse fix, see `../CableShock`; it supersedes the fixed grace timing described by the earlier evidence.

| ID | Cable | Price |
|---|---|---:|
| stock_Cable | Stock, 11 kN | Included |
| cable_14 | Light, 14 kN | 200 CR |
| cable_18 | Standard, 18 kN | 450 CR |
| cable_26 | Reinforced, 26 kN | 1400 CR |
| cable_36 | Professional, 36 kN | 3400 CR |

The free starting cable is now 11 kN. The previous 14 kN stock becomes the first purchasable upgrade at 200 CR; 18 kN remains available for 450 CR. Existing stock-equipped builds receive the intended weaker stock rating; purchased 18/26/36 kN items keep their IDs, strength and prices. No profile migration, reset or real PlayerPrefs modification.

`RecoveryRules.BreakForce` is the 11000 N starter/default/tutorial threshold; `GaragePart` and `GaragePerformance` use the same default. Open-world upgrades still use the equipped host-calculated cable strength. Cable-force formula, break grace periods, winch rate, R01 resistance, traction and mission/economy rewards are unchanged.

`GarageAuthoring.RefreshCableProgression` updates only the cable progression. Full catalog authoring uses the same configuration method, so regeneration preserves it. No assembly/prefab/world/forest regeneration was run. Current catalog has 382 entries.

Verified:

- Unity compilation passed; Console compilation errors zero.
- 41 direct Edit Mode checks passed, with per-test setup, across garage, extraction, scenarios, cable and delivery rules. Includes affordable first upgrade, unowned upgrade rejection, equipped upgrade save round-trip, existing 26/36 kN preservation, unique IDs and stock stats.
- Latest new build/fresh model reset uses 11000 N. Already loaded Edit Mode component backing fields can retain pre-reload values until the next model reset; they are not runtime evidence.
- Static R01 budget: 5600 N breakaway, 12400 N Scout nominal total drive, 11000 N stock cable. Gentle pulling has headroom; full nominal drive can exceed stock strength. Actual towing and shock balance needs user Play Mode testing.
- Scene stayed clean and stopped. No Test Runner, Play Mode, physics simulation, network startup or player build. No commit/push.

Evidence: Catalog.json, Rules.json, StarterBudget.json, VehicleReset.json. Backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/CableProgression-20261101`.

User test: start a fresh Play session, check Garage > Towing > Cable, equip stock 11 kN and pull R01 gently versus with a shock. Compare the 14 and 18 kN upgrades. Existing owned upgrades remain available; no save reset is needed. Both cooperative peers must use matching updated code/catalog. Live breaking feel and multiplayer remain unverified.
