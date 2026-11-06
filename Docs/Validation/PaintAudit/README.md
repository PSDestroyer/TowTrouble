# Paint audit and fixes — 2026-11-06

## Confirmed faults

1. The starter stored its selected color only in `RecoveryProfile.paint`, while garage normalization inserted `stock_Paint`. Customization then replaced that color with paint index zero. Reproduced by previewing a Mustard Scout, confirming it, and inspecting its fitted materials: the preview used a Mustard atlas, but the fitted car used the red source atlas multiplied by a Cream tint. The finish was also absent from ownership.
2. Customization multiplied the entire main material by the paint tint. That atlas includes body paint, dark trim, metal, wood and other details. This unnecessarily recolored/darkened non-painted regions and differed from the starter preview.
3. Network presentation cached only build JSON. A changed fallback color or base model with the same JSON could retain an old appearance. Both derived values now invalidate the appearance cache.

## Current behavior

- Starter confirmation atomically grants exactly one car and its selected finish, without spending credits. The chosen finish is fitted and, for Cream/Olive/Mustard, owned for later free refitting. Other finishes retain the existing 100 CR purchase price.
- The fitted build is authoritative for paint. An explicitly fitted Stock finish wins over a legacy fallback. Missing legacy paint can still use its fallback color. A one-time additive profile repair recovers the old globally selected starter color when no explicit paid finish was fitted, preserving money, progress and existing explicit finishes. `paintSelectionRevision` prevents a later intentional Stock selection being overwritten again.
- Preview, confirmed starter, Main, garage and deployed player cars use the same customization renderer. Browsing remains temporary. Leaving restores the fitted finish; Buy & Fit charges once; owned finishes refit free; saved setups include their finish.
- Eight owned texture variants cover both vendor atlas families. Only the documented upper-left paint strip is recolored. Glass, decals and trim pixels remain authored. Original source materials/textures are never edited. Runtime paint uses these shared baked textures, with no texture readback/baking during play; identical materials are reused within each fitted vehicle.
- Stock/Original on a starter means that model's default finish. On a purchased body it means that body's factory texture. Consequently an Original finish can visually match one of the named colors on a starter; the mode still follows the factory finish when changing bodies. Starter and garage swatches now use the same palette data.
- Paint has no effect on pulling force, mass, speed, grip, cable or winch performance. Cosmetic paint alone does not label a fresh starter as mechanically Modified on Main.
- Practice purchases remain isolated and career paint/body/inventory/money are restored on exit.

## Evidence

- Unity 6000.3.19f1, OpenWorldRecovery; final compilation passed.
- Actual Unity Test Runner: **266 passed / 0 failed / 0 skipped**, including **23 new paint tests** covering all 16 starter/model-color combinations, legacy recovery, explicit finish preservation, preview/purchase/refit/setups, asset coverage, paint-mask boundaries and unchanged performance across all body/paint combinations.
- Atlas GPU readback comparison: **zero changed bytes outside the paint strip** for all eight textures, with 49,152 changed paint-region pixels in each. The existing vendor atlas/material files were not reauthored (`atlas-validation.json`).
- Live material/rendering matrix: **164 combinations** = four starters × four finishes plus 37 purchasable bodies × four finishes. Main body textures, neutral base tint, glass alpha, decal textures and bounded material instances checked. Maximum owned material instances observed per fitted build: four (`live.json`).
- Live flow: 1,524 advancing frames, 19 checks passed for preview-to-confirm equivalence, ownership, save/load, Solo deployment, unbought preview, exit reversion, real 100 CR purchase, free preset/refit, purchased body color preservation, factory reset, Main reload and Solo rejoin. No runtime Error/Exception events captured.
- Controlled replicated-field presentation checks passed for both slots, including color changes with unchanged build JSON. **This is receiver/derived-state coverage in one Editor, not a two-peer network test.** Two actual connected peers, late join and latency remain unverified.
- Actual practice garage purchase applied Olive, then ending practice restored the career's purchased body, Mustard finish, inventory, money and saved finish (`practice.json`).
- Screenshots are actual Game-view captures of an isolated profile. Pixels/material values were inspected in addition to visual captures.

## Scope and cleanup

Player paint/customization was audited; rescue targets retain their intentionally separate authored palette and map colors. No mission/equipment balance or art source assets were changed. The real profile was backed up privately before testing and compared exactly afterward; no real save was reset or replaced. Validation keys and input/runtime overrides are cleaned and Play Mode stopped (`cleanup.json`). No build or commit.

Owned palette regeneration, if source atlases change: `Tow Trouble → Rebuild Vehicle Paint Palette` in Edit Mode, implemented by `RecoveryPaintAuthoring.Bake`. It writes only the owned paint resources and palette asset, without regenerating scenes or the garage catalogue.
