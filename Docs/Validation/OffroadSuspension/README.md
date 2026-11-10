# Offroad suspension lift — 2026-11-11

Confirmed sign error in RecoveryVehicle.UseAssembly: catalog Lift=.15 raised suspension anchors relative to chassis, causing the supported chassis to settle lower. Collider center was independently raised .15 despite no matching body-mesh/hitch displacement.

Correction: lower suspension anchors by Lift, allowing ground support to raise chassis; keep collider center in the same authored frame as body and hitches. Travel, stiffness multiplier, pricing, item IDs, mass and balance remain unchanged.

Native before/after comparison on Scout fixture: anchor delta +.15 -> -.15; collider delta +.15 ->0; hitch delta remains0. Regression covers four base models and catalogue body assemblies, two consecutive Offroad applications, unchanged collider/hitch alignment, wheel preview positions and return to Stock. Static spring-balance calculation includes20kg extra mass and existing stiffness multiplier; predicts approximately15–17cm higher chassis under equal level-ground conditions. This is not a live physics settling/terrain test.

Five directly invoked garage/camera NUnit tests passed; compilation0errors/0warnings, diffcheck clean. Temporary preview scenes only, no build/Play/scene save or career modification. Existing purchased suspension uses the correction automatically on configuration apply; no save reset needed.
