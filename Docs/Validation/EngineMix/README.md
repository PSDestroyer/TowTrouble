# Engine mix — 2026-11-11

EngineBoostDb=6 (amplitude multiplier1.995262), applied only to engine loops. Existing EngineGain=.85, all bus gains, music, winch and one-shots unchanged. EngineGain remains tunable separately. New boost is Inspector-editable on RecoveryAudioBank.

Idle has steady mass-dependent pitch and fades under throttle. Drive follows throttle with a smaller coasting contribution. Load uses existing Engine Drive recording at lower pitch (no new recording), with SmoothStep effort envelope .2–.85 and throttle gating; effort already includes tow reaction/mud/slip. Removes inappropriate winch motor sample from engine load. Existing three sources per car reused, no new audio assets/dependencies. Volume ramp1.2 per second; existing pitch ramp unchanged. Authoring Configure preserves distinct engine/winch assignments.

Validation: Unity compile0errors/0warnings;14 directly invoked audio NUnit cases passed; isolated native source confirms Vehicles routing and effects/app-pause mute; asset confirms EngineLoad=Engine Drive, WinchMotor=Motor loop. Default6dB sum-of-engine-gains budget remains below1 at full effects/EngineGain=.85 (not a full-mix limiter or acoustic clipping guarantee). git diff --check clean.

No Play, scene save, career mutation or build. Not auditioned during live driving or on Switch hardware. User should compare idle, free acceleration, loaded towing and pedal release, including effects mute. Balance is a first tuning pass.
