# Cinematic main-menu camera — 2026-11-10

RecoveryCameraMenu.MenuPose now frames the player's fitted vehicle from a low front three-quarter angle, relative to the car's own +Z front. The previous fixed world-space direction placed the camera behind the car when it was parked at the garage facing 180 degrees. Composition reserves the left menu and lower-right specification card. Vehicle-local visual bounds refresh on vehicle/geometry change and fit the current aspect/safe area. A very small slow yaw drift is disabled by Reduced Motion.

Only the menu pose changed; title/starter/workshop framing, gameplay follow distance, networking and saves were not changed. No scene/prefab/material edits or new packages.

## Validation

Unity compilation completed without errors. Native static Editor renders of the actual OpenWorldRecovery scene and actual BuildMain UI at 1280×720 and 1280×800 were visually inspected. Temporary inactive presenters used a new SuppressSaving profile; source world/vehicle were read only. Scene and UI were rendered separately and alpha composited through Unity's GPU to match the overlay menu's order, without applying scene fog/post-processing to the UI. These are Edit Mode previews, not gameplay captures; live COZY/vehicle simulation was not run.

Both final previews emitted no errors; projected vehicle bounds remain clear of the left panel and above the specification card. Original camera local Z was -5.38m (rear); new camera local Z is approximately +8.2m (front). Existing active scene and dirty state were preserved. Temporary roots/resources and preview keys cleaned up; platform preview restored. Earlier temporary preview-script errors and historical Console were retained. No Play, Test Runner, player build, commit or push; no Switch hardware/controller or every-body visual claim.
