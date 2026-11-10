# Contract incident cinematics — 2026-11-10

Accepting RECOVER now plays a short, local reenactment at the actual recovery location. The target's correct body and paint approach the blocked position, settle into its authored pitch/roll/depth, briefly struggle and stop. Engine failures show the existing smoke system and a dying engine. Mud, snow, sand, rock and roadside scenarios use their real ground, obstacles and final pose; this is procedural presentation, not a separate handcrafted crash movie for each site.

## Behaviour

- About5.85 seconds including transitions. An outgoing250ms fade hides the cut to the incident; the camera cuts back under the final opaque fade before gameplay reappears. Terrain clearance is checked after shortening the camera arm as well as before. Tree fading follows the reenactment car.
- The normal gameplay camera and existing URP settings are reused. No extra rendering camera, render texture, fullscreen effect, package, scene or prefab wiring. One temporary mesh-only vehicle, one small existing engine loop routed to the Vehicles mixer and at most48 smoke particles. Model meshes/materials/clips are shared without creating asset copies.
- Moving visual wheels use the existing axle rotation and suspension projection. No source vehicle/body/collider moves, no forces, extraction changes, tracks, reward/save schema or authoritative gameplay outcomes come from animation.
- Solo pauses simulation and the mission clock while the sequence advances in unscaled time. In co-op every current peer sees a local presentation and brakes only its own car; the shared world continues. An unbuffered FishNet notification prevents old incidents replaying for late joiners. The Switch offline transport remains unchanged.
- Skip after700ms with Enter/Space/Esc or controller Confirm/Cancel/Start. Initial held submit must first be released. The skip frame cannot operate a cable or open Pause. Reduced Motion removes camera drift and chassis rocking. Focus/pause suspends progression and mutes the incident engine.
- Finish/skip/contract change/disconnect/owner disable/destruction dispose the visual copy, restore original renderer flags and release an owned solo pause. Runtime-only state is not saved. Training does not trigger these open-world introductions.

## Verification

- Native Unity compilation:0 errors/0 warnings. Five new NUnit methods plus five existing camera startup checks were invoked directly in Editor, all10 passed. Covers source immutability, no copied physics/scripts, visibility restoration, early cancellation/idempotent disposal, Reduced Motion, skip fade ordering and camera entry regression. No Test Runner or player build.
- All24 real locations sampled50 times each:1200 camera poses, finite values, final authored position reached and minimum terrain clearance1.14999m. A downward bank normal exposed an initial shortened-arm defect; the final camera protects the adjusted arm too. MotionFrames.png contains six representative sites × four times, using temporary photo presentation settings for inspection.
- Actual isolated offline Editor Play through the existing board acceptance: R01 full sequence517 rendered frames,335 intro frames; R24 engine failure/Enter skip337 rendered frames,234 intro frames. Both had zero recorded runtime errors, a paused solo mission clock, hidden gameplay HUD, active cinema overlay, positive camera clearance, automatic return to Gameplay, timeScale1 and no remaining actor/engine voice. The real New Input System received Enter from a temporary keyboard which was removed afterward. R24 smoke reached10 particles while timeScale0.
- One probe initially encountered an already-active contract and was rejected by its Explore precondition; it was reset within the temporary profile before the recorded final runs. Historical tool/Console records were retained.
- Play stopped, real save/backup/previous hashes unchanged, validation scope cleared, original scene/dirty state preserved. Fourteen scene/terrain/pipeline/quality/weather files SHA256-identical. PC pipeline, original grading23/15/.2, main camera/far2300 and timeScale1 restored; no temporary actor/keyboard. Earlier recovery photo work retained.
- Final presentation removes the decorative upper-band caption, leaving a clean letterbox and the recovery title/skip hint below. The R01 live screenshot precedes that small text cleanup; R24 shows the clean upper band.

## Evidence and limits

[Validation](Validation.json), [location camera checks](Locations.json), [NUnit checks](Tests.json), [complete run](Live.json), [actual input skip](LiveSkip.json), [restored state](FinalState.json).

[Live forest cinematic](Live-Cinema.png), [engine-failure scene](Live-Skip.png), [representative motion frames](MotionFrames.png).

No build, commit or push. No Nintendo hardware, physical gamepad or two-peer multiplayer test; no FPS claim. Existing models, source resting poses and map geometry set the visual limits. No additional Inspector setup is required.
