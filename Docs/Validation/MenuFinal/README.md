# Tow Trouble menu integration — 2026-11-06

Status: ready for user testing, with hardware and two-peer coverage still outstanding. No build or commit was created.

## Implemented flow

| Area | Current behavior |
| --- | --- |
| First launch | Title → comfort preferences → preview four starters → confirm exactly one free starter → optional training or exploration. Starter selection is no longer an accessible vehicle shop. |
| Main | Play Solo, Play Co-op, How to Play, Settings; smaller Credits and Quit. Actual fitted car and Stock/Modified pull/cable summary. |
| Vehicles | Different bodies and upgrades are bought in the gameplay workshop. Existing owned equipment and legacy profile migration remain supported. |
| Tutorial | Current OpenWorldRecovery scene, solo practice profile, temporary cones and practice casualty; driving, camera, cable, winch, extraction, delivery and workshop. No permanent terrain/scene edits. |
| Practice economy | Separate 5,000 CR practice budget, including for a new player with zero credits. Finishing or ending training restores career money, fitted parts and receipts; tutorial flags and comfort preferences are saved, with career receipts untouched. |
| How to Play | Driving / Recovery / Workshop guide, Q/E or shoulders, contextual Back, replay training from Main. |
| Controls | WASD/arrows or D-pad/left stick, Enter/semantic Confirm, Esc/semantic Back, shoulder tabs, focused sliders, release latch, focus memory, contextual keyboard/controller hints. |
| Controller families | Automatic detection and manual override; Xbox, PlayStation and Nintendo glyphs. Nintendo east A confirms, south B returns. Virtual keyboard for name and host address. |
| Co-op | Separate host/join areas, direct-IP explanation, editable name/address, connection status, Retry/Cancel, recoverable error messages. Lobby requires two ready drivers; clear Ready/Not Ready and host-only departure. |
| Settings | Audio / Display / Controls / Accessibility / Game. Rebinding rejects duplicate/reserved keys. Defaults ask first. Audio volume no longer disables visual recovery effects. |
| Display | Unique resolutions, window/fullscreen, Quality/Performance. Performance uses a temporary URP copy rather than editing imported render assets. Apply → Keep, or automatic 15-second revert; leaving/disabling restores an unconfirmed preview. |
| Accessibility | Text size, reduced motion, camera inversion/sensitivity, tension warning symbols. Shared controller hints retain a dark background for contrast. |
| Progress reset | Explicit confirmation defaults to Cancel. Deletes career receipts/equipment/starter choice, preserves preferences, returns to starter selection. Disabled during a session. |
| Pause / results | Resume, Recoveries or training guide, Settings, Reset Vehicle and confirmed Main Menu exit. Practice delivery has a separate completion flow, never the paid career result. |
| Credits | Neutral Tow Trouble presentation. Creator/studio identity deliberately deferred at the user’s request. Separate scrollable third-party notices package existing FishNet and font license texts, not an exhaustive catalogue of purchased art. |

## Evidence

- Unity 6000.3.19f1, connected Editor, OpenWorldRecovery; final compilation completed with no compiler errors (`compile.json`).
- Actual Unity Test Runner: **243 passed / 0 failed / 0 skipped**, assembly `TowTrouble.Tests` (`regression-final.json`). Includes 12 new menu/settings tests. Two stale pre-existing expected values were aligned to the already integrated economy: payout 652 CR and winch upgrade price 1,600 CR; runtime economy was not changed for those assertions.
- **52 isolated panel renders** at text scale 100%/130%, **1,688 text components**, no reported overflow or build exceptions. Panels cover starter, settings, guide, co-op/lobby, confirmations, training, board and garage. Static preview fixtures do not establish live networking or gameplay (`capture.json`).
- Live menu navigation: **615 advancing player frames**, first preferences, directional starter preview, one-starter receipt, sliders, all settings tabs, display revert, binding rejection/acceptance, semantic Xbox/PlayStation/Nintendo confirmation/back, controller text entry and notices (`live-ui.json`). Display expiry was accelerated in the fixture after asserting an active deadline bounded at 15 seconds.
- Live edge cases: **471 advancing frames**, guide return to originating settings tab, display Keep, solo occupant co-op gating, Ready, confirmed lobby exit, unavailable host error, Retry/Cancel, loaded controller glyphs, confirmed reset and preference persistence (`live-edges.json`). This is single-instance lobby coverage, not two-peer multiplayer proof.
- Live full training: **1,528 advancing frames, 27.42 seconds**, zero-credit Ranger career, stock practice equipment, F cable input, actual winch/extraction physics, tow delivery, G workshop, temporary purchase, preservation of changed audio preferences, exact restoration of normalized career equipment/money, cleanup and Main Menu scene reload (`live-training.json`). An additional run restored a career with 2,430 CR and a fitted engine upgrade (`live-training-modified-career.json`).
- Successful live runs recorded no runtime Error/Exception events. Earlier fixture failures were investigated: stock equipment is represented by normalized stock IDs rather than an empty array, and headless Editor input routing can consume synthetic key events outside the player update. Final training injects virtual events before Dynamic Update and temporarily ignores Editor focus, restoring the original input settings on completion. Neither issue required a gameplay input workaround.
- New UI images and source .meta references imported through Unity. Original user font edits and platform glyph source folders preserved. No scene, prefab, imported render asset, QualitySettings asset, mission balance or reward values were reauthored in this menu pass.

## Save safety and cleanup

All live tests used `TowTrouble.Validation.MenuFinal`, never the real profile key. The real profile had already been deleted at the user’s explicit request before this implementation. Final cleanup verifies those real keys remain absent, removes validation keys, resets the override and stops Play Mode. See `cleanup.json`.

`RecoveryProfile.SuppressSaving` protects the practice profile; Editor previews cannot persist careers. Validation namespaces compile only in Editor/development configurations. Test profiles and preference backups are not included here.

## Remaining coverage

- Physical Xbox, DualShock/DualSense and Nintendo controllers, hot-plug adapters and target-platform glyph detection.
- Two real peers joining, both ready, gameplay deployment, disconnect/rejoin and network latency after this menu integration. Existing networking authority is retained, but one instance cannot establish this coverage.
- Actual fullscreen/monitor switching outside the Editor, ultrawide/4:3 hardware, and all 24 recoveries with every starter. Static canvas sizing uses a centered 1600×900 safe frame with CanvasScaler Expand; tested renders are 16:9.
- Creator/studio names are deferred. Final distribution licensing review should confirm any additional notices required by art/audio included in the shipped build.

Selected PNGs distinguish static panel renders from actual live Main captures. Screenshots depict an isolated test profile.
