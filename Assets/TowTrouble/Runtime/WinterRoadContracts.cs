using System;
using UnityEngine;

namespace TowTrouble
{
    internal enum WinterJob { Supply, Recovery, Tree, Plow }

    internal sealed partial class WinterRoadContracts : IDisposable
    {
        internal readonly struct Offer
        {
            internal readonly string Name, Brief;
            internal readonly int Reward;

            internal Offer(string name, string brief, int reward)
            {
                Name = name;
                Brief = brief;
                Reward = reward;
            }
        }

        internal static readonly Offer[] Offers =
        {
            new Offer("Supplies for Pinewood", "Deliver one crate intact. Take the packed road or the forest shortcut.", 850),
            new Offer("Workshop Meadow rescue", "Pull the pickup onto firm ground, then tow it back to the workshop.", 1450),
            new Offer("Timber on Lakeside Road", "Choose an end of the fallen pine and swing the whole trunk out of the road.", 1200),
            new Offer("Plow Pinewood Road", "Fit the service blade and open a continuous lane through the three drifts.", 1050)
        };

        private readonly RecoveryWorld world;
        private readonly Transform owner;
        private readonly Vector3 workshop, forest, rescue, treeRoad, layBy;
        private readonly Vector3 rescueEntry, treeEntry, rescuePullPoint, treePullPoint;
        private GameObject pickupExitMarker;
        private bool passedPickupExit;
        private readonly Quaternion rescueHeading, treeHeading;
        private GameObject parkedTree;
        private bool lakesideCleared;
        private GameObject props, crate, blade;
        private GameObject currentMarker, deliveryMarker;
        private ParticleSystem plowSnow;
        private Transform towEye;
        private float dwell;
        private bool loaded, reachedTow, pickupFreed, passedEntry;
        private Material wood, steel, orange, snow, paint, particles, marking;
        private PhysicsMaterial snowyBark;
        private Mesh driftMesh;
        internal WinterJob? Active { get; private set; }
        internal Rigidbody TowBody { get; private set; }
        internal Transform TowEye => towEye;
        internal bool PlowFitted => blade != null;
        internal bool CargoLoaded => loaded;
        internal bool DeliveryLeg => Active == WinterJob.Supply ? loaded :
            Active == WinterJob.Recovery ? pickupFreed : reachedTow;
        internal bool ExtractingPickup => Active == WinterJob.Recovery && reachedTow && !pickupFreed;
        internal bool GuidingPickupExit => Active == WinterJob.Recovery && pickupFreed && !passedPickupExit;
        internal Vector3 PickupExit => rescueEntry;
        internal int Cleared => cleared;
        internal bool AtPlowStart(Vector3 point) => Active == WinterJob.Plow && !PlowFitted && Near(point, plowStart, 5f);
        internal bool Reconnecting(bool attached) => TowBody != null && reachedTow && !attached && !TowInBay();
        internal bool ApproachingEntry => TowBody != null && !reachedTow && !passedEntry;
        internal Vector3 EntryPoint => Active == WinterJob.Tree ? treeEntry : rescueEntry;
        internal bool MarkingPullPoint(Vector3 position) => Active == WinterJob.Recovery && TowBody != null && !reachedTow && Near(position, TowBody.position, 34f);
        internal Vector3 PullPoint => Active == WinterJob.Tree ? treePullPoint : rescuePullPoint;
        internal bool AtPullPoint(Vector3 position) => Near(position, PullPoint, 3.5f);
        internal float HoldProgress => Mathf.Clamp01(dwell / 1.2f);

        internal WinterRoadContracts(RecoveryWorld world, Transform owner, int completedJobs)
        {
            this.world = world;
            this.owner = owner;
            workshop = Array.Find(world.Map.Deliveries, bay => bay.Id == "A").Position;
            forest = Array.Find(world.Map.Deliveries, bay => bay.Id == "B").Position;
            var meadow = Array.Find(world.Map.Sites, site => site.Id == "R01");
            rescue = Ground(meadow.Position, out _) + Vector3.up * .72f;
            rescueHeading = Quaternion.Euler(0, meadow.Heading, 0);
            rescueEntry = Ground(new Vector3(-97f, 22f, 82.5f), out _);
            rescuePullPoint = Ground(new Vector3(-106f, 22f, 86.4f), out _);

            // Road centreline and lay-by on the forest-to-lakeshore link.
            treeRoad = Ground(new Vector3(-165.9f, 22f, -76.7f), out _);
            Vector3 roadDirection = new Vector3(.25f, 0, -.97f).normalized;
            layBy = Ground(treeRoad + Vector3.Cross(Vector3.up, roadDirection) * 8.7f, out _);
            treePullPoint = Ground(layBy + Vector3.Cross(Vector3.up, roadDirection) * 10.6f, out _);
            treeEntry = Ground(new Vector3(-172f, 22f, -68f), out _);
            treeHeading = Quaternion.LookRotation(roadDirection);
            BuildPinewoodRoad((completedJobs & (1 << (int)WinterJob.Plow)) != 0);
            lakesideCleared = (completedJobs & (1 << (int)WinterJob.Tree)) != 0;
            RestoreParkedTree();
        }

