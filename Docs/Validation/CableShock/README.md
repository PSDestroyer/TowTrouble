# Cable impulse exploit — 2026-11-01

User observed that accelerating before a pull could deliver a large short impulse without reaching the .8-second rupture timer, weakening equipment progression. Expected: a large over-capacity shock should fail quickly, before its force is transferred to the casualty; stronger cables should survive the same absolute demand more readily.

## Evidence / hypotheses

1. **Fixed grace window ignores shock magnitude — supported by code and synthetic reproduction.** Old rupture required .8 s at R01 / .55 s elsewhere, whether overload was small or extreme. Pulses shorter than that could survive.
2. **Taking-up-load smoothing hides overload — confirmed in pure-rule reproduction.** In the old R01 path, .2 seconds of 30000 N demand on an 11000 N cable produced only4888.89 N final smoothed force and zero recorded overload time. It transferred537.78 N·s without breaking. This is a mathematical reproduction, not a Rigidbody playtest.
3. **Force cap hides demand on stronger cables — confirmed by the calculation path.** Damage previously used the force already capped at42000 N; the uncapped spring/damper demand was discarded. New tests verify those quantities remain separate.

## Correction

- RecoveryRules.CableDemand exposes uncapped spring/damper demand for a taut rope. Slack still means zero tension even at speed. CableForce retains its original42000 N physical safety ceiling and original finite-input results.
- RecoveryRules.CableStressAfter evaluates demand / equipped rated strength before load smoothing and force application. Normal rated-or-lower load does not add stress. A≥165% demand fails in the current physics step. Lesser excess consumes a normalized stress budget with continuous failure time `.18 / (1 + 8 * (ratio - 1))` seconds.
- World checks failure **before** calling AddForceAtPosition on either vehicle. A broken impulse transfers no towing force in that step. Existing smoothing remains for safe gentle loading; it no longer grants impact immunity. No new force, teleport, pause or speed cap is added.
- Stress relaxes at .5 budget units/second below rated load (two seconds from full budget). Repeated short pulses accumulate. Detaching/reconnecting does not instantly erase stress; disconnected cables also relax with simulation time. Broken rope/recovery reset clears it. This is temporary stress, not saved item wear or a repair economy.
- Existing host ownership and snapshot shape retained. Tension HUD reads raw demand rather than smoothed force. Yellow starts above60%, orange above82%, red at100% of rated demand; red label identifies the limit/break risk. Rating exactly100% is at capacity and does not itself add stress.
- Applies consistently to R01, other recoveries and tutorial; the previous .8/.55 timers are removed. Tier ratings/prices, traction, winch, extraction resistance and mission state remain unchanged.

## Timing examples

Measured using the project's approximately20 ms fixed step. Actual time is quantized to a physics step:

| Requested load / rating | Measured continuous failure time |
|---|---:|
| 105% | ~.14 s |
| 125% | ~.08 s |
| 150% | ~.04 s |
| ≥165% | Current step (~.02 s) |

The same21000 N demand breaks11kN in one step,14kN in~.04s and18kN in~.08s. It remains below26/36kN ratings. A30000 N shock on11kN now breaks in the first step with zero transmitted tow impulse in the synthetic reproduction.

## Verification

- Unity compilation passed, Console compilation errors zero. Existing Unity AI account/network warning remains unrelated.
- 51 direct Edit Mode tests passed: ten new stress regressions plus41 existing garage/extraction/scenario/rule checks, with per-test setup. Checks include gentle/rated loads across five tiers, immediate peak failure, smoothing/cap bypass, magnitude-sensitive timing, progression, repeated pulses, gradual relaxation, slack safety and different fixed-step durations.
- Original reproduction rerun as pure logic; Before.json / After.json retain the numerical evidence. No active scene changes or new runtime network messages. No persistent player data changed.
- No Play Mode, Test Runner, Rigidbody/particle simulation, network startup, build, commit or push. Live driving feel, terrain-induced shock peaks, controller reaction and multiplayer remain unverified, per the user's ownership of Play Mode testing.

Changed code: RecoveryRules.cs, RecoveryWorld.cs, RecoveryHud.cs and new Tests/Editor/CableStressTests.cs. Backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/CableShock-20261101`.

## User retest

1. Fresh Play session, R01,11kN stock: a gentle aligned pull should remain usable. At roughly5.6kN initial resistance there is nominal headroom; actual physics still needs verification.
2. Leave slack, gain speed, then take up the line. A sufficiently large shock should snap it before launching the casualty. A high speed while still slack must not break the cable by itself.
3. Repeat smaller overload pulses quickly. They should accumulate stress; briefly disconnecting must not reset that budget instantly. Unloaded simulation time lets it recover.
4. Compare the same approach with14/18/26/36kN; stronger rated equipment should tolerate greater absolute force. Even36kN can fail under an extreme raw demand.
5. Confirm warning colors and run the same comparison with host/client on matching code. No runtime result is inferred from these Edit Mode checks.
