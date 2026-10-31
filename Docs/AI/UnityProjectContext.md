# Tow Trouble — Unity project context

Analyzed 2026-10-30. Initially there was no Git repository. The user moved the project to `/Users/gheorghe/GitHub/TowTrouble`; current branch is main and baseline commit before Living Mud integration is 7063f1a.

## Confirmed baseline
- Root: `/Users/gheorghe/GitHub/TowTrouble` (renamed/moved by the user).
- Unity 6000.3.19f1, URP 17.3.0; active asset `PC_RPAsset`.
- Input System 1.19.0, activeInputHandler 1; uGUI 2.0.0; Test Framework 1.6.0.
- Pipeline 0.9.1-exp.1 connects to the running Editor. Use `unity command script eval` through the CLI; the MCP approval policy currently rejects direct calls.
- Original startup: `Assets/Scenes/SampleScene.unity`, clean, containing Main Camera, Directional Light, Global Volume.
- No first-party gameplay, networking, UI, assembly definitions, tests or repository instructions existed. `HubForceResolve.cs` is template bootstrap code.
- Editor reported no compilation errors before changes.

## Demo contract
Latest authoritative revision: `Docs/Design/TOW_TROUBLE-open-world.md` (approved 2026-10-31). It supersedes the earlier separate-zone/menu-first plan.

Historical design document: `/Users/gheorghe/Desktop/TowTrublw/TOW_TROUBLE.md`. Read it before further implementation. Required environment: **Stylized Nature & Environment MEGA PACK (So Stylized)**; road assets: **Low Poly Road Pack (Broken Vector)**. The earlier Low_Poly_Environment_World selection was an agent error and is not an approved substitute. The user's explicit FishNet choice overrides the design document's older undecided networking section. Vehicle reference is Hot Rod Constructor; verify local availability rather than inventing a substitute.

Create a playable stylized vehicle recovery demo from the supplied Tow Trouble images: canyon/forest, classic cars, tow cables, tension HUD, recover and deliver mission, host/join lobby using FishNet. Formula racing images are presentation references only.
Desktop keyboard/gamepad, two cooperative players, direct IP/LAN for the first demo. Host owns physics and mission outcome. Keep user source images and external asset library unchanged.

## Implementation conventions
First-party runtime in `Assets/TowTrouble/Runtime`, authoring tools in `Assets/TowTrouble/Editor`, authored content in `Assets/TowTrouble`. Namespace `TowTrouble`. Serialized references private where practical. World and scene generated through Unity Editor API, never hand-edited YAML. Imported assets retain their original GUIDs.

## Validation
Compile in the connected Editor, exercise lobby and mission in Play Mode, check advancing frames and Console, then use a second Unity Editor peer to verify host/client authority and synchronization where available. Do not create builds. Record actual results and limitations in `Docs/DemoGuide.md`.

Sources: ProjectVersion.txt, manifest.json, packages-lock.json, EditorBuildSettings.asset, ProjectSettings.asset, live Editor inspection, local asset catalog.