        internal Vector3 Destination
        {
            get
            {
                switch (Active)
                {
                    case WinterJob.Supply: return loaded ? forest : workshop;
                    case WinterJob.Recovery: return pickupFreed ? workshop : TowBody.position;
                    case WinterJob.Tree: return reachedTow ? layBy : TowBody.position;
                    case WinterJob.Plow:
                        return PlowFitted ? NextSnowPoint : plowStart;
                    default: return workshop;
                }
            }
        }

        internal string DestinationName
        {
            get
            {
                switch (Active)
                {
                    case WinterJob.Supply: return loaded ? "Pinewood drop-off" : "Workshop loading bay";
                    case WinterJob.Recovery: return pickupFreed ? "Workshop receiving bay" : "Stranded pickup";
                    case WinterJob.Tree: return reachedTow ? "Marked lay-by" : "Fallen tree";
                    case WinterJob.Plow: return PlowFitted ? PlowStageName : "Plow fitting point";
                    default: return "";
                }
            }
        }

        internal string Instruction(RecoveryVehicle driver, bool near, bool attached, bool rearCable, bool canHook)
        {
            if (Active == WinterJob.Plow) return PlowInstruction(driver, near);
            if (Active == WinterJob.Supply) return SupplyInstruction(near);
            if (Active == WinterJob.Tree) return TreeInstruction(attached, canHook);
            if (!reachedTow)
            {
                if (ApproachingEntry)
                    return "Follow the marked entrance into the meadow";
                if (!MarkingPullPoint(driver.transform.position)) return DestinationName;
                return canHook && rearCable ? "Attach the rear cable and reel in" : "Back into the marked pull point";
            }
            if (Reconnecting(attached)) return "Return to the tow eye and reconnect";
            if (ExtractingPickup) return pickupStall > 1.5f ?
                "Ease the cable and move toward the two orange posts" : "Hold the brake and reel toward the firm bank";
            if (TowInBay()) return "Hold still while the load settles";
            if (GuidingPickupExit) return "Take the marked bend wide · leave room for the pickup";
            return rearCable ? "Tow the pickup into the marked bay" : "Reel in, then reconnect from the rear to tow";
        }

        internal void Begin(WinterJob job)
        {
            Clear();
            if (job == WinterJob.Tree)
            {
                Remove(parkedTree);
                parkedTree = null;
            }
            Active = job;
            props = new GameObject(Offers[(int)job].Name);
            props.transform.SetParent(owner, true);
            EnsureMaterials();
            switch (job)
            {
                case WinterJob.Supply:
                    BuildCargo();
                    BuildSupplyRoute();
                    currentMarker = Zone("Loading bay", workshop, 5.5f);
                    deliveryMarker = Zone("Unloading bay", forest, 5.5f);
                    break;
                case WinterJob.Recovery:
                    BuildRescue();
                    currentMarker = Zone("Recovery pull point", rescuePullPoint, 3f);
                    Arrow(currentMarker.transform, rescuePullPoint + rescueExit * 2.9f, rescueExit);
                    Gate(rescueEntry, Vector3.right, "Meadow entrance");
                    deliveryMarker = Zone("Receiving bay", workshop, 6f);
                    pickupExitMarker = Zone("Meadow exit", rescueEntry, 2.5f);
                    pickupExitMarker.SetActive(false);
                    break;
                case WinterJob.Tree:
                    BuildTree();
                    currentMarker = Zone("Turnout approach", treeEntry, 3f);
                    deliveryMarker = Zone("Lay-by", layBy, 4f);
                    break;
                case WinterJob.Plow:
                    SetPinewoodRoad(false);
                    currentMarker = Zone("Plow fitting bay", plowStart, 5f);
                    Arrow(currentMarker.transform, plowStart + plowDirection * 2.8f, plowDirection);
                    break;
            }
            if (deliveryMarker != null) deliveryMarker.SetActive(false);
        }

        internal void AttachedTarget()
        {
            reachedTow = true;
            if (Active == WinterJob.Tree || pickupFreed) SwitchToDeliveryMarker();
        }

        private void SwitchToDeliveryMarker()
        {
            if (currentMarker != null) currentMarker.SetActive(false);
            if (deliveryMarker != null) deliveryMarker.SetActive(true);
        }

