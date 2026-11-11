# Scout Muscle — 2026-11-11

Only ModelIndex 0 (Scout, including its fitted bodies) selects these three clips. Other vehicles retain the original audio bank; missing Scout clips fall back to that bank. Existing three sources per driver and Vehicles mixer bus are reused. Existing startup one-shot is unchanged; Muscle_Off was not assumed to be a startup or deceleration loop.

Source: user-imported Assets/TowTrouble/Audio/Engine/Engine_Muscle_{Idle,Low,High}.wav. Originals are untouched. User confirmed the sounds came from their project and authorized reuse. No new external download.

prepare.py creates derived clips: smooth presence EQ, 18ms circular seam, target RMS .22 and peak <=.68. No saturation, pitch resampling, or runtime DSP filter. Signal evidence in signal-audit.json. Idle pitch stays 1; Low .92–1.12; High .9–1.04. High contributes at most 16% of the driving blend. Throttle/speed demand is smoothed with .4s attack/.65s release, effort .5s. Effort adds up to18% loudness instead of a sporting high-RPM layer. Legacy modulo-speed pitch resets are bypassed for Scout.

Authoring: Tow Trouble/Audio/Configure Scout Muscle, Edit Mode only. Native bank references saved; scene untouched. Mono ADPCM/22050Hz, preloaded, explicit Switch override, identical sound profile on PC/Switch. Decoded PCM estimate272896 bytes for all three clips (not total Unity audio memory).

Validation: Unity compilation0 errors/0 warnings.18 NUnit cases invoked directly in Edit Mode passed (existing15+new3), plus9 isolated native rig checks for Scout selection, Hauler/Buddy fallback, restored legacy clips, bus routing, effects mute, application pause and unchanged source count. No Play Mode, build, scene save or player-save writes. The pre-existing dirty active scene was preserved. No listening/device validation claimed; audition stationary idle, gentle cruise, acceleration and loaded towing in Editor, then on Switch speakers.

## Comfort balance after player feedback
ScoutEngineGain=.68 now replaces EngineGain(.85)*EngineBoost(+6dB) for Scout only: -7.94dB relative to the approved timbre version. This leaves pitch, source WAVs, transitions and midrange unchanged. Legacy engines retain their existing gain. Open/Close .22→.18; Accept .33→.24; Complete .45→.32; Attach .50→.38. These values are in the saved bank and Configure audio library defaults. Critical Snap/Reject/winch feedback unchanged.

Measured source music RMS: five tracks about-20dB, Wooden Inn-22.4dB; ambience insects-26, birds-28, water-31.6, winds-24.4dB before runtime mix. These are signal measurements, not LUFS/listening validation. Music/ambience levels preserved. Profile defaults remain master.8/effects.8/UI.7/music.45/ambience.7; all old preferences and mutes preserved. Bank balance takes effect for both new and existing profiles.

Validation: compilation0 errors/warnings;19 direct NUnit cases pass, plus native Vehicles routing/effects-mute/app-pause checks. No Play Mode or build. User audition on Editor and Switch remains the subjective balance check.
