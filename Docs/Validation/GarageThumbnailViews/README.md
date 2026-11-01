# Garage thumbnail exposed-face correction — 2026-11-01

User screenshots showed wheel backs, rear views of bodies, open rear sides of bonnets and inside faces of front bumpers.

## Confirmed cause

GarageThumbnailAuthoring used one world-space camera direction (+X,+Y,-Z) for every native prefab. The sanitized assemblies and front accessories face+Z; wheel rim details face-X. AngleSurvey.png compares opposite/front/side cameras on the reported assets. AccessoryAngleSurvey.png checks exhausts, engines, roof accessories and chassis independently.

## Fix

The generator now selects a category-specific exposed-face view. Wheels face the rim from-X with a small tread/elevation angle. Bodies and bonnets use a front-left three-quarter view from-Z's opposite (+Z,-X). Front bumpers look toward their front finish; rear bumpers look toward the rear exterior. Fenders/exhausts choose the named left/right side; roof/chassis/engine views are more elevated. The instance transform accounts for source scale/reflection. Key/fill lights follow the chosen view so the corrected surfaces remain lit.

350 unique native renders were regenerated into the same four2048² atlas files, serving355 visual catalogue entries. Stock/technical vector icons are unchanged. Per-category contact sheets and Views.json record every result. No runtime cameras, geometry, physics, stats or pricing changes.

## Validation

- All10 native visual categories inspected as contact sheets; reported Bucket/Bug front wheels now show rim/hub details, Bulldog body/bonnet show grille/headlights, front plates show their finished outside face.
-16 actual static garage grids rendered and checked; price ordering preserved, zero text overflow, final row focus still valid.
- Catalog and scene files are byte-identical to the backup; atlas metadata/GUIDs and UV bindings preserved. Only cached image pixels and editor authoring logic change.
- Connected Editor compiles without errors. No Play Mode entry, physics/particles simulation, Test Runner, networking startup, builds or direct profile writes. The user's active test was stopped under their existing authorization.

Backup: /Users/gheorghe/Desktop/TowTrublw/Backups/GarageThumbnailViews-20261101.
