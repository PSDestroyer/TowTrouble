# Career economy and six-job introduction

This iteration replaces the earlier independent price/reward tuning. It retains the three Available cards, scrolling Completed history, durable completion IDs, owned inventory and each player's single chosen free starter. No terrain, extraction forces or target masses are regenerated.

The shared opening order is Workshop meadow → Forest ruts → Fishing shore → Reed bank → Gravel bend → Below the mountain road. Four starter models are checked individually before purchases; paid-body baselines then converge deliberately. Remaining work is ordered by difficulty and authored location order, so unfinished Hard jobs cannot occupy all three slots while easier work remains. Completed history retains authored order and every replay remains accessible.

Prices are shared between the catalogue and authoring through RecoveryCareerBalance. First bodies retain1400–4600CR and10–16.96kN. Engine2 costs800CR (+20% drive); Engine3 costs2600CR total (+35%), crediting800CR when Engine2 is traded in. Low range1000CR (+25% drive/−20% speed), off-road suspension1000CR. Mud/sand tires600CR, winter800CR, alpine1800CR; visual wheel variants are valued by their actual grip profiles. Winch14kN1600CR and22kN4200CR; traction/anchoring still limit both. No engine/winch/body buff is substituted for soil physics.

Cable progression is11kN included,14kN200CR,18kN1200CR,26kN2000CR,36kN4200CR. Other starter platforms remain optional1200CR alternatives, rather than recommended power upgrades. Cosmetic part prices stay unchanged. Existing inventory and money are preserved; old version1 historical credit migration keeps its old payout rule.

World offers use the actual authored road network for outbound and loaded return distances, plus setup/extraction/obstacle time and a difficulty-dependent fee. They are fixed for the accepted contract. Actual elapsed time never increases the payout; there is no timer deadline or speed bonus. The UI shows an approximate planning duration, not a promise. Missing road routes use a conservative distance fallback. The three original tutorial contracts keep their tier fee fallback.

First completion adds25%; no resets and no cable breaks each add10%. Replays retain50/55/65/75/80% of the base fee by difficulty, then performance bonuses apply; the reduced fee is shown before acceptance. The exact final coefficients and timing evidence are recorded in Docs/Validation/CareerEconomy after controlled Play measurements. The route model is a starting calibration, not a claim that all24 missions have been driven with every model.

Final timing model and final-price tables: `Docs/Validation/CareerEconomy/CareerAudit.json`; validation and limits: `Docs/Validation/CareerEconomy/README.md`. Representative live road samples are23/46/86s; the separate stock extraction/delivery probe took159s. These measurements calibrate the planning estimate without rewarding slow play.
