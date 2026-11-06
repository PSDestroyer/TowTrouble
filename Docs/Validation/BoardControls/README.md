# Recovery board controls — 2026-11-06

Preserved Available's three cards, Completed's scrolling history and the existing right-hand details. Added keyboard/controller hints, persistent recovery selection and dedicated menu navigation independent of native uGUI focus.

- Q/E or shoulders select Available/Completed. W/S, Up/Down or D-pad/left stick select cards; repeat uses unscaled time and clamps at either end.
- Enter/confirm always invokes the primary Accept/Abandon action, with a release latch and existing abandonment confirmation. Esc/back returns to driving. Host authority remains enforced.
- Xbox: A/B, LB/RB; PlayStation: Cross/Circle, L1/R1; Nintendo: A/B, L/R (east confirms, south cancels). The common identity/prompt helper is a foundation for the user's future full gamepad rollout. Other menus/gameplay still require that broader migration.
- Native EventSystem navigation is suspended for this board/its confirmation, while pointer click/scroll still work. The previous navigation setting is restored on exit/disable/destruction. Closing actions consume the frame to prevent gameplay input leaking through.

## Actual validation

Authorized isolated Play/Solo session: **29 checks passed over 1,226 advancing frames** in `Live.json`. Used Input System state events for a keyboard and virtual Xbox, Sony and Nintendo devices. Covered first selection, W/S/arrows, empty Completed, twelve-entry history and selected-card scrolling, boundaries, tab memory, acceptance, held Enter not bypassing confirmation, cancel/back restoration, shoulders/D-pad, all three confirm/back mappings and confirmed abandonment.

The twelve completed records were installed in a memory-only test profile; no mission was completed or paid, and no items were bought. Original protected profile keys matched exactly after stopping Play, with zero restoration writes. Whole preference plist byte identity is not claimed. Initial cold tool-started Solo did not connect; a warm Host retry succeeded. No build, multiplayer-peer test, physical-controller test or performance benchmark was run.

The captures show the production HUD. Eight production previews (keyboard/Xbox/PlayStation/Nintendo, text100/130%) passed240 text checks; scene dirty state and protected preferences remained identical. Nine focused controller identity/physical mapping/prompt test cases passed by direct Edit Mode invocation. C# compiled; final Console had no errors. Private original profile backups remain outside the repository under `/private/tmp/TowBoardControls`.
