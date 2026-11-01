# Natural edges and visible tire burial — 2026-11-01

All 15 broad zones now separate their existing physical support mesh from a shallow visual loose layer. Central cover thickness: mud 0.27 m, snow 0.30 m, sand 0.24 m; smoothly declines to zero toward the perimeter. Does not move wheel transforms, alter vehicle traction, or lower terrain. Existing raycast suspension follows its prior support bed, so tire tread is visibly occluded by loose material. The three dynamic mud surfaces keep their deformation integration.

Twelve static zones use one feathered surface sampling the native terrain textures/paint and blending loose material into them, with vertex alpha fading the outer silhouette. Eliminates the second skirt and sharp bright/dark rim. Dynamic mud retains terrain blend margins. Removed depth bias from the terrain blend shader; it previously exposed submerged geometry. Puddle contours now feather with vertex alpha. Materials/mesh assets remain shared and authored once, no new per-frame work or emitters.

Verified in Edit Mode: compilation successful, zero current Console errors (one prior account warning), all 15 cover depths checked, all 96 wheel contacts supported, R01 settings/terrain and road/delivery/reward metadata preserved, no missing scripts, repeated generation without duplicate roots. Static 23 contract photos updated; close-up renders of R04/R14/R22 included. No Play Mode, simulation, Test Runner, networking or build. Live extraction and transition appearance remain user tests.

Files: OpenWorldBroadGround.cs (visual/support separation and feathered authoring), OpenWorldRecoveryReconciliation.cs (terrain margin), GroundBlend.shader (native/loose blending). Authoring: Tow Trouble / Recovery / Build Broad Soft Ground Zones. Backup: Desktop/TowTrublw/Backups/NaturalGroundEdges-20261101.

Dynamic Mud: opaque visual triangles stop inside the fully terrain-covered transition; full support mesh/UV bounds remain intact. This removes the exposed opaque perimeter without disabling the existing dynamic mud integration.
