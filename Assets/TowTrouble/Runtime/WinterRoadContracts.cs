using System;
using UnityEngine;

namespace TowTrouble
{
    internal enum WinterJob { Supply, Recovery, Tree, Plow }

    internal sealed class WinterRoadContracts : IDisposable
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
            new Offer("Supplies for Pinewood", "Pick up one supply crate at the workshop and deliver it to Pinewood.", 850),
            new Offer("Workshop Meadow rescue", "Winch out the stranded pickup and tow it back to the workshop yard.", 1450),
            new Offer("Timber on Lakeside Road", "Hook the fallen tree and drag it into the marked lay-by.", 1200),
            new Offer("Plow Pinewood Road", "Fit the service blade at the road marker and clear the drift.", 1050)
        };

        private const int DriftCount = 8;
        private const float DriftSpacing = 3f;
        private const float BladeWidth = 3.15f;
        private readonly RecoveryWorld world;
        private readonly Transform owner;
        private readonly Vector3 workshop, forest, rescue, treeRoad, layBy, plowStart, plowDirection;
        private readonly Vector3 rescueEntry, treeEntry, rescuePullPoint, treePullPoint;
        private readonly Quaternion rescueHeading, treeHeading;
        private readonly GameObject[] drifts = new GameObject[DriftCount];
        private readonly GameObject[] piles = new GameObject[DriftCount];
        private readonly GameObject[] finishedScenery = new GameObject[Offers.Length];
        private readonly Vector3[] driftPoints = new Vector3[DriftCount];
        private readonly float[] clearedMetres = new float[DriftCount];
        private GameObject props, crate, blade;
        private GameObject currentMarker, deliveryMarker;
        private ParticleSystem plowSnow;
        private Transform towEye;
        private float dwell, nextSpray;
        private bool loaded, reachedTow, pickupFreed, passedEntry;
        private int cleared;
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
        internal int Cleared => cleared;
        internal bool AtPlowStart(Vector3 point) => Active == WinterJob.Plow && !PlowFitted && Near(point, plowStart, 5f);
        internal bool Reconnecting(bool attached) => TowBody != null && reachedTow && !attached && !TowInBay();
        internal bool ApproachingEntry => TowBody != null && !reachedTow && !passedEntry;
        internal Vector3 EntryPoint => Active == WinterJob.Tree ? treeEntry : rescueEntry;
        internal bool MarkingPullPoint(Vector3 position) => TowBody != null && !reachedTow && Near(position, TowBody.position, 34f);
        internal Vector3 PullPoint => Active == WinterJob.Tree ? treePullPoint : rescuePullPoint;
        internal bool AtPullPoint(Vector3 position) => Near(position, PullPoint, 3.5f);
        internal float HoldProgress => Mathf.Clamp01(dwell / 1.2f);

        internal WinterRoadContracts(RecoveryWorld world, Transform owner)
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
            plowStart = Ground(new Vector3(-30.5f, 22f, 10.45f), out _);
            plowDirection = new Vector3(-.987f, 0, .16f).normalized;
            for (int i = 0; i < DriftCount; i++)
                driftPoints[i] = Ground(plowStart + plowDirection * (6f + i * DriftSpacing), out _);
        }

        internal bool MissedPlowSection(Vector3 front)
            => PlowFitted && Vector3.Dot(front - Destination, plowDirection) > 2f;

        internal bool BladeLinedUp(RecoveryVehicle driver)
        {
            if (!PlowFitted) return false;
            var side = Vector3.Cross(Vector3.up, plowDirection);
            return Vector3.Dot(driver.transform.forward, plowDirection) >= .7f &&
                Mathf.Abs(Vector3.Dot(blade.transform.position - plowStart, side)) <= .95f;
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
                        int nextDrift = Array.FindIndex(clearedMetres, metres => metres < DriftSpacing);
                        return PlowFitted ? driftPoints[Mathf.Max(0, nextDrift)] : plowStart;
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
                    case WinterJob.Plow: return PlowFitted ? $"Snowdrift · {cleared}/{DriftCount} cleared" : "Plow fitting point";
                    default: return "";
                }
            }
        }

        internal string Instruction(RecoveryVehicle driver, bool near, bool attached, bool rearCable, bool canHook)
        {
            if (Active == WinterJob.Plow)
            {
                if (!PlowFitted) return near ? "Stop inside the fitting bay" : "Drive to the plow fitting point";
                if (MissedPlowSection(driver.FrontHitch)) return "Snow left behind · back up past the marker";
                return BladeLinedUp(driver) ? "Drive forward through the drift" : "Center the blade between the poles";
            }
            if (Active == WinterJob.Supply)
            {
                if (HoldProgress > 0) return loaded ? "Unloading crate · stay stopped" : "Loading crate · stay stopped";
                return near ? (loaded ? "Park inside the marked unloading bay" : "Park inside the marked loading bay") : DestinationName;
            }
            if (!reachedTow)
            {
                if (ApproachingEntry)
                    return Active == WinterJob.Tree ? "Take the marked opening before the fallen tree" : "Follow the marked entrance into the meadow";
                if (!MarkingPullPoint(driver.transform.position)) return DestinationName;
                if (Active == WinterJob.Tree)
                    return canHook && AtPullPoint(driver.transform.position)
                        ? "Attach the cable and reel in from here" : "Circle around the tree to the marked pull point";
                return canHook && rearCable ? "Attach the rear cable and reel in" : "Back into the marked pull point";
            }
            if (Reconnecting(attached)) return "Return to the tow eye and reconnect";
            if (ExtractingPickup) return "Hold brake and reel the pickup toward firm ground";
            if (TowInBay()) return "Hold still while the load settles";
            if (Active == WinterJob.Tree) return "Reel in until the whole trunk clears the road";
            return rearCable ? "Tow the pickup into the marked bay" : "Reel in, then reconnect from the rear to tow";
        }

        internal void Begin(WinterJob job)
        {
            Clear();
            Remove(finishedScenery[(int)job]);
            finishedScenery[(int)job] = null;
            Active = job;
            props = new GameObject(Offers[(int)job].Name);
            props.transform.SetParent(owner, true);
            EnsureMaterials();
            switch (job)
            {
                case WinterJob.Supply:
                    BuildCargo();
                    currentMarker = Zone("Loading bay", workshop, 5.5f);
                    deliveryMarker = Zone("Unloading bay", forest, 5.5f);
                    break;
                case WinterJob.Recovery:
                    BuildRescue();
                    currentMarker = Zone("Recovery pull point", rescuePullPoint, 3f);
                    var exit = new Vector3(9.6f, 0, -3.3f).normalized;
                    Arrow(currentMarker.transform, rescuePullPoint + exit * 2.9f, exit);
                    Gate(rescueEntry, Vector3.right, "Meadow entrance");
                    deliveryMarker = Zone("Receiving bay", workshop, 6f);
                    break;
                case WinterJob.Tree:
                    BuildTree();
                    currentMarker = Zone("Tree pull point", treePullPoint, 2.5f);
                    Gate(treeEntry, Vector3.forward, "Lay-by approach");
                    deliveryMarker = Zone("Lay-by", layBy, 4f);
                    break;
                case WinterJob.Plow:
                    BuildDrift();
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
                    if (!Held(Near(driver.transform.position, Destination, 5.5f) && driver.Speed < .65f && !attached)) return false;
                    if (loaded) return true;
                    loaded = true;
                    SwitchToDeliveryMarker();
                    crate.transform.SetParent(driver.transform, false);
                    crate.transform.localPosition = driver.transform.InverseTransformPoint(driver.RearHitch) + new Vector3(0, .83f, .48f);
                    crate.transform.localRotation = Quaternion.identity;
                    return false;
                case WinterJob.Recovery:
                    Vector3 shift = TowBody.position - rescue;
                    if (reachedTow && !pickupFreed && shift.x * shift.x + shift.z * shift.z >= 12.25f)
                    {
                        pickupFreed = true;
                        SwitchToDeliveryMarker();
                    }
                    return Held(pickupFreed && TowInBay() && TowBody.linearVelocity.magnitude < 1.1f);
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
            Vector3 side = treeHeading * Vector3.right;
            float overhang = Mathf.Abs(Vector3.Dot(TowBody.transform.forward, side)) * 4.6f;
            float clearance = Vector3.Dot(TowBody.position - treeRoad, side) - overhang;
            return Near(TowBody.position, layBy, 4f) && clearance > 3.1f;
        }

        private bool Held(bool condition)
        {
            dwell = condition ? dwell + Time.deltaTime : 0;
            if (dwell < 1.2f) return false;
            dwell = 0;
            return true;
        }

        internal void FitPlow(RecoveryVehicle driver)
        {
            if (!AtPlowStart(driver.transform.position) || driver.Speed > .65f) return;
            currentMarker.SetActive(false);
            blade = new GameObject("Service plow blade");
            blade.transform.SetParent(driver.transform, false);
            blade.transform.localPosition = driver.transform.InverseTransformPoint(driver.FrontHitch) + new Vector3(0, -.30f, .52f);
            Piece(blade.transform, "Moldboard", PrimitiveType.Cube, Vector3.zero, new Vector3(BladeWidth, .60f, .16f), orange, Quaternion.Euler(-18, 0, 0));
            var edge = Piece(blade.transform, "Cutting edge", PrimitiveType.Cube, new Vector3(0, -.28f, .09f), new Vector3(BladeWidth, .13f, .19f), steel);
            Piece(blade.transform, "Mount", PrimitiveType.Cube, new Vector3(0, .03f, -.34f), new Vector3(1.1f, .28f, .65f), steel);
            plowSnow = blade.AddComponent<ParticleSystem>();
            plowSnow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = plowSnow.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.09f, .25f);
            main.startSpeed = 0;
            main.gravityModifier = .8f;
            main.maxParticles = 90;
            var emission = plowSnow.emission;
            emission.enabled = false;
            var shape = plowSnow.shape;
            shape.enabled = false;
            var renderer = plowSnow.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = edge.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterial = particles;
            plowSnow.Play();
        }

        internal void FixedTick(RecoveryVehicle driver)
        {
            if (Active != WinterJob.Plow || !PlowFitted || cleared >= DriftCount) return;
            var forward = driver.transform.forward;
            float speed = Vector3.Dot(driver.Body.linearVelocity, forward);
            if (speed < .15f || Vector3.Dot(forward, plowDirection) < .7f ||
                !(driver.TryGetWheelContact(0, out _) || driver.TryGetWheelContact(1, out _))) return;
            Vector3 contact = blade.transform.position;
            var side = Vector3.Cross(Vector3.up, plowDirection);
            for (int i = 0; i < DriftCount; i++)
            {
                if (clearedMetres[i] >= DriftSpacing) continue;
                var delta = contact - driftPoints[i];
                if (Mathf.Abs(delta.y) > 1.5f || Mathf.Abs(Vector3.Dot(delta, side)) > .95f ||
                    Mathf.Abs(Vector3.Dot(delta, plowDirection)) > DriftSpacing * .5f + .12f) continue;
                float before = clearedMetres[i];
                clearedMetres[i] = Mathf.Min(DriftSpacing, before + speed * Time.fixedDeltaTime * 1.24f);
                float remaining = 1 - clearedMetres[i] / DriftSpacing;
                var scale = drifts[i].transform.localScale;
                scale.y = Mathf.Max(.015f, .68f * remaining);
                drifts[i].transform.localScale = scale;
                piles[i].transform.localScale = new Vector3(.85f, .04f + (1 - remaining) * .32f, 3.2f);
                if (clearedMetres[i] >= DriftSpacing)
                {
                    drifts[i].SetActive(false);
                    cleared++;
                }
                driver.Body.AddForceAtPosition(-forward * (300 + 220 * speed), contact);
                if (Time.time >= nextSpray)
                {
                    nextSpray = Time.time + .065f;
                    for (int j = 0; j < 5; j++)
                    {
                        float sign = j == 0 ? -1 : 1;
                        var snowBurst = new ParticleSystem.EmitParams
                        {
                            position = contact + side * (sign * UnityEngine.Random.Range(.55f, 1.4f)),
                            velocity = side * (sign * UnityEngine.Random.Range(1.6f, 3.7f)) + Vector3.up * UnityEngine.Random.Range(1.2f, 2.7f),
                            rotation3D = UnityEngine.Random.rotation.eulerAngles
                        };
                        plowSnow.Emit(snowBurst, 1);
                    }
                }
                break;
            }
        }

        // Each removed drift reveals the existing hard-packed road underneath.
        internal float DriftAt(Vector3 point)
        {
            if (Active != WinterJob.Plow) return 0;
            var delta = point - driftPoints[0];
            float along = Vector3.Dot(delta, plowDirection);
            if (Mathf.Abs(delta.y) > 1.4f || Mathf.Abs(Vector3.Dot(delta, Vector3.Cross(Vector3.up, plowDirection))) > 1.7f ||
                along < -1.5f || along >= (DriftCount - .5f) * DriftSpacing) return 0;
            int i = Mathf.Clamp(Mathf.RoundToInt(along / DriftSpacing), 0, DriftCount - 1);
            return 1 - clearedMetres[i] / DriftSpacing;
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

        private void BuildRescue()
        {
            var target = UnityEngine.Object.Instantiate(world.Casualty, props.transform);
            target.name = "Stranded Meadow pickup";
            target.SetModel(3);
            target.SetPaint(2);
            target.SetRecoveryMass(1350);
            target.SetRecoveryJammed(false);
            target.WinterDriving = true;
            target.SetSpawn(rescue, rescueHeading);
            target.SetSimulation(true);
            target.gameObject.SetActive(true);
            TowBody = target.Body;
            towEye = Eye(target.transform, target.transform.InverseTransformPoint(target.FrontHitch));
        }

        private void BuildTree()
        {
            var root = new GameObject("Fallen pine");
            root.transform.SetParent(props.transform, false);
            root.transform.SetPositionAndRotation(treeRoad + Vector3.up * .37f, treeHeading * Quaternion.Euler(0, 82f, 0));
            var trunk = Piece(root.transform, "Trunk", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.55f, 4.55f, .55f), wood, Quaternion.Euler(90, 0, 0), true);
            var collider = trunk.GetComponent<CapsuleCollider>();
            collider.radius = .49f;
            collider.sharedMaterial = snowyBark;
            Piece(root.transform, "Snow cap", PrimitiveType.Sphere, new Vector3(0, .21f, -.35f), new Vector3(.56f, .26f, 7.8f), snow);
            Piece(root.transform, "Split base", PrimitiveType.Cylinder, new Vector3(0, 0, -4.56f), new Vector3(.47f, .055f, .47f), orange, Quaternion.Euler(90, 0, 0));
            for (int i = 0; i < 6; i++)
            {
                float sign = i % 2 == 0 ? 1 : -1;
                Piece(root.transform, "Broken limb", PrimitiveType.Cylinder, new Vector3(sign * .48f, .09f, -2.0f + i * 1.03f),
                    new Vector3(.13f, .77f, .13f), wood, Quaternion.Euler(28, 0, sign * 58));
            }
            TowBody = root.AddComponent<Rigidbody>();
            TowBody.mass = 480f;
            TowBody.linearDamping = .8f;
            TowBody.angularDamping = 2.8f;
            TowBody.interpolation = RigidbodyInterpolation.Interpolate;
            TowBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            towEye = Eye(root.transform, new Vector3(0, .17f, 4.3f));
            var verge = treeHeading * Vector3.forward * 1.5f;
            Stake(layBy + verge, "Lay-by marker");
            Stake(layBy - verge, "Lay-by marker");
        }

        private void BuildDrift()
        {
            var rotation = Quaternion.LookRotation(plowDirection);
            var side = rotation * Vector3.right;
            Stake(plowStart + side * 2.1f, "Blade station");
            Stake(plowStart - side * 2.1f, "Blade station");
            for (int i = 0; i < DriftCount; i++)
            {
                var point = driftPoints[i];
                drifts[i] = Snowbank("Snowdrift", point + Vector3.up * .035f, new Vector3(3.8f, .68f, 4.35f), rotation);
                var edge = Ground(point + side * 1.95f, out _);
                piles[i] = Snowbank("Plowed snow", edge + Vector3.up * .01f, new Vector3(.85f, .04f, 3.2f), rotation);
                if (i % 2 == 0 || i == DriftCount - 1)
                {
                    Stake(point + side * 2.55f, "Snow pole");
                    Stake(point - side * 2.55f, "Snow pole");
                }
            }
        }

        private GameObject Snowbank(string name, Vector3 point, Vector3 scale, Quaternion rotation)
        {
            if (driftMesh == null) driftMesh = BuildDriftMesh();
            var bank = new GameObject(name);
            bank.transform.SetParent(props.transform, false);
            bank.transform.SetPositionAndRotation(point, rotation);
            bank.transform.localScale = scale;
            bank.AddComponent<MeshFilter>().sharedMesh = driftMesh;
            bank.AddComponent<MeshRenderer>().sharedMaterial = snow;
            return bank;
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

        internal void Clear()
        {
            Remove(currentMarker);
            Remove(deliveryMarker);
            Remove(crate);
            Remove(blade);
            Remove(props);
            props = crate = blade = null;
            currentMarker = deliveryMarker = null;
            plowSnow = null;
            TowBody = null;
            towEye = null;
            Active = null;
            loaded = reachedTow = pickupFreed = passedEntry = false;
            dwell = nextSpray = 0;
            cleared = 0;
            Array.Clear(clearedMetres, 0, DriftCount);
        }

        internal void Finish(WinterJob job)
        {
            if (job == WinterJob.Tree || job == WinterJob.Plow)
            {
                if (TowBody != null) TowBody.isKinematic = true;
                finishedScenery[(int)job] = props;
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
            Clear();
            foreach (var scenery in finishedScenery) Remove(scenery);
            foreach (var material in new[] { wood, steel, orange, snow, paint, particles, marking })
                if (material != null) UnityEngine.Object.Destroy(material);
            if (snowyBark != null) UnityEngine.Object.Destroy(snowyBark);
            if (driftMesh != null) UnityEngine.Object.Destroy(driftMesh);
        }
    }
}
