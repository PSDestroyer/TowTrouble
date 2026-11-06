# Persistent mission status — 2026-11-06

A compact, fixed upper-left plate displays the current recovery name and the next mission objective. It remains visible during gameplay independently of tutorial coaching.

- No mission: bound Tab/L3 key followed by `Choose a recovery`; clients see `Waiting for the host`.
- Travelling: `Reach the stranded vehicle`.
- At the site without a cable: `Attach your cable to the vehicle`.
- Connected/recovering: `Pull the vehicle free`.
- Obstacle missions use fallen-tree approach, cable attachment and clearance instructions.
- Delivery: `Tow the vehicle to the drop-off`; inside the destination the instruction becomes `Stop the recovered vehicle` at or above 2 m/s, then `Delivering the vehicle…` below 2 m/s. The authoritative completion hold is unchanged.
- Either driver's cable can represent the recovery effort in co-op. After completion/return to exploration the heading can announce `NEW RECOVERY AVAILABLE`, retaining the bound-key instruction.

RecoveryHudMissionStatus reuses the existing uGUI theme, at reference position (28,22), 380×86 pixels before accessibility scaling. Navigation notices stack below the status, and projected waypoints reserve both panels' vertical space. The old temporary bottom Tab reminder is removed to avoid duplicating the permanent objective. Garage and urgent cable/reset prompts remain contextual.

## Verification

Unity compiled successfully. A production-HUD preview matrix exercised twelve variants at 100% and 130% text scale: all 48 title/action measurements passed overflow and line-count checks. These include a rebound board key and client/obstacle/delivery states. Native scenes and protected profile keys were unchanged by the static previews. The first preview found two 100% overflows; shorter cooperative/obstacle text and a taller action area corrected them. Final measurements are in Layout.json.

The actual upper-left no-mission panel and its coexistence with the garage marker were visually confirmed in a running Solo game through the native Unity window. CLI status reported frameCount 1780 and playerLoopTicking true. Pipeline reconnection was delayed and produced transport errors. The Play session stopped before the planned isolated full-cycle probe could run; no live acceptance/extraction/delivery sequence is claimed for this change. No temporary profile was installed, no test contract was accepted, and no old profile backup was restored over current player data. Final Editor state was stopped with no compilation failure. Current Console errors were Pipeline transport failures, not identified HUD exceptions.

No build, controller-device test or second multiplayer peer was run.
