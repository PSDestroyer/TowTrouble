# Switch colour and expanded soundtrack — 2026-11-10

The user reports satisfactory stable FPS after the Switch scene-rendering pass but some lost colour. They requested a sound-design review and improvements on all platforms, and clarified that they like the existing music and want more tracks. This change preserves both original music files and the Switch rendering budget. No player builds were made.

## Colour

RecoverySwitchColorGrade adjusts the existing LDR ColorAdjustments LUT only on Switch: saturation+8, contrast+4 and exposure−0.06EV in daylight; night uses+4/+1/−0.02EV, with the saturation addition reduced by up to20% in wet weather. The correction restores the values and override states it borrowed. RecoveryAtmospherePresentation releases it before applying the next base grading update, so it composes with the optional automatic day/night mode and cannot accumulate. PC retains the user's manual Volume values. There are no additional renderer features or fullscreen passes, and HDR/bloom/Better Fog stay disabled on Switch.

The inspected scene's base grading is saturation23, contrast15, exposure0.2. Static Switch previews read back31/19/0.14; PC remains23/15/0.2. Real gameplay exposure starts from the existing platform/session setting, so the correction is an offset rather than a replacement preset. Garage and lake before/after images are included. Clouds move with the Editor's COZY update and are not pixel-identical reference objects. Resolution0.8, FSR, shadows18m, camera600m, terrain/foliage/rock budgets and instancing remain unchanged.

## Sound-design findings and improvements

| Area | Observed state | Result |
|---|---|---|
| Listening and routing | One enabled main listener in Edit Mode; three disabled legacy vehicle sources. Runtime director owns28 sources through the six-group first-party mixer. | Retained routing, pooled one-shots and source budget. No new mixer effects, buses or DSP-buffer changes. |
| Music variety | Two tracks, about6m54s; the menu theme also appears in exploration. | Six tracks, about19m01s. Existing two files are byte-identical. A local shuffle bag retains Sunset Plains as the familiar first exploration track, cycles through available tracks and avoids immediate repeats, including the theme just played in the menu. Null/duplicate entries have safe handling. |
| Music transitions | Linear3s crossfade and fixed quiet gaps. |4s equal-power crossfade on the same two decks,200ms DSP scheduling lead, gentle ending fade,3s menu /16s exploration gaps. Rapid context changes finish the current transition before reusing a deck. Music selection uses System.Random independently of gameplay RNG. |
| Mix balance | Music source gain0.30; recorded engine layers substantially louder. | Music0.36 and engine-layer multiplier0.85; user master/effects/music values are preserved. Both values and transition timing are editable in RecoveryAudioBank. |
| Recovery feedback | Music ducks under high load, but an open panel could override the stronger ducking. | Critical load/stall ducking also applies with a panel open; faster reduction and slower return leave mechanical cues readable. Existing winch/attach/snap/creak/impact cues and their routing remain. |
| Ambient beds | Raw mono-compatible RMS about0.0035 wind,0.0030 mountain wind,0.013 birds,0.0066 insects and0.0026 water, before the already low source volumes. | First-party library copies calibrated to0.060/0.060/0.040/0.050/0.0262 RMS respectively, with peak ceilings and180ms seam overlaps. Water hits its peak ceiling before reaching the nominal0.065 RMS target. No clipping was added; vendor originals are unchanged. Existing distance, biome, day/night and weather envelopes remain. |
| Pause/focus | Separate notifications could unpause a source despite the other suspended state. | Combined focus-and-pause gate; a focus notification cannot resume a paused app. Music gaps/fades stop advancing while suspended, category mute remains immediate, and state transitions use unscaled time. |
| Sound character | Shared engine recordings, body pitch/load variation, granular foley adapted for six surfaces; mapped proximity for shore and waterfall audio. | Preserved. This pass improves mix/score rather than claiming new engine recordings, dedicated cascade recordings or a complete tire-audio redesign. |

Rain already routes through the Ambience group and the player's ambience setting, with a0.24 maximum FX volume. It is retained. UI uses semantic cues, cooldowns and same-frame priority; no extra clicks or new sustained alarms were added. Audio remains local presentation and does not change physics, networking, saves, rewards or gameplay RNG.

## Added music

| Track | Author | Duration |
|---|---|---:|
| Wooden Inn | Indieteur |3:25|
| One Step at a Time | Alex McCulloch / Pro Sensory |2:35|
| First Light Particles | Yoiyami |2:12|
| Deep Blue Ambient Piano | Yoiyami |3:55|

All four are CC0 source recordings. Public source pages, exact downloads, edits and SHA256 hashes are recorded in Docs/Audio/Sources.json, with source-page/download evidence in SourceDownloads.json. In-game third-party notices credit the new tracks. Source stereo/timing is retained, with DC removal, restrained edge fades and RMS calibration toward the existing tracks, constrained by a peak ceiling. This is signal-level matching, not a perceptual LUFS certification. The original Revelation and Sunset Plains files were not remastered.

All six imported score clips read back as stereo44100Hz, Vorbis quality0.65, Streaming, no preload, background loading, with explicit Switch overrides. Only the two active music decks stream during playback; the playlist does not create a source per track. The refresh authoring menu updates the four new importers and the owned bank. The full-library authoring utility also retains the expanded playlist, and now saves only its bank instead of Save All.

## Validation and limits

- Unity6000.3.19f1 recompilation: zero errors/warnings.
-30 direct Editor NUnit invocations passed:13 existing audio cases,7 new music/colour cases and10 graphics regressions. Covers complete shuffle bags/no repeats, duplicate/null fallback, gameplay RNG preservation, crossfade power, critical ducking, colour ownership/override restoration, fixed Switch profile and PC preservation. No Test Runner or Play Mode.
- Temporary native rig check passed: original menu theme, familiar exploration track, independent music mute, combined application pause/focus gate,28 sources, two music sources, all routed. It uses a synthetic inactive FishNet phase fixture and invokes presentation manually. This verifies setup, scheduling paths and control state; it is not a sustained listening/output-wave capture or full live-game test.
- Four post-change native scene renders (PC/Switch ×garage/lake) were visually inspected alongside the four before references. Rendering reported no new errors, original material JSON/references restored, source scene and dirty flag preserved.
- All65 source/derived library clips decode successfully and match their65 provenance hashes. Native import settings were reread after reimport.18 protected files, including PC/Switch pipelines/renderers, mixer, AudioManager, weather materials, source scene and the two existing music files, retain their entry hashes.
- Final hardware colour/FPS, speakers/headphones balance, long-session audio CPU/memory, and Switch suspend/resume remain user/device validation. No player build, commit, push, new dependency, networking startup or career write was performed. Stable FPS after the colour/audio revision is not claimed from desktop Editor measurements.
