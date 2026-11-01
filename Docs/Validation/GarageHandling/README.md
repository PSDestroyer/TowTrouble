# Wheel and body characteristics — 2026-11-01

User requested real characteristics and card information for every wheel, plus more differentiated bodies. Existing grid layout, imagery, prices, IDs and inventory are retained.

## Implemented

- All 87 wheel entries have an authored GarageTireKind and positive dry/mud/sand/snow grip, plus mass, mud rolling resistance and speed tradeoffs where appropriate. Rim/paint variants may share the same profile. Road tires improve firm-ground grip at the cost of loose-surface grip; studded tires favor snow; wide tires favor sand; tractor/mud tires favor wet ground; large off-road tires add mass and reduce speed.
- The five established specialist upgrades retain their approved grip levels (mud1.30 / sand1.45 / winter1.65 / alpine2.35), prices and existing mass. Generic 500-credit options have smaller specialist bonuses. Their primary characteristic is visible on every card; the selected description lists all surface modifiers and non-neutral mass/resistance/speed effects.
- All 38 Body choices show factory mass and nominal four-wheel pulling force. The 37 purchasable assemblies use their own mass/drive/speed baseline instead of adding the same30kg to whichever hero model was selected. Families and meaningful variants have different tradeoffs; sport/open bodies are generally lighter/faster, work bodies heavier/slower with more nominal pull. Examples: Bucket1020kg/10.4kN; Lorry1980kg/16.4kN; Pickupster Tow2040kg/~17kN. Factory body values combine with equipped tire/engine/transmission/suspension modifiers.
- The original body still follows Scout/Hauler/Buddy/Ranger stock stats. All four fully stock vehicle configurations are unchanged. Mission casualty models/load profiles are separate and unchanged.
- Real physics integration: GaragePerformance applies tire surface coefficients and selects the body baseline before multiplying upgrades, independent of item order. RecoveryVehicle uses dry tire grip in its contact friction budget; mud/sand/snow coefficients flow through the existing solver. No client-provided force coefficients, new RPCs or save version changes.
- The wheel description area has28px more height for full trait details; body cards use a compact mass/force line. Existing previews and geometry are unchanged. Native visual diameter/radius handling remains the established assembly system.

## Authoring

Tow Trouble → Garage → Refresh Wheel and Body Characteristics. GarageHandlingAuthoring applies deterministic profiles from the sanitized native wheel/body names and explicit specialist IDs. Full catalogue generation and specialist refresh also apply the profiles, retaining thumbnails by stable ID. Unknown future body families require review rather than silently receiving a generic force value.

## Verification

- Compiles in the connected Editor; no Play Mode, Test Runner, physics simulation, networking startup, builds or real profile writes.
-27 direct Edit Mode checks passed: every wheel/card covered, surface tradeoffs, specialist progression, independent body baselines for all four heroes, upgrade composition/order, stock baselines, plus existing garage/economy and grid tests.
-38 actual customization applications on an inactive kinematic clone bind mass, drive and dry grip to the vehicle as calculated. No physics advancement.
-32 static layouts (16 slots EN100% / RO130%) pass with zero text overflow.250 description/card checks cover every wheel and body in both languages at130% text; zero overflow. Body and wheel images inspected.
- All part IDs, prices, prefabs, mounting sockets, images and other-slot performance fields preserved; no scene file changes. See Preservation.json.

Profiles.json / Profiles.csv contain all125 wheel/body inputs. BodyDrive is force per wheel in newtons; the displayed nominal pull is BodyDrive×4. Drive remains limited by actual ground grip, slope, cable and winch capacity. Live feel, route selection and cooperative play remain with the user.

Backup: /Users/gheorghe/Desktop/TowTrublw/Backups/GarageHandling-20261101.
