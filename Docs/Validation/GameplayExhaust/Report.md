# Gameplay exhaust visibility — 2026-11-07

Scope: exhaust smoke during gameplay. Engine/physics/snapshot policy and menus are unchanged. The final diff changes RecoveryVehicleEffects and its existing tests only, plus this documentation.

Baseline: three stock starters (Scout, Hauler, Ranger) had no exhaust particle outlet; Buddy had one measured factory outlet. Existing smoke was sparse, short-lived and faint. Factory bodies without a separate measured pipe now get one rear-underbody outlet derived from current fitted geometry, with height bounded by the rear hitch so suspension settling does not put it in the ground. A visible fitted exhaust's measured ports take precedence; no pipe geometry, profile schema or networking messages were added.

The per-car emission budget is4/s at idle to18/s under load, shared across a maximum8 outlets. Existing20-particle cap per outlet is retained. Vapor lasts1.1–1.7s, starts at0.14–0.23m, has a clearer gray alpha and gentle world-up drift. Shape-only scaling keeps smoke size consistent when a fitted pipe has a different transform scale. Emission remains gated by actual EngineRunning; no cosmetic menu idle override remains.

Validation:
- Unity6000.3.19f1 compilation: zero errors/warnings. All5 existing NUnit effect test methods directly invoked in the Editor passed (not a Test Runner run). Stock coverage, emission-off/budget, brake atlas, remote engine snapshot flags, rebind cleanup and Editor no-particle-play behavior checked.
- Actual local Solo/Explore session on a memory-only profile:4 starters x idle/accelerating/engine-off states passed (12 checks). Idle4/s with5–7 live particles; throttle.85 gave15.9/s and18–19 live particles. Engine-off was injected via existing SetSimulation(false) for the controlled check; all particles naturally expired to0 within2s. Acceleration-load checks held the handbrake; they were not movement tests.
- Separate unbraked drive with final visual values reached7.07m/s (~25km/h) with18 live exhaust particles; actual Game-view capture Gameplay-final.png. No contract accepted or completed.
- Same-frame native camera comparison with particle renderers hidden/shown confirms real rendering: final production color changed2504 pixels by >2 channel levels, peak50/255. Render-1.png uses production settings, without the overlay HUD. A darker trial changed2671 pixels/peak67 and was discarded. No manually spawned or simulated particles were needed for these gameplay checks.
- Final current Console errors0; existing Metal depth/AI account warnings remain. Profile scope restored; Play stopped; scene clean; real save, backup and temporary save-file hashes unchanged (Cleanup.json).

Existing measured upgraded-pipe mapping and remote telemetry tests retained. No real second-peer, Switch hardware or build validation; no frame-time performance claim. Particle budget remains bounded. No scene/prefab/material/shader changes or commit.
