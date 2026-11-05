# Alternative extraction directions — 2026-11-05

Reported: towing a recovered car opposite to the authored exit left the job in Recover and did not display its delivery destination.

Reproduced before the change by directly stepping the production recovery model in Edit Mode:20m opposite displacement yielded zero progress, TakingLoad, IsFree=false. RecoveryContract.IsExtracted also returned false for the opposite route. RecoveryWorld changes to Deliver only after these predicates succeed, explaining the missing delivery marker.

Fix: both extraction variants now measure horizontal displacement instead of positive projection on the preferred exit. The arcade model still requires both chassis and blocked wheel displacement, a prior loaded cable, supported contact, the contact hold and smooth release. Movement/stall reporting follows outward motion rather than only the authored heading. ExitDirection remains the preferred lower-effort angle; trees retain their separate clearance stage. Legacy tutorial extraction thresholds are unchanged. No scene/profile/network schema changes.

Validation: Unity compilation zero errors/warnings;26 direct Edit Mode regression checks passed. Actual24 map contract configurations accept the opposite route in the fallback and enabled arcade rule. Cases cover forward/side/reverse/diagonal travel, terrain-aware settings, insufficient body/wheel movement, vertical movement, airborne support, detaching, hold/reset and persistent release. These are model/contact-rule checks, not a live driving or multiplayer run. No Play Mode, physics simulation, Test Runner or build was started. The Pipeline transport briefly timed out before compilation; subsequent checks completed and current Console error count was zero.

User check: start Solo, accept a job and hook the car. Pull it out on the opposite side until fully clear and supported, then confirm Deliver and the delivery marker appear; tow to the marked bay. Compare the recommended route and a sideways exit, and confirm idle throttle or a jump alone does not complete extraction.
