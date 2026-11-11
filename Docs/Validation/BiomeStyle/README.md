# Approved style extended to biome recoveries — 2026-11-11

Scope: R02–R24 in OpenWorldRecovery. R01 retained without modification. No builds.

## Changes

- Shared-by-biome copies of existing materials;55 new material assets, using existing texture references. No new renderers, geometry, colliders, lights or runtime scripts.
- Foliage uses the approved subtle desaturation/toning; lower saturation reduction for snow/coastal/desert palettes. Only renderer instances beneath identified recovery groups are rebound. Rock materials and the Switch rock-instancing catalog are untouched.
- Wheel impressions reuse the R01 stylized shader, subdued texture contrast, shallower visual channel shading and organic edges. Track meshes, physics and emission systems unchanged.
- Broad loose ground uses an isolated GroundBlend derivative. Native texture hue and terrain blending are preserved; biased mip sampling reduces fine detail without an extra texture sample. Existing main-light shadows are moderated. _TextureMean is retained as the internal name for the filtering-softness multiplier, not a replacement colour.
- Dynamic Living Mud keeps its original shader, displacement/stamp texture bindings and runtime session material setup. Normal strength.20, dynamic normal amount.85; forest smoothness.17, wet-bank smoothness.26. Slightly different earthy tints preserve wet/dry distinction.
- No new fullscreen pass, SSAO, render texture, normal map or texture duplication. The original nature assets/materials remain unchanged.

## Validation

Initial application compared map JSON, every terrain-height sample, mesh references, renderer/collider counts and original material serialization: all unchanged (Preservation.json). Every remaining recovery has styled renderers (Authoring.json).

Before/after galleries use the same native cutscene sampling,24 locations×50 samples, one displayed frame per location. SwitchPreview uses the actual Switch graphics preference inside Editor and restores it afterwards; this is not a device or frame-time test. Camera reports cover finite poses, correct final positions and source/map preservation. PC and Switch-profile renders checked visually; shader-error inspection occurs after rendering.

R02–R24 contract photographs regenerated with existing stable references and Switch compression. Scene saved after validation. No hardware FPS claim.

Menu: Tow Trouble / Art Integration / Apply Approved Biome Material Style. Reapply after regenerating underlying showcase materials. Each B<biome>_<GUID> material resolves its original source for repeatable re-authoring. Scene backup: /private/tmp/TowBiomeStyleBackup/Before.unity.
