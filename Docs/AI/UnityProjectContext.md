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
Authoritative design document: `/Users/gheorghe/Desktop/TowTrublw/TOW_TROUBLE.md`. Read it before further implementation. Required environment: **Stylized Nature & Environment MEGA PACK (So Stylized)**; road assets: **Low Poly Road Pack (Broken Vector)**. The earlier Low_Poly_Environment_World selection was an agent error and is not an approved substitute. The user's explicit FishNet choice overrides the design document's older undecided networking section. Vehicle reference is Hot Rod Constructor; verify local availability rather than inventing a substitute.

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
