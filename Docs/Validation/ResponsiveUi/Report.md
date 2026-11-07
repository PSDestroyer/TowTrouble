# Responsive UI integration — 2026-11-07

The existing menu compositions, three-column garage catalogue, three available recoveries and continuous Completed list are retained. Runtime-generated UI now shares its fitted content geometry with the preview cameras.

## Implemented

- `RecoveryScreenLayout` fits the reference 1600×900 composition inside normalized safe area. `RecoveryHudScreen` applies this on page creation and resolution/safe-area changes, without rebuilding the menu or discarding its selection/scroll state.
- Fixed gameplay widgets occupy a horizontally centered, maximum-reference-width HUD area. Mission information, cable controls, crew status and speed now use consistent corners. Speed remains at the bottom on 16:10. World-projected labels remain in the full gameplay canvas; waypoint/hitch/caption/tag bounds honor safe area.
- Modal dimmers and their raycast blockers cover the entire canvas, including ultrawide side regions. Header backgrounds extend across the screen while the content retains its approved composition. Regular garage preview space remains free of a full-screen raycast blocker, preserving mouse rotation.
- Garage camera distance fits all eight focused-geometry corners into the actual free preview rectangle, including perspective depth. Camera aim/position share that rectangle and final position retains the outside-body/environment checks. Starter and main-menu camera framing account for the fitted content/safe area.
- Mission-status width and vertical padding accommodate the keyboard Tab glyph at 100% text.
- Display choices favor 1280×720 or larger modes. Desktop windowed players request that minimum after an undersized resize if the display can accommodate it. This is excluded from Editor and console builds; unusually small hardware retains a current-resolution fallback. Accessibility advice suggests 120–130% text for small displays. Preferences and save schema are unchanged.

## Native Editor validation

- **1,092 presentation cases:** 42 menu/HUD states × 13 sizes × 100/130% text. No enabled TMP overflow or builder exceptions. Modal bounds cover every tested render target and remain raycast-enabled.
- Sizes: 854×480, 1024×576, 1280×720, 1920×1080, 2560×1440, 3840×2160, 1280×800, 1920×1200, 1024×768, 2560×1080, 3440×1440, 3840×1080, 1080×1920.
- **120 additional resize/safe-area cases:** ten states, three sizes, two text scales, symmetric 5% TV margins and asymmetric simulated cutout margins. UI is built at the reference size and then resized in place. No text overflow; garage/Completed scroll position retained; every modal background still covers the full target. Includes a long body-compatibility review with scrolling.
- **27 regression cases passed:** new safe-area/aspect/geometry/display-mode assertions plus existing garage camera/body-replacement and menu-setting tests, invoked directly as NUnit assertion methods in the running Editor. Includes real catalogue geometry and smooth camera transitions. This is not a new Unity Test Runner/Play Mode run.
- Actual vehicle/UI preview renders visually inspected at 1280×720, 1280×800 and 3440×1440. Other inspected renders cover HUD positioning, full-screen modal coverage and simulated TV safe area.
- Unity compilation completed with zero reported errors/warnings; `git diff --check` passed. Temporary preview scenes/devices were cleaned up. Native fixtures confirmed real save bytes unchanged and active scene clean; no contracts, payments, purchases or network sessions were triggered.

`Checks.json` records compact final results; `Tests.json` records assertion outcomes. Representative original-size PNGs accompany this report. The native previews use the production builders and actual font/glyph assets, with the installed CanvasScaler Expand rule reproduced in an isolated render target. The vehicle previews use an isolated real prefab and flat preview lighting, not the world scene.

## Limits

Below-minimum and portrait render targets are best-effort containment checks, not a claim that all text is readable there. No compact/reflow layout was introduced. The minimum-window enforcement branch needs a standalone-player check; Editor Game View intentionally remains resizable below the player minimum. Actual handheld readability, TV overscan, console SDK/device behavior, live multiplayer labels and physical-controller interaction were not newly tested. No player build or commit was made.
