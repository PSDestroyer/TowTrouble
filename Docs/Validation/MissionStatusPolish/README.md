# Compact mission status — 2026-11-06

The fixed 380×86 panel left too much vertical space and used a tiny unscaled flag. The new layout measures the actual title/action only when content or scale changes, adapts width between 260 and 440 reference pixels, and derives height from the action's preferred text height. Long instructions can still wrap. Standard single-line states are 62 px tall (28% shorter). The reported attach example is 375×62 with unchanged 14/18 px title/action fonts.

The pictogram is now 36 reference pixels and scales with accessibility text. A subtle divider separates it from the aligned two-line text block. Icon selection follows existing mission state: flag for choosing, car for travelling/returning, hook for attaching/reconnecting, winch for extraction, tree for obstacle work and finish flag for delivery. Navigation notices and projected waypoint clearance continue to use the measured panel height.

Unity compiled and rendered the production UI in temporary preview scenes. Twelve variants at 100% and 130% passed all 48 title/action overflow and line-limit checks; a standalone attach render passed two additional checks. Captures were visually inspected. Native scene state and protected preference keys stayed identical during rendering. No Play Mode scenario, physics test, Test Runner or build was needed for this presentation-only change. An early preview attempted before a deferred recompile returned a temporary reflection error; final previews ran against the compiled fields successfully.

![Compact attach panel](Attach.png)