        internal bool Tick(RecoveryVehicle driver, bool attached)
        {
            if (ApproachingEntry && (Near(driver.transform.position, EntryPoint, 5f) || AtPullPoint(driver.transform.position)))
                passedEntry = true;
            switch (Active)
            {
                case WinterJob.Supply:
                    return TickSupply(driver, attached);
                case WinterJob.Recovery:
                    return TickPickup(driver);
                case WinterJob.Tree:
                    return Held(reachedTow && TowInBay() && TowBody.linearVelocity.magnitude < 1.1f);
                case WinterJob.Plow:
                    return cleared == DriftCount;
                default:
                    return false;
            }
        }

        private bool TowInBay()
        {
            if (TowBody == null) return false;
            if (Active == WinterJob.Recovery) return Near(TowBody.position, workshop, 6f);
            return Near(TowBody.position, layBy, 5f) && !TreeEndInLane(-1) && !TreeEndInLane(1);
        }

        private bool Held(bool condition)
        {
            dwell = condition ? dwell + Time.deltaTime : 0;
            if (dwell < 1.2f) return false;
            dwell = 0;
            return true;
        }

        private void BuildCargo()
        {
            var root = new GameObject("Strapped supply crate");
            crate = root;
            root.transform.SetParent(props.transform, false);
            root.transform.position = Ground(workshop, out _) + new Vector3(-2, .31f, 1);
            Piece(root.transform, "Case", PrimitiveType.Cube, Vector3.zero, new Vector3(.94f, .58f, .67f), paint);
            for (int i = -1; i <= 1; i += 2)
                Piece(root.transform, "Tie-down", PrimitiveType.Cube, new Vector3(i * .31f, 0, 0), new Vector3(.055f, .61f, .70f), orange);
            Piece(root.transform, "Shipping label", PrimitiveType.Cube, new Vector3(0, .04f, -.34f), new Vector3(.27f, .16f, .02f), snow);
        }

        private static Mesh BuildDriftMesh()
        {
            const int size = 11;
            var vertices = new Vector3[size * size];
            var triangles = new int[(size - 1) * (size - 1) * 6];
            int next = 0;
            for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
            {
                float u = 2f * x / (size - 1) - 1, v = 2f * z / (size - 1) - 1;
                float noise = Mathf.PerlinNoise(u * 2.2f + 8, v * 2.8f + 5) - .5f;
                float outline = Mathf.Pow(Mathf.Abs(u + .07f * v), 2.4f) + Mathf.Pow(Mathf.Abs(v), 3.5f);
                float height = Mathf.Pow(Mathf.Clamp01(1 - outline), .75f) * (.57f + noise * .13f) - .12f;
                int a = z * size + x;
                vertices[a] = new Vector3(.5f * u, height, .5f * v);
                if (x == size - 1 || z == size - 1) continue;
                triangles[next++] = a;
                triangles[next++] = a + size;
                triangles[next++] = a + 1;
                triangles[next++] = a + 1;
                triangles[next++] = a + size;
                triangles[next++] = a + size + 1;
            }
            var mesh = new Mesh { name = "Wind-packed snow", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }

        private void Stake(Vector3 point, string name, Transform parent = null)
        {
            var ground = Ground(point, out _);
            parent = parent == null ? props.transform : parent;
            Piece(parent, name, PrimitiveType.Cylinder, ground + Vector3.up * .52f, new Vector3(.085f, .55f, .085f), orange);
            Piece(parent, "Reflector", PrimitiveType.Cylinder, ground + Vector3.up * .93f, new Vector3(.091f, .065f, .091f), snow);
        }

        private GameObject Zone(string name, Vector3 center, float radius)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(props.transform, false);
            int dashes = radius > 4f ? 16 : 10;
            for (int i = 0; i < dashes; i++)
            {
                float angle = 360f * i / dashes;
                Vector3 Edge(float degrees) => center + Quaternion.Euler(0, degrees, 0) * Vector3.forward * radius;
                MarkLine(zone.transform, Edge(angle), Edge(angle + 96f / dashes), Edge(angle + 192f / dashes));
            }
            Stake(center + Vector3.right * radius, name + " boundary post", zone.transform);
            Stake(center - Vector3.right * radius, name + " boundary post", zone.transform);
            return zone;
        }

        private void Gate(Vector3 center, Vector3 across, string name)
        {
            Stake(center + across * 2f, name, currentMarker.transform);
            Stake(center - across * 2f, name, currentMarker.transform);
        }

        private void Arrow(Transform parent, Vector3 tip, Vector3 direction)
        {
            Vector3 back = tip - direction * 1.6f;
            Vector3 side = Vector3.Cross(Vector3.up, direction) * .85f;
            MarkLine(parent, back + side, tip, back - side);
            MarkLine(parent, tip - direction * 3.1f, back, tip);
        }

