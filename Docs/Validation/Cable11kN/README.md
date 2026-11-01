# 11 kN starting cable — 2026-11-01

User explicitly requested one weaker starting tier at 11 kN.

Subsequent rupture behavior is documented in `../CableShock`: magnitude-sensitive failure replaces the earlier .8/.55-second timers. Tier values and prices below remain current.

Current progression: **11 stock/free → 14 /200 CR → 18 /450 CR → 26 /1400 CR → 36 /3400 CR**.

The existing free `stock_Cable` ID now rates 11000 N, including default vehicle stats and tutorial. New `cable_14` is the first purchasable upgrade. Existing `cable_18`, `cable_26` and `cable_36` IDs, strengths and prices are preserved; only the 18 kN description is updated because it is no longer the first upgrade. Existing stock-equipped saves intentionally get the new weaker stock rating. No profile version change, save reset or real PlayerPrefs writes.

Catalog grew 381→382 entries. All non-cable part records stayed identical; applying the focused authoring action twice is idempotent. No scenes or prefabs regenerated. Force formula, overload grace, R01 resistance and rewards unchanged. R01 breakaway remains 5600 N, below the new 11000 N cable threshold; full nominal Scout drive is 12400 N, so a blocked full-throttle pull can exceed stock strength. Live balance remains for the user.

Unity compilation passed and 41 direct Edit Mode checks passed with per-test setup, covering stock selection, all five tier values/prices, first upgrade purchase, unowned upgrade rejection and purchased 14/18/26/36 kN save round-trips, plus existing extraction/scenario/cable/delivery checks. Editor stayed stopped and scene clean. No Play Mode, Test Runner, physics/network simulation, build, commit or push.

Evidence: Rules.json, Catalog.json, VehicleReset.json. Backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/Cable11kN-20261101`.

Retest in a fresh Play session: Garage > Towing > Cable, equip the 11 kN stock and compare gentle versus abrupt pulls at R01, then the 14 kN upgrade. Owned stronger cables remain equipped unless stock is explicitly selected. Both co-op peers need matching code/catalog.
