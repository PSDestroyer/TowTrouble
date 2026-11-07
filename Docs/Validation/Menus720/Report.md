# Menu readability at 1280 × 720

2026-11-07. Native Unity 6000.3.19f1 uGUI previews, using production builders and the project's actual fonts, thumbnails and input-glyph atlas.

## Changes

- Garage: larger standalone control glyphs for tabs, selection, rotation, tire filters and actions; readable category/filter labels, status badges, item names, prices and performance. The catalogue remains three columns with continuous scrolling. Body statistics use three readable lines. Auto-sizing now retains the authored size floor instead of shrinking again after Canvas layout.
- Starter choice: separated paint, description and confirmation rows. The confirmation action no longer covers the paint bar above the general footer.
- Main menu, first comfort settings, pause, credits and text-entry keyboard: tighter cards and spacing. Pause reserves its extra cable-information area only when attached. Settings help-card height accounts for the longest focus description on the current tab.
- Garage sale/overwrite/compatibility review: measured content height, consistent padding and readable action glyphs. Long compatibility reviews retain a bounded scrolling viewport.
- Recovery board and results: larger control hints and secondary information. Available retains three cards and the right-side description; Completed retains continuous scrolling.
- Co-op status and fitted-vehicle summary stay clear of bottom actions/footer. Long on-screen-keyboard drafts show the visible end and caret without reducing the font; the full draft is retained.

## Validation method and limits

Every preview uses a fresh disabled HUD/session fixture in an isolated Editor preview scene, with a memory-only profile (`SuppressSaving`). No buttons are activated, network sessions started, purchases made, contracts accepted or results paid out. Result previews call the presentation snapshot builder, not the completion/payout method.

The matrix uses keyboard, virtual Xbox, PlayStation and Nintendo devices at 100% and 130% text size. It covers title/main/new career, starter choice, first preferences, five settings tabs, co-op/lobby, three guide tabs, credits/notices, pause, confirmations, tutorial prompts/completion, on-screen keyboard including a long address, connection feedback, available/completed/empty recovery states, garage categories/modals and solo/co-op/client results. Device state and the preview scene are cleaned up afterwards; real save bytes and active-scene dirty state are checked.

`Checks.json` contains the final case results, text overflow and effective text-size checks, standalone glyph dimensions, and preservation checks. Representative unmodified 1280 × 720 renders are included alongside this report. These previews deliberately omit the world and car behind the UI. Clipping at scroll-viewport edges is intentional; text overflow inside an item is checked separately.

This is layout and presentation validation in the native Editor, not a new physical-controller, live network, driving or console-build test. The paused cable details' attached-state dimensions were adjusted in source; the matrix uses the disconnected/no-cable preview.

Final result: **328/328 unique presentation cases passed**, with 104 affected cases re-rendered after the final garage/board corrections. No enabled text overflow, no nonempty labels below 12 physical pixels in the tested scales, and standalone glyphs at least 25.6 pixels high. Compilation completed without errors or warnings. Real save bytes were unchanged and the active scene remained clean.
