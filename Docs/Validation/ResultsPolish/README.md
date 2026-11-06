# Recovery results presentation — 2026-11-06

The previous result modal placed three unexplained stars above a single statistics paragraph and left a large empty footer. The new card separates achievement criteria, payout, run statistics and the next action.

- Three labelled objective cards: Recovered, No resets, No cable breaks. Existing earned conditions are retained; unavailable bonuses are muted and labelled Bonus missed.
- A warm paper payout card emphasizes the actual credited reward, with the wallet balance beneath it. A separate row displays base reward and total bonuses. The first-recovery note is shown only when the real payout matches the existing economy's first-recovery calculation; no profile flag or economy rule is modified for presentation.
- A compact statistics column displays elapsed time, resets and cable breaks.
- A contextual banner shows the delivery destination or a newly available recovery. Crew contributions are retained in cooperative results.
- Open-world results keep one primary Continue Exploring action and a keyboard/controller hint. Clients retain the disabled action and a host-waiting explanation. Tutorial next/replay/contracts/vehicle controls keep their existing handlers and host guards.

RecoveryHudResults owns a private presentation snapshot and card renderer. BuildResults still invokes the original RecordCompletion entry point before taking that snapshot. The preview renderer can therefore be exercised without calling RecordCompletion or saving a profile. The open-world solo card is 840×606 reference pixels; cooperative and tutorial cards accommodate their additional content.

## Verification

Unity compiled and rendered the production result card in temporary preview scenes. Eight scenarios at 100% and 130% text scale covered perfect repeat/first rewards, missed/partial bonuses, expert reward and billion-credit balance, cooperative long names, client action gating and tutorial actions. All 344 text overflow checks and sixteen button-count/host-gating/on-screen layout summaries passed. Native scenes and protected profile values remained identical.

The final banner alignment was then rendered in a standalone card preview: all21 text checks passed and scene/profile state stayed identical. Final.png was visually inspected, along with normal and large-text full layouts. The final capture's tool response timed out, but the completed image and FinalLayout.json independently record successful rendering and cleanup; no duplicate capture mutation was issued.

No Play Mode completion/payment was triggered for this presentation-only update, no Test Runner or build was used, and button handlers were inspected rather than exercised live. Source payout, completion and authority rules are unchanged.

![Final production-UI preview](Final.png)
