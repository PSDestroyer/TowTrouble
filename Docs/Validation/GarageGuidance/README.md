# Garage location and entry guidance — 2026-11-05

The existing compact waypoint now identifies the actual workshop parking/interaction center during free exploration. It shows the garage icon and `GARAGE`, then distance, `Park here`, `Stop to customize`, or the bound key followed by `Customize`. The default ready caption is **G · Customize**; controller input uses X. The caption uses plain visible text so the key remains readable against the plate.

`RecoveryHudGarageGuidance.cs` reuses the current waypoint; it does not add a second large panel. The marker appears within 75 m and stays until beyond 85 m, with a 12 m altitude limit. Final parking guidance starts below 16 m on the same level. Visibility hysteresis does not relax entry eligibility. The marker follows the final camera projection before the canvas renders, and hides during active recoveries and menus. The six-second Tab discovery hint starts after the garage marker relinquishes attention, avoiding duplicate prompts at spawn.

`RecoveryWorld.InGarageArea` shares the existing horizontal distance below 8 m and height difference below 3 m checks with `AtGarage`. The existing entry conditions remain: speed below 1.2 m/s, no attached cable and Explore phase. `DemoSession.CanOpenGarage` also requires a connected free-roam session, no open menu and a garage catalog; both the displayed ready action and `OpenGarage` use that gate. Input bindings, progression, purchases and save formats are unchanged.

## Confirmed static validation

The production waypoint was rendered in a temporary preview scene at 100% and 130% text scale. Distance, `Stop to customize` and **G · Customize** all fit on a single line without overflow. The recorded plate widths are 261 reference pixels at 100% and 314 at 130%. Scene state and all four protected preference keys stayed unchanged. The report is `GarageHud.json`. The final 130% capture was visually checked with G clearly visible; earlier previews with an obscured key are excluded.

![Garage guidance at 130%](GarageHud-130.png)

## Verified Play/Solo validation

`GarageHintProbe.json` records **eight passing checks across 342 real frames**:

- At 20 m, the garage marker shows distance without offering entry.
- At 12 m, it shows `Park here`.
- Just outside 8 m, the ready action is absent.
- At 3.05 m above the parking level, entry remains unavailable.
- Moving inside the area shows `Stop to customize`.
- Once eligible, the actual entry gate and **G · Customize** agree.
- A queued G key event opens the real workshop UI and hides the marker.
- A queued Escape key event closes it and restores the ready marker.

The probe used controlled placements and rigidbody constraints for spatial boundaries, then one initial velocity and normal live braking for the moving/ready transition. G and Escape were delivered through the normal Input System update; no manual input update or physics step was invoked. This validates HUD/access/input behavior, not a full manually driven route or controller interaction.

The isolated stock profile made no purchases, saved builds or contract completions. All protected preference values and key existence stayed identical throughout the run and at probe cleanup. After stopping Play, the final restore check confirmed identical protected keys with zero restoration writes. The whole macOS plist byte hash changed with Unity metadata; whole-file identity is not claimed. Private backup/preferences files are excluded from this folder.

The live ready marker and opened workshop captures were visually checked. Final Editor state was stopped and ready, with no compilation failure and zero Console errors.

![Ready to enter the garage in live Play Mode](Garage-Ready.png)

No player build, performance benchmark, controller-device test or multiplayer-peer validation was performed.
