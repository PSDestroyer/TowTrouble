# Docked / handheld garage transitions — 2026-11-11

No production code changes needed during this follow-up. No build, Play, real console connection, scene save or user profile changes.

Code inspection: garage camera reads FOV/aspect/safe-area each pose calculation. HUD RefreshScreenLayout checks root size, pixel dimensions and safe area each Update, independent of timeScale. ApplyScreenLayout adjusts existing page transforms without Show/BuildGarage or selection changes. Switch does not apply PC profile resolution settings.

Native Editor simulation (Unity6000.3.19f1):
- Same camera/vehicle/session reused through1920x1080 ->1280x720 ->1920x1080 ->1280x720 ->1920x1080. All16 garage slots; first3 frames per transition checked:240 cases,0 failures (focus projection and camera outside vehicle body).
- Same open garage page reused through four resolution changes, full and inset safe areas. Assembly/Winch, en/ru/ja/ar, text100%/130%:128 UI cases,0 overflow, hierarchy/categorystate changes, page containment failures or logged errors.
- Native static captures of Russian130% UI at720p and1080p manually inspected. These UI-only preview scenes intentionally contain no car; camera tested separately.
- Active scene and its preexisting dirty state preserved. No runtime files edited for this request.

Limits: simulation drives native camera pixelRect/aspect and the HUD's existing ApplyScreenLayout entry point. Does not emulate Switch OS docking notifications, CanvasScaler update timing, transient Screen.safeArea values, graphics-device recreation, actual gamepad focus events or GPU/device behavior. Hardware retest should dock/undock repeatedly while garage open, while orbiting and selecting parts, and confirm focus/scroll/preview survive and returning to driving works.