## Implemented demo
- Startup scene: `Assets/TowTrouble/Scenes/CanyonRecovery.unity`; original SampleScene retained.
- FishNet 4.7.2 installed locally under Assets/FishNet, Tugboat UDP 7777, two slots. Scene-only NetworkObject; no player prefab spawning. Optional Steam transport excluded.
- Confirmed/resolved `com.unity.modules.vehicles` 1.0.0 through Package Manager; the baseline backup already contains this module. Runtime uses a raycast suspension/traction solver; authored WheelColliders are disabled while playing.
- First-party runtime assembly `TowTrouble.Runtime`; tests `TowTrouble.Tests`. Runtime input is Input System; UI is generated uGUI/TMP with serialized font asset.
- Low_Poly_Environment_World (UA-0275) was an incorrect choice and has been deleted from Assets, together with its derived material and obsolete procedural prefabs.
- Required art now imported: So Stylized MEGA PACK; Broken Vector Low Poly Road Pack; purchased Slava Z Hot Rod Constructor products 303248 (Battle Cars, Assets/Hot_Rod_Oldtimers) and 346948 (Off-Road Cars, Assets/Hot_Rod_OffRoad). Downloaded through the connected Unity Package Manager.
- Authored So Stylized demo valley is copied and adapted with rotated terrain maps, trees and details. Source vendor assets remain unchanged. Startup scene path retains CanyonRecovery.unity, but the playable zone is Valea Verde.
- Three contracts (MD section 8), local versioned profile/progression, title/onboarding/main/contracts/vehicles/lobby/settings/pause/results/credits uGUI pages. Graphic logo extracted from the user banner via imagegen, stored locally.
- Four actual Hot Rod model choices: Scout (Historic_Taxi), Hauler (Historic_Villager), Buddy (Historic_Bulldog), extra Ranger (Goblin_Offroad), with cosmetic palettes. Original weapons are excluded from playable models.
- Current refit validation is separate under Docs/Validation/SpecRefit; older reports/builds describe the superseded prototype until replaced.
- Host owns physics and mission. SyncVars carry lobby/phase; 15 Hz snapshots interpolate clients; 20 Hz commands validate sender slots. No prediction, relay or Steam service.
- Current validation: 10 EditMode tests passed. Two Unity Editor peers completed a cooperative contract, independent input, disconnect cleanup and lobby reset. Current reports are under Docs/Validation/SpecRefit; earlier native reports describe the superseded prototype.
- Changed project configuration: EditorBuildSettings startup; PlayerSettings product/window/background settings; ShaderGraphSettings serialized by Unity; generated volume/URP scene settings. Package manifest/lock match the baseline backup after resolution. No Git repository was initialized.

## User constraint (latest)
The user explicitly said not to create builds. Do not invoke BuildPipeline or build run, do not launch native build validation. Validate through the connected Unity Editor and Play Mode. Any running build must be canceled through a supported cancellation path.

## Living Mud integration — 2026-10-31
- Living Mud URP 1.0 core surface, Mud03 textures, tire stamp and dependencies imported under Assets/Living Mud, preserving GUIDs. Demo scenes, original package manifest and optional VFX Graph content excluded.
- Tutorial contract 03 now uses Contract mud - Living Mud, a separate irregular surface/collider with native DMS material and batched wheel marks for both drivers and the casualty. Physics remains host-owned; see the later per-wheel traction update for resistance behavior.
- Integration assembly Assets/TowTrouble/Integrations/LivingMud; isolated session material/render targets; runtime cleanup; legacy vendor keyboard Update disabled on the surface component. DynamicMudSurface has one public RefreshReferences hook for rebinding session resources.
- The user tests the game. Do not enter Play Mode, run runtime tests or make player builds. Compilation and static Edit Mode inspection only. See Docs/Validation/LivingMud and Docs/DemoGuide.md.

- Wheel mud correction: native DMS Flip UV must be 1 for this mesh; stamp positions use physics hit UVs. RecoveryWheelMud uses the original Texture with Mud Shader Graph only on wheel renderer paint slots, with per-wheel MaterialPropertyBlock coverage. All model/color variants are wired in the tutorial. Static GPU/contact report: Docs/Validation/LivingMud/WheelContactFix.json. Runtime retest remains with the user.


