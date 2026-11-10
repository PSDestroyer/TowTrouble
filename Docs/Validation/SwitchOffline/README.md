# Nintendo Switch fully offline session — 2026-11-10

Switch solo now uses an owned one-player in-process FishNet transport rather than UDP loopback. The gameplay authority, RPCs, SyncVars, simulation, weather, garage and contract rules run through the existing code. No sockets, DNS, NIC enumeration, network worker threads or remote authentication are used by RecoveryOfflineTransport. The shipped Yak files are empty stubs; they were neither activated nor edited. No new dependency or FishNet Pro feature is required.

## Changes

- RecoveryPlatform.OfflineOnly follows the existing Switch-only single-player policy. Scoped Editor preview also selects this path; desktop networking policy stays available.
- DemoSession prepares the transport in Awake at default execution order -32769, before FishNet NetworkManager's -32768 default. TransportManager receives the memory transport before FishNet subscribes to events. This happens on each scene load, including returning to the menu. The original authored Tugboat component remains uninitialized on the offline path.
- Host refuses any non-memory transport on an offline-only target and returns a local error before starting either manager. Transport setup returns before setting addresses, ports or bind interfaces.
- RecoveryOfflineTransport delivers only to player0, copies borrowed send buffers into pooled arrays, preserves channel/offset/order, queues at most256 packets per direction with1350-byte MTU, releases buffers on delivery/disconnect/disposal, and supports repeated starts/stops. Its client/server names are internal FishNet roles in one process, not network connections.
- ProjectSettings/ProjectSettings.asset disables switchSocketInitializeEnabled and switchNetworkInterfaceManagerInitializeEnabled. Applied through native Unity API and checked against persisted settings. No override asset containing conflicting values was found. No build target switch/build/SDK invocation.
- SaveManager already uses Nintendo local-user/filesystem storage. No Nintendo network-service account or online API is called by this integration. Its save schema and filesystem implementation are unchanged. UnityConnect/Analytics/cloud diagnostics were disabled; native effective EngineDiagnosticsSettings.enabled and Analytics.enabled read false. Editor services/package manager traffic is outside the game's transport and was not disabled.
- The sole additional lifecycle hook is Editor-only and reads TowTrouble.ValidationOffline from SessionState before scene Awake, enabling isolated testing across domain reload. Default/restored false. It is absent in the player.

## Validation

Unity6000.3.19f1 compiled without errors.15 directly invoked NUnit checks passed:7 offline transport/configuration cases,3 existing platform guards,5 graphics selection/restoration cases. Covers both channels, borrowed-buffer reuse, invalid client IDs, duplicate start/stop, disconnect during receipt, clean restart, early transport selection and fail-closed socket fallback. Tests use disposable inactive preview objects; no external transport was started.

Actual Play verification ran on the loaded map with standalone Editor emulating the Switch-only session policy, using the existing isolated in-memory profile facility. Confirmed:

1. Memory transport selected before initialization; original Tugboat.NetworkManager remained null.
2. Successful handshake into Explore, local slot0, exactly1 occupied player, no co-op, no configured port.
3. Open garage and commit current configuration.
4. Open contract board, accept forest contract0 and reach HookUp.
5. Request cable attachment through the real RPC route, reach Recover and observe Attached=true.
6. Local profile save/load roundtrip against isolated MemorySaveStorage.
7. Leave to main through normal scene reload, then start a second successful offline Explore session with original socket transport still uninitialized.
8. Stop Play, restore Editor/audio/frame/input/profile preview state, release isolated storage. Active scene and prior dirty state preserved; real save.json/backup/previous hashes unchanged.

An OS read during Play found0 UDP descriptors on the game's7777 port for the Editor process. Other Editor-owned discovery/debug sockets exist; this is not an assertion that the entire Unity Editor process makes no network traffic.

The first Play probe exposed the initially insufficient session execution order (-10000 versus FishNet -32768); it was corrected before the successful cold start/reload checks. Tests now compare the two actual execution-order attributes. Historical errors from that failed probe and stale Volume Inspector references are retained in Console; the tracker was rebuilt after final tests. Do not describe Console as empty or those historical records as current compilation failures.

## Limits

No Unity player build, Nintendo SDK binary validation, actual Switch airplane-mode test, traffic capture on hardware, or real desktop two-peer regression test was performed. The Editor's graphics target remained PC/60; actual UNITY_SWITCH policy source was separately compiled as a small C# policy-only check and selected offline-only/Switch/30. This was not a Unity player build. Desktop/other-player policy branches also passed. No scene/prefab/package/save-format changes, commit or push. User's untracked Nintendo legal/rating metadata preserved.

Relevant official architecture reference: [FishNet transport model](https://fish-networking.gitbook.io/docs/guides/high-level-overview/transports). Native diagnostics status checked against the installed Editor API; [EngineDiagnosticsSettings.enabled](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/enginediagnostics/enginediagnosticssettings/enabled).
