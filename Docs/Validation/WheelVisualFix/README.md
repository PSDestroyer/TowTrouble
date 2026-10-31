# Wheel presentation correction — 2026-10-31

User report: wheels appear to rotate incorrectly or oscillate sideways while driving. The user owns driving and multiplayer validation. No Play Mode, Test Runner, physics simulation, network startup or player build was started for this correction.

## Cause and change

The host cached wheel centers in world space during FixedUpdate, but rendered the chassis with Rigidbody interpolation. Between physics ticks the chassis moved while each cached wheel center stayed behind. Both host and remote paths also placed the hub at `hit.point + hit.normal * radius`; a changing road normal could therefore move a wheel sideways instead of along its suspension axis.

RecoveryVehicle now stores previous/current scalar suspension offsets, reconstructs the hub from the rendered wheel anchor, and interpolates host suspension samples between physics ticks. Remote suspension uses the same axis-constrained placement with render-time smoothing. Steering and axle spin are explicitly composed as quaternions. Model/assembly changes, resets, simulation ownership changes and remote teleports reset the presentation samples. Contact forces, traction, tire angular velocities and network payloads are unchanged.

Static inspection found correctly oriented axles across 212 assembled/base-model wheels and 81 replacement wheel variants. The largest measured radial mesh-center deviation in the assembled survey was about 3.7 mm; Scout's current defaults were below 0.001 mm. These inspections did not support rebuilding the imported vehicle geometry.

## Validation performed

- Runtime and Editor test assemblies compiled in the connected Unity 6000.3.19f1 Editor. Final Console observation: compilationFailed=false, compiling=false, no new errors after cursor 11364 (session b2258ee3a29b4d328966e97322320092).
- Seven regression checks passed by direct invocation, including pure pose math at 30/60/144 render Hz against 50 Hz physics samples, forward/reverse rotation, suspension bounds and reset/model/teleport checks on an inactive disposable preview copy. This is a mathematical cadence check, not measured runtime FPS.
- The sloped-contact fixture exposes over 32 cm of lateral shift with the former formula and zero lateral displacement with the corrected placement.
- Five existing initialization checks passed by direct invocation on inactive preview copies. Preview objects/scenes were destroyed afterward.
- OpenWorldRecovery remained the active scene, clean. Its saved bytes, CanyonRecovery, RecoveryWorld and RecoveryCustomization match pre-fix SHA-256 hashes. Existing uncommitted scene/art work was preserved.

Evidence: RegressionChecks.json, InitializationChecks.json, WheelGeometry.json, ReplacementWheelGeometry.json and Preservation.json.

## User retest

Drive slowly forward and in reverse, steer both ways on flat ground and slopes, then repeat over uneven terrain. Watch front and rear hubs relative to the body. Also check a garage wheel/model change and a vehicle reset. Remote multiplayer appearance remains unverified until the user can test with another peer. Live disappearance of the reported symptom has not been independently verified.