## Mud traction and feedback — 2026-10-31
- Baseline for this update: 19d7a4f, initially clean. Tutorial only; do not alter vendor Mega/Desert scenes.
- RecoveryMudTerrain on the actual mud collider: longitudinal grip .78, lateral .60, resistance 375 per wheel scaled by mass, aggressive-throttle grip loss .42. RecoveryTraction holds force math; the existing host raycast solver limits combined drive/brake/side forces per wheel.
- Contract 03 casualty resistance 1700 is a minimum spread over four possible real mud contacts, no world-Z persistence. Contract 02 ditch drag remains as before. Dry wheel grip is now physically capped too; driving feel needs user validation.
- Per-wheel angular velocity/slip, input, mud fraction and engine effort are added to VehicleFrame snapshots at the existing 15 Hz. Both peers need matching code. Physics remains host-owned. Actual network play was not tested for this change.
- RecoveryWheelMud drives twelve authored manual mesh-particle emitters (28 particles each, playOnAwake false), coverage and native Living Mud tire shader. Existing engine audio responds to load/throttle/slip. Dust reduces with mud contact; contextual HUD hint follows tutorial setting.
- LivingMudAuthoring.ApplyPhysicsToTutorial reapplies bindings safely in Edit Mode; normal demo generation also includes them. No new package or audio library.
- Static force/scene report: Docs/Validation/LivingMud/PhysicsAndFeedback.json; all four models, friction ellipse, reverse symmetry, resistance direction/rest, 12 bound emitters, zero missing scripts/references. C# compiled. Do not claim driving or multiplayer validation: user owns Play Mode testing. No builds.


## Open-world systems, points 1–4 — 2026-10-31
- User approved implementing design/economy, Hot Rod catalog, functional garage and Tab contract board. Point 5 (Mega/Desert open-world biome authoring) is explicitly deferred. Baseline f6436ad, initially clean.
- Current authoritative revision: Docs/Design/TOW_TROUBLE-open-world.md, linked from historical source MD. Keep the existing tutorial flow. New Explore entry uses the current workshop yard and existing three missions; do not imply the large map exists.
- RecoveryGarageCatalog asset under Assets/TowTrouble/Garage, 380 entries / 16 slots; audit of 717 source prefabs in Docs/Design/HotRod-AssetAudit.csv. GarageAuthoring.Apply() generates sanitized derivative prefabs and binds components only in CanyonRecovery. Full assemblies have explicit wheel anchors/radii and structural socket metadata. Source packs and Mega/Desert scenes remain unchanged.
- RecoveryBuild and GaragePerformance contain configuration/rules. RecoveryCustomization rebuilds a sanitized visual assembly on configuration changes, binds geometry to the existing vehicle solver, uses isolated runtime materials and native Living Mud tire shader. RecoveryWheelMud rebinding follows GeometryVersion so new wheels still get mud.
- RecoveryProfile version 2 retains the v1 PlayerPrefs key, migrates old settings/progress/paint, credits historical completions once, and adds wallet/inventory/per-model builds/presets/paid run IDs. Previous profile is backed up. RecoveryEconomy owns pure buy/sell/trade-in/payout rules. No PlayerPrefs were modified during validation.
- RecoveryPhase.Explore appended after Complete, keeping old enum values. FreeRoam SyncVar selects the new flow. Garage at (16,0,92), input G / controller X; Tab / left-stick-click opens board. One active contract; host accepts and moves only the target, never players. A host-generated run ID deduplicates payout. Solo overlays pause; co-op overlays brake the local driver. Garage occupancy blocks accepting a contract.
- Identity/build RPCs validate sender slot, phase, parked garage access, known IDs and bounded JSON. No client-supplied physics coefficients. Wallet/owned parts are local cooperative progression, not a server economy. Both peers need matching code. Live multiplayer remains untested per user's deferral.
- Current verification: 14 pure Edit Mode assertions directly invoked (not Test Runner), 38 assembly configurations and multi-part kit checked in a temporary preview scene, no missing scripts/prefabs, static UI renders. Docs/Validation/Garage. User still owns Play Mode validation. No builds; never enter Play Mode or run runtime tests without new instruction.


