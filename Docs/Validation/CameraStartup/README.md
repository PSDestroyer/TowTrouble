# Startup camera and Switch menu — 2026-11-10

The supplied video showed a transient session-failure view/lobby and camera passing beneath terrain after Play Solo. Live reproduction in the actual OpenWorldRecovery scene confirmed a startup-order defect: ResetRecovery placed the driver at the legacy tutorial spawn before ConfigureSession enabled FreeRoam; the client transport callback also released the busy screen before the world and player slot were ready. Camera smoothing then followed the temporary underground vehicle position.

Map lobbies now spawn at their garage from their first initialization. Connecting remains true until the local slot and intended initial phase are ready, so presentation holds its previous valid pose during setup. The first gameplay pose/rotation is applied together, without interpolating through scenery. Subsequent following still smooths movement and checks the final smoothed camera arm for obstruction. Camera distance and scene assets are unchanged.

Platforms without multiplayer show PLAY, omit SOLO from the title footer, and use neutral pause/failure wording. Desktop retains PLAY SOLO alongside PLAY CO-OP.

## Editor evidence

- Actual rendered production-camera frames sampled in beginCameraRendering for about five seconds per run, comparing camera Y against the terrain below its X/Z.
- Before: 297 frames, 28 below terrain, minimum clearance -12.88915m; views Busy/Main/Gameplay.
- After Scout: 297 frames, none below terrain, minimum clearance 5.434788m; views Busy/Gameplay.
- Repeat through normal leave/reload with body model3: 294 frames, none below terrain, minimum clearance 5.093153m; views Busy/Gameplay.
- Live menu under Switch capability preview: PLAY / HOW TO PLAY / SETTINGS / CREDITS / QUIT, title contains no Solo.
- 20 directly invoked NUnit methods passed: five startup camera/readiness/spawn checks, five button-selection checks, three platform guards, seven offline transport checks. Final invocation emitted no errors. Test fixture cleanup now detaches borrowed TMP atlas/material references before destroying its font clone and avoids assigning a null EventSystem; the earlier test run's cleanup errors remain in historical Console records.
- Unity compilation completed without errors. Existing Console history was preserved; no claim that historical Console is empty.

Play used the existing isolated in-memory profile and offline transport policy preview in the desktop Editor; no game socket connection was started. Play stopped, validation scope cleared, real save/backup/previous hashes unchanged, original active scene and dirty state preserved. No scene save, player build, commit or push. No Nintendo hardware, physical controller or actual two-peer validation; terrain-clearance samples cover these starts, not every map camera path.
