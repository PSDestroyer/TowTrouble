# Career economy calibration — 2026-11-06

Unity6000.3.19f1; Unity CLI1.0.0-beta.17 confirmed latest. Existing OpenWorldRecovery scene/catalogue, single free chosen starter, English UI. No scene regeneration, force/load changes, build or commit.

## Final rules

Available retains three cards; Completed retains authored history and smooth scrolling. The opening career order is Workshop meadow, Forest ruts, Fishing shore, Reed bank, Gravel bend, Below the mountain road (indices0,1,4,5,8,9). Afterwards unfinished jobs sort by tier and authored order. Completion IDs are unchanged; there are no equipment ownership gates.

Prices: Engine2 800CR; Engine3 2600 total or1800 after trading Engine2; low range/suspension1000 each; mud/sand600, winter800, alpine1800; winches14kN1600 and22kN4200; cables14/18/26/36kN200/1200/2000/4200. First body1400 and all37 paid body stats/prices stay intact. Generic wheel variant prices reflect their authored grip profiles. `RecoveryCareerBalance` supplies catalogue generation/refresh prices; current native asset also updated.

World offers combine actual authored road distance, positive ascent, setup/recovery/obstacle allowance and tier. Model:90s setup + outbound metres/6.5 + towing metres/3.0 +20s/tier + max(0, blockage−8000N)/300 +0.4s/outbound ascent metre +1s/loaded ascent metre +30s for a separate tow obstacle. Fee is round-to50(200 + expected minutes×(180+35×tier) +100×tier), bounded below by600/850/1200/1800/2500. The3 original tutorial contracts retain tier fallback fees. Offers are cached from the map, never increased by player elapsed/idle time. Missing routes use1.25×straight distance. Timing is a planning estimate without deadline or speed bonus.

First completion adds25%, no resets10%, no cable breaks10%. Replay base retains50/55/65/75/80% for Local/Standard/Specialist/Hard/Expert, then performance bonuses apply. Board cards/detail and results use the same fee/payout functions. First time rewards and new contract variety are favored; replaying remains an allowed source of money. No claim that farming is mathematically impossible.

## Automated rules and preservation

- `Tests.json`:109 direct-invoked NUnit cases passed, not the complete Unity Test Runner suite. Includes all four starter/body advantages, two unclean first fees covering first body, introduction/history stability, five tiers of quote→claim replay consistency/idempotence, catalogue/authoring prices, Engine2→3 trade, legacy historical credits, buying/selling/preview/save/handling/input regressions.
- `Preservation.json`:79 catalogue entries changed only Price. IDs, thumbnails, prefab references, compatible sockets and physical stats unchanged. Scene file untouched; no target masses/terrain/pockets/roads/dropoffs modified. Legacy v1 credit migration deliberately keeps the old historical payout; v2–v4 money/inventory are not recalculated.
- `Final.json`: four original protected PlayerPrefs values/existence identical after restoring the isolated test (two keys restored); Editor stopped, no compilation, OpenWorldRecovery clean. Private backups are under `/private/tmp/TowEconomy` and are not committed. Plist byte hash is not claimed identical.

## Career and full-map force estimates

`CareerAudit.json` contains48 introduction steps:4 starters ×2 buying routes ×6 jobs. Route A buys first body after job2, mud tires+Engine2 after job3; Route B buys mud tires+Engine2 after job2, first body after job3. Both then buy18kN cable after job4,14kN winch after job5, low range after job6. All use actual first-completion payouts with BOTH performance bonuses missed. No replay, resale or gifted money. Every purchase is affordable, ending with2274CR and6600CR invested. The first two pay2375CR total even with both performance bonuses missed; first body leaves975CR. Across all48 approach estimates, minimum positive steady-force margin is1.735kN.

The same report includes96 late-game estimates (24 sites ×4 platforms) with the heavy Pickupster Tow body, Engine3/low range, suitable tires,36kN cable,22kN winch and suspension. Every site has a positive sampled margin, minimum3.961kN. Samples use35 approach points per configuration, target wheel contact plane/gravity, driver slope/soil/traction/anchor/cable budget and existing clearance checks. Separate tree removal is not combined with casualty force. These are possible controlled setups, not actual completed routes or a guarantee for arbitrary player positioning, shocks and geometry. Purchasing the same complete body deliberately converges different starter baselines.

## Actual Play timing and payment

Isolated fresh Ranger950kg/8.6kN,11kN cable/8kN winch; normal Rigidbody rotation and actual wheel forces. Original progress was backed up first. Cold Solo initialization required a guarded warm retry; no networking changes were made.

- `Drive.json`:9132 actual frames; three road-following probes at target25km/h from controlled garage-road start, excluding final off-road approach. Workshop meadow143.84m in23.13s; Forest ruts305.79m in46.06s; Gravel bend474.65m in86.37s. The final outbound model metres/6.5 +0.4×ascent predicts22.13/47.50/85.41s for those samples. This is a tiny calibration set, not a universal speed test.
- `Delivery.json`:9264 actual frames. R01 extraction and actual delivery completed in158.68s from a controlled attachment approach;8.63s to release,150.05s for road joining/hauling/parking;0 breaks. Actual payment matched the then-current offer870CR. It excludes outbound driving and is not a single uninterrupted full mission from the garage. The long manoeuvre time motivated the final90s allowance and3m/s loaded route estimate. Final R01 planning time171s compares with approximately182s when adding the separately measured outbound; fastest human runs may be much shorter.
- Final time/ascent/replay coefficients were applied AFTER those physical probes.109 rule cases, all career/late force estimates and production UI captures were rerun against final source. The physical driving probe is unchanged physics; the870CR recorded live payment is the earlier fee, not the final1015CR clean first R01 payout. Do not relabel it as a final-price live claim.

## UI / Console / limits

12 production Board previews (Available start, Expert, Completed; keyboard/Nintendo; text100/130%) passed612 TMP checks.16 production result previews passed360. `Layout-summary.json`; representative PNGs. Completed static capture selects the first visible history card; live scrolling/nav retains previous validation. Preferences and scene dirty state preserved. Synthetic result previews use illustrative times, not timing evidence.

Final Console ground truth: compilationFailed=false, errors0, warning1. Earlier Pipeline HTTP transport errors during reload/timeouts remain in captured history; Console was not manually cleared. No gameplay exception observed during successful probes. A transient ambiguous Object compile error was corrected before validation.

Still needed: human full-route timing for all24 jobs and all four starters, measured late-game CR/min with faster equipment, true Hard/Expert physical acceptance, awkward angles/terrain/obstacles/reconnects, co-op combined pulling/payout pace, physical gamepads, builds/performance. No automatic price/refund/profile reset was applied to the user's inventory. This is a coherent playable economy baseline with representative calibration, not a finished live balance claim.
