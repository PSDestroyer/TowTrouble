# Rescue vehicle variety — 2026-11-01

Each of24sites now has explicit TargetModel/TargetPaint. Four existing scene models(Scout,Hauler,Buddy,Ranger) each appear six times, with finish variation. R01 retains model0/paint0 and its tested pose/profile. Other locations refitted using that model's wheel anchors/radius; existing positions,IDs,tiers,resistance,rewards,deliveries and roads preserved. Existing950kg casualty mass tuning retained.

RecoverySite → RecoveryMap.Contracts → RecoveryContract carries target appearance. RecoveryWorld.ApplyContract selects body/paint before scenario activation/spawn; clients resolve the same choices from the existing synchronized contract index, including late join. No new RPC/frame payload and no runtime instantiation. Default0fields preserve existing tutorial/old-scene appearance. Four bodies were already present on the casualty actor.

Authoring:Assign Varied Rescue Vehicles(Tow Trouble/Recovery). Reuses existing driver cream/olive/gold material variants by renderer path/slot; target finish0remains its original rust. Tire mud materials stay untouched across palettes. No new textures/material instantiation per frame. GeometryVersion rebinding continues to refresh wheel presentation/coating/effects through the existing vehicle code.

Static contract photos now clone site.TargetModel,apply site.TargetPaint,and pose wheels from model-specific geometry. Preview resets target paint0 in finally.23photos updated;R01 retained. FitSituationCar/PoseSituationPhoto use ModelAt rather than the currently active scene model. Future map tools therefore preserve correct seating for each job.

Validation:clean compilation,zero current Console errors(existing account warning);four direct pure tests for default compatibility,contract propagation and cache refresh;all96wheel support raycasts and96coating shader checks passed;R01 unchanged;other contract metadata/roads/deliveries preserved. All24photos inspected in ContactSheet.jpg. No Play Mode,simulation,Test Runner,build or multiplayer session was run. Live acceptance/retry/reset/late join and coating need user testing. Suggested checks:R02Buddy,R03Hauler,R04Ranger,R06Scout,plus R12slope andR05water.

New model/paint fields and refitted Depth/Pitch/Roll intentionally differ from older evidence snapshots. Generic wheel validators must use Casualty.ModelAt(site.TargetModel) and that model's radius instead of CurrentModel. See Validation.json and Assignments.json.

Changed:RecoveryMap.cs,RecoveryContract.cs,RecoveryVehicle.cs(accessors),RecoveryWorld.cs(contract presentation),OpenWorldRescueVehicleVariety.cs,OpenWorldRecoverySituations.cs(fitting),OpenWorldLandmarks.cs(photos),RecoveryTargetVehicleTests.cs,scene/photos. Backup:Desktop/TowTrublw/Backups/RescueVehicleVariety-20261101. No commits/pushes.
