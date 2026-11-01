# R01 arcade extraction prototype — 2026-11-01

Baseline commit: 791b4bb. Existing uncommitted forest, terrain, photo and context work was retained.

## Player behavior

Open OpenWorldRecovery, enter Play manually, choose Explore and accept R01 / Poiana atelierului (Workshop meadow). The existing E / gamepad A connection, driving and winch controls remain in use.

The front-left wheel has a small local rut and the car starts 6 cm lower for valid initial suspension contacts. Load rises gradually over roughly .45 seconds toward full cable capacity. An aligned pull must overcome static resistance; resistance then includes lower sliding friction and a speed-dependent drag. A sideways pull is less effective. Progress is the smaller of chassis and blocked-wheel displacement along the authored exit direction, divided by 2.2 m. Time spent holding throttle adds no progress.

Both the chassis and blocked wheel must cross the exit plane. The selected wheel needs ground contact and the chassis must be sufficiently upright for a .2-second contact hold. Resistance then fades over .4 seconds, with no added launch impulse. The car stays released for the rest of that contract; delivery begins after the fade. Native dust emits at the contact point and the HUD shows the release for two seconds. Existing load-dependent engine audio follows the changing cable load; no new audio asset was added.

Only R01 enables the profile. Other 23 sites, fallen-tree stages, tutorial extraction, economy, delivery rules and progress IDs retain their behavior. The new settings serialize disabled on other sites. The extra recovery state uses the existing host-owned 15 Hz snapshots, without a new RPC. Both peers must use matching updated scripts.

## Main files and tuning

- Runtime/RecoveryExtraction.cs: settings and deterministic displacement/force rules.
- Runtime/RecoveryWorldExtraction.cs: host integration, ground-contact checks and replicated presentation.
- Runtime/RecoveryWorld.cs and RecoveryWorldScenarios.cs: cable loading, .8-second overload tolerance for the pilot, extraction transition and existing snapshots.
- Runtime/RecoveryHudExtraction.cs: contextual Romanian/English feedback using the existing HUD row.
- Runtime/RecoveryMap.cs and RecoveryContract.cs: per-site configuration.
- Runtime/RecoveryVehicle.cs: small native contact-point dust burst.
- Editor/ExtractionPrototypeAuthoring.cs: focused, repeatable R01 authoring. No world/forest generator runs.
- Tests/Editor/RecoveryExtractionTests.cs: rule regressions.

In RecoveryMap > Sites > R01 > Extraction: Enabled, BlockedWheel=0, Travel=2.2 m, BreakawayForce=5600 N, MovingForce=3600 N, MovingDrag=4200 N/(m/s), ClearHold=.2 s, ReleaseDuration=.4 s. These absolute forces fit the existing Scout force budget; they are not percentages of its 31000 N cable rating. The proposed 6–10 second feel target still needs the user's driving test. There is no artificial minimum completion timer.

Authoring menu: Tow Trouble > Recovery > Apply R01 Extraction Prototype. R01TerrainBaseline.json retains the original height patch and original site description/pose. Reapplying uses the fixed baseline and keeps the spawn-depth adjustment at .06 m; it does not deepen the rut repeatedly. Existing R01 tire ribbons are fitted locally so they do not float above the modified terrain. Original scene, terrain, context and ribbon files were copied to this chat's work/extraction/before directory before modification. Preserve pre-existing work when reverting; do not reset the whole scene/repository.

## Verified in Edit Mode

- Unity 6000.3.19f1 compiled the feature with no compilation errors.
- 23 directly invoked Edit Mode checks passed: 15 new extraction/HUD/cable checks and 8 existing scenario/cable/delivery checks. Test Runner was not started.
- Stationary throttle, lateral/reverse/vertical movement, wheel-only rotation, chassis-only crossing and airborne exit cannot complete recovery.
- Contact hold, smooth release, free-state latching, detaching/partial progress, angle resistance and truthful movement feedback checked.
- Four authored spawn wheel probes hit the terrain. R01 terrain reapplication was byte-for-byte identical at the sampled height-array level.
- Six height samples changed, all within 2 m of the selected wheel; maximum added sampled depression about .214 m. Other 23 site records and all dropoffs were unchanged at authoring time.
- Static posed camera captures inspected. These are not runtime screenshots or proof of driving feel.
- Tutorial scene, R01 contract photo, painted ground map and open-world graph stayed identical to the pre-task copies. No package or Project Settings changes.
- Console has no feature/compilation errors. Unity AI Assistant reports an account-service/network warning independently of this feature.

Evidence: Rules.json, SceneAudit.json, ContactsAndRepeatability.json, Authoring.json and the two static PNG captures.

## User Play Mode test

1. Fresh solo Explore session, accept R01 with the default Scout. Verify the wheel/rut is legible from the normal camera.
2. Attach and pull roughly along the trail with gentle throttle. Expect initial load, slow physical movement, one release and then delivery.
3. Try a sideways pull, then reposition toward the exit. Expect a poor-angle hint and an easier aligned extraction.
4. Stop/detach halfway and reconnect. Position is retained unless actual terrain physics moves the car; holding throttle while blocked must not fill progress.
5. After release, turn/reverse and continue towing: no repeat resistance or dust burst. Reaccepting the contract restores the blocked state.
6. Try a short acceleration shock and a sustained overload. The pilot grants .8 s overload tolerance; warning/rupture feel is uncalibrated.
7. Retest R02's fallen-tree stage and the tutorial; they should use their existing rules.
8. When ready for co-op validation, use matching code on host/client and check shared stage feedback and late joining.

No Play Mode, physics simulation, network startup, Test Runner, build, commit or push was performed. Runtime driving feel, timing, particles/audio in motion and multiplayer remain unverified, following the project's user-owned Play Mode testing rule.