## Logo-inspired UI finish — 2026-10-31
- Baseline 5dbc54a. Native uGUI/TMP theme: RecoveryPlateGraphic, RecoveryIconGraphic, RecoveryHudTheme; gold/charcoal/cream, rounded display type, focus/press feedback using unscaled time. Keep sparse reference HUD placement.
- Garage categories grouped Style / Tech / Towing; stable control names restore selection after rebuilding a page. Contract cards use posed static renders of actual tutorial geometry. No economy, networking or contract-rule changes. Garage camera starts in front of workshop and clips against environment obstacles.
- Derived OFL typography and licenses under Art/UI/Theme/Fonts; Romanian composites included. UiSkinAuthoring applies cached fonts/photos to CanyonRecovery through Editor APIs; DemoBuilder binds cached theme when available. Vendor Mega/Desert untouched.
- Verified compile, Console zero errors, two valid TMP fonts, 27 static page renders (RO100/RO130/EN130), two static HUD fixtures, 14 directly invoked pure Edit Mode assertions. Reports Docs/Validation/UI-Finish. No Play Mode, Test Runner, runtime tests or builds. User owns live interaction/controller/multiplayer validation.


## Open-world map, stage 5 — 2026-10-31
- User approved the 24-recovery / 8-delivery concept and authorized map creation. Baseline a7788dc. New authored scene Assets/TowTrouble/Scenes/OpenWorldRecovery.unity; original Mega, Desert and CanyonRecovery .unity files remain byte-identical to pre-authoring hashes.
- OpenWorldAuthoring and partial files generate a private 1 km² Mega-derived TerrainData, URP terrain material/layers, 68 road edges / 66 nodes, six biomes, river and two crossings, garage, signs, six Living Mud patches and 24 posed static photos. Asset sources stay in SoStylized and BrokenVector. Node C and sites R19/R23 adjusted to avoid crossing routes at different elevations.
- Editor/OpenWorldPlan.json is the stable graph/source; RecoveryMap serializes runtime positions and contracts in scene. Create guards existing map files; RefineRoads resets terrain/profile before FinishLandmarks; CaptureMapPhotos only poses visual clones in Edit Mode. Never run Play Mode or builds.
- RecoveryWorld.Map provides 24 AvailableContracts and map-relative extraction / altitude-aware delivery; tutorial All remains three contracts. Completion IDs 3–26 reserve distinct existing v2 bit positions. RecoveryEconomy.Claim overload accepts the authoritative contract; tutorial lastContract is not changed by world payouts.
- Scene travel routes Explore / co-op to OpenWorldRecovery; tutorial remains accessible from Main. Both scenes registered in EditorBuildSettings, preserved when DemoBuilder rebuilds tutorial. FishNet scene object uses unique scene ID 9104737622; state remains host-owned. Both peers need matching updated scene/code. Multiplayer deferred.
- RecoveryGroundSurface on sand/snow roads and terrain supplies local contact grip. tires_sand ×1.45 sand grip and tires_winter ×1.65 snow grip, existing mud tradeoffs retained. RecoveryVehicle uses map road-surface checkpoints for safe resets; deep lake water resets below 9.5m. No runtime feel/performance validation.
- Reports Docs/Validation/OpenWorld: 19 pure assertions directly invoked, road grade/clearance and ground checks, connectivity, source hashes, static Board RO/EN 130%. User owns driving, timing/economy and FishNet validation. Existing tutorial lighting assets were already dirty before map generation; preserve them. TMP dynamic atlases expanded for new labels.


## Solo startup initialization fix — 2026-10-31
- User reported failure to enter solo. Editor.log showed ResetVehicle → SetSpawn → RecoveryWorld.ResetRecovery, on the inactive casualty before Awake. Reproduced on an inactive StrandedClassic copy in Edit Mode: Rigidbody component existed but cached body was null.
- RecoveryVehicle.EnsureInitialized is idempotent and shared by Awake, SetSpawn, ResetVehicle and SetSimulation. The initialized flag is set before the initializer calls SetSimulation(false), avoiding recursion. Spawn state is written after initialization, and delayed Awake does not overwrite it. Inactive particle lookup includes children. No scene, prefab, visual or networking-flow edits for this fix.
- Five directly invoked Edit Mode regression assertions passed; Scout/Hauler/StrandedClassic initialization checks passed. Original isolated call succeeds. Docs/Validation/SoloStartup. No Play Mode, Test Runner, physics simulation, real profile writes, networking startup or builds. User retests Play → Explorează (already Host(true,true), automatic ready), then accepts R01.

