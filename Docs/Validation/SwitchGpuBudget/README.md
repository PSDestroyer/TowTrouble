# Nintendo Switch GPU budget — 2026-11-10

User measured the installed game on the console's own screen. Visible log samples: 9.97 FPS / GPU98.39ms / CPU-render14.55ms and13.01 FPS / GPU76.56ms / CPU-render12.28ms. The earlier photo also contains GPU waits. These indicate a likely GPU bottleneck; CPU-render is not a complete main-thread profile. Target30 FPS has33.3ms per frame. No post-change console measurements are available, so stable30 FPS is not verified or guaranteed by this report.

## Dedicated Switch budget

| Setting | Quality | Performance |
|---|---:|---:|
| Internal render scale |0.65|0.60|
| Main shadow distance |18m|14m|
| Main shadow map |512|512|
| Cascades / MSAA |1 /1|1 /1|
| LOD bias |0.65|0.50|
| Terrain pixel error |6|6|
| Grass range /density |40m /0.4|40m /0.4|
| Terrain tree range /billboard start |300m /65m|300m /65m|
| Full-detail terrain trees |24|24|
| Terrain basemap distance |400m|400m|
| Camera far clip |600m|600m|

Switch_RPAsset disables HDR, probe blending/box projection/atlas, cookies, lens flares and LOD crossfade; retains depth texture for water/surface shaders, per-pixel headlights with2 additional lights/object, one hard shadow cascade and an LDR16 LUT. Bilinear upscale, Screen Space Overlay UI remains at output resolution. At1280×720 output, scale0.65 gives832×468 scene rendering, scale0.60 gives768×432. The platform still owns output-resolution changes (existing PerformanceMode behavior); desktop saved sizes are ignored and desktop resolution/window controls are hidden only on Switch. Output dimensions are logged for development retests. Relative to the earlier authored presets, pixel count is approximately34% lower in Quality and36% lower in Performance; these are pixel-count calculations, not measured FPS gains.

Switch_Renderer disables its own Better Fog feature. RecoveryAtmospherePresentation selects native Exp2 fog with the existing compiled fog mode, weather-tinted haze, no noise/bloom/vignette. COZY uses mobile cloud/sky shaders on2 owned runtime material copies and restores/releases them on destruction. Day/night, precipitation, audio, manual color controls, vehicle/collider physics, contracts, networking and saves remain unchanged. Existing full offline Switch transport remains in place.

UsesSwitchRendering is target-driven under UNITY_SWITCH; an independent Editor-only PreviewSwitchRendering supports scoped verification. Controller labels and multiplayer preview do not select graphics. PC/Mobile renderer assets and first-party weather materials remain unchanged;18 protected files including the open-world scene match their entry SHA256 hashes. PC still selects its original pipeline, Soft clouds/Desktop sky, HDR and Better Fog. Existing PC Performance behavior is retained.

SwitchGraphicsAuthoring.Configure (Tow Trouble > Platform > Configure Switch Rendering Budget) natively updates only the dedicated Switch pipeline/renderer and Switch quality level. ProjectSettings/QualitySettings.asset changes only that level. Because generic SaveAssetIfDirty did not persist the game-manager settings, the helper saves a detached native QualitySettings copy; it never uses Save All. A regression test reads the actual native file. Terrain/scene assets and GUIDs are unchanged. Constants are in RecoverySwitchGraphics.cs; rerun the authoring menu if changing authored budget values.

## Verification and limits

- Unity6000.3.19f1 compilation completed with0 errors/0 warnings; no player build or Test Runner.
-18 directly invoked Editor NUnit methods passed with no logged errors:10 graphics/persistence/policy/camera cases,3 existing platform guards,5 button-focus regressions. Includes repeated Quality/Performance toggles, immutable authored pipelines, settings/lens disposal and independence of graphics/offline previews.
- Two native Display UI checks passed: PC retains window/resolution selectors; Switch keeps brightness, quality and Apply with console-managed output.
- Six actual-scene native Editor renders (garage/lake ×PC/Switch Quality/Switch Performance) were visually inspected. Active sky/water shaders report no compilation errors. Each scope recorded no errors, original material JSON unchanged and sky references restored. These are desktop Editor previews with manually invoked presentation phases, not console or running-game performance tests. PC samples' raw global terrain settings are inactive when overrides=None; effective original scene values remain105m grass/700m trees.
- Final Editor returned to PC_RPAsset /2300m camera /Soft clouds /Desktop sky, with graphics/offline preview flags false and0 temporary materials/pipelines/roots. Original active scene and dirty state preserved; no scene save, Play, package change, commit or push. Console history was retained, including earlier corrected authoring/Inspector records.

## Console retest

Use the updated project through the user's usual deployment workflow. Start with Display > Performance. Compare60 seconds at the same garage view after warm-up, then forest, lake/water, snow/mountains and rain/night driving. Capture FPS and GPU/CPU timings for both modes; also capture the development-only `[Tow Trouble Switch] target=30fps output=... quality=Switch scale=...` line to confirm actual settings.30 FPS remains a target until these device timings confirm it. No Nintendo SDK shader compilation, GPU trace, actual controller driving, rainfall/night visual validation or console memory benchmark was performed by this change.