        private void MarkLine(Transform parent, Vector3 a, Vector3 b, Vector3 c)
        {
            var segment = new GameObject("Snow marker").AddComponent<LineRenderer>();
            segment.transform.SetParent(parent, false);
            segment.positionCount = 3;
            segment.widthMultiplier = .13f;
            segment.sharedMaterial = marking;
            segment.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            segment.receiveShadows = false;
            segment.SetPosition(0, Ground(a, out _) + Vector3.up * .10f);
            segment.SetPosition(1, Ground(b, out _) + Vector3.up * .10f);
            segment.SetPosition(2, Ground(c, out _) + Vector3.up * .10f);
        }

        private Transform Eye(Transform parent, Vector3 point)
            => Piece(parent, "Tow eye", PrimitiveType.Sphere, point, Vector3.one * .24f, orange).transform;

        private void EnsureMaterials()
        {
            if (wood != null) return;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            wood = Make(lit, "Pine bark", new Color(.19f, .105f, .062f));
            steel = Make(lit, "Blade steel", new Color(.14f, .18f, .21f));
            orange = Make(lit, "Service orange", new Color(1f, .36f, .045f));
            snow = Make(lit, "Fresh road snow", new Color(.86f, .92f, .98f));
            snow.SetFloat("_Smoothness", .15f);
            paint = Make(lit, "Supply case blue", new Color(.13f, .33f, .49f));
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            particles = Make(unlit, "Plow snow particles", new Color(.91f, .97f, 1f));
            marking = Make(unlit, "Winter job markings", new Color(1f, .35f, .03f));
            snowyBark = new PhysicsMaterial("Bark on packed snow")
            {
                staticFriction = .20f,
                dynamicFriction = .14f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
        }

        private static Material Make(Shader shader, string name, Color tint)
            => new Material(shader) { name = name, color = tint };

        private static GameObject Piece(Transform parent, string name, PrimitiveType kind, Vector3 position,
            Vector3 scale, Material material, Quaternion rotation = default, bool solid = false)
        {
            var piece = GameObject.CreatePrimitive(kind);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localRotation = rotation == default ? Quaternion.identity : rotation;
            piece.transform.localScale = scale;
            piece.GetComponent<MeshRenderer>().sharedMaterial = material;
            var collider = piece.GetComponent<Collider>();
            collider.enabled = solid;
            if (!solid) UnityEngine.Object.Destroy(collider);
            return piece;
        }

        private static Vector3 Ground(Vector3 point, out Vector3 normal)
        {
            bool found = Physics.Raycast(point + Vector3.up * 4, Vector3.down, out var hit, 9, 1 << 8, QueryTriggerInteraction.Ignore);
            normal = found ? hit.normal : Vector3.up;
            return found ? hit.point : point;
        }

        private static bool Near(Vector3 a, Vector3 b, float range)
        {
            var delta = a - b;
            return Mathf.Abs(delta.y) < 3.5f && delta.x * delta.x + delta.z * delta.z < range * range;
        }

        internal void Clear(bool restoreRoad = true)
        {
            var previousJob = Active;
            if (restoreRoad && previousJob == WinterJob.Plow) SetPinewoodRoad(pinewoodCleared);
            ReleaseCargo();
            Remove(currentMarker);
            Remove(deliveryMarker);
            Remove(pickupExitMarker);
            Remove(crate);
            Remove(blade);
            Remove(props);
            props = crate = blade = null;
            currentMarker = deliveryMarker = pickupExitMarker = null;
            plowSnow = null;
            bladeSnowWave = null;
            TowBody = null;
            towEye = treeBaseEye = null;
            pickup = null;
            Active = null;
            if (restoreRoad && previousJob == WinterJob.Tree) RestoreParkedTree();
            loaded = reachedTow = pickupFreed = passedEntry = passedPickupExit = false;
            dwell = 0;
        }

        internal void Finish(WinterJob job)
        {
            if (job == WinterJob.Plow) pinewoodCleared = true;
            if (job == WinterJob.Tree)
            {
                if (TowBody != null) TowBody.isKinematic = true;
                lakesideCleared = true;
                parkedTree = props;
                props = null;
            }
            Clear();
        }

        private static void Remove(GameObject instance)
        {
            if (instance == null) return;
            instance.SetActive(false);
            UnityEngine.Object.Destroy(instance);
        }

        public void Dispose()
        {
            Clear(restoreRoad: false);
            Remove(parkedTree);
            Remove(pinewoodRoad);
            foreach (var material in new[] { wood, steel, orange, snow, paint, particles, marking })
                if (material != null) UnityEngine.Object.Destroy(material);
            if (snowyBark != null) UnityEngine.Object.Destroy(snowyBark);
            if (driftMesh != null) UnityEngine.Object.Destroy(driftMesh);
        }
    }
}
