# Recovery board — Available / Completed

2026-11-02. Narrow replacement of regional filters and pagination following the user's revert. The original three left cards and right detail geometry/content are preserved.

- **Available:** first three unfinished jobs in authored order; no paging or scrolling. A completion removes that job and fills its place from the remaining jobs. One/two remaining jobs and all-complete state are handled.
- **Completed:** all completed jobs in one vertically scrolling list, selectable for replay through the existing Accept Recovery action. No repeated/looping entries. History offset and each tab's selection are remembered.
- No new progression gates, prices, payouts, job requirements, map/scene changes, packages or save schema.

## Verification

- Five focused NUnit regression methods invoked directly in Edit Mode, without Test Runner: fresh queue; completion/history/refill; tutorial vs world completion IDs; final slots/all-complete; replay payout and duplicate-run protection.
- Layouts.json: 425 successful static assertions over 24 layouts: fresh, tutorial-only, one completion, non-contiguous completions, last two/one/zero available, zero/one/three/twelve/twenty-four completed; each EN100 and RO130.
- Native scroll hierarchy, original card/detail/photo dimensions, exact contract IDs, correct selected detail/reward, valid selection/empty accept disabled, scroll range, conditional scrollbar, keyboard focus first/last, offset restoration and no duplicate cards verified.
- PNGs are static Camera.Render captures in a temporary preview scene with a memory-only profile. Footer host guidance/disabled accept reflects the fixture's disconnected session; live host acceptance was not exercised.
- Original detail text/typography retained verbatim. The overflow field records the existing constrained description field, including some long descriptions that extend toward the bonus row. This patch does not redesign that panel.
- Compilation succeeded; current Console has no errors. No Play Mode, runtime/test-runner loop, physics/particle simulation, network session, build or saved-profile writes. Temporary scene and objects removed in finally.

User Play Mode check: complete an Available recovery, reopen board, confirm it moved to Completed and the next job entered Available. Scroll Completed, select a deep entry, confirm matching right details, accept/replay it; verify switching tabs and reopening preserve selection/offset.
