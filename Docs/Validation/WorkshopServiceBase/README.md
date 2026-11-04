# Workshop service base — 2026-11-04

The workshop now has a connected, terrain-painted service court, a receiving bay away from the road, two closed exterior service doors, recovered-car parking with a sanitized Suntail canopy, and a shaded rest corner. Existing workshop interior furniture and lights were removed. New props use shared materials, combined detail meshes, distance culling, and simple colliders.

The workshop receiving bay moved from (36, -27) to (-31, -9) in X/Z. R01, R08, R16 and R20 retain their rescue positions and now deliver to this yard. The other seven destinations remain unchanged. Orange receiving marks are controlled by the active open-world delivery phase; permanent corner paint is separate. The entire marked area fits inside the existing 7 m acceptance radius.

The first 35 m of the east road approach is eased into the workshop junction, including its drive collider. Its preserved source meshes make repeated authoring stable. Road feather edges follow the revised ground. The new road access is registered in route navigation and wheel-track surfaces; regional grip tuning is unchanged.

## Verification

- Eight focused Edit Mode tests passed: delivery highlight rules/lifecycle, route-cache refresh and road connectivity, garage camera framing across all bodies and bumper replacements.
- Scene audit: 24 rescue positions preserved, 8 logical destinations, exactly four destinations reassigned to the new workshop bay. Navigation reaches the bay; the receiving corridor contains no new solid props. New service root: 110 renderers, 22 colliders, 21 LOD groups, zero real-time lights.
- Play Mode/Solo: an actual R09 target was released using normal winch/driving input, then used in a bounded workshop towing fixture. Only fixture setup teleported the vehicles. Approximately 99.24 m were driven forward through the receiving area and back onto the road using normal host physics; no cable breaks, max tilt 0.68 degrees. This test deliberately did not award a contract payout.
- Live Customize: BODY, FRONT BUMPER, REAR BUMPER and WHEELS were opened through real category callbacks. All four settled views had clear environment sightlines, finite framing and no camera-body penetration. Garage closed normally afterward.
- Test session checked the existing profile on every measured frame. Detailed profile backups stay outside the repository. Editor timings are not a standalone performance benchmark.

## Re-authoring

Use `Tow Trouble > Visuals > Compose Workshop Service Base`. The full `Compose County Landscape` pass also composes this base last. The previous disconnected yard stays inactive as a source for its two static vehicle visuals; no old yard renderers or colliders remain active. No packages or project settings were changed and no build was produced.

Final state: Edit Mode, scene saved, no compilation or current Console errors. All four protected profile keys were identical; zero values required restoration. Unity changed its own editor/session metadata in the disk preferences file.

![Workshop courtyard](Courtyard.png)

![Closed exterior service frontage](Frontage.png)
