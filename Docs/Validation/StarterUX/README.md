# Starter selection UX — 2026-11-06

The starter screen now compares Scout, Hauler, Buddy and Ranger in one vertical column. Every card shows its role, actual pull, mass and top speed. The chosen model has a gold card/check, a larger dedicated preview, a short description, compact paint swatches and a separate confirmation action. No starter balance, prices or economy rules changed.

The default camera uses the vehicle’s local front rather than the map’s world axes. It frames each model’s body bounds in the open area, retaining the front three-quarter view during the confirmation dialog. Selecting another model resets the orbit; mouse drag/right stick rotates, C/right-stick press resets. Preview changes do not fade the complete screen. Input hints follow device changes.

Navigation: Up/Down (W/S or D-pad/left stick) compares cars. Right enters paint, Left from the first paint returns to the chosen car. Down from paint reaches Choose. Enter/semantic Confirm on a car opens the review; on paint it previews only. Esc/semantic Back cancels. The review defaults to Cancel and retains the car/paint preview when cancelled. Final confirmation still grants exactly one free starter.

Validation in Unity 6000.3.19f1, OpenWorldRecovery:
- Editor compilation passed.
- Live validation advanced 823 player frames with no captured runtime Error/Exception events.
- 47 assertions passed, plus eight camera/layout records: all four actual model indices at 100% and 130% text. All eight projected body corners stayed in the open preview area, default camera was in front of each vehicle (front dot ~0.852), and no TMP text overflow was reported.
- Live keyboard selection, paint navigation, preview isolation, camera rotation/reset, device-hint switching, confirmation/cancel, Nintendo east A / south B and exactly-one-starter persistence passed in a separate validation namespace.
- Real profile keys were backed up privately before testing and compared afterward; see cleanup.json. Test keys were removed, input settings restored and Play Mode stopped. Scene/asset edits were not saved. No build or commit.

PNG files are actual live Game-view captures. Test controllers were virtual; physical controller hardware and non-16:9 displays were not exercised in this iteration. No broad gameplay or economy regression suite was rerun for this focused UI/camera change.
