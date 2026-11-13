using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        private const int IntactCargoBonus = 200;
        private const float CargoMass = 140f;
        private static readonly Vector3[] ShortcutRoute =
        {
            new(-64f, 22f, 16.5f), new(-78f, 22f, 10f), new(-100f, 22f, 3.5f),
            new(-124f, 22f, -7f), new(-147f, 22f, -15f), new(-169f, 22f, -18f)
        };
        private Transform cargoRoute, supplyAnchor;
        private readonly Vector3[] shortcutPoints = new Vector3[ShortcutRoute.Length];
        private RecoveryVehicle cargoCarrier;
        private Vector3 cargoVelocity, cargoPosition, crateSeat;
        private int cargoGeometry, shortcutLeg;
        private float cargoDamage, cargoRattle, spinSeconds;
        private bool cargoSampleReady, passedFork, usingShortcut;
        internal Transform SupplyAnchor => loaded ? supplyAnchor : null;
        internal int CargoBonus => cargoDamage < .25f ? IntactCargoBonus : 0;
        internal bool LoadedRoute => Active == WinterJob.Supply && loaded;
        internal bool AtSupplyFork => LoadedRoute && !passedFork;
        internal bool OnShortcut => LoadedRoute && usingShortcut;
        internal Vector3 SupplyRoutePoint => AtSupplyFork ? shortcutPoints[0] :
            OnShortcut ? shortcutPoints[shortcutLeg + 1] : forest;
        internal string SupplyRouteName => AtSupplyFork ? "Forest junction" : OnShortcut ? "Forest shortcut" : "Pinewood drop-off";
        internal bool CargoImpact { get; private set; }

        private void BuildSupplyRoute()
        {
            cargoRoute = new GameObject("Pinewood route choice").transform;
            cargoRoute.SetParent(props.transform, false);
            cargoRoute.gameObject.SetActive(false);
            for (int i = 0; i < ShortcutRoute.Length; i++)
                shortcutPoints[i] = Ground(ShortcutRoute[i], out _);
            var fork = shortcutPoints[0];
            Vector3 packedRoad = new(-70f, fork.y, 19f);
            Arrow(cargoRoute, packedRoad, (packedRoad - fork).normalized);
            Vector3 shortcutEntry = (shortcutPoints[1] - fork).normalized;
            Arrow(cargoRoute, fork + shortcutEntry * 7f, shortcutEntry);
            for (int i = 1; i < ShortcutRoute.Length; i++)
            {
                Vector3 along = (ShortcutRoute[i] - ShortcutRoute[i - 1]).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, along);
                for (float d = 0; d < Vector3.Distance(ShortcutRoute[i], ShortcutRoute[i - 1]); d += 7f)
                {
                    Vector3 point = ShortcutRoute[i - 1] + along * d;
                    Stake(point + side * 2.6f, "Forest shortcut", cargoRoute);
                    Stake(point - side * 2.6f, "Forest shortcut", cargoRoute);
                }
            }
            Vector3 post = Ground(shortcutPoints[4] + new Vector3(-2.3f, 0, 1.6f), out _);
            Stake(post, "Shortcut winch post", cargoRoute);
            supplyAnchor = Piece(cargoRoute, "Shortcut winch eye", PrimitiveType.Sphere, post + Vector3.up * .76f,
                Vector3.one * .32f, orange).transform;
        }

        private bool TickSupply(RecoveryVehicle driver, bool attached)
        {
            if (loaded)
            {
                Vector3 position = driver.transform.position;
                if (!passedFork && (Near(position, ShortcutRoute[0], 7f) || position.x < ShortcutRoute[0].x - 9f))
                    passedFork = true;
                float offset = ShortcutOffset(position, out shortcutLeg);
                usingShortcut = offset < 8f &&
                    position.x < ShortcutRoute[0].x - 3f && position.x > ShortcutRoute[ShortcutRoute.Length - 1].x + 2f;
                float slip = (driver.SlipAtWheel(0) + driver.SlipAtWheel(1) + driver.SlipAtWheel(2) + driver.SlipAtWheel(3)) * .25f;
                spinSeconds = usingShortcut && driver.Speed < .75f && slip > .29f ? spinSeconds + Time.deltaTime : 0;
            }

            if (!Held(Near(driver.transform.position, Destination, 5.5f) && driver.Speed < .65f && !attached)) return false;
            if (loaded) return true;
            loaded = true;
            cargoDamage = 0;
            passedFork = usingShortcut = false;
            cargoRoute.gameObject.SetActive(true);
            SwitchToDeliveryMarker();
            cargoCarrier = driver;
            crate.transform.SetParent(driver.transform, false);
            FitCargo();
            return false;
        }

        private void FitCargo()
        {
            cargoGeometry = cargoCarrier.GeometryVersion;
            cargoCarrier.Body.mass += CargoMass;
            crateSeat = cargoCarrier.transform.InverseTransformPoint(cargoCarrier.RearHitch) + new Vector3(0, .83f, .48f);
            crate.transform.localPosition = crateSeat;
            crate.transform.localRotation = Quaternion.identity;
            cargoSampleReady = false;
        }

        private void ReleaseCargo()
        {
            if (cargoCarrier != null && cargoGeometry == cargoCarrier.GeometryVersion)
                cargoCarrier.Body.mass -= CargoMass;
            cargoCarrier = null;
            cargoRoute = supplyAnchor = null;
            cargoRattle = spinSeconds = 0;
            cargoSampleReady = CargoImpact = false;
        }

        internal void TickCargo(RecoveryVehicle driver)
        {
            CargoImpact = false;
            if (!loaded || cargoCarrier != driver) return;
            if (cargoGeometry != driver.GeometryVersion) FitCargo();
            Vector3 velocity = driver.Body.linearVelocity;
            Vector3 position = driver.Body.position;
            if (cargoSampleReady && (position - cargoPosition).sqrMagnitude < 4f)
            {
                float speedLoss = cargoVelocity.magnitude - velocity.magnitude;
                float jolt = Mathf.Max(speedLoss, Mathf.Abs(velocity.y - cargoVelocity.y));
                if (jolt > 3f && cargoRattle <= 0)
                {
                    cargoDamage = Mathf.Min(1, cargoDamage + Mathf.Lerp(.27f, .55f, Mathf.InverseLerp(3f, 7f, jolt)));
                    cargoRattle = .55f;
                    CargoImpact = true;
                }
            }
            cargoSampleReady = true;
            cargoVelocity = velocity;
            cargoPosition = position;
            cargoRattle = Mathf.Max(0, cargoRattle - Time.fixedDeltaTime);
            crate.transform.localRotation = Quaternion.Euler(Mathf.Sin(cargoRattle * 54f) * cargoRattle * 17f, 0,
                Mathf.Cos(cargoRattle * 47f) * cargoRattle * 11f);
            crate.transform.localPosition = crateSeat + Vector3.up * (cargoRattle * .065f);
        }

        private string SupplyInstruction(bool near)
        {
            if (HoldProgress > 0) return loaded ? "Unloading crate · stay stopped" : "Loading crate · stay stopped";
            if (near) return loaded ? "Park inside the marked unloading bay" : "Park inside the marked loading bay";
            if (!loaded) return DestinationName;
            if (spinSeconds > 1.8f) return "Ease the throttle · use a winch post or return to the packed road";
            if (AtSupplyFork) return pinewoodCleared ? "Choose the plowed road or the forest shortcut" :
                "Choose the packed road or the forest shortcut";
            if (cargoRattle > 0) return "Crate hit hard · intact delivery bonus lost";
            return (OnShortcut ? "Follow the paired poles through the powder" : "Deliver the crate to Pinewood") +
                (CargoBonus > 0 ? " · crate intact" : " · crate damaged");
        }

        private static float ShortcutOffset(Vector3 position, out int leg)
        {
            leg = 0;
            float offset = float.PositiveInfinity;
            position.y = 0;
            for (int i = 0; i < ShortcutRoute.Length - 1; i++)
            {
                Vector3 a = ShortcutRoute[i], b = ShortcutRoute[i + 1];
                a.y = b.y = 0;
                Vector3 line = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(position - a, line) / line.sqrMagnitude);
                float distance = (position - (a + line * t)).sqrMagnitude;
                if (distance >= offset) continue;
                offset = distance;
                leg = i;
            }
            return Mathf.Sqrt(offset);
        }

        private float SupplyDriftAt(Vector3 point)
        {
            if (!LoadedRoute || point.x > -79f || point.x < -159f) return 0;
            float offset = ShortcutOffset(point, out _);
            return .68f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.05f, 2.65f, offset)));
        }
    }
}
