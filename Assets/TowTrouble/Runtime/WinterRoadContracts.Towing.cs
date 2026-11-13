using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        private const float LaneEdge = 3.1f;
        private Transform treeBaseEye;
        private RecoveryVehicle pickup;
        internal RecoveryVehicle Pickup => pickup;
        private Vector3 rescueExit, rescueBank;
        private int blockedAxle;
        private float pickupStall, pickupFreeTime;
        private GameObject pickupSnow;
        private Vector3 TreeSide => treeHeading * Vector3.right;

        internal Transform FindTowEye(RecoveryVehicle driver, out bool rear)
        {
            rear = false;
            float best = 16f * 16f;
            Transform selected = null;
            Consider(towEye);
            Consider(treeBaseEye);
            if (selected != null)
                rear = (selected.position - driver.RearHitch).sqrMagnitude < (selected.position - driver.FrontHitch).sqrMagnitude;
            return selected;

            void Consider(Transform eye)
            {
                if (eye == null) return;
                Vector3 hitch = (eye.position - driver.RearHitch).sqrMagnitude < (eye.position - driver.FrontHitch).sqrMagnitude
                    ? driver.RearHitch : driver.FrontHitch;
                float distance = (eye.position - hitch).sqrMagnitude;
                if (distance >= best || Physics.Linecast(hitch, eye.position, 1 << 8, QueryTriggerInteraction.Ignore)) return;
                selected = eye;
                best = distance;
            }
        }

        internal string TowEyeName(Transform eye) => Active != WinterJob.Tree || eye == null ? "Tow eye" :
            eye == treeBaseEye ? "Thick end · tow eye" : "Tip · tow eye";

        private void BuildRescue()
        {
            pickup = UnityEngine.Object.Instantiate(world.Casualty, props.transform);
            pickup.name = "Stranded Meadow pickup";
            pickup.SetModel(3);
            pickup.SetPaint(2);
            pickup.SetRecoveryMass(1350, .5f);
            pickup.SetRecoveryJammed(true);
            pickup.WinterDriving = true;
            pickup.SetSpawn(rescue, rescueHeading);
            pickup.SetSimulation(true);
            pickup.gameObject.SetActive(true);
            TowBody = pickup.Body;
            towEye = Eye(pickup.transform, pickup.transform.InverseTransformPoint(pickup.FrontHitch));
            rescueExit = Vector3.ProjectOnPlane(rescuePullPoint - rescue, Vector3.up).normalized;
            Vector3 front = (pickup.WheelAnchor(0) + pickup.WheelAnchor(1)) * .5f;
            Vector3 rear = (pickup.WheelAnchor(2) + pickup.WheelAnchor(3)) * .5f;
            blockedAxle = Vector3.Dot(front - rear, rescueExit) > 0 ? 2 : 0;
            var buriedAxle = Ground((pickup.WheelAnchor(blockedAxle) + pickup.WheelAnchor(blockedAxle + 1)) * .5f, out _);
            rescueBank = Ground(buriedAxle + rescueExit * 3.2f, out _);
            pickupStall = pickupFreeTime = 0;

            pickupSnow = new GameObject("Snow packed around blocked wheels");
            pickupSnow.transform.SetParent(props.transform, false);
            for (int i = blockedAxle; i < blockedAxle + 2; i++)
            {
                Vector3 wheel = Ground(pickup.WheelAnchor(i), out _);
                Snowbank("Buried wheel snowbank", wheel + Vector3.up * .10f,
                    new Vector3(1.18f, .65f, 1.35f), rescueHeading, pickupSnow.transform);
            }
            Vector3 side = Vector3.Cross(Vector3.up, rescueExit);
            Stake(rescueBank + side * 2.15f, "Firm bank", pickupSnow.transform);
            Stake(rescueBank - side * 2.15f, "Firm bank", pickupSnow.transform);
        }

        private bool TickPickup(RecoveryVehicle driver)
        {
            if (!reachedTow) return false;
            if (!pickupFreed)
            {
                bool onBank = WheelOnFirmBank(blockedAxle) && WheelOnFirmBank(blockedAxle + 1);
                pickupFreeTime = onBank ? pickupFreeTime + Time.deltaTime : 0;
                if (pickupFreeTime < .23f) return false;
                pickupFreed = true;
                pickup.SetRecoveryJammed(false);
                pickup.ShowExtractionRelease(rescueBank);
                Remove(pickupSnow);
                pickupSnow = null;
                SwitchToDeliveryMarker();
                deliveryMarker.SetActive(false);
                pickupExitMarker.SetActive(true);
            }

            if (!passedPickupExit && (Near(driver.transform.position, rescueEntry, 8f) ||
                Near(TowBody.position, rescueEntry, 8f) || TowInBay()))
            {
                passedPickupExit = true;
                pickupExitMarker.SetActive(false);
                deliveryMarker.SetActive(true);
            }
            return Held(TowInBay() && TowBody.linearVelocity.magnitude < 1.1f);
        }

        private bool WheelOnFirmBank(int wheel)
        {
            if (!pickup.TryGetWheelContact(wheel, out var hit) || hit.normal.y < .84f) return false;
            Vector3 delta = hit.point - rescueBank;
            return Vector3.Dot(delta, rescueExit) >= 0 &&
                Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, rescueExit))) < 5f &&
                hit.point.y >= rescueBank.y - .27f;
        }

        internal void ApplyTowResistance(Transform eye, Vector3 pull, float tension)
        {
            if (ExtractingPickup)
            {
                if (pull.sqrMagnitude < 1)
                {
                    pickupStall = Mathf.Max(0, pickupStall - 2 * Time.fixedDeltaTime);
                    return;
                }
                Vector3 direction = Vector3.ProjectOnPlane(pull, Vector3.up).normalized;
                float alignment = Vector3.Dot(direction, rescueExit);
                float advance = Vector3.Dot(TowBody.linearVelocity, rescueExit);
                float resistance = Mathf.Lerp(2550, 650, Mathf.InverseLerp(.10f, .85f, alignment));
                float available = Mathf.Max(0, Vector3.Dot(pull, direction) * .82f);
                TowBody.AddForceAtPosition(-direction * Mathf.Min(available, resistance + Mathf.Max(0, advance) * 170f), eye.position);
                pickupStall = tension > .42f && advance < .22f ? pickupStall + Time.fixedDeltaTime :
                    Mathf.Max(0, pickupStall - 2 * Time.fixedDeltaTime);
            }
            else if (Active == WinterJob.Tree && eye == towEye && treeBaseEye != null && TreeEndInLane(-1))
            {
                Vector3 speed = Vector3.ProjectOnPlane(TowBody.GetPointVelocity(treeBaseEye.position), Vector3.up);
                TowBody.AddForceAtPosition(Vector3.ClampMagnitude(-speed * 1500f, 850f), treeBaseEye.position);
            }
        }

        private void BuildTree()
        {
            TowBody = CreateTree(props.transform, treeRoad + Vector3.up * .37f, treeHeading * Quaternion.Euler(0, 82f, 0));
            towEye = Eye(TowBody.transform, new Vector3(0, .24f, 4.3f));
            towEye.name = "Tip tow eye";
            treeBaseEye = Eye(TowBody.transform, new Vector3(0, .26f, -4.3f));
            treeBaseEye.name = "Thick end tow eye";

            var along = treeHeading * Vector3.forward;
            Stake(layBy + along * 2f, "Turnout marker", props.transform);
            Stake(layBy - along * 2f, "Turnout marker", props.transform);
            for (int edge = -1; edge <= 1; edge += 2)
            {
                var line = treeRoad + TreeSide * (edge * LaneEdge);
                MarkLine(props.transform, line - along * 6.5f, line, line + along * 6.5f);
                Stake(line - along * 6.5f, "Road clearance", props.transform);
                Stake(line + along * 6.5f, "Road clearance", props.transform);
            }
        }

        private Rigidbody CreateTree(Transform parent, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Fallen pine");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);
            var trunk = Piece(root.transform, "Trunk", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.55f, 4.55f, .55f), wood,
                Quaternion.Euler(90, 0, 0), true);
            var collider = trunk.GetComponent<CapsuleCollider>();
            collider.radius = .49f;
            collider.sharedMaterial = snowyBark;
            Piece(root.transform, "Snow cap", PrimitiveType.Sphere, new Vector3(0, .21f, -.35f), new Vector3(.56f, .26f, 7.8f), snow);
            Piece(root.transform, "Split base", PrimitiveType.Cylinder, new Vector3(0, 0, -4.56f), new Vector3(.47f, .055f, .47f), wood, Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 6; i++)
            {
                float sign = i % 2 == 0 ? 1 : -1;
                Piece(root.transform, "Broken limb", PrimitiveType.Cylinder, new Vector3(sign * .48f, .09f, -2.0f + i * 1.03f),
                    new Vector3(.13f, .77f, .13f), wood, Quaternion.Euler(28, 0, sign * 58));
            }
            var body = root.AddComponent<Rigidbody>();
            body.mass = 480f;
            body.linearDamping = .8f;
            body.angularDamping = 2.25f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            return body;
        }

        private bool TreeEndInLane(int end)
        {
            var point = TowBody.position + TowBody.transform.forward * (end * 4.6f);
            return Vector3.Dot(point - treeRoad, TreeSide) <= LaneEdge + .42f;
        }

        private string TreeInstruction(bool attached, bool canHook)
        {
            if (!reachedTow)
                return canHook ? "Attach to the tip for an easier swing, or pull from the thick end" :
                    "Approach the turnout · both ends have a tow eye";
            if (TowInBay()) return "Stop and let the trunk settle";
            if (!attached) return "Reconnect to either end and pull toward the turnout";
            bool baseInLane = TreeEndInLane(-1);
            bool tipInLane = TreeEndInLane(1);
            if (baseInLane && tipInLane) return "Pull toward the turnout to rotate the trunk";
            if (baseInLane) return "Thick end still blocks the lane · pull farther across";
            if (tipInLane) return "Tip still blocks the lane · change your pulling angle";
            return "The lane is clear · settle the trunk inside the turnout";
        }

        private void RestoreParkedTree()
        {
            if (!lakesideCleared || parkedTree != null) return;
            var tree = CreateTree(owner, layBy + Vector3.up * .33f, treeHeading);
            tree.isKinematic = true;
            parkedTree = tree.gameObject;
        }
    }
}
