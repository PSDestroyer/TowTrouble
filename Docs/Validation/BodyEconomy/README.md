# Body economy, speed and catalogue ordering — 2026-11-06

The prior catalogue priced every one of37 purchasable bodies at1,200CR despite mass980–2,040kg, nominal pull10.0–16.96kN and differing travel speed. The new body-only authoring pass is **Tow Trouble/Garage/Rebalance Body Prices and Speeds**; normal garage regeneration also uses the same profiles.

## Price and progression

Stock remains free. Purchasable bodies now span1,400–4,600CR. For an authored body:

`round_to_100(600 + (nominal pull kN - 10) × 450 + (mass kg - 1000) × 0.9 + (top speed km/h - 54) × 40)`, bounded1,200–6,000CR.

Pull carries the strongest weight; heavier bodies offer tire/anchoring capacity but cost travel speed and more uphill work. Fast/light bodies are alternatives for travel and nimble recovery, rather than universal towing upgrades. Same mass/pull/speed profiles have the same price. Existing mass, pulling force, grip, geometry and compatible sockets were retained;37 body entries alone changed in the catalogue. Inventory IDs and ownership are preserved; purchases use new prices and resale retains the existing70% current-catalogue-price rule. No profile is migrated or refunded for repricing.

`Bodies.csv` includes all38 entries in actual UI order, stats and illustrative clean-repeat run costs (540/960/1,560CR for local/standard/specialist). Example prices: Midjet Wood1,400; Bucket1,700; Speedster LeMans2,700; Lorry4,400; Pickupster Tow4,600. Campaign jobs continue to offer varied rewards and first-recovery bonuses; repeated-run counts are examples, not measured playtime.

## Speed and UI

- Base archetypes: Scout68, Hauler60, Buddy72, Ranger66km/h. Purchasable bodies span52–80km/h. Fastest: Speedster LeMans and Martian LeMans80km/h. Heavy towing bodies retain52–62km/h travel tradeoffs.
- GaragePerformance applies the80km/h ceiling after all multipliers, preserving low range's20% speed reduction. The vehicle fallback/model-reset path uses the same stock speeds.
- SpeedLimitedDrive offsets ordinary powered rolling/Rigidbody damping near cruise so the configured speed is attainable on a dry flat road. It never requests more than the existing nominal per-wheel force; gravity, soil, cargo and grip continue to affect achieved speed. It is a powered travel governor, not a hard velocity clamp for falling/downhill motion.
- The grid stays price ascending, with mass/pull/speed tie breaks for bodies and natural numeric names for equal-price parts (level2 before level10), stable ID final tie break. A visible price-order hint and two-line mass/pull/top-speed body card make comparison explicit. Body cards236px high; other cards keep220px.

## Evidence

33 focused NUnit methods passed by direct Edit Mode invocation: body balance, all3,306 body/wheel combinations, numeric/price ordering, buying/resale/save round trip, stock baselines, engine/transmission composition and existing garage regressions. Tests are memory-only; no purchase/payment was made in a real profile. Four production garage previews at100/130% passed628 text measurements. Static preview retained protected preference values and scene dirty state. Owned visuals, thumbnails and socket references remain unchanged.

Runtime flat-track evidence is recorded separately after the final driving probe. No build, map authoring, mission load recalibration or live multiplayer peer test.

## Actual driving probe

An offline Play fixture cloned the production driver and applied actual catalogue builds on a temporary dry flat cube track outside the map. No network/session/profile replacement was used. Rotation was frozen to isolate straight-line speed; this is not a handling, loaded towing or complete map-driving benchmark. Over837 advancing frames, settled samples measured Speedster LeMans79.997km/h against80 configured, Lorry53.997 against54, stock Ranger65.997 against66. All three passed; full samples are in Drive.json. The first fixture accidentally compared a cloned Ranger stock body with Scout's68km/h expectation; expected configuration was corrected to the clone's real ModelIndex and the entire probe rerun. Production code did not change for that fixture correction.

Temporary track/cars were destroyed and Play stopped. No build or actual contract/payment/purchase. Protected original preference values are checked at cleanup; no automatic migration of inventory is required.

Final cleanup: four protected original preference values/existence matched, zero restoration writes; plist byte identity is not claimed. Editor stopped/ready, C# compilation passed and Console errors0 (one pre-existing warning). Preservation.json confirms identical part IDs/order and only Description/EnglishDescription/Price/BodySpeed fields changed across37 body records; non-body data and all body visual/geometry/pull/mass fields remain identical to the pre-change asset.
