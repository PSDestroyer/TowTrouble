# Engine presence fix — 2026-11-11

User still barely heard engine on PC and Switch after +6dB mix update. Editor was stopped when inspected (no live source measurement available). Saved effects/master both .8; previous +6dB persisted.

Confirmed asset defect: idle and drive loops contained99.98%/99.97% signal energy below200Hz, dominant43Hz. Low idle pitch pushed the dominant tone below40Hz. Original cached source was similarly bass-dominated; simple gain did not supply audible midrange. This is a confirmed spectral deficiency, not proof that all possible runtime/device causes are ruled out.

Reworked existing source offline using band-limited saturation harmonics and midrange/body balance. This is processed existing material, NOT a new engine recording. Preserved original in Docs/Audio/EngineSource; runtime WAV GUIDs/settings unchanged. Idle/drive now76.0%/89.2% energy above200Hz; source peaks .70. Unity imported ADPCM22050Hz mono peaks .701/.697 and RMS -13.84/-14.65dBFS. Retained existing +6dB engine-only gain and three-layer mix. No additional runtime DSP or sources, unrelated effects/music/ambience untouched.

Regression test checks imported PCM retains midrange at low idle pitch and peak headroom. Native routing/mute tests retained. No actual driving audition, speaker-response measurement, hardware profiling or Switch test claimed. No build, Play, scene save or profile modification. External replacement source lookup was unavailable; no new downloads used.
