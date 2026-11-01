# Garage category camera — 2026-11-01

The local garage camera now chooses its orbit, elevation, focus and distance from the selected category and the currently previewed assembly. Front bumper views face +Z; rear bumper and towing equipment face -Z. Wheels use the exposed left front rim. Body/paint retain an overview; bonnet, intake/engine, suspension/chassis, fenders, exhaust and roof have appropriate detail views.

The shared garage builder publishes the selected slot/part to the local session. Bounds are recalculated only on category, selected part or GeometryVersion changes. Visible renderer bounds are transformed into vehicle space, supporting rotated vehicles and differently sized bodies. Nested structural sockets are excluded from skin bounds. Factory bodies without a separate bumper mesh use their authored tow point. The cache follows the car transform each frame.

Transitions use unscaled SmoothDamp on yaw, elevation, distance and focus, then construct a circular camera path. The camera remains outside an expanded body box and retains the existing environment linecast. A second Cartesian position lerp is avoided during garage viewing. Manual mouse/gamepad orbit is retained within the same category, and reset smoothly to the new category on switching. Driving and vehicle-selection camera recipes remain unchanged. No field-of-view, gameplay, save, catalogue, scene or network data changes.

Validation is Edit Mode only, in temporary preview scenes, with an in-memory session/profile and a kinematic clone. No Play Mode, PlayerLoop, physics/particle simulation, Test Runner, network, profile saves or build. The private presentation helper is invoked directly with explicit 1/60-second unscaled deltas for trajectory checks.

- 16 static UI captures: all categories; focus stays inside the open preview column; front/rear direction, wheel exterior and UI-to-camera signal verified.
- Three 180-step mathematical camera transitions stay outside the expanded vehicle bounds and converge. Manual rotation persists through a same-category preview change and resets on category change.
- 38 body choices × 5 view types = 190 geometry/frame variants. Detail bounds fit the open UI space, with the camera outside the body.
- Scripts compile with zero Console errors. Scene file is byte-identical to the task backup.

`Checks.json` and `BodyVariants.json` record the direct checks. PNGs show static UI framing; they do not demonstrate live motion. The user owns final mouse/gamepad, live transition feel and garage-environment acceptance.

Backup: `/Users/gheorghe/Desktop/TowTrublw/Backups/GarageFocusCamera-20261101`.
