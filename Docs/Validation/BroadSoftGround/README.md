# Broad soft ground — 2026-11-01

Replaced the 15 narrow wheel-bank presentations with landscape-sized contact surfaces: six marsh basins (28 × 36 m), three deep snow fields (24 × 32 m), and six loose sand basins (30 × 38 m). Irregular boundaries, terrain-following margins, shallow visual puddles and broad snow/sand drifts replace the narrow supports. R01 retains its authored extraction setup. Water, rocks and failed-engine sites retain their situations. Terrain heights are unchanged by this pass; vegetation inside broad zones is cleared. Photos R02–R24 refreshed through static Camera.Render only.

The three existing Living Mud surfaces are reused, retaining their existing render texture budgets. The other twelve zones use static mesh colliders; puddles/margins have no colliders. Snow/sand use shared materials; surface spray reuses the twelve existing emitters (28 particles each). No per-frame terrain deformation or additional dynamic-mud render textures. This is structured for later optimization, not performance-benchmarked.

## Vehicle behavior

Ground contact now applies planar rolling drag on snow/sand in addition to existing traction coefficients. Per-wheel drag: snow 110 on normal terrain / 340 in deep fields; sand 75 / 260. Existing mud uses 375. Appropriate tire grip upgrades reduce loose-ground drag by sqrt(grip), never remove it. Forces remain in the authoritative vehicle physics path; airborne wheels receive no ground resistance. Resistance opposes planar velocity and does not introduce vertical suction or artificial stationary holding force.

RecoveryWheelMud now owns contact-based coating for all three materials, using existing tire shader properties through MaterialPropertyBlock. Snow uses white tint/snow texture; sand uses tan tint/sand texture; mud retains its brown coating. Spray uses the same surface tint. Coverage accumulates per contacting wheel and decays gradually off that surface. Teleport/reset and model changes clear coverage. Presentation is local on each peer; no additional RPCs. Gameplay tuning and visible live particles still need the user's Play Mode assessment.

## Verification

- Clean compilation, current Console: zero errors; pre-existing account warning remains.
- Nine deterministic test methods invoked directly in Edit Mode; no Test Runner.
- All 24 sites / 96 wheel anchors have ground support by read-only raycast.
- R01 settings and terrain preserved; IDs/rewards/roads/deliveries and five tow obstacles preserved.
- Twelve tires have the required coating shader properties and configured texture references.
- Twelve bounded spray emitters; nine deep snow/sand surface components; finite geometry.
- Repeated authoring regenerates owned roots without duplicate zones; reconciliation retains broad zones.
- Static photos inspected. No Play Mode, physics/particle simulation, network session or build was run.

## User play checks

Compare R04 mud, R14 snow and R22 sand with nearby firm ground. Drive in slowly, accelerate, turn, reverse, then leave: check resistance, wheel coating, colored spray, gradual cleaning, and tow/winch extraction. Also try upgraded tires. Adjust force values after the driving test.

Authoring menu: Tow Trouble / Recovery / Build Broad Soft Ground Zones. Parameters currently live in OpenWorldBroadGround.cs and RecoveryTraction.SoftGroundDrag; prior map-detail settings remain available independently.
