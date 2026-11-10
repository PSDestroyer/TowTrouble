# Cinematic recovery photographs — 2026-11-10

Replaced all 24 images currently bound to Recoveries (R01–R24) with native Unity captures of the actual authored recovery locations, vehicle models, paint and poses. No generated illustrations or invented obstacles. Existing unused tutorial/fallback images remain unchanged.

## Presentation

- Closer three-quarter views fit the vehicle within both the 774×178 contract header and 184×112 history thumbnail. Camera candidates account for terrain and nearby trees; level horizons retain the real vehicle pitch/roll.
- Supersampled 2560×1024 capture downsampled to 1280×512, SMAA, subtle depth of field, reduced photographic fog and gentle vehicle fill. These are temporary capture settings; gameplay grading and cameras are restored.
- Engine-failure captures use the existing smoke particle system with deterministic simulation. Other situation geometry comes directly from the map.
- Fixed the previous NPOT ToNearest import that converted 720×400 images into 512×512 textures. NPOT None preserves the new image proportions; no mipmaps/readable copy. Switch alone caps textures at1024 with ASTC6×6. Compression payload estimate is in Validation.json, not a measured RAM/FPS result.

## Integration and reruns

The original PNG paths and all 24 GUIDs remain intact, so no HUD rebind or scene save is required. `Tow Trouble/Recovery/Regenerate Cinematic Recovery Photos` regenerates established map photos through RecoveryPhotoAuthoring. Existing-map CaptureMapPhotos uses the same source-preserving workflow; initial map creation retains its legacy fallback until bindings exist. Only editor code executes the capture recipe; runtime uses static textures.

## Validation

- Native compilation: zero errors/warnings. Four six-image batches completed without logged errors.
- Invoked the actual private ContractPhoto method on inactive temporary HUD instances: all48 texture/UV bindings verified; all24 native PC imports are1280×512 and all24 Switch overrides match the intended settings.
- Inspected all24 detail and history crops in the contact sheets. Numeric silhouette bounds fit both crops for all24.
- All24 GUIDs match the pre-task copies. Fourteen protected scene/terrain/pipeline/quality/weather files remain SHA256-identical. Each capture batch verified unchanged Map/Casualty/Hud serialization and preserved scene/dirty state.
- Editor restored PC quality/pipeline, main/COZY camera, far clip2300 and source grading23/15/.2; no temporary photo objects remain. Source scene was already dirty and was not saved.
- No Play, Test Runner, network startup, player build, commit or push. No Switch hardware image/performance test.

## Evidence

- [New detail crops](After-Details.png), [new history crops](After-History.png), [full frames](After-Full.png), [previous detail crops](Before-Details.png)
- [Validation](Validation.json), [capture poses and protected hashes](Captures.json), [actual UI bindings](UiBindings.json)
