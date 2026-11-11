# R01 stylized material study — 2026-11-11

Applied and saved in OpenWorldRecovery; R01 contract photograph regenerated. No builds.

- R01-only Wheel Impressions shader derivative reduces soil texture detail to22%, softens existing main-light shadow contribution to72%, retains directional lighting/fog and organic edges. No new normal maps, screen-space effects or additional lights.
- Bed and banks use muted brown/olive tones, shallower visual relief and a clearer compacted bed. Clods use plain matte colour rather than a detailed albedo.
- Nine local foliage material copies, shared textures unchanged. Relevant shader colour parameters are desaturated12% and slightly toned down; original SoStylized materials and other recovery sites are untouched.
- One feathered terrain-following floor mesh,625vertices/1152triangles, one renderer with distance culling. No collider or runtime update component. Local shader keeps the existing single-pass transparent architecture, adds no screen passes.
- Terrain heights, collider count and map serialization verified unchanged. Wheel mesh bounds at the final cinematic pose overlap the sampled ground by~0.17–0.20m; this is a visual bounds check, not a tire-contact solver test. Vehicle pose/suspension unchanged. Contact improvement is material contrast/edge treatment, not new contact-shadow technology.

Before.png and After.png use the same native cinematic camera sampling (R01 only,50samples/fourframes). SwitchPreview.png uses RecoveryGraphicsPreference.Apply("Switch",false) in Editor and restores the profile afterwards. This is not an on-device frame-time test. Shader messages empty after rendering, C# compiled, scene saved; source casualty/map preserved by preview checks. Existing BetterFog inspector cleanup exceptions on domain reload remain outside this change.

Menu: Tow Trouble / Art Integration / R01 Stylized Material Study. Run after base R01 showcase authoring if rebuilding its materials. Backups are in /private/tmp/TowR01StyleBackup. Comparison remains local; do not propagate the style to the rest of the map without reviewing it.
