# Fleet engine audio — 2026-11-11

Model0 Scout: approved Muscle clips/gain .68 unchanged. Model1 Hauler: Bus Idle/Low/High. Model2 Buddy: Generic Idle/Low/Med. Model3 Ranger: Buggy Idle/Low/Med. Runtime High field refers to the upper blend layer, not necessarily the source High recording. Medium clips intentionally avoid the aggressive maximum rev sound for Buddy/Ranger. Fitted bodies retain their base model's engine character.

Each new profile uses independent gain .68, no legacy +6dB boost. Shared smoothed throttle/speed/effort, restrained pitch, maximum16% upper layer and18% effort increase preserve the approved Scout mix behavior. Source-switch cache tracks model IDs (not just customized/noncustomized) so garage changes replace all3 clips. All profiles reuse existing3 sources per driver; no extra voices, allocations in Update, or networking changes. Startup remains the existing shared one-shot.

prepare.py reads user-imported originals and writes only9 derived clips with presence EQ and18ms loop seam. Original hashes verified in signal-audit.json. No Scout audio processing repeated. Import mono ADPCM22.05kHz, preloaded; explicit Switch override. Native imported new clips total962048 bytes decoded PCM, excluding Unity overhead. Hauler source crest factor makes its RMS lower than target when peak headroom is limiting; this conservative mix requires listening rather than automatic boost.

Authoring: Tow Trouble/Audio/Configure fleet engines (Edit Mode). Profile references saved in existing RecoveryAudioBank. Existing customized profile gain is preserved on reconfiguration. Missing/incomplete profile safely falls back to the legacy bank.

Validation: compilation0 errors/0 warnings;20 NUnit audio cases invoked directly in Editor passed;60 isolated native rig assertions cover all model changes on both driver slots, routing/mute, unchanged source count, missing-profile fallback and application pause. No Play Mode, builds, active scene save or profile writes. Existing dirty scene preserved. User must audition subjective character/relative level and actual Switch speakers. No claim of audible/hardware validation.