## Open-world visual pass — 2026-10-31
- Latest user authorization: improve scene appearance. OpenWorldRecovery only; no Play Mode or builds. Solo initialization fix remains in place.
- New Editor partials OpenWorldVisuals/Scenery/WaterVisuals author 1025 terrain at explicitly fixed 1000m size, curved roads with separate 6-column visual / 2-column collision meshes, native texture/normal mapping and feathered shoulders, owned TerrainLayers and native material variants, workshop details/logo/Hot Rod spare tires, vegetation, horizon, smoothed water and timber bridges, separate Volume and native cloud dome.
- All assets under TowTrouble/OpenWorld/Visuals are owned derivatives. Vendor Mega/Desert and tutorial .unity hashes unchanged. Do not invoke legacy RefineRoads / FinishLandmarks / AlignRecoverySigns on the finished map: these recreate the old straight-road geometry/checkpoints. Visual authoring order is 1 then 2 then 4 then 5, then contract photos. Final roads are splatted into TerrainLayers; the 6-column overlay Renderers are disabled to avoid coplanar seams. Drive colliders remain enabled. The visual mesh is no longer two vertices across: geometric validation uses MeshCollider.sharedMesh.
- 24 sites / 8 dropoffs retained; 363 curved-road safe checkpoints. Static verification and before/after Camera.Render images: Docs/Validation/VisualPolish. User tests driving/performance/FishNet; no runtime assertion is inferred from these images.

## Integrated scene art improvements — 2026-10-31
- User authorized all improvements from the scene audit. OpenWorldRecovery now uses OpenWorldArtTerrain/Dressing/Workshop/Finish/Surface/Safety/Yard editor partials. ApplyCompleteArtPass is the ordered entry point; individual legacy generators can overwrite this composition and must not be run casually.
- Continuous graded landforms, curved/worn roads, irregular native Living Mud boundaries, shore planting, constrained biome groves, instanced ground cover, native distant mountains, workshop details/lighting and fitted signs. All derivative assets are owned under OpenWorld/Visuals; source scenes and tutorial .unity hashes unchanged. Sites/dropoffs serialization preserved.
- RecoveryGroundMap is a shared baked 512² byte grid of painted surface kinds, referenced by terrain and road RecoveryGroundSurface components. Sampling is allocation-free; absent maps retain the old behavior. RecoveryGroundMapTests plus existing OpenWorldRulesTests: seven pure Edit Mode checks passed by direct invocation.
- MeshColliders reduced 1912→600; duplicate colliders across distinct LOD renderers reduced 402 groups→0. No measured FPS claim. Static geometry checks: 68 road segments, max grade 17.29°, 1050 lane samples clear, 363 supported reset points, no missing scripts/shader errors in checked materials.
- Art captures, preservation hashes and validation: Docs/Validation/ArtIntegration. Native assets, tutorial, UI design, economy and FishNet authority preserved. 24 static contract photos refreshed. No Play Mode, Test Runner, runtime simulation or builds; user owns driving/performance/multiplayer validation.
- Pre-integration backup: /Users/gheorghe/Desktop/TowTrublw/Backups/TowCounty-before-art-integration-20261031.tar.gz. Existing uncommitted work retained; no commit/push.


