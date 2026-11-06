# Audio integration validation — 2026-11-07

Functional Editor validation passed. This is a first complete audio presentation pass, not a release/hardware or perceptual mastering certification.

## Evidence

- Unity6000.3.19f1 / macOS, connected Editor; no player builds.
- **317/317 actual Unity EditMode tests passed**, including13 audio cases covering additive settings/mute preservation, preference copying/serialization, motor states, engine pitch bounds, counter wrap/order, music ducking and invalid gains. `Tests.json` is the native Test Runner result.
- **29 live checks across737 actual frames** in `PlayProbe.json`: menu and exploration playback, isolated Solo startup, one listener, every new source routed, absent crew silence, actual physical attach/detach/reattach and shock-induced cable rupture, plus emitted event clips. Winch stall/rate/ducking use an explicitly frozen telemetry fixture after the physical test. Pause, independent effects/music/environment mute, focus loss/restore and five sliders at130% passed.
- **12 further checks across247 actual frames** in `ReplicatedStateProbe.json`: initial/new-run counter seeding, second-driver spatial feedback, repeating the same crew signal, release/result transitions without duplicate completion cue, track ending, a quiet gap and starting the next track. These are injected replicated-state/phase fixtures in one Editor, not a two-peer network test or a physically completed recovery. Track ending was exercised by seeking near the end and then allowing native playback to finish.
- `Settings-130.png` was visually inspected: all five controls fit and remain readable. Native TMP overflow checks passed at130%.
- Native AudioListener output samples were nonzero during gameplay; captured snapshots were around0.025–0.029 RMS. These spot samples are not a sustained peak/clipping, loudness or listening assessment.
- `NativeImports.json`: six mixer groups including Master;21 sound events; six surface sets; three actual cascade bounds;61 owned imported clips. Music and ambient read back as Streaming, no preload, background load; short clips are mono ADPCM/DecompressOnLoad. Explicit Switch overrides are present.
- `Memory.json`:28 managed AudioSources, one enabled listener,54 referenced clip objects and1,964,045 bytes reported for those clip objects in this Editor sample; seven are streaming clips. This excludes total process/mixer/decoder/stream-buffer peak memory and is not a Switch performance result.
- All61 `Sources.json` file hashes were checked against actual audio bytes and all files decoded successfully. Source files total111.53MiB;50 are CC0 source/derived assets and11 use the existing licensed Suntail source material. The native bank references54 clips; a few source variants remain available for editing outside Resources.

## Problems found and corrected

The initial Play probe caught an offline-menu access to network event telemetry before the NetworkBehaviour was ready. Reads are now guarded by connected/telemetry-ready state. The subsequent successful runs had no new NullReferenceException/MissingReferenceException in the captured Editor log interval.

A second probe caught environment mute fading down slowly instead of respecting zero immediately. Category mute now stops managed loops immediately; ordinary state changes retain their fades. The complete repeated probe passed after this correction.

The scene-route utility also flushes pending Undo records before saving, preventing deferred Undo bookkeeping from marking a just-saved scene dirty. A native save dialog during the final Test Runner launch was resolved by saving the already reviewed source-routing overrides. The scene diff contains only AudioSource overrides for the three existing vehicle instances: disabled legacy playback, replacement clip, zero Doppler and mixer assignment.

## Cleanup and scope

`Cleanup.json` confirms the real `save.json` was byte-identical, Play Mode stopped, scene clean, validation namespace cleared and the temporary run-in-background override restored. Tests used the existing SaveManager's isolated memory storage, not the career. Generated font/quality changes were restored to the pre-test bytes. No commits or player builds were created. Source vendor assets were not changed.

The final authoring utility and builder retain the same routing when regenerating first-party content. Runtime audio source ownership and settings do not alter vehicle balance, traction, mission requirements, economy or gameplay random state. Signal serial SyncVars are cosmetic and require matching code on both peers.

## Remaining validation

A listening pass on actual speakers/headphones is needed to judge timbre, repetition and the final mix. The source engine is shared between bodies with pitch/load variation; surface textures include adapted granular foley. A real second peer, Switch SDK build/devkit audio behavior, long-session CPU/voice peaks and platform suspend/resume are not verified. Shore/biome ambience currently uses site proximity, not a continuous shoreline analysis; casualty-only incidental impacts and every individual prop material do not have dedicated events.

Final compile status was clean and the native import audit reported a clean scene. `git diff --check` reports six trailing spaces in Unity-generated empty `value:` fields of the scene's new prefab overrides; these were left in the native serializer's format. No serialized scene/mixer/importer files were edited as text.
