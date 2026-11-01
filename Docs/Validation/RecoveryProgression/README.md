# Recovery progression and recessed wheel impressions — 2026-11-01

Implemented in OpenWorldRecovery in Edit Mode. No Play Mode, physics/particle simulation, networking, Test Runner, builds, or profile writes were used. The user owns live driving and multiplayer acceptance.

## Gameplay changes

All 24 world missions carry a scene-owned physical load description. Target mass now follows its actual Scout / Hauler / Buddy / Ranger body (1350 / 1750 / 1150 / 1550 kg), with 35 kg retained water at R05 and 180 kg at R08. Tutorial contracts retain the original 950 kg behavior. R01 keeps every previous extraction setting and its pose; its target now has Scout mass.

RecoveryLoad calculates separate rolling, soil burial, exit lip, chassis contact and seized-wheel forces. The formula has no tier input. Scenario severity is authored separately per location in metres and contact fractions. Static ground force scales with weight; the wheel radius affects soil/lip resistance. RecoveryExtraction uses the actual wheel contact plane and gravity; the driver's existing traction solver accounts for its own mass, grade, tires, throttle/slip and rolling resistance. Existing movable trees remain separate recovery steps (400 kg at R02/R06/R18, 580 kg at R04/R14), with their friction load included separately in the audit rather than added to the casualty pull.

All world sites now use the existing displacement-based release system. Deep pockets keep most of their resistance through the exit instead of shedding it immediately after a tiny jerk. Release still requires real body/wheel travel and support. The established cable shock failure and loaded-force ramp remain in place. Stock solo force cannot reach the Hard load thresholds in the tested force rules. Two players can legitimately combine their pulls; no inventory or tier gate is introduced.

Engine-failure jobs now differentiate sticking rear brakes (R09), a seized transmission in snow (R16), and jammed brakes in sand (R24). Locked wheels stay visually still until the jam releases; smoke, skid trails, loose-material accumulation and descriptions explain the load. R16 and R24 have two small feathered visual accumulation meshes over separate support meshes; the shared TerrainData was not excavated. Their two support colliders stay on the original ground (+12 mm).

New catalog items: `tires_winter_pro` (heavy alpine tires, snow grip ×2.35, mud grip ×0.80, +35 kg, price 3000; reuses the validated studded wheel visuals) and `winch_3` (22 kN, 0.85 m/s, price 4800). Existing stock cable 11 kN, 14/18/26/36 kN progression, stock/improved winches and save IDs are retained. No new RPCs or snapshot fields; contract identity selects the same load on every peer.

## Force audit

Loads.csv / Loads.json contain every site, input parameters, force components, gravity contribution, separate obstacle mass/load, 4 vehicle bodies × 3 equipment stages and sampled setup coordinates. Candidates span 7–19 m within the cable envelope and include shoulders. Clearance checks reject obstructed body space and unsupported wheel footprints. Upgraded candidates have at least 4% calculated static margin. These are low-speed static estimates, not completed driving runs; transient suspension, load transfer, route access, user input and co-op still need live acceptance.

