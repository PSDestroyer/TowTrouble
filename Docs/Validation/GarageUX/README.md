# Garage UX integration — 2026-11-06

Implements the approved garage list: single-part preview/fit, explicit item states, fitted→preview stats, surface-aware advice for the board-highlighted job, body compatibility review, price/role/ownership filtering, four named saved setups, and keyboard/controller navigation throughout the garage.

## Validation

- Unity 6000.3.19f1, URP, OpenWorldRecovery; Unity CLI beta17.
- 57 direct-invoked NUnit cases passed across workflow, body balance, handling, catalogue, grid and input prompt suites. This is direct Edit Mode invocation, not the Unity Test Runner.
- Keyboard static previews: 18 layouts (nine scenarios at text100/130%), 2,056 TMP checks passed. A later small label/width refinement is covered by the controller layouts.
- Controller static previews: Xbox/PlayStation/Nintendo × three scenarios × text100/130%, 3,432 TMP checks passed. Main scene identity/dirty state and four protected preference values/existence stayed unchanged. A CLI timeout occurred before one final response, but its render report and images completed; results were checked on disk.
- Static rendering found and fixed oversized recommendation text, cramped tire comparison numbers, duplicate Active setup states and an Owned-filter label clipped by the longer PlayStation Square hint.

- Authorized isolated Play/Solo: 75 scripted steps / 55 checks passed across 2,250 actual frames, plus D-pad/left-stick navigation: seven checks across 247 frames. Vehicle constraints were frozen near the workshop; purchases and saves used a copied test profile with fresh inventory and controlled credits/progression. This validates actual UI input and transactions, not a driven recovery route. The first cold Solo attempt timed out during initialization; guarded warm Host retry succeeded.
- Live coverage: board/garage native navigation ownership; grid arrow/WASD scroll; recommendation; insufficient money; no preview accumulation; one-part purchases; owned/stock free refit; compatibility review before payment; held-confirm suppression; cancel; replaced inventory retained; 70% sale/cancel/in-use restrictions; saving fitted rather than preview; role labels; applying/overwrite review; All/Owned and empty roles; semantic Xbox/PlayStation/Nintendo Confirm/Back; shoulders/triggers; right-stick rotate/reset; no cable/pause on exit. Supplemental D-pad/LS test covered grid column movement, category entry/change/return, no spend and exit.
- No project gameplay error was captured after warm Solo initialization (cursor22501). Existing Console contained two Pipeline transport errors and three warnings; these were not attributed to the garage. Compilation-failure flag remained false.

- Cleanup: Play stopped; OpenWorldRecovery remained the active scene with isDirty=false. All four protected preference keys matched the original values/existence after restoration (two keys restored). Whole-plist byte identity is not claimed. Private original profile/plist evidence remains under /private/tmp/TowGarageUX and is excluded from the repository.

## Scope and limits

Existing body economy/speed changes, mission progression, garage access gates and host validation remain in force. Advice uses fitted surface grip and nominal breakout/cable/pull values and prioritizes suitable owned parts. It does not simulate the entire route or guarantee recovery success. Names cycle preset role labels without a text-entry keyboard. Saved setup keys remain legacy-compatible; Label is an optional additive field.

Input family tests use virtual Input System devices. Physical controller hardware, multiplayer peers, performance and player builds are not covered. Full gameplay/other-menu controller conversion is outside this garage iteration. No commits or player builds.


## Evidence images

`Body-130.png`, `DualShock-WheelSnow-130.png`, `Switch-Compatibility-130.png`, `Xbox-Setups-130.png` are controlled Edit Mode renders with a plain background. `Wheels.png`, `Nintendo.png` and `Grid-scroll.png` are actual Play captures. Screenshot requests are asynchronous: a capture may show the following fitted state rather than the preceding preview request. Static modal captures therefore supply the stable overlay evidence.
