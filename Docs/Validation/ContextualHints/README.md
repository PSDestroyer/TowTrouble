# Contextual recovery guidance — 2026-11-05

All seven approved guidance additions are integrated into the existing HUD and original Available/Completed board layout. New player-facing text is English.

- Near a recovery hitch, a hook icon and `Move closer to attach` appear up to 26 m away. `E · Attach cable` appears only within the existing 14 m three-dimensional action range. Keys follow the profile/controller binding. The plate follows the actual tow point, including fallen-log targets.
- The first cable connection introduces `Hold Q to reel in` alongside the existing Q/icon/meter/icon/F controls for up to ten seconds, ending on actual winch use. While stationary, the attached cable can show the bound detach key. Warning/advice has priority over detaching.
- After two seconds of an unsuccessful pull, guidance distinguishes observed wheel slip, a stalled winch and inefficient extraction alignment. It offers firm ground, repositioning or a suggested direction. No automatic diagnosis of insufficient engine power is made from a stationary car alone.
- A real increase in the authoritative snap count produces a short `Cable snapped` or `Crew cable snapped` notice. First-time coaching explains steady pulling. Normal hitch guidance resumes afterwards.
- Board details compare the currently fitted build's surface grip and cable strength with the selected contract's authored material and blockage resistance. At most two equipment recommendations are shown. They are recommendations, not a simulation of slope/traction or a guarantee of success. Contract description text fits the existing detail area with local autosizing down to 14 px.
- The Tab recovery hint returns after completion and periodically during idle exploration (six-second display, sixty-second reminder interval). `New recovery available` appears only when completion adds a new entry to the next-three selection; replays and the final shrinking selection do not claim a new job. Garage guidance keeps priority nearby.
- After six seconds overturned at low speed, or trying to drive without meaningful displacement and without a connected cable, a contextual safe-point key appears. Normal parking, braking, useful movement and slow attached recoveries do not offer reset. The active run's no-reset bonus consequence is stated before use.

The hitch and contextual bottom hint fade over 0.16 seconds. Urgent snap/reset guidance suppresses competing hitch/navigation coaching. Tutorial coaching respects the existing hints setting; interaction availability and cable failure remain observable. No physics, economy, save schema, scene, package or network-authority changes were made.

## Validation

- Unity runtime compilation passed. Four NUnit test methods were invoked directly in Edit Mode (20 assertions); this was not a Test Runner execution. They cover delayed/cause-specific advice, false reset suggestions, equipment changes across mud/snow/sand and actual next-three progression/replay behavior.
- Temporary preview scenes rendered production HUD text at 100% and 130%: all 18 contextual text checks and 60 detail-panel checks passed. Protected profile keys and native scene dirty state stayed identical during static checks. `ContextHints.json` and `BoardLayout.json` contain the measurements. The first board preview exposed description overflow; the final previews pass.
- A live stock Solo probe advanced 1,151 real frames and passed eight checks: near/far hitch eligibility, actual E connection, Q introduction/use, stationary detach hint, an actual physics overload breaking the cable, delayed overturned-car reset guidance, and actual R resetting and clearing the hint. `LiveProbe.json` records the results. Vehicle poses, constraints and a fixed camera were controlled test fixtures. The first fixture pulled across the preferred exit and correctly received angle advice instead of the expected detach hint; the successful fixture aligned the vehicles with the authored exit.
- A second live probe advanced 424 frames and passed eight checks: actual E connection, supported extraction transitioning to Deliver, the normal delivery hold completing the job, the result notice, actual Enter continuing exploration, the contextual Tab notice, actual Tab opening the three-card board and a visible current-equipment recommendation. `CompletionProbe.json` records the results. Vehicle travel and delivery placement were controlled, not manually driven routes.
- The first cold Solo connection failed; a warm retry connected. Final live Console reported zero errors and no compilation failure (three warnings). Earlier tooling produced Pipeline transport errors during reload; these were not gameplay exceptions.
- Play was stopped. The post-Play profile restore returned `keysIdentical: true`, with two protected preference values restored after the isolated completion/payout. Original key values and existence match the backup. The whole OS plist hash differs; no whole-file identity is claimed. Private preference backups are excluded from this folder.

No build, manual handling evaluation, controller-device test or second multiplayer peer was run. Advice selection has direct tests; live wheel-slip driving and multiplayer snap attribution still need playtesting.

![Live first winch guidance](03-FirstWinch.png)

![Live contextual reset guidance](05-SafePoint.png)
