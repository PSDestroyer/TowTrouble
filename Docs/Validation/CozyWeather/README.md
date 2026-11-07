# COZY weather, day/night and headlights — 2026-11-07

Implemented in OpenWorldRecovery with COZY 3.6.2, Unity 6000.3.19f1 / URP 17.3.

## Behavior

- Session starts at 09:36. A full day takes 32 real minutes of scaled gameplay time. Pause stops the clock; title/main showcase stays at the starting morning. A new hosted session starts a new clock.
- Four cloned weather profiles: Mostly Clear, Partly Cloudy, Overcast, Light Precipitation. A deterministic host-seeded forecast selects five-minute segments with 30-second blends; adjacent segments may select the same weather. No gameplay UnityEngine.Random state is consumed.
- Local mapped snow / northern high-altitude region blends to snow over seven seconds, replacing rain. Light mountain flurries remain even in clear weather. One overhead probe per quarter second suppresses precipitation under collidable canopies. This is visual/audio weather; grip, extraction difficulty, terrain coating and economy have not been retuned.
- Automatic warm headlights fade through dusk/dawn and require the vehicle engine to run. Two spotlights per vehicle, range36m, no added headlight shadow maps. Existing headlamp emission responds as well. Measured bonnet metadata follows fitted parts; bonnet kits without visible lamps get two functional recovery lights.
- Rain uses the existing Ambience mixer and saved ambience volume. Birds taper at night/rain; insects increase at night. Runtime moon profiles are cloned so scene references do not dirty source satellite profiles.
- Host elapsed environment time and seed ride the existing buffered 15Hz world snapshot. Clients derive the same global sky; regional precipitation depends on each local car location. Peers must use matching code. No COZY save/load module, PlayerPrefs or parallel save system was added.

## Authored setup

`Assets/TowTrouble/Weather` contains first-party profile/material/particle copies. The scene root is `Tow Trouble - COZY Weather`; the previous static sun/sky renderers are disabled. Required `FX Block Zone` tag is registered through native Unity APIs. Native scene authoring is in `CozyEnvironmentAuthoring`, menu Tow Trouble / Weather / Integrate COZY and Night Headlights (one-time integration, refuses duplicates).

COZY's native LateUpdate writes ambient/fog after the early weather adapter. `RecoveryCozyVisibility` runs at order180 to apply the game's readable night floor afterward. Rain/snow particle budgets are capped and particle collisions disabled; mobile-style COZY clouds selected. These limits are configuration choices, not a measured console GPU budget.

## Validation evidence

- Tests.json: 15 NUnit test methods directly invoked in Editor, all passed (4 weather/headlight +11 effects/debris/steering regressions). This was not a Unity Test Runner batch.
- Gameplay.json: actual isolated Solo Explore; noon lamps off, night lamps on, dusk proportional intensity, real mountain snow and warm-region rain particles, exact forecast midpoint, pause/resume clock. Same-frame main-camera headlights enabled/disabled comparison changed43,576 pixels. Images are actual camera renders; UI hidden for comparison.
- AllHeadlights.json: 74 actual garage Preview/Commit/Close configurations passed: four starters,37 body builds,33 compatible bonnet builds. Exactly two enabled lights, forward direction and plausible mounting positions checked after each fit.17 unreachable bonnet entries are explicitly skipped by existing garage compatibility rules; see JSON for exact count/list. This is structural and state validation across variants, not a separate visual review of every beam.
- ConsoleSummary.json: final current Console0 errors, compilationFailed=false; existing Account API timeout warning remains.
- Cleanup.json: Play stopped, scene clean, real save and backup artifact hashes unchanged, validation scope restored. Test ownership and item purchases existed only in an isolated memory profile.

During implementation the missing COZY block-zone tag caused repeated native errors; the tag and authoring guard fixed it before the final runs. The final sweep resumed after18 passing cases when its initial harness tried reopening the garage before a large body settled; the corrected harness waits for the production CanOpenGarage gate. A prior dusk assertion sampled a stale rendered frame; the final harness waits for the expected environment day before asserting. These were retained as investigation evidence in the temporary validation workspace.

Not validated: a real second network peer/late join, Nintendo hardware/build, hardware controller behavior, GPU profiling, long-session float precision or final artistic night/weather tuning. No build or commit was made.
