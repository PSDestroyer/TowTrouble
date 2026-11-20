# Winter waypoint ownership — 2026-11-20

Reported symptom: a plow fitting-point caption says414m while its badge sits above the nearby garage.

Reproduced in isolated Play Mode by showing garage guidance at the main garage, then accepting Hidden Bay Access. RefreshExpedition displayed the correct413m route distance and projected the remote destination. The pre-render callback still had garage ownership, so it replaced that projection with the garage position: displacement1,077.99 canvas units. This was a regression after adding garage guidance to Winter free roam.

The active expedition now registers ownership of the shared waypoint and the pre-render callback projects its current guidance point after the camera moves. The garage flag is cleared when the expedition takes over. The classic rendering branch is unchanged.

After the fix, the same caption remains413m and the pre-render displacement is0. Another68 positioning checks passed:27 jobs, three surveys outbound and returning, each with a fixed and moved camera, plus a regional-service-to-job transition. The main garage marker resumed correctly after abandonment. These are HUD/state checks, not mission playthroughs.

Compilation passed. Existing Console localization errors remain. No scenes, saves, packages or mission balance were changed by this fix. Real save hashes were unchanged, Play Mode stopped and the initial scene restored. The temporary background-running setting was reverted. No build was made. Results: [Checks.json](Checks.json).

Manual regression: begin beside the garage with no active job; accept a distant plow job; close the journal; rotate the camera and confirm that the badge and edge arrow point toward the worksite; abandon and confirm that the garage marker resumes. Repeat from the restored regional service. Investigation followed unity-bug-investigation evidence, hypothesis, baseline and UI guidance.
