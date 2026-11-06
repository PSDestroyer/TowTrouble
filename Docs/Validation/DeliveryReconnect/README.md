# Lost delivery vehicle guidance — 2026-11-06

The Deliver phase previously kept both the mission instruction and route aimed at the drop-off after losing the cable. It now distinguishes travelling with the recovered car from returning to reconnect it.

- Outside the drop-off with neither player's cable attached, the route and amber car waypoint target the recovered vehicle's current position. The mission says `Return to the recovered vehicle`.
- Within the actual three-dimensional attachment range, the mission says `Reconnect your cable to the vehicle`; the existing hitch action shows the bound key followed by `Reconnect cable` (default F).
- Reconnecting restores the drop-off waypoint, route and `Tow the vehicle to the drop-off` instruction. Route caching explicitly changes its destination mode on both transitions.
- A partner's active cable still counts as towing. An unattached car already inside the delivery zone keeps the authoritative stop/delivering instructions. Extraction-release captions are not replayed while returning to an already-freed car.

Physics, attachment range, mission phase and completion rules are unchanged.

## Verification

Unity compiled. Seven NUnit methods were invoked directly in Edit Mode: three new reconnect regressions (ten assertions) and four existing delivery-guidance tests. No Test Runner execution was used.

Five production mission-status variants rendered at 100% and 130% text scale; all twenty title/action measurements fit. Native scene state and protected profile keys remained identical during preview rendering.

A live isolated stock Solo fixture advanced 474 real frames and passed nine checks (Live.json): normal F connection and supported extraction into Deliver, F detachment producing reconnect status, the actual nearby F prompt, return instructions at distance, route/waypoint targeting the car, return to the nearby reconnect instruction, actual F reconnection restoring the drop-off route, and a real physics cable overload returning to car recovery guidance. Poses, constraints and the camera were controlled; this was not a manually driven route. Near.png and Far.png were visually inspected.

Final live Console had zero errors and no compilation failure (three warnings). Play was stopped. Post-Play verification confirmed exact protected preference keys (`keysIdentical: true`, zero keys needing restoration). The full OS plist hash differed; no whole-file identity is claimed. No test completion, purchase or build was performed. Controller and multiplayer peers were not exercised; partner-connected behavior is covered by the shared towing predicate/rule check only.
