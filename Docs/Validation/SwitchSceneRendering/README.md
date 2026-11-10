# Switch scene rendering and fixed graphics profile — 2026-11-10

The user measured approximately26 FPS after the first Switch GPU pass, using unchanged defaults, but found the reduced resolution too blurry. The mode and post-change GPU timing were not confirmed. This pass replaces the Switch Quality/Performance choices with one fixed, sharper profile and reduces environment rendering work. PC settings and visuals must remain unchanged. No builds were requested or performed.

## Current Switch profile

- Internal render scale0.8, replacing the previous default0.65. FSR1 upscale, sharpness0.75; URP falls back if the hardware does not support FSR. At1280×720 output this renders1024×576 instead of832×468. This is51.5% more scene pixels, not a measured performance improvement. Output resolution is still owned by the console. Overlay UI stays at output resolution.
- Existing Switch HDR/fog/sky/shadow/light budgets remain from the earlier pass: LDR, mobile sky/clouds, native Exp2 fog,512 hard shadow18m,2 additional per-pixel lights, depth retained for water, far clip600m, LOD bias0.65. No Forward+ or GPU Resident Drawer migration.
- Switch ignores legacy saved Quality/Performance and brightness preferences. Settings removes the entire Display tab, including Apply, resolution, mode and quality controls. Initial setup keeps text size without a brightness control. Audio, controls, accessibility and gameplay settings remain. PC retains its previous Display tab, brightness and performance mode.

## Environment rendering

The actual scene has15,660 active MeshRenderers and zero Terrain tree instances. Thus the previous terrain tree range did not limit the scene's GameObject trees. Native terrain instancing and SRP Batcher were already enabled. There is no baked occlusion data in the scene.

RecoverySwitchSceneRendering is created by DemoSession only on Switch (or an explicit scoped Editor graphics preview). It indexes eligible static environment meshes in the camera's scene once. Grass and foxtail meshes use60m, bushes110m, rocks250m, trees300m, measured to expanded group bounding spheres; native LOD/frustum handling continues for foliage. The actual map scan manages7,899 groups /13,892 renderers. Colliders, contracts, cars and other physics objects stay active. Render flags and rock LOD state are restored on disable, release and destruction.

Terrain occlusion checks48m spatial cells against TerrainCollider only, never against moving vehicles or trees. All eight expanded-bound corners and the centre must be blocked before the nearest cell face minus8m. Near cells within40m remain visible. Only two cells are tested per frame, at most18 terrain rays per terrain. A camera displacement greater than1m releases previously hidden cells before retesting. This is conservative, amortized relief occlusion, not an Umbra bake or GPU occlusion. Open flat areas will gain little from this stage; distances and LOD remain the main reduction there.

Rocks use explicit Graphics.RenderMeshInstanced while the rest retains SRP Batcher. Eligible LOD groups keep their meshes, transforms, material properties, layer/rendering-layer, shadows and final LOD cull thresholds. Batches are split by spatial cell and mesh/submesh/material/render settings; submissions contain at most128 matrices. The scene has2,064 eligible rock groups in the Editor. Unsupported hardware/materials, rigidbodies, per-renderer property overrides, lightmaps and static batches retain native rendering. Source objects and colliders remain. Tree fade materials are not instanced.

Nine Switch-owned material copies with instancing enabled are referenced by a Resources catalog, retaining instancing variants without editing vendor or PC materials. Refresh the catalog using **Tow Trouble > Platform > Refresh Switch Rock Instancing** after adding new rock material families; entries from other scenes are retained. Budget constants live in RecoverySwitchGraphics.cs. Reapply authored pipeline values using **Configure Switch Rendering Budget** after changing constants. These tools save only their owned assets; no scene save or blanket Save All.

## Verification

- Unity6000.3.19f1 compilation: no errors or warnings.23 direct Editor NUnit methods passed, without Test Runner or Play. Cases cover fixed Switch preference, PC mode preservation, quality/lens restoration, material/LOD ownership, physics and property-block exclusions, terrain occlusion fail-open behaviour, platform guards and button focus.
- Actual map initialization:286.6ms on this desktop Editor, not a Switch startup benchmark. One map-camera sample produced58 instances in24 instancing submissions; this is only the rock path, not total frame draw calls.6,525 groups were hidden by the initial distance/occlusion budget. Full source renderer and LOD states restored afterward.
- Native Settings construction: PC retains Display, Quality, Apply, Resolution and Window controls; Switch has none of those controls and redirects a stale Display tab to Audio.
- Garage and lake screenshots compare PC and the fixed Switch profile with the actual map, manually invoking presentation/render phases. Garage:17 rock instances /14 submissions; lake:21 /10. Active sky/water/rock shaders and render calls reported no errors. Source sky/cloud material JSON and references restored. These are desktop Editor previews, not console gameplay frames or timing measurements.
-18 protected PC/Mobile pipeline, weather and open-world scene files match the entry hashes. The original active scene and dirty state were preserved; no scene save, Play, package, player build, commit or push.

## Device retest

There is now one automatic Switch profile. Compare the same garage view after warm-up, then drive through forest, lake, snow/mountains and rain/night. Check clarity, foliage transitions and rocks while moving. Capture FPS plus GPU/CPU timings and the development log lines `[Tow Trouble Switch]` (output, scale, upscaler) and `[Tow Trouble Switch scene]` (managed renderers, instanced rock groups and hardware instancing support). The target remains stable30 FPS; no console measurement after this pass is available. Raising the render scale increases pixel cost, so the net gain must be measured on hardware.

References: [Unity GPU instancing and SRP Batcher](https://docs.unity3d.com/6000.0/Manual/SRPBatcher-Incompatible.html), [Unity6.3 Graphics shader variant retention](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/editor-settings-reference/comp-manager-group/class-graphics-settings), [GPU Resident Drawer requirements](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/in-urp/reduce-draw-calls-urp/reduce-rendering-work-on-cpu/gpu-resident-drawer).
