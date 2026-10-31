# Brake lights and exhaust — 2026-10-31

Implemented in RecoveryVehicleEffects, attached to Scout, Hauler, StrandedClassic and the current OpenWorldRecovery vehicles. Native asset files are unchanged.

- Rear lamps use a separate red running/braking emission atlas. Front-light cells do not change when braking.
- Brake input (Space/handbrake) or throttle opposing signed movement lights the rear lamps; steady reverse does not. Disabled recovery targets do not emit exhaust.
- Exhaust uses visible mesh identities, with 23 mesh variants / 28 authored outlets. Rebinding follows model and garage assembly changes. Empty exhaust sockets and capped T_Plug trim have no emitter.
- Idle output totals 2 particles/sec; full load totals 16/sec across the fitted outlets, with at most 8 outlets and 20 particles per outlet. Particles are local presentation; existing FishNet snapshots carry brake/engine flags.
- No additional packages. No builds, Play Mode, Test Runner, physics simulation or network startup were used.

## Validation

Compilation: zero errors/warnings. Five direct Edit Mode checks passed, including reverse/brake distinction, source atlas isolation, remote snapshot flags/reset and exhaust cleanup across model changes. Static garage application passed for all 23 supported catalog options. Shader compilation has no errors.

Running/Braking PNGs are static camera renders, not driving captures. ExhaustOutlets-Static.png shows red markers and direction lines on the 23 source meshes; row-major names are in ExhaustOutlets-Index.json. Particle motion, road driving, frame cost and live multiplayer remain untested.

## User Editor check

1. Start Solo. Press Space: rear lamps brighten red. Release: lamps return to their dim state.
2. Drive forward, press reverse: lamps brighten while slowing; after reversing steadily they dim. Repeat in the other direction.
3. Select Buddy (stock rear exhaust), or equip an exhaust in the garage. Look for subtle idle vapor and stronger output while accelerating. Cars without fitted pipes intentionally produce none.
4. Change model/exhaust repeatedly: only the currently fitted visible pipes should emit, without duplicate smoke. Check sloped roads and towing as usual.
5. Multiplayer later: both peers need the same updated VehicleFrame schema; check the other player's lamps and exhaust. No live networking validation is claimed.

Camera distance 7 and existing surface-track work were preserved. Prefab integration is repeatable via Tow Trouble > Vehicles > Configure Brake Lights and Exhaust.
