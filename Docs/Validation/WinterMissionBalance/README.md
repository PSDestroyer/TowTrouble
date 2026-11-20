# Winter mission balance — 2026-11-20

Scope: the 27 Winter road jobs and three surveys. All changes are in DLC logic or the Winter branch of the shared mission selector. Shared garage prices, classic recovery behavior and durable completion IDs are unchanged.

Rewards now reflect road approach, delivery distance, ascent and work difficulty. Base total changes from43,700 to35,000CR; individual jobs pay500–2,350CR. Seven intact cargo bonuses total1,400CR. Available missions sort by Easy / Standard / Demanding and then estimated duration, without equipment locks. Duration is an estimate from the garage, not a deadline. Catalog values and before/after route measurements are in [Checks.json](Checks.json) and [Balance.txt](Balance.txt).

Snow removal now removes a fixed depth per sweep, preserving deep material for another pass. Cargo mass varies from80 to360kg. Moderate jolts can retain the intact bonus; fragile cargo is more sensitive to impacts. Extraction resistance scales with target mass.

## Observed in Unity

- Ten plow jobs completed with stock vehicle: three needed one pass and seven needed two. Each worksite began at a validation fixture; subsequent passes used forward/reverse driving, with blade raised for repositioning. Work times13–74seconds exclude the commute.
- Two complete depot-to-depot cargo routes driven with normal loading and unloading: Supplies for Pinewood stock vehicle,95seconds; High Cabin Provisions winter tires, low range and engine_2,128seconds. High Cabin arrived intact and paid1,950+300CR. Approach from garage excluded.
- Six physical vehicle extractions passed using winter tires, low range, engine_2 and stock winch. Full towing delivery routes were not driven.
- All seven cargo loading/unloading/mass cleanup flows passed using depot positioning fixtures. Moderate-jolt bonus retention and severe-impact bonus loss passed.
- All27 distinct job completion bits and rewards, all three survey payouts, replay behavior and JSON serialization compatibility passed using fixtures. These are not30 complete playthroughs. Real career writes were suppressed.
- Eighteen selector layout variants at100% and130% text passed without overflow, with30 cards and31 loaded photographs. [Board.png](Board.png) shows the final selector.
- Final Editor compilation passed. The isolated run still emits the existing `SelectedLocale is null` errors in Unity Localization / RecoveryLocalization; Console is not clean. No player build was made.
- Play Mode stopped; original OpenWorldRecovery scene and profile scope restored. Real save, backup and previous file hashes were unchanged.

Estimates use network route length and climb, with reference work/delivery speeds. Full route timing for every mission, every vehicle and every equipment combination remains unverified. Automation scripts and transient telemetry were kept under `/private/tmp/tow-winter-balance`; the durable measured results are in Checks.json.