| Site | Tier | Target kg | Material | Aligned initial pull kN | Stock estimate kN | Equipped estimate kN |
| --- | --- | ---: | --- | ---: | ---: | ---: |
| R01 | Local | 1350 | Firm | 5.24 | 10.45 | 21.70 |
| R02 | Standard | 1150 | Mud | 4.44 | 10.45 | 21.70 |
| R03 | Specialist | 1750 | Mud | 11.07 | 10.45 | 21.33 |
| R04 | Hard | 1550 | Mud | 13.60 | 10.45 | 21.70 |
| R05 | Local | 1185 | Silt | 3.74 | 10.45 | 10.77 |
| R06 | Standard | 1350 | Silt | 7.14 | 10.45 | 21.70 |
| R07 | Specialist | 1550 | Mud | 8.06 | 10.45 | 21.70 |
| R08 | Hard | 1930 | Silt | 16.44 | 10.45 | 22.09 |
| R09 | Standard | 1350 | Firm | 4.50 | 10.45 | 22.03 |
| R10 | Specialist | 1550 | Mud | 11.47 | 10.45 | 24.70 |
| R11 | Hard | 1150 | Mud | 15.61 | 10.45 | 24.70 |
| R12 | Expert | 1750 | Snow | 17.49 | 10.45 | 24.70 |
| R13 | Specialist | 1550 | Snow | 8.47 | 10.45 | 24.18 |
| R14 | Hard | 1350 | Snow | 13.82 | 9.40 | 22.32 |
| R15 | Expert | 1750 | Snow | 18.82 | 9.30 | 22.22 |
| R16 | Expert | 1150 | Snow | 17.74 | 10.45 | 23.28 |
| R17 | Local | 1350 | Sand | 2.95 | 10.45 | 21.51 |
| R18 | Standard | 1150 | Sand | 4.91 | 10.45 | 21.80 |
| R19 | Specialist | 1550 | Sand | 9.34 | 10.45 | 21.27 |
| R20 | Hard | 1750 | Sand | 14.56 | 10.45 | 23.09 |
| R21 | Standard | 1150 | Sand | 4.94 | 10.45 | 17.99 |
| R22 | Specialist | 1550 | Sand | 10.05 | 10.45 | 18.47 |
| R23 | Hard | 1350 | Sand | 13.81 | 10.45 | 20.08 |
| R24 | Expert | 1750 | Sand | 18.16 | 10.45 | 19.16 |

## Wheel impressions

Fixed both visibility causes: the track mesh used the hidden support bed instead of the higher visual loose-material skin, and its transparent draw order preceded the loose-material cover. Seventeen serialized regular-grid bindings now project tracks onto their visual skin without reading Mesh.vertices per wheel per frame. Impressions render after that cover (queue 3005).

Snow/sand/soil banks now have approximately 11 / 8 / 6 cm relief, plus recessed-bed texture parallax, channel wall normals, compaction shading and tread detail. This is visual relief, not persistent excavation or new track collision. Living Mud keeps its own batched stamp system, now with a continuous channel normal around the native tread and stronger dynamic normals. No duplicate Living Mud stamping is added. Original hard/wood surfaces have no rut relief. Pool limit remains 36 chunks × 128 segments; no unbounded track history, extra track colliders or RPCs.

## Validation

- 91 directly invoked Edit Mode logic/fixture checks passed, including existing extraction, cable stress, winch, target variety, soft ground, garage, track surface and track continuity tests. No Test Runner.
- All 96 mission wheel contacts pass; 68 contact projections correctly follow the higher loose-material skin.
- Map site positions, poses, model/paint assignments, tiers, progress IDs, roads and dropoffs preserved; R01 extraction values preserved.
- All 24 load audits pass: Hard/Expert exceeds the stock shock allowance and clearance-checked equipped solo candidates have static force margin.
- Static six-surface Camera.Render inspected; shader stamp Graphics.Blit/readback passes channel-normal and alpha checks. Both shaders compile; native source/vendor assets untouched.
- R09/R16/R24 mission photographs refreshed and inspected. Scene saved in Edit Mode.
- Current Console: no compilation errors; recurring Unity Account API connectivity warnings are external to gameplay.

## User acceptance

Compare stock and equipped solo at R04 (mud/ditch + separate tree), R14 (snow + tree), R15 (deep snow benchmark), R20 (sand) and R24 (damaged heavy target). Check forward/reverse/poor-angle pulls, cable shocks, winch stall, partial progress/detach/reconnect, final release and delivery. Test impressions on natural terrain and all loose-material zones, with reset/model change and long driving to exercise pool recycling. Co-op, late joining, driving feel and measured performance remain untested by the agent.

Backup: /Users/gheorghe/Desktop/TowTrublw/Backups/RecoveryProgression-20261101.
Authoring: Tow Trouble → Recovery → Calibrate Mission Loads / Deepen Loose Ground Wheel Tracks / Audit Mission Load Progression; Tow Trouble → Garage → Refresh Specialist Recovery Equipment. Legacy full map regeneration can overwrite finished poses/landscape and should not be run for balance tuning.
