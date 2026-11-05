# Natural parking and receiving places — 2026-11-05

The garage parking and all eight delivery places now use worn ground, tire impressions, modest English destination signs and small exterior service details. The former raised orange outlines are retired. The towing space stays open; decoration sits alongside the receiving area.

B, E, F, G and H moved off their original roads. Six access connectors link B, D, E, F, G and H to the existing named road junctions; D keeps its delivery location and gains a proper connection. All 24 contract destinations and the reset points follow the authored places. Terrain heights, mission IDs/order, progression and delivery rules are unchanged. There are no new interiors or real-time lights. Shared materials, combined meshes and LODs keep the decoration organized for later optimization.

Authoring is in `OpenWorldReceivingPlaces.cs`, `OpenWorldReceivingDetails.cs` and `OpenWorldGarageParking.cs`. Use **Tow Trouble / Visuals / Naturalize Parking and Delivery Areas**. The workshop service-base composition invokes the same helpers, so the later county composition also retains them.

The existing delivery waypoint stays visible and changes its second line on the final approach: **Bring recovered car here**, **Stop to deliver**, then **Delivering…**. Position and speed come from the recovered car. It uses the existing horizontal radius below 7 m, altitude difference below 4 m and speed below 2 m/s; the host still completes only after the uninterrupted hold exceeds 1.2 s. No separate UI countdown or authority/save rule was added. Redundant navigation/attach prompts are hidden when delivery feedback owns that context.

## Evidence

- `FinalValidation.json`: static audit passed for 24 contracts, eight receiving groups, six graph connections, terrain-height preservation and sampled delivery/tow-space clearance. No warnings, errors, missing scripts or added lights in the recorded audit. Its final sentence describes the static audit's limits; the subsequent Play check below supplies the final-stage completion evidence.
- `DeliveryHud.json`: all three instructions fit at 100% and 130% text scale, using the longest authored destination name. Preview validation left scene state and protected preferences unchanged.
- `Tests.json`: eight directly invoked Edit Mode tests passed: four delivery guidance branches/boundaries and four waypoint projection cases. This was not a Test Runner invocation.
- `Idempotence.json`: a second authoring pass left map data, renderer/mesh counts and every terrain paint weight identical; scene saved. Inactive legacy anchor lookups remain compatible with mountain/sign authoring, and shared connector meshes register Undo from every composition entry point.
- `DeliveryProbe.json`: connected Solo host advanced through 230 real frames at moved bay B. All three instructions appeared, followed by the normal Complete/results/payout flow. Measured time from first eligible sample to Complete was 1.2218 s.

The Play test used an isolated stock profile. Initial vehicle placement, the already-released delivery phase, initial velocity and a braking request were controlled fixtures. Subsequent physics, containment, speed checks, hold, completion and payout followed normal runtime code. It did not validate extraction or a full driven/towed route. The test profile's temporary save was restored; after stopping Play the four protected PlayerPrefs keys matched their original values and existence exactly, with zero further restoration writes needed. macOS plist serialization changed its byte hash. Private profile snapshots and raw preference files are intentionally excluded from this folder.

No player build, performance benchmark or multiplayer-peer test was performed. Static clearance samples do not establish every dynamic towing turn. The idempotence/compatibility follow-up was performed in Edit Mode after the recorded delivery fixture.

## Representative images

![Garage parking](Garage-parking.png)

![Forest receiving approach](Bay-B-Approach.png)

![Mountain receiving approach](Bay-D-Approach.png)

![Live delivery feedback](Delivery-Delivering.png)
