# Unified lighting, COZY, Better Fog and Global Volume — 2026-11-07

Unity6000.3.19f1 / URP17.3, COZY3.6.2. Better Fog was initially imported only as common assets; the supplied URP6 unitypackage was imported natively. The user's other imports and existing palette edits were preserved.

## Final setup

- Existing global `Tow County afternoon grade` / `County afternoon volume.asset` retains brightness ownership in DemoSession. Tonemapping now Neutral; saturation8 daytime→5 night; contrast5→2; subtle bloom.12→.16 and vignette.065→.09. The existing user brightness/exposure setting is untouched.
- Existing COZY sphere owns time, sky, sun color and global forecast. Soft cloud shader replaces hard silhouettes. Stars22% of original brightness and constellation/galaxy intensity35%, retaining existing time curves. The scene Skydome was inactive at baseline; native authoring reactivates it so COZY SetupReferences succeeds.
- RecoveryAtmospherePresentation order220 runs after native COZY and the previous visibility floor. It uses the existing Volume.profile runtime clone. Better Fog alone owns geometry fog; Unity built-in fog is disabled to prevent doubling. WetAmount is independent of roof suppression; SnowRegionAmount uses the existing seven-second biome blend.
- Distance haze: Exp2 density.0008 clear→.00145 rainy, +.00012 snow, +.00016 morning. Foreground offset−70m, intensity.68→.80. Color follows available daylight/night and weather. Low height layer follows valley datum or locally blended snow altitude; low-frequency height-only noise. SSMS/scattering blur, custom depth and custom offset disabled. Performance additionally disables noise.
- Existing fill/sun and ambient floors reveal car/contact shadows without raising exposure: live non-title sun intensity.95, fill.25 day/.18 night, mild neutral/warm daylight and cool night fill. Title retains its warm sun pose. SSAO intensity.4→.26 and direct-light influence.25→.12.
- Both PC_Renderer and Mobile_Renderer contain one Better Fog feature before postprocessing. Mobile_RPAsset now requests camera depth, required by the effect. PC render scale/shadow presets remain unchanged; existing Performance pipeline continues its runtime copy/clamps.

## Integration ownership and vendor patch

First-party adapter/editor authoring lives in `Assets/TowTrouble/Integrations/BetterFog` under the predefined assemblies because the vendor has no asmdef; gameplay assemblies do not reference Assembly-CSharp. The existing COZY integration only exposes wet/snow weights. Menu: Tow Trouble / Visuals / Configure Weather Lighting and Better Fog. Rerunning updates owned overrides/features rather than adding duplicates. No interiors, additional light rigs, gameplay physics/economy, save or networking payload changes.

BetterFog-URP17.patch records necessary local vendor integration repairs. An unused runtime UnityEditor import is editor-guarded; custom-renderer lookup uses the current camera and is skipped when custom features are off. Fog requests and declares depth reads. Temporary copy/factor targets use graph-derived descriptors and graph ownership instead of persistent RTHandles, optional targets only exist when used, and all graph handles reset per frame/camera. Reimporting the vendor URP6 unitypackage overwrites this patch; reapply/revalidate it. The native vendor SMSS/custom-depth paths are retained but disabled and not tested.

## Validation

- Gameplay.json: 12 actual isolated Solo states passed. Workshop clear/cloud/rain/night, alpine snow/storm, lake dawn, desert, Performance day/night and explicit Mobile renderer day/night on the development Mac. Each renders1600×900 panorama, same-frame fog-on/off comparison, and1280×720 production-camera view. All states had a visible fog contribution, no detected magenta shader pixels, no overlapping built-in fog, correct snow/headlights, noise disabled for Performance. Frame counters advance in the results. Render dimensions are offscreen render targets, not claims about physical display resolution or platform builds.
- Menus.json: title/main camera lighting checked at1280×720, engine-off headlights0. Garage.json: actual garage body/front camera flows checked. Images omit screen-space overlay UI intentionally; this validates world lighting, not new UI layout.
- Lifecycle.json: three disable/enable cycles, VolumeProfile object count8→8, no shader compiler errors for the active fog/soft-cloud shaders. This is a bounded duplication check, not whole-game memory/GPU profiling.
- Saved setup restarted before final sweep. ConsoleSummary current errors0 / compilationFailed=false; Account API warning remains. Initial vendor compile also emitted an obsolete displayName warning on Unity6000.3.
- Cleanup: Play stopped, scene clean, real save/backup hashes unchanged and isolated profile scope restored. No build/commit, actual second peer, Nintendo hardware, GPU benchmark or exhaustive artistic camera review.

During authoring COZY ResetQuality initially failed because the inactive Skydome is not found by its native active-child search; activating the intended sky fixed it before runtime validation. A preliminary ten-state pass and cloud-style comparison selected the final Soft shader. Shader/package imports temporarily reloaded the Editor; tests waited for a ticking player loop. Extra BrokenVector import changes observed during the task were not edited or reverted.
