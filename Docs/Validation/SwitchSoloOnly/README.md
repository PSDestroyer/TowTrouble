# Nintendo Switch: solo only — 2026-11-09

RecoveryPlatform uses UNITY_SWITCH to disable multiplayer automatically, including Editor compilation targeting Switch. Other players retain multiplayer; controller glyph family does not determine platform capability. PreviewSinglePlayerOnly exists only in the Editor for scoped verification and is restored after each check.

Main menu omits PLAY CO-OP and closes the gap; title says SOLO. Show redirects Co-op/Lobby/address keyboard to Main; direct co-op/lobby builders also refuse construction. Disconnect navigation and solo failure/timeout messages do not expose host/IP/port advice on Switch. Co-op crew HUD and signals are disabled through the same policy.

Host(false) and Join return before profile/network access. Solo retains the existing FishNet local-host architecture. Switch host configuration uses maximum1 client, IPv4 loopback127.0.0.1 and IPv6 loopback::1. RecoveryWorld rejects non-local client loading and refuses cooperative configuration. No FishNet removal, package/scene/settings/save schema changes.

Validation: Unity C# compilation passed;3 directly invoked Editor checks passed;8 menu-builder comparisons verified Switch/desktop Main, Title, Co-op and Lobby. Temporary preview sessions remained inactive with memory-only profiles, so Awake/profile loading/settings were not invoked. Main PNGs show isolated UI layout; footer input glyphs were not fully resolved in this static fixture and are not device-rendering evidence. Tugboat configuration check confirmed loopback/max1 with both endpoints stopped; no server/client startup. A tiny standalone C# policy harness checked the actual UNITY_SWITCH branch=false and desktop branch=true without a Unity player build or Switch SDK.

No Play Mode, Test Runner, real multiplayer, Nintendo hardware test or player build was run. Console/platform-specific socket backend behavior and certification are not validated. User can select Switch as the active build target when its SDK/module is installed; no separate game setting is required. Restore desktop target to show desktop multiplayer again.
