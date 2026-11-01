# Garage catalogue grid — 2026-11-01

Implemented across the shared runtime garage UI, preserving the existing plate/gold/cream visual theme and the 3D vehicle preview. No Play Mode, Test Runner, physics/particle simulation, network startup, build or real profile writes were used. The user stopped Play Mode before project changes were applied.

## Result

- Customize sidebar widened from 246 to 300 reference pixels. Style / Tech / Towing tabs are wider and never wrap; category buttons, back button and workshop hints share the extra width.
- Every one of the 16 slots uses the same unpaginated 3-column grid. The catalogue panel grows from 540 to 646 pixels; all compatible items remain accessible by vertical wheel/drag/scrollbar or keyboard/controller focus.
- Cards show native part thumbnail or a category icon / paint swatch, name, actual trade-in-aware price or stock/owned status, and an equipped check. Relevant modifiers advertise force percentage, tire grip, clearance, mass/track width, cable strength or winch force/speed. Cosmetic items make no power claim.
- 355 visual catalogue entries use 350 unique baked renders, deduplicated into four 2048² cached atlas textures. There are no extra cameras or 3D thumbnail renders during gameplay. The 29 stock/technical/paint items without geometry use vector category icons and paint swatches.
- Names get two lines with readable minimum sizes; duplicate cable/winch capacities are placed in the performance line instead of repeated in the title. The selected description area is taller, and stat/preset tiles fit the narrower centre column.
- Scroll position survives selection and page rebuild. New categories start at the top. The scrollbar is shown only for overflowing catalogues. Keyboard/controller selection brings the full card into view; pointer selection does not move the target away before its click.
- Grid children keep layout-owned positions: the existing button feedback still highlights/presses them but no longer moves them back to their pre-layout origin.

## Integration / preservation

Runtime: RecoveryHudGarage / RecoveryHudGarageGrid, RecoveryGarageCardFocus, RecoveryGarageCardInfo, RecoveryUiFeedback. GaragePart adds optional Thumbnail / ThumbnailUV fields with safe defaults. Catalog reconfiguration retains cached images by stable part ID. Editor authoring: Tow Trouble → Garage → Bake Part Thumbnails.

All prior catalogue gameplay fields, prices, part IDs, performance coefficients, prefabs and sockets are byte-equivalent after removing only the new thumbnail fields. The OpenWorldRecovery scene file is unchanged. Purchase / ownership / sale / presets / commit / vehicle preview behavior reuses the existing session and economy methods. No progression or save format changes.

## Verification

- Connected Unity Editor compiles with no errors.
- 32 isolated static UI fixtures: all 16 slots in EN100% and RO130%. No text overflow; correct three-column grid / masked viewport; every last row can be fully focused and the body catalogue retains its scroll on rebuild. Largest compatible category in the fixture: 87 cards.
- 20 direct Edit Mode checks passed (new focus / honest card stats / cached icon preservation, plus existing garage and economy regression checks). No Test Runner or live profile writes.
- All 355 thumbnail bindings have valid in-bounds atlas UVs. Static Body, Wheels, Engine, Winch and bottom-of-list images inspected.
- No measured FPS claim. Live mouse-wheel/drag, gamepad input, car rotation and buy/fit/sell interactions remain for the user to test in Play Mode.

Backup: /Users/gheorghe/Desktop/TowTrublw/Backups/GarageGrid-20261101.
