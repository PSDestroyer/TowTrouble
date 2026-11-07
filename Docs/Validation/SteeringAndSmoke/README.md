# Steering wheel and exhaust smoke — 2026-11-07

Gameplay exhaust start size increased from 0.14–0.23 m to 0.17–0.28 m (about 21%). Emission budget, lifetime, engine gating and maximum particle count are unchanged from the preceding exhaust fix.

RecoveryVehicle now animates native steering meshes around their authored local Y column axis, up to ±135°, following the same speed-sensitive steering angle as the road wheels with exponential smoothing. Existing replicated ControlInput drives remote presentation; no physics, input bindings, save format or network payload changes. GeometryVersion invalidates a per-body cache; disable, reset and model changes restore the authored pose. No per-frame hierarchy searches or added materials.

Native Unity PrefabUtility restored missing source steering wheels in Goblin_Offroad and Pickupster_Low assemblies, plus the Ranger model in Scout, Hauler and StrandedClassic prefabs. Existing body atlas materials reused. No vendor assets or scenes changed.

Validation:
- Unity 6000.3.19f1 / CLI beta.17 compilation: no errors or warnings.
- Seven NUnit methods directly invoked in the native Editor (two steering, five existing effects tests): all passed. This was not a Test Runner run.
- Actual Solo Explore using the isolated memory-only validation profile: 41 configurations (four starters and 37 purchased body variants), using SetDriver / SendGarageBuild. Each passed left/right rotation, stationary hub, center-return checks and live enlarged exhaust checks. No contract accepted.
- Wheel-close.png visually inspected: hub and spokes retain correct alignment. Gameplay-smoke.png is an actual gameplay frame.
- Play stopped; active scene clean; actual save and backup hashes unchanged; validation scope restored. Current Console: 0 errors, 3 warnings. Historical captured buffer retains older entries.
- No build, physical controller/console or real second-peer multiplayer validation performed. Remote snapshot input and reset behavior covered by the Editor checks.

Direction correction: the user reported inverted steering-wheel motion. Removed the inverted sign in the visual target. Added explicit signed left/right assertions on all four starters; all seven Editor checks passed again, compilation clean. Earlier gameplay captures and 41-configuration checks predate this correction; they checked rotation magnitude and hub stability, not direction. No additional Play run for this one-line correction.
