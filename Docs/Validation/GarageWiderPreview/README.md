# Wider garage preview — 2026-11-01

User requested the right catalogue be20% narrower so the centre car can be larger.

- Shared RecoveryGarageLayout reference dimensions: catalogue646→516.8px, its left edge918→1047.2; preview width538→667.2px (+24.0%). Right outer edge1564 and24px inter-column gap retained. Sidebar300px retained. All catalogue actions follow the narrowed column, while summary/statistics/presets follow the wider preview.
- Three columns and continuous vertical scrolling retained. Cards148.267px wide, same220px height. Image80px, title66px/three rows with selective14px minimum for long names, price/stat positions retained. Other UI titles keep their original typography rules.
- Camera focus centre follows the shared preview centre. Horizontal framing scales with the available width; body/paint overview uses slightly closer vertical framing. Existing particle/line/trail exclusion is retained.
- Validation:32 complete static UI layouts EN100/RO130, generated hierarchy width20% smaller, scroll/focus/restoration checks; all catalogue card labels separately measured in both variants;38bodies×5frames(190) fit in the open preview; two existing effect-bound/camera-transition regression methods passed through direct invocation in Edit Mode.
- Stock Body/Front/Rear static scene-backed captures use native garage surroundings and an in-memory Speedster clone. Neutral UI captures are layout-only (overview camera retained across categories). Native source driver renderers temporarily hidden during world captures and restored in finally.

No Play Mode was started by Codex, no Test Runner/physics or particle simulation/build. User entered Play Mode during the last checks; it was stopped under their earlier authorization to finish the Edit Mode layout checks. No scene/catalogue/save edits by this task. Backup Desktop/TowTrublw/Backups/GarageWiderPreview-20261101.