## Road, river and off-road recovery corrections — 2026-10-31
- Latest user report: unnatural road embankments, targets/signs on the road, absent dynamic shallow wheel ruts, floating bridges/water, trees in the river and insufficient mountain rocks. Changes apply to OpenWorldRecovery; no Play Mode, simulation, Test Runner, networking startup or builds.
- OpenWorldRecoveryPullouts stores lateral offsets in OpenWorldPlan.json. All 24 target positions/headings and signs now use RecoveryPoint, while original road nodes stay connected. Minimum target-center road-edge clearance 14.59m; signs 8.23m. Mud patches, vignettes, access masks and photos follow new positions.
- OpenWorldHydrology solves a monotone river, carves its bed from an idempotent graded snapshot, builds four actual crossings with supported decks/ramps, and clips flat river faces against the shared lake polygon to eliminate overlap. Cascade uses native material with its own normalized UV mesh; lake/river use matching world UVs. RiverDressing clears the corridor and adds 678 mountain outcrop/scree objects.
- Supersedes older collision notes: legacy planar road MeshColliders are disabled; painted roads drive on TerrainCollider, bridges on authored decks/ramps. BakeGroundSurfaceMap now also bakes a dirt-impression coverage mask and grounds all 363 reset points on actual physics surfaces. Road slope paint excludes cliff faces.
- VisualAsset explicitly updates Mesh buffers through Mesh API (including UVs, indices, bounds and UploadMeshData) instead of CopySerialized alone, which left stale GPU geometry despite correct CPU vertices. GUIDs preserved.
- RecoveryWheelTracks uses real wheel hits and local bounded pooled strips (36 chunks ×128 segments), sand/snow/dirt materials and shallow visual relief. No permanent TerrainData excavation or network-authority changes. Living Mud still owns mud tire marks/coating. Teleports, model changes and surface gaps break strips; lobby resets release them.
- Docs/Validation/RoadRiverFixes: 18,213 road samples clear/supported, max sampled grade 22.05°, 363 supported reset points, 369 wet/downhill river samples, 4 connected bridges, 0 river trees/missing scripts/shader errors. Five pure assertions invoked directly and a 60-segment static geometry preview passed. Static capture evidence only; user owns runtime driving/performance/multiplayer testing. Tutorial and original Mega/Desert match Git HEAD.
- Backup: /Users/gheorghe/Desktop/TowTrublw/Backups/TowCounty-before-road-river-fixes-20261031.tar.gz. No commit/push. Keep all existing uncommitted work.


## Composed mountain gorge — 2026-10-31
- User approved implementing the cascade/mountain composition advice. Baseline 0b55b74; saved OpenWorldRecovery in Edit Mode. No Play Mode, builds, simulation, Test Runner or network startup.
- New OpenWorldGorgeComposition/Water partials: 3 drops, curved plunge pools with curvature-limited water width, 16 source-derived SoStylized cliffs/shelves, dry/wet/snow triplanar material copies, local erosion/paint and clustered foliage/talus. 3 URP curtain meshes, 9 foam patches, 3 runtime mist emitters capped at 72 particles total. No new runtime scripts or network changes.
- ComposeMountainGorge is the focused editor entry. ApplyCompleteArtPass calls RecomposeMountainGorge before bake/photos. Owned raw height/splat baselines make local reauthoring repeatable; downstream river uses the original graded baseline to avoid repeated ramp blending. Preserve or deliberately recapture baselines when redesigning terrain. Original scene/library assets stay intact.
- CaptureMapPhotosWhere supports refreshing only the affected north-map photos; public CaptureMapPhotos still captures all 24. Six photos refreshed this pass; no UI layout edits.
- Docs/Validation/GorgeComposition: 18,213 lane samples clear/supported, max grade 22.05°, 363 reset points supported, 440 wet/downhill river samples, four bridges, no centerline rock obstructions or river trees, no missing scripts/shader errors. Static camera previews include road-level views. User owns runtime VFX/driving/performance and FishNet validation.
- Tutorial, original Mega/Desert and OpenWorldPlan SHA256 unchanged. Backup /Users/gheorghe/Desktop/TowTrublw/Backups/TowCounty-before-geology-20261031-211003.tar.gz. No commit/push.
