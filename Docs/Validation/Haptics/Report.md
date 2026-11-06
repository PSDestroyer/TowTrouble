# Tow Trouble haptics — 2026-11-07

The old fixed 80%-load vibration is replaced by a single local mixer, evaluated every rendered frame. This is presentation only: no handling, recovery resistance, prices, rewards or cable limits were changed.

| Situation | Feedback |
|---|---|
| Ordinary driving / slack cable | Silent |
| Cable load | Smooth low-frequency ramp above 40% load |
| Cable risk | Distinct high-frequency pulses above 84% load |
| Winch winding / paying out | Light motor feedback |
| Winch stalled under load | 4 Hz pulses |
| Connect / detach | Different short ticks, 75 / 55 ms |
| Cable snaps | Stronger 190 ms impulse; priority over impact and detach |
| Vehicle extracted | Two gentle pulses over 250 ms, for attached participants |
| Meaningful collision | Short impulse scaled by normal closing speed and velocity change |

The mixer uses maximum composition rather than adding multiple effects. Motor speeds are capped at 0.35 / 0.25 before the player's gain. Default intensity is 70%; Settings > Game exposes Vibration intensity plus the existing on/off switch. The additive profile field is saved by SaveManager, copied with comfort preferences, retained when resetting progress and restored to 70% by Restore Tab Defaults. The new Game panel was visually checked at 130% text scale; no visible text overflow was reported.

Haptics stops on menus, pause, keyboard/mouse takeover, app focus loss/pause, disconnect/disable, device change, session/component shutdown and exit. The actual selected local Gamepad is used; no global ResetHaptics affects another player's devices. Device lifecycle events invalidate cached output even if a disconnect/reconnect occurs between rendered frames. Zero output bypasses the 30 Hz command throttle. Pulse history is discarded instead of being replayed after resuming.

The server records per-driver attach/detach/break and impact counters in the existing 15 Hz RecoveryFrame snapshot. A sequence number rejects duplicate/out-of-order cosmetic telemetry, including counter wrap. Clients wait for an initial telemetry baseline; new runs, local-slot changes and resets do not replay historical events. Remote peers use their own local settings/device. Both peers need the updated snapshot schema. No new event RPC or haptic save state was introduced.

Collision feedback is sampled only for simulated driver vehicles in OnCollisionEnter. It requires at least 2.5 m/s normal closing speed and 0.75 m/s impulse/mass velocity change, with a 300 ms cooldown. Spawning/resetting suppresses impact feedback for 500 ms. This filters minor wheel/road contacts and persistent scrapes.

## Validation

- Unity 6000.3.19f1 compilation passed. Full Editor tests: see tests.json; existing gameplay/economy/save tests plus deterministic haptic mixing, event priority, latency/baseline handling, intensity, motor ownership, disabling/reconnecting and immediate stop tests.
- Advancing Play Mode, isolated profile, Solo host/local slot 0, simulated Gamepad with instrumented motor output.
- Actual cable attach/detach/reattach and server physics rupture generated distinct command bursts and then zero output. Native counters progressed Attached 1→2, Detached 0→1, Broken 0→1.
- An actual collision with a temporary wall produced Impact 1, normalized strength about 0.987 and a short motor burst, then zero.
- Under a deliberately injected sustained-load/stall state, focus loss and pause stopped both motors. The injected state tests lifecycle safety; it is not evidence of physical winch balance.
- Settings slider set to 35% through the real uGUI Slider callback, saved and reloaded from the isolated SaveManager store. Settings at 130% text scale had no overflowing visible text.
- Real save bytes were compared after cleanup; test-created scene objects and modified presentation assets were not retained. Play Mode stopped. No build or commit.

## Hardware/platform limits

The output uses Gamepad.SetMotorSpeeds/ResetHaptics so installed device-specific Input System implementations can handle Xbox, PlayStation and console Nintendo devices. It does not implement adaptive triggers or a separate Nintendo HD-rumble protocol. The installed desktop SwitchProControllerHID implementation does not supply its own rumble output. Nintendo console haptics requires the console-specific input package and SDK/devkit validation. Controller family detection alone cannot guarantee rumble for every OS/driver/connection.

Physical controllers and two independent multiplayer processes were not tested here. The native test used a simulated Gamepad; waveform feel and target hardware still need a hands-on pass. Unity's [Input System 1.19 rumble documentation](https://docs.unity.cn/Packages/com.unity.inputsystem@1.19/manual/Gamepad.html#rumble) describes the platform/device restrictions.
