# Natural daylight palette and grass scuffs — 2026-11-07

Follow-up to the user's screenshot showing fluorescent foreground grass, pale yellow roads and uniform dark green tracks. Target: a warm, colorful landscape with the vehicle remaining the focal point.

## Changes

- Existing atmosphere coordinator: daytime saturation8→3, contrast5→7, bloom.12→.08, sun.95→.84 and fill.25→.21 with a more neutral daylight tint. User brightness/exposure ownership remains unchanged. Night values remain5/2/.16/.95/.18.
- Existing global Volume gains a Hue-vs-Saturation curve: yellow-green/green approximately14–16% less saturation, red and blue neutral. This uses URP's existing color-grading LUT. Terrain grass remap(.64,.72,.51)→(.62,.64,.53); soil(.86,.76,.58)→(.80,.73,.60); road albedo(.94,.90,.80)→(.82,.81,.77). Only first-party layers/materials edited; paint, terrain height, physics/grip and snow/sand albedos unchanged.
- Clear daylight haze density.0008→.00105, starting110m instead of70m; night blends back to the previous.0008/70 values. Existing rain density and precipitation behavior retained.
- Grass track material now uses the soil texture with a brown tint and lower alpha(.88→.46), shallow.004m relief shading and opt-in world-anchored noise on edges/opacity. Mud/sand/snow/hard/wood retain breakup0 and their previous shader response. No extra textures, runtime allocations, meshes, colliders or networking added. Both track-authoring commands preserve the new grass settings; main visual authoring contains repeatable palette calibration.

## Validation

- Before.json / After.json: four actual isolated Solo states (09:36, noon, rain, night), fixed panorama and production-camera renders at1200×720 after cold restart. Same time/forecast settings; animated clouds, car settling and wind prevent exact pixel registration. Frames advanced1689→1942 before,766→1012 after. Captures visually inspected; the sky stays blue and night road/vehicle remain readable.
- BeforeTracks / AfterTracks: actual driving43.1m /43.6m with real wheel contacts; grass chunks produced. Final track capture uses an overhead pose because the original gameplay-camera capture was partially obscured by a tree. It is evidence of final scuff appearance, not a registered A/B image.
- Snow.json: real R13 snow.4, rain0, active wheel shader has no compiler errors; breakup.8 on grass and0 on the other five materials. Snow frame inspected.
- Tests.json: eight existing NUnit methods directly invoked in Editor, all passed (surface blending/grip invariants, teleport boundaries, stationary allocation gate, bridges/wood, holes and loose-ground relief). No new tests mirroring visual constants were added.
- Editor compiled without errors. ToolingErrors.json preserves Pipeline transport/disconnected-response errors observed while a second Unity project was starting and stale URP Inspector serialized-object errors during profile reload. Requests explicitly targeted TowTrouble; Inspector rebound to the saved VolumeProfile. After preserving evidence, Console was reset. A slow native scene save completed after the CLI response timed out and added two transport errors, preserved in ConsoleBeforeFinalReset. Final status/clear used native Unity MCP after verifying the TowTrouble project; ConsoleSummary reports0 current errors/warnings. No gameplay, Render Graph or shader exception was found in those captured errors.
- Cleanup confirms Play stopped, scope restored and real save/backup hashes unchanged. Remaining post-Play dirty flags were inspected (COZY sun/moon/particles and temporary TMP meshes); scene saved natively afterward. Final saved scene/progress state is in Cleanup.json.

No build, commit, actual second-peer or Nintendo performance test. Existing work/imports were preserved. Global foliage grading can also soften green paint slightly; red/blue hue regions remain neutral and overall saturation is intentionally a little lower.
