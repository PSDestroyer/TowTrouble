# SaveManager and input glyph integration — 2026-11-06

## Persistence

All active C# under Assets now uses the imported HalvaStudio.Save.SaveManager for game persistence. No old preference API calls remain. The profile DTO keeps version 4, its additive paint/control migrations, ownership receipts, builds, presets, paid run IDs, preferences, starter and completion masks. Co-op host address uses the same local store. Training remains non-persistent. Development validation uses independent in-memory stores before scene initialization, never save/restore writes over a real career.

Desktop and Editor use Application.persistentDataPath/save.json. An outer versioned document carries the JSON payload and its SHA-256 digest. Writes flush a temporary file, atomically replace the destination, and retain the previous valid document in .backup. Failed writes retain the newest pending state for retry. A corrupt, empty or unsupported save without a valid backup is protected from overwrite; the UI reports the failure. Original imported manager JSON is accepted and unknown fields are preserved.

The existing local career and its previous revision were exported before editing, migrated byte-for-byte, then independently loaded from the new file. Only the exact exported legacy keys are removed after verification. No DeleteAll or progress reset. A .migration-backup of the converted local save is retained beside it. No personal save content is committed to the repository.

Switch uses the imported native user-account/archive backend, with idempotent initialization, checked reads/writes/commit, handle cleanup and an exit-handling section closed in finally. The imported archive/file names remain compatible. Repeated requests coalesce over a five-second interval using real time; pause, focus loss and quit flush pending data. Data/journal allocations were zero; native PlayerSettings serialization now configures 16 MiB each and requires a startup account. Save DTO metadata is preserved for IL2CPP. A local profile is never replicated over FishNet. Each participating client claims and saves its own reward receipt; the host never replaces another client's inventory or settings.

FishNet's Editor fixed-timestep scratch value now uses Editor SessionState, while players retain their initial timestep in memory. This is temporary engine state, not career persistence. This narrow vendor patch must be retained when upgrading FishNet.

## Input presentation

137 glyphs share one compressed 2048x2048 atlas: 82 original keyboard keycaps and the Xbox, PlayStation and Nintendo exports supplied in PlatformSpecifics. The generic PlayStation d-pad is composed from its four supplied direction states. Original source assets remain intact; obsolete runtime copies were removed. Keyboard sources can be regenerated using Tools/UI/GenerateKeyboard.swift; rebuild the atlas using Tow Trouble > UI > Rebuild Input Glyph Atlas.

RecoveryInputDevices follows meaningful events from the local keyboard, mouse or gamepad. Stationary analog packets and small drift do not continually override keyboard hints. All gamepad consumers share the selected device. Reconnection/disconnection and changing between families update the hints. Known device identity takes precedence over old manual-family preferences; Switch players use Nintendo labels and button semantics. A generic unidentifiable pad falls back to Xbox conventions. A USB adapter that hides its real identity cannot be positively identified as Sony/Nintendo without device metadata.

Menus, starter selection, garage actions/tabs/filter/setup dialogs, recovery board, training/help, mission instructions, hitch/garage prompts, cable controls, results and notices share the atlas. Standalone caps preserve their aspect ratio independently of font metrics. Inline hints use a TMP preprocessor on explicitly opted-in text; normal descriptions and player names are untouched. Settings displays the connected controller's mapping or keyboard rebinding controls. The detail card explains the selected action rather than repeating the complete mapping.

## Evidence and limits

- Unity 6000.3.19f1 Editor compilation succeeded.
- 282 EditMode tests pass, covering existing gameplay and new save corruption/recovery/atomic replacement/retry/legacy import/independent peer reward receipts, device identity/selection and glyph coverage.
- Actual advancing Play Mode frames were checked; Solo host connected with local slot 0 and opened the garage. A recovery cable attached in Recover phase and displayed Nintendo winch/detach hints.
- Keyboard, virtual Xbox, virtual Sony and virtual Nintendo prompts were exercised. Final keyboard and PlayStation board captures have zero visible text overflow and the expected icon family only. Text scale 130% was exercised; a duplicate overflowing Controls description was corrected and rechecked.
- Test progress was isolated. Real career values were independently compared after stopping Play. Test-modified fonts and quality settings were restored to their pre-test versions; no scene was authored by the validation.
- No Switch SDK/module or devkit is installed here. Native console compilation, suspend/exit behavior and archive behavior still require validation in that environment. Native Switch settings were configured, but no console build or certification claim is made.
- No second network process or physical controllers were tested. Independent local career/reward persistence is covered by integration tests; a real two-peer/hardware pass remains required.
- No player build or commit was created.
