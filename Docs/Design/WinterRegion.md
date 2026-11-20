# Winter Expeditions — regional recovery

The first regional story is Pinewood. Weather and persistent completed works apply across the DLC. Existing mission IDs, rewards, shared garage ownership and profile version4 are preserved.

## Pinewood story

1. Plow Pinewood Road (job3) reopens the northern access.
2. Workshop Meadow rescue (job1) brings the service pickup to the forest depot.
3. Supplies for Pinewood (job0) restocks the station.
4. Pinewood weather station (survey0 / board27) records conditions at the forest depot and files the report at Lakeside.

After all four receipts, the station at the forest depot has warm lights and generator exhaust. Stop between its orange posts, detach the cable and use the normal garage control (G by default) to customize through the shared garage. Cargo must be unloaded first. The original garage remains available.

The selector shows the next prerequisite on locked steps. Existing completed steps keep their replay access, even on old profiles completed out of order. Other regional work remains freely selectable. The active-mission abandon confirmation retains its original behavior.

## Persistent world

The existing `winterJobs` and `winterSurveys` receipts determine the world state. All ten plow sites and four timber sites are reconstructed near the player. Completed roads have open lanes and deposited snow on their shoulders; completed trees are parked in their turnouts. The seven deliveries and six rescued vehicles leave displays at their destination depots.

Active work owns separate temporary props. Abandoning a replay restores the already completed site. Unfinished snow work is reset on abandon; arbitrary tree poses and wheel-rut data are not saved across application sessions. Completed works reconstruct a canonical cleared state without adding a new save schema.

## Route planning

Supplies for Pinewood and Lakeside Repair Stock have a choice between the packed route and a forest cut in opposite directions. Click the route row in the selector, press V, or click the gamepad's right stick. The selected route controls the actual road chevrons and guidance, and can be changed while viewing the journal.

- Before Timber on Lakeside Road is cleared, the packed route uses a roughly749m northern detour through the workshop and reopened forest access.
- After timber clearance, the packed link is about287m.
- The forest cut is about254m, with deep powder, paired blue stakes and two winch anchors. Its final approach includes the timber worksite; clear it before taking loaded cargo through the cut. Winter tires and low range are recommended.

Nine scene trees were relocated to make the cut and the two-position recovery's maneuvering pads physically accessible. Both tree visuals and their colliders move together, only in the running Winter scene. Source scenes, terrain and vegetation assets are unchanged.

## Recovery problems

- Workshop Meadow, Refuge Road Breakdown and Canyon Freight Rescue retain the bank-extraction flow.
- Lakeshore Stranded Scout starts crosswise. Use either tow eye to turn it toward the exit posts before extracting it.
- Mountain Link Snow Rescue requires a loaded cable while stopped for2.5seconds to secure the sliding vehicle. A snow catch limits runaway travel so arriving late does not make the recovery impossible.
- Logging Truck Snow Trap first requires a sideways pull until the blocked axle moves1.2m out of its rut. Detach, drive to the second marked ring and reconnect at the other tow eye for the forward extraction. First-stage resistance follows the sideways working direction.

## Weather

Light flurries → Snowfall → Blowing snow → Freeze. Each period lasts240seconds and transitions over35seconds using the existing world clock. The journal and driving HUD show the forecast; the HUD warns about reduced visibility and icy patches. Solo menus pause the clock.

COZY drives the snowfall and cloud changes, while the existing atmosphere presentation increases distant haze during blowing snow. Four exposed road patches become visible during a freeze and reduce grip locally. Powder in the forest cut becomes deeper during gusts, with two visible temporary wind drifts. Completed road lanes remain open throughout the cycle.

Weather restarts with the existing session clock on a new session; it does not overwrite completed-work receipts. Classic TowTrouble retains its existing weather and driving behavior.

## Setup and boundaries

Runtime initialization is owned by WinterExpedition; no Inspector wiring or additional packages are needed. Shared physics, garage and HUD changes are guarded by the presence of WinterExpedition. OpenWorldRecovery must be enabled in the scene list for the existing return-to-classic route; this task re-enabled that flag without editing the scene.

Validation and its limits: [WinterRegion](../Validation/WinterRegion/README.md).
