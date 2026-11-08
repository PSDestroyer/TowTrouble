# Inspector color grading — 2026-11-08

User requested artistic Saturation/Contrast controls only in Unity Inspector.

Select scene object `Tow County afternoon grade`, then Volume > Color Adjustments. Edit Saturation and Contrast in Edit Mode for persistent profile changes. Play Mode edits preview the instantiated runtime profile.

RecoveryAtmospherePresentation no longer overwrites those two parameters by default. Optional Automatic Color Grading retains the previous day/night policy. Disabling that option or the component restores values cached when automatic control began. Exposure, fog, weather, bloom and vignette ownership remain unchanged. No settings UI, save schema or network changes.

Validation: Unity 6000.3.19f1 / URP 17.3, actual isolated Solo Explore; all nine checks in Gameplay.json passed against both the instantiated profile and effective camera Volume stack. Manual positive/negative values survive day/night; optional automation and restoration/disable/reenable passed. Low.png and High.png were visually inspected; deliberately exaggerated values demonstrate effective grading and are not production defaults. Compilation succeeded. Final Console: zero errors; existing Account API warning and obsolete BetterFog displayName compiler warning observed.

Play stopped, scene clean, validation scope restored and real save artifacts hash-preserved (Cleanup.json). No platform build, actual second peer, Nintendo hardware or GPU performance validation.
