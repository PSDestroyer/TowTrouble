# Starter progression validation — 2026-11-06

Scope: one free Scout for fresh profiles, paid alternative platforms, ownership persistence/migration, and an early cable ladder that keeps the 14 kN tier relevant. Stock power/traction/speed and mission physics remain unchanged.

The baseline scene is OpenWorldRecovery, Unity6000.3.19f1 / URP; Unity CLI beta17. No scene regeneration, package change, build or commit.

All83 direct-invoked NUnit cases passed, including invested legacy vehicles and the four-job budget. Direct invocation is not the Unity Test Runner. The final static showroom render passed184 TMP checks across four model/ownership/budget states at100/130%, preserving scene dirty state and protected preference values/existence.

The fresh Scout force audit reads all24 live authored sites, probes target wheel normals and35 clearance-checked approach samples per equipment stage, using the existing RecoveryLoad/Traction estimates and winch holding budget. Four stages yield96 configurations: stock, 14kN cable, 14kN+surface tires, and full specialist equipment. Static output is in Loads.json; it is not completed driving/towing proof. Examples: R01 demand5.24kN versus stock steady10.45; R03 demand11.07 versus stock10.45 and14kN-cable12.07; R05 demand3.74 versus stock8.06; R08 demand16.44 versus stock10.45. Every Hard/Expert site exceeds the sampled stock capacity. Full Scout still falls short at R15/R24, supporting an eventual platform/body change. Other platform physics are unchanged from the earlier progression audit.

Physical controllers, co-op peers, full driven recovery routes, performance benchmarks and player builds are outside this validation. Private original-profile evidence stays outside the repository.


## Actual Play validation

- Isolated showroom:18 checks /235 actual frames passed, including fresh ownership, rejected direct selection, no browse spending, missing credits, no vehicle grant from completion, review/cancel, Xbox-confirmed purchase, one charge, saved ownership, retained equipment, Nintendo/PlayStation cancellation and restored navigation. Supplemental Nintendo D-pad/semantic A/B navigation passed3 checks /143 frames. Virtual Input System devices, not physical hardware.
- Solo used a fresh stock Scout (1350kg,11kN cable,8kN winch), with controlled approaches at actual authored sites. First stationary winch probe:4,168 frames. R01 released in6.31s; R03 Specialist and R08 Hard stayed blocked for18s with no snaps. R05 did not release from the stationary fixture. These are bounded winch observations, not proof that powered/alternate-direction attempts cannot work.
- The first fixture froze vehicle rotation and exhausted winch travel on the R05 bank. Follow-up restored normal rigidbody rotation and permitted gradual powered pulling after10s at R05:1,570 frames; R01 released in7.27s on winch alone; R05 released in14.32s with powered assist. Both had0 cable breaks. No implementation physics was altered to obtain these results. This validates controlled extraction, not full driving/extraction/delivery routes or campaign duration.
- During the R03 probe, the first-party LivingMud integration logged missing UV0 when reading RaycastHit.textureCoord on the existing soft-ground support mesh. The collider lacked UVs although its visual mesh had a matching vertex grid. A native targeted asset update copies visual UVs into the support; the broad-ground authoring helper now writes the same UVs for future regeneration. Vertex positions, triangle indices, bounds and GUID are unchanged (MudUv.json). A collider-ray regression verifies the actual material UV lookup. Runtime purchase/navigation validation predates this isolated editor/mesh fix; no additional full Play session is claimed after it.
- Original four protected PlayerPrefs keys matched exactly after restoration (two restored keys). Whole plist byte identity is not claimed. No real player purchase, payout or reset was retained. Private profile/plist copies remain outside the repository.

Final regression includes a native MeshCollider.Raycast UV lookup on the repaired support mesh, with matching visual UVs. No player build or additional Play run followed that isolated UV asset/authoring repair. Historical Console messages were retained.

Final state: OpenWorldRecovery active, isDirty=false, Play stopped, compilation idle, and all four protected preference values/existence still identical after the last regression run (Final.json).
