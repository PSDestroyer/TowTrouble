# Steering direction in actual gameplay — 2026-11-07

Status: passed for the requested Solo gameplay/garage scope. No production code or asset changes were needed during this validation.

Scene: OpenWorldRecovery, Unity 6000.3.19f1, CLI beta.17. Existing direction fix compiled clean before this run; not recompiled because source was unchanged. Before Play: Editor clean, 0 current Console errors/warnings, commit4802351 with the existing uncommitted steering/exhaust work.

42 actual Play Mode cases: four stock starters, 37 purchased body variants, and a final return to Scout Stock. Every case used OpenGarage → PreviewGarage → CommitGarage → CloseOverlay, then actual session validation steering input through the normal Drive RPC and advancing physics/presentation frames. Input was combined steering/throttle with brake held to keep the vehicle in the garage yard; the game normalizes that vector. Checked visible steering mesh, unchanged hub position, positive/negative signed wheel rotation and both rendered front-wheel axle directions, then centered all three. All42 passed. Right steering +120.24° to +120.59°, left −120.59° to −120.20°, maximum center residual0.142°. Each road-wheel pair had the matching sign and centered within1°.

Three additional real UI action cases: Rod Electron → Pickupster Low → Ranger Stock. Used the garage card selection handlers and the primary Button.onClick (where a new part was required), verified the equipped profile ID and actual visible steering mesh against the selected prefab, then exited with the UI CloseGarage handler. All three passed the same gameplay direction and center checks. UIFlow.cs records these steps; it invokes UI actions through the native Editor, rather than synthesizing physical keyboard/gamepad presses.

Right.png and Left.png were visually inspected as a matching close camera pair for an open body. The temporary camera only rendered validation images and was destroyed afterward. The42 case sweep used garage session methods, not42 synthesized button clicks; the three supplemental cases explicitly exercised the UI action.

HarnessFirstAttempt.json preserves the initial harness threshold error: checking raw steering>0.9 rejected normalized input x≈0.894 even though steering/road-wheel signs matched. Adjusted only that harness input threshold to0.8; rotation-sign thresholds were unchanged. No game source changed to make the validation pass.

Cleanup: Play stopped, active scene clean, validation scope restored, SHA256 hashes of actual save and backup/temp artifacts unchanged. Final current Console:0 errors,3 warnings; recent warnings include Unity AI Account API timeout. Historical captured log counts are not current errors. No contract accepted, build produced, real second peer or console/controller hardware tested.
