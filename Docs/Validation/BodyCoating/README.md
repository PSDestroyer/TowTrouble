# Vehicle body coating — 2026-11-20

Implemented the DLC snow treatment as a shared vehicle body shader in classic TowTrouble and Winter Expeditions. Actual wheel contacts independently accumulate snow, mud or sand. Deposition and surface changes use time-based exponential mixing, followed by display smoothing. Old deposits linger instead of disappearing at a biome boundary. Rolling on dry hard ground/roads gradually removes deposits: mud lasts longest, then snow, then sand. A parked vehicle does not clean itself; wheelspin can deposit material without forward movement.

Coating is restricted to lower body panels and wheel arches. Paint atlases, material properties and emissive lamps are retained; transparent/cutout surfaces and tire materials are excluded. Stock and fitted assemblies have lifecycle binding. Runtime material copies preserve source assets. Per-material property blocks preserve unrelated values. Repainting/refitting retains the accumulated state on the vehicle; the visual state is session-local, not written to career saves. Vehicle handling, mission balance and world assets are unchanged.

## Unity verification

Unity 6000.3.19f1, macOS Editor, URP 17.3. No player build or Switch/device/multipeer validation.

- Six targeted EditMode tests passed: accumulation, mixed transitions, progressive cleaning, parked/air/pause behavior, sample-rate independence, bounded mixtures and classic/DLC contact classification. `RulesTests.json`.
- All 23 existing RecoveryPaintTests passed, covering starter finishes, paint migration, palette/atlas coverage and purchase/refit behavior. `PaintTests.json`.
- Shared shader supported, no ShaderUtil messages, C# compilation finished. `ShaderCheck.json`.
- **Classic Play Mode:** same starter vehicle drove with real throttle/reverse/physics in the existing R03 mud, desert sand and R13 snow areas. Contact labels and per-wheel displayed values are captured every two simulation seconds. Placement at each area's start is a test fixture; these are local terrain probes, not one continuous drive across the whole map. No terrain or coating values were manufactured.
- Mud: 38 simulation seconds / 95.7 m; mean body mud reached 0.810. Sand: 44 s / 198.0 m; mean sand reached 0.823 while previous mud remained at 0.143. Snow: 42 s / 76.4 m; mean snow reached 0.615 with residual mud/sand. Snow drive included uneven-ground/air contacts, so wheel values differ naturally.
- Dry return: 95 s / 254.8 m on the garage/workshop approach and adjacent hard terrain. Road meshes and the native combined terrain supplied contacts; this was not an all-wheels-on-road lane-following test. Mean snow fell 0.618 → 0.121, mud 0.048 → 0.019, sand 0.260 → 0.029, without resetting the coating. `RoadDrive.json`.
- Twenty runtime lifecycle configurations passed: four stock models × four paints, plus one fitted configuration per model. Stored amounts survived all remounts. An unrelated property-block value survived a coating update. `Lifecycle.json`.
- **DLC smoke:** entered Winter Expeditions through its existing menu action, started an isolated host, and drove 30 s / 64.4 m on existing winter terrain. Snow mean reached 0.662; mud and sand stayed zero. `DlcDrive.json`.
- Native Game View captures inspected: `Mud.png`, `Sand.png`, `Snow.png`, `Cleaned.png`, `Dlc.png`. These use the normal gameplay camera, not replacement preview materials.
- Real save and backup SHA-256 hashes unchanged after Play; isolated scope released, original OpenWorldRecovery scene and background/FPS/time-scale settings restored. `Restoration.json`.

Console retains pre-existing Unity Localization / Addressables startup failures (`SelectedLocale is null`, missing table/locale entries). Two validation CLI mistakes also logged errors: an out-of-root capture path and a misspelled command group; both were corrected. No coating exceptions or shader errors observed. This is not an error-free Console claim. `ConsoleSummary.json` records errors since the baseline cursor; Console was not cleared.

A preparatory test call containing `Profile.Save()` was rejected by automatic approval review as a potential real-save write. That call never ran; the DLC test proceeded without it. Career-save integrity was verified afterward.

Driving harness source is preserved as `.cs.txt`; it requires the explicit isolated validation profile. It positions the car only at each probe's start and drives it through the normal validation input seam. It is not part of shipped gameplay.
