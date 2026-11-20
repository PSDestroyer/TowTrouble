# Winter region integration — 2026-11-20

Implemented persistent completed works across the DLC, the four-step Pinewood story and local service, packed/forest route selection for the Pinewood deliveries, three distinct recovery preparations, and a forecast-driven winter weather cycle. Design and controls: [WinterRegion.md](../../Design/WinterRegion.md).

## Observed in Unity 6000.3.19f1

All probes used `TowTrouble.Validation.WinterRegion`. Real career writes were suppressed, and final file hashes were unchanged. Play Mode stopped and the original OpenWorldRecovery scene was restored. No player build was made.

- Plow Pinewood Road completed with the stock vehicle in one physical pass, about13seconds at the worksite.
- Workshop Meadow pickup was extracted and towed along the road to the forest depot in about96seconds of driving after extraction. No repositioning after the starting pull-point fixture.
- Timber on Lakeside Road was physically rotated and settled in its turnout, about11seconds.
- Supplies for Pinewood was loaded at Lakeside, driven the full forest cut and unloaded at Pinewood in about71seconds. Winter tires, low range and stock engine; the intact bonus was retained.
- The Pinewood weather readings were logged at the forest depot, then driven to Lakeside and handed in, about100seconds. This activated regional service.
- Lakeshore Stranded Scout was turned and extracted in about8seconds. Mountain Link Snow Rescue was secured and extracted in about5seconds. Logging Truck Snow Trap completed its side pull, disconnect, drive to the second position, opposite-eye reconnect and extraction in about31seconds. These probes used winter tires, low range, engine_2, winch_2 and cable_18. They did not drive every subsequent towing delivery route.
- Chain locks, ordered unlocking and out-of-order legacy replay access passed. A native SaveStore/FileSaveStorage roundtrip through a temporary file preserved the earned Pinewood receipts and wallet.
- A separate all-completed fixture was reconstructed through that file and a scene reload:10 open lanes with shoulder snow,4 parked trunks,13 destination displays and unlocked service. Replaying and abandoning a plow job preserved its permanent cleared state. This is restoration coverage, not30 complete playthroughs or a full application restart.
- V switched the route, G opened the regional garage with authoritative garage state, and Escape closed it through normal Input System keyboard events, with the brake held and the vehicle actually stopped.
-54 available-selector layouts and72 final completed-selector/HUD variants passed at100% and130% text without overflow.
- Three NUnit weather tests passed through Unity's Editor test runner and their assertions were re-executed against the final compiled rules. A five-state Play Mode cycle confirmed snow amounts0.22/0.62/0.96/0.14, no rain, storm haze, four visible freeze patches and local grip multiplier0.68 during freeze. Cleared lane depth remained0 and progress receipts stayed unchanged.
- Returning through the real DLC LeaveToMain flow loaded OpenWorldRecovery. The shared wallet stayed8,450CR, original garage access and three available classic cards/24 classic contracts passed. Winter physics, regional props and regional service were absent there.

Each worksite/depot scenario started from a validation positioning fixture. Driving between work phases, depot-to-depot routes and the two recovery positions used vehicle controls. The749m northern detour was verified as a road-network route, not driven end to end in this turn. Every vehicle/equipment combination, physical gamepads and Switch hardware remain untested.

## Corrections found during validation

The forest cut initially intersected standing trees. Its scene vegetation and colliders were moved together to make a real corridor. The two-position recovery needed maneuvering room, resistance aligned to its first working direction, and an axle-based release test rather than body-center displacement. All three recovery variants passed after these corrections.

The existing scene list had OpenWorldRecovery disabled, causing the DLC return route to fail. Its enabled flag was restored; scene order and GUIDs were retained, and the actual return flow then passed. No scene content or classic balance was changed. Background-running changes used during validation were reverted.

Editor compilation passed. Console is **not clean**: Unity Localization/Addressables errors occur during scene initialization, including the previously known `SelectedLocale is null`. The log also retains the resolved missing-scene error and two errors from validation-only attempts to serialize an already persistent settings singleton. None of these logs were cleared. The scene-list correction on disk is one verified enabled flag.

An initial CLI connection dropped during assembly reload; the Editor returned ready and testing continued after checking its state. CLI1.0.0 was used. The updater advertised1.0.1 but its network check failed; no tools or packages were installed.

## Evidence

[Checks.json](Checks.json) contains measured results and limitations. [Board.png](Board.png) shows the selector, [Service.png](Service.png) the restored station and forecast HUD. Weather captures: [flurries](Weather-0.png), [blowing snow](Weather-2.png), [freeze](Weather-3.png). Transient scripts, file fixtures and pre-change source copies remain in `/private/tmp/tow-winter-region`.

Guidance used: unity-feature-implementation foundations for architecture, lifecycle, testing, serialization and performance; gameplay, UI, save-system and VFX specializations; unity-build-validation safety/evidence guidance; Unity CLI integration and state inspection. Scene-list API reference: [Unity6000.3 EditorBuildSettings.globalScenes](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/editorbuildsettings/globalscenes).
