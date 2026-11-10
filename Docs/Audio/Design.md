# Tow Trouble audio — integrated 2026-11-07

The intended sound is a calm recovery trip: a warm vehicle close to the player, readable mechanical feedback, an outdoor soundscape and restrained instrumental music. This pass changes presentation and adds two audio preferences; traction, vehicle performance, cable strength, mission difficulty and prices are unchanged.

## Library and provenance

61 curated source/derived clips live under `Assets/TowTrouble/Art/Audio/Library`. `Sources.json` records the author, stable source page, license, edits and SHA-256 of every file. `SignalAudit.json` contains decoded duration, channels, sample rate, peak and RMS measurements, not a perceptual listening review. Kenney license files are retained in `Licenses`. Credits' third-party notices include the audio contributors.

New downloads are CC0: Kenney Interface/Impact/RPG packs; engine recordings by domasx2 and looneybits; machinery by jwiese (CC0 option); mud recordings by rubberduck; chain winch textures by bart; music by Yoiyami and Indieteur. Existing licensed Suntail files provide wind, birds, insects, water and dirt textures. Source vendor files are unchanged. Dirt-derived sand textures, low-pass idle and the layered cable recoil are labelled as edits. These are shared recordings with pitch/layer variation, not unique engine recordings for every vehicle.

Music selections:
- **Revelation — Indieteur:** menu theme, also part of the exploration rotation.
- **Sunset Plains — Yoiyami:** longer calm guitar/ambient exploration track.

Music files have matched approximate RMS and retain their original timing. This is not a measured LUFS/mastering or listening certification. Source WAV files are about 111.5 MiB in version control; they are not all decoded into game memory.

## Runtime presentation

`DemoSession` creates one `RecoveryAudioDirector` from the Resources `RecoveryAudioBank`. The bank is the editable catalogue of clips, gains, cooldowns, priorities, surfaces, mixer references and waterfall positions. No new per-tree or per-decoration emitters are needed.

| Layer | Behavior |
| --- | --- |
| Vehicle | Recorded idle, drive and mechanical-load layers; bounded pitch follows speed, throttle, wheel slip and body mass. Only active, running drivers sound. A stationary disabled recovery target has no engine. |
| Tires | Contact-based granular dirt, grass, wooden bridge, mud, snow and sand textures. Speed/slip controls activity and level; grounded contact is cached at 10 Hz per car. Includes the towed target. |
| Cable | Distinct attach, detach, recoil snap, load creaks and extraction release. High load ducks the music instead of adding a persistent alarm. |
| Winch | Recorded motor with start/stop transients, slower pitch under load, a different stalled rate and smooth loop fades. The prototype oscillator is bypassed. |
| Collisions/obstacles | Meaningful driver impacts use authoritative impact telemetry. Obstacle-clear events add a brief material impact. Ordinary contact does not spam collisions. |
| Interface | Separate navigation, confirmation, back/open/close, rejection, part fit/purchase, contract acceptance and completion sounds. Stronger same-frame UI feedback replaces the generic click; cooldowns limit repeated navigation. |
| Co-op | Both drivers' cosmetic attach/detach/snap/impact counters are read locally. Remote signals are spatial; a serial counter permits repeating the same signal. Initial replicated counters are seeded and historical events are suppressed. No sound changes server physics or economy. |
| Environment | Wind, birds and insects follow the local position; altitude changes wind; the nearest mapped biome changes vegetation ambience; nearby lake/beach sites add shore sound. Three actual cascade renderer bounds anchor a nearby waterfall source. Occasional workshop foley is localized at the garage. |
| Music | Two sources crossfade over 3 s between menu and exploration; tracks rotate with 22 s of space between exploration songs. Load and overlays duck music. No repeated completion cue on result-page refresh. |

The single local listener follows the local driver's chassis, with camera orientation. Camera zoom/orbit distance therefore does not change proximity to the player's engine. Doppler is disabled on the new sources. The previous camera listener is disabled for the rig lifetime and restored on teardown.

The rig has 28 sources total, including a fixed 12-voice one-shot pool. Critical events can replace lower-priority pool voices. Audio is presentation only: private clip variation uses frame/slot indexing rather than gameplay random state. Focus loss pauses playback; pause fades mechanical loops; UI remains available at timeScale0. Muting a category responds immediately.

The native `TowTrouble.mixer` has Master → Vehicles, Recovery, UI, Ambience, Music. Group levels are left at unity; settings and contextual fades are applied once at the managed sources, with Master on AudioListener. There are no hidden reflection-created groups or hand-authored mixer files. Scene legacy engine components are disabled, do not play on awake, and route to Vehicles; runtime presentation owns the replacement sources.

## Settings and portability

Settings/Audio now has Master, Vehicle and recovery effects, Music, Environment and Interface. Music defaults 45%, Environment 70%; previous profiles get additive v4 defaults while preserving saved mute/volume choices. Comfort preference copying, training restoration, reset-progress preservation and Restore Tab Defaults include the new fields. All persistence uses the existing SaveManager.

Short clips are mono 22.05 kHz ADPCM/DecompressOnLoad. Ambient is mono 32 kHz Vorbis/Streaming, with background loading and no preload. Music is stereo 44.1 kHz Vorbis/Streaming, also without preload. Explicit Switch overrides match this policy. Runtime native audio is used; no extra audio middleware/package is required. Both network peers need matching code for signal serial SyncVars.

Regeneration: `Tow Trouble/Audio/Configure audio library` sets owned importers and rebuilds the bank, overwriting manual bank edits. `Tow Trouble/Audio/Route scene audio sources` applies the legacy-source routing to a clean open recovery scene through Editor APIs. Mixer structure is authored in Unity's Audio Mixer window.

## Practical limits

This is the complete functional first sound pass, not final acoustic mastering. Surface textures include adapted granular foley; body differentiation currently uses shared engine recordings with mass/pitch/layer changes. Shore/biome ambience uses authored recovery-site proximity rather than a continuously sampled water shoreline. Dedicated engine-off smoke, every individual prop material and incidental casualty-only collision sounds are not separately authored.

The Mac Editor verifies functionality and rough signal/memory behavior. Final listening on speakers/headphones, a real two-peer co-op session and Switch hardware performance remain necessary. No player build, Switch SDK build or devkit measurement was made.

## Soundtrack and mix revision — 2026-11-10

The player likes the existing musical style. Revelation and Sunset Plains remain byte-identical; Wooden Inn (Indieteur), One Step at a Time (Alex McCulloch), First Light Particles and Deep Blue Ambient Piano (Yoiyami) add roughly12m06s, taking the six-track exploration catalogue to19m01s. All four additions are CC0 and are credited in the in-game notices and Sources.json. The current library has65 clips; the original61-clip figures above describe the initial pass.

Music now uses a local shuffle bag with a familiar first exploration track, no immediate repeat,4s equal-power transitions, a200ms DSP scheduling lead and3s/16s menu/exploration gaps. The bank exposes MusicGain.36, EngineGain.85 and transition timing; player volume preferences are untouched. Critical cable-load ducking also works over an open panel and recovers slowly. Application suspension combines focus and pause state. There are still two music sources and28 director sources.

Five first-party ambience copies received DC/seam and level calibration because their source levels were very low before runtime attenuation. Original licensed vendor files and the mixer remain unchanged. SignalAudit.json reflects the current65 files. Dedicated vehicle recordings, a complete tire-loop replacement and dedicated cascade recordings remain future sound work. See Docs/Validation/SwitchColourAndSoundtrack for measured levels, native import checks,30 passing direct tests, scoped rig checks and visual comparisons. Full native listening and device profiling remain unverified; no build was made.
