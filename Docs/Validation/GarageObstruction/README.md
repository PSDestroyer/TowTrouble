# Garage preview obstruction — 2026-11-11

Report: Switch docked garage UI visible, car missing behind pale/diagonal background. Exact device failure remains unverified locally.

Confirmed related defect: GarageCameraPose shortened the camera arm on an environment hit but retained the old offset look target. At a close hit the car could end up behind the lens. Native static probing of the open OpenWorldRecovery scene reproduced this in 2/60 sample positions/headings (garage z+6, headings90/120, Handtruck returned beside delivery collision). Vehicle geometry in this sweep is representative bounds, not simulated driving. Original projected focus z=-.34 and -.10.

Correction: try neighbouring clear orbit angles before shortening the arm; keep look offset proportional to distance if shortening is unavoidable. Check lens clearance. Garage entry initializes the intended frame instead of inheriting a gameplay position; preview rotation applies with the pose. No scene/asset/render-quality/profile changes.

Validation:
- Native Unity6000.3.19f1 compilation:0errors,0warnings.
- Four directly invoked RecoveryGarageCameraTests passed, including close-obstacle reproduction, first frame from an underground starting pose, particle-bound exclusion and body/bumper replacement coverage.
- Identical scene sweep after correction:0/60 focus projection failures.
- 720p/1080p pixel rectangles:120 cases,0 focus projection failures. Projection/physics checks, not device rendering or visual captures.
- git diff --check passed. No build, Play, Test Runner, scene save or career changes.
- Existing active scene was dirty before work; preserved. Initial test fixture used a Play-only scene API; corrected to additive Editor scene and all tests rerun successfully.

Hardware follow-up: repeat entering the garage at the reported location/heading in docked and handheld mode, orbit and switch body/front/rear categories, undock/redock while open. Verify the car stays visible and return to gameplay is correct. Fully enclosed/confined positions may still force a tighter crop; this is not a dedicated showroom camera.
