# Welcoming title lighting — 2026-11-07

RecoveryCameraLighting reuses the scene's existing sun and Soft sky fill for the Title view. Sunlight now comes from the parked vehicle's front side, with a warm afternoon tint and a softer warm-neutral fill. This reveals the grille, tires, cabin edges and workshop facade. Original rotations, colors and intensities are cached once on entry and restored on every exit, missing-session path and camera disable. No added lights, shadow-casting sources, materials, post-processing changes, pipeline settings or serialized scene edits. Player brightness and the approved UI remain unchanged.

The initial two-spotlight prototype was removed after visual comparison: increasing it created unwanted bright pools on the foreground. The final approach uses the existing two directional sources and restores them outside Title.

Unity6000.3.19f1 compilation: zero errors/warnings. Sixteen native production renders covered four starter cars x four paint palettes; examples of all four models and dark/bright paints inspected. Eight checks verified exact restoration of original light values on Main, Starter, Lobby, Settings, Credits, the Gameplay view, camera disable, and unchanged light count (3→3 in the loaded scene). These view transitions were tested offline in the isolated profile, not a networked mission.

Final.png is an actual Game-view capture. Frames advanced7457→10717 during final checks; current Console errors0, with existing Metal depth/AI account warnings. No standalone/platform build, physical console or multiplayer validation. The pass adds no lighting sources; no GPU-performance measurement is claimed.

Cleanup.json confirms Play stopped, scene clean, validation scope restored, and real save/backup/temp hashes unchanged. Existing in-progress scene/decor and UI changes preserved; no commit.
