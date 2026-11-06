# First-run starter choice validation — 2026-11-06

Unity6000.3.19f1, latest Unity CLI1.0.0-beta.17. Existing OpenWorldRecovery scene and native catalogue; no map regeneration, build or commit. This replaces the old Scout-only player baseline, not authored recovery target masses/loads.

## Automated and layout evidence

- `Tests.json`:88 direct-invoked NUnit cases passed (not a claim that the complete Unity Test Runner suite ran). Covers single free choice for all four models, repeat/invalid purchases, legacy migration and roundtrip, body advantage with identical upgrades, cable reward budgets, body performance/ordering/handling and the previous mud support UV regression.
- Sixteen isolated production Vehicles layouts: pending/returning × four previews × text100/130%.368 TMP checks fit; original preferences and active scene state preserved. `Layout-summary.json`; representative final renders `Choose-0-100.png`, `Choose-3-130.png`.
- `Loads.json`:384 static configurations,24 authored sites × four starters × four equipment stages,35 sampled approach poses per configuration. All40 starter-only Hard/Expert configurations have negative sampled force margin; first two interventions have positive margins for every starter. Uses the existing audit approximation, not actual driven routes or campaign completion. Grip, ground contact, approach and impulses can differ in actual play.

## Actual Play / Solo

- `Live.json`:17 checks over658 actual frames passed using a virtual Nintendo Input System device. Title→first preferences→starter choice→Ranger preview→review cancellation→confirmed free Ranger→save/load→returning Main. Host cannot bypass pending choice; held confirms cannot grant; exactly Ranger is owned, no implicit Scout gift. Physical showroom uses950kg, Stock label names Ranger. First preferences require navigating to Let's Go; the initial fixture incorrectly assumed it was already focused, then the corrected fixture used four real D-pad moves and passed. No production change was needed for that fixture assumption.
- `Choice-live.png`: actual pending Ranger choice in the world.
- `Upgrade.json`:8 checks over182 actual Solo frames passed. Controlled isolated profile budget1562CR. Actual workshop preview does not buy/fit; primary buys first body Midjet Wood1400CR once, leaving162CR. Performance improves Ranger950kg/8.6kN/58km/h to980kg/10kN/74km/h. Refitting Ranger Stock is free and preserves paid inventory, starter identity and the single free ownership bit. Closing returns to driving. `Body-fitted-live.png` was captured after fitting because screenshot capture resolves at frame end.
- `Pulls.json`:2505 real Solo frames. Stock Ranger950kg,11kN cable,8kN winch, normal Rigidbody rotation, controlled approach poses and braking. R01 released in7.14s with0 breaks. R17 reached0.872 progress and the3.5m minimum cable length, but did not release within30s while stationary.
- `PullsAssist.json`:937 real frames, R17 follow-up with the same stock equipment and gradual drive assist after10s. Released in13.25s with0 breaks. These are extraction probes, not full drives/deliveries, payout tests or campaign timings. Target remained1350kg in both cases.

## Cleanup and limits

Original four protected PlayerPrefs key values/existence restored exactly after Play (two keys restored); private backup remains outside the repository. Plist byte hash differs after native preference writes; logical protected values are identical. `Final.json`: Editor stopped, no compilation, OpenWorldRecovery scene clean. Final layout rerender also preserved preferences. Console has historical/current Pipeline HTTP transport errors from Editor reload/timeouts (latest14:53UTC), not gameplay errors from the subsequent successful fixtures; Console was not cleared. No additional gameplay errors observed during the validated fixtures.

Actual Play exercised Ranger; all four choices/baselines/body advantages have automated coverage, not four full campaign playthroughs. Physical controller hardware, a multiplayer peer, builds and performance profiling were not tested. Existing user's money, inventory, completion and active build were not reset. The earlier `StarterProgression` evidence uses old stronger Scout stats and should not be reused as physical validation of this iteration.
