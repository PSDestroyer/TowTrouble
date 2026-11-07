# Workshop forecourt and larger title car — 2026-11-07

Title camera fitted area increased and minimum distance reduced: the hero car is larger beside the original logo/prompt/text. Camera safety still uses actual fitted geometry and safe screen content. Other camera modes retain their framing.

OpenWorldWorkshopForecourt authors nine permanent exterior props under `Workshop service base/Workshop forecourt working details`: a drawered trolley/spanners, removed wheel, jack/chocks, cable/strap workbench, spare wheel, spare-parts crate, two fluid drums, and a handtruck. Three working clusters sit beside the bays and along the garage apron. The workbench was repositioned/resized after actual-frame inspection to clear the slogan. Art is cloned from the existing authored workshop activity library with shared meshes/materials. No vendor source changed, no new mesh/material/texture assets, interiors, runtime scripts or lights. Nine simple collision bounds; both 3.8m-wide bay/approach envelopes remain clear. Static geometry,28 mesh renderers,9 LOD groups with130m cutoff. This is a bounded art addition, not a measured performance optimization.

Validation:
- Unity6000.3.19f1 / CLI1.0.0-beta.17 compilation: zero errors/warnings.
-16 production-camera starter/aspect cases passed (four models at1280x720,1280x800,1024x768,2560x1080).114 body/aspect corner-containment checks passed (38 bodies x3 aspects); Lobby/Main/Starter visibility retained. No real network peer claim.
- Native authoring repeated without duplicate roots. Reserved bay/approach checks passed each application. Decor.json confirms shared assets, all-static objects, no scripts/lights.
- Final.png is an actual Game-view capture. Final isolated Play frames advanced132→3785; current Console zero errors, with existing Metal memoryless depth warnings. No build or commit.
- Test profile was memory-only. Cleanup.json confirms original profile scope restored, Play stopped, saved scene clean, and real save/backup/temp hashes unchanged. Auto-generated font atlas changes were rolled back to their clean pre-task versions.
