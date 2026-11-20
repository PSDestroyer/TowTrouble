using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    // The profile's existing mission receipts are the authority for all permanent region changes.
    // Active-job props remain disposable, so abandon/replay never deletes a restored road or depot.
    internal sealed class WinterRegion : IDisposable
    {
        private readonly RecoveryWorld world;
        private readonly RecoveryProfile profile;
        private readonly Transform root;
        private readonly WinterRoadContracts[] sites = new WinterRoadContracts[WinterRoadContracts.Offers.Length];
        private readonly GameObject[] deliveries = new GameObject[WinterRoadContracts.Offers.Length];
        private readonly List<Vector3> forestRoute = new(), reverseRoute = new();
        private readonly List<Vector3> packedRoute = new(), reversePackedRoute = new(), detourRoute = new(), reverseDetourRoute = new();
        private readonly List<Transform> routeAnchors = new();
        private readonly List<Mesh> meshes = new();
        private readonly List<GameObject> icePatches = new();
        private readonly List<Transform> windBanks = new();
        private readonly List<(Transform Tree, Vector3 Position)> relocatedTrees = new();
        private readonly Material timber, metal, blue, orange, white, glow, ice;
        private readonly GameObject stationLights;
        private Vector3 viewer;
        private int activeJob = -1, appliedJobs = -1, appliedSurveys = -1;
        private float nextVisual;
        private readonly float packedMetres, forestMetres, detourMetres;
        internal IReadOnlyList<Transform> RouteAnchors => routeAnchors;
        internal Vector3 ServicePosition { get; }
        private bool Done(int id) => (profile.winterJobs & (1 << id)) != 0;
        internal bool ServiceReady => Done(3) && Done(1) && Done(0) && (profile.winterSurveys & 1) != 0;
        internal string ProgressText => ServiceReady ? "Pinewood · service restored · customize at the regional station" :
            !Done(3) ? "Pinewood 1/4 · reopen the access road" : !Done(1) ? "Pinewood 2/4 · recover the service pickup" :
            !Done(0) ? "Pinewood 3/4 · deliver the station supplies" : "Pinewood 4/4 · complete the weather report";

        internal WinterRegion(RecoveryWorld world, Transform parent, RecoveryProfile profile)
        {
            this.world = world; this.profile = profile;
            root = new GameObject("Winter region progress").transform;
            root.SetParent(parent, false);
            timber = Material("Winter service timber", new Color(.22f, .14f, .08f));
            metal = Material("Winter service steel", new Color(.18f, .24f, .28f));
            blue = Material("Forest route blue", new Color(.14f, .48f, .7f));
            orange = Material("Winter service orange", new Color(.96f, .39f, .08f));
            white = Material("Winter region snow", new Color(.88f, .94f, .98f));
            glow = Material("Restored station warmth", new Color(1, .68f, .24f));
            glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(1, .52f, .12f) * 2);
            ice = Material("Exposed road ice", new Color(.62f, .77f, .83f)); ice.SetFloat("_Smoothness", .85f);

            var depot = Bay("B").Position;
            ServicePosition = Ground(depot + new Vector3(11, 0, 5));
            stationLights = BuildStation();
            BuildForestRoute();
            ClearForestCorridor();
            forestMetres = Length(forestRoute);
            AppendRoad(packedRoute, Bay("E").Position, depot);
            reversePackedRoute.AddRange(packedRoute); reversePackedRoute.Reverse();
            packedMetres = Length(packedRoute);
            // Before the timber clearance, use the northern access reopened by Pinewood's first job.
            AppendRoad(detourRoute, Bay("E").Position, world.GaragePosition);
            AppendRoad(detourRoute, world.GaragePosition, WinterRoadContracts.Offers[3].RoadStart);
            AppendRoad(detourRoute, WinterRoadContracts.Offers[3].RoadStart, depot);
            reverseDetourRoute.AddRange(detourRoute); reverseDetourRoute.Reverse();
            detourMetres = Length(detourRoute);
            BuildIce();
            RefreshProgress();
        }

        private RecoveryDropoff Bay(string id) => Array.Find(world.Map.Deliveries, bay => bay.Id == id);
        internal void SetActiveJob(int id)
        {
            activeJob = id;
            ShowNearbySites();
        }
        internal void RefreshProgress()
        {
            if (appliedJobs == profile.winterJobs && appliedSurveys == profile.winterSurveys) return;
            int changed = appliedJobs ^ profile.winterJobs;
            for (int id = 0; id < sites.Length; id++)
            {
                if ((changed & (1 << id)) != 0 && sites[id] != null) { sites[id].Dispose(); sites[id] = null; }
                if (!Done(id))
                {
                    if (deliveries[id] != null) { UnityEngine.Object.Destroy(deliveries[id]); deliveries[id] = null; }
                    continue;
                }
                if (deliveries[id] != null) continue;
                var offer = WinterRoadContracts.Offers[id];
                if (offer.Kind == WinterJob.Supply || offer.Kind == WinterJob.Recovery) BuildDelivery(id, offer);
            }
            stationLights.SetActive(ServiceReady);
            appliedJobs = profile.winterJobs; appliedSurveys = profile.winterSurveys;
        }
        internal void Tick(Vector3 player, WinterWeatherState weather)
        {
            viewer = player;
            RefreshProgress();
            ShowNearbySites();
            if (Time.time < nextVisual) return;
            nextVisual = Time.time + .3f;
            foreach (var patch in icePatches) patch.SetActive(weather.Ice > .35f);
            for (int i = 0; i < windBanks.Count; i++)
                windBanks[i].localScale = new Vector3(4.6f, .06f + weather.Gusts * .3f, 2.6f);
        }

        private void ShowNearbySites()
        {
            for (int id = 0; id < sites.Length; id++)
            {
                var offer = WinterRoadContracts.Offers[id];
                if (offer.Kind != WinterJob.Plow && offer.Kind != WinterJob.Tree) continue;
                bool nearby = (viewer - offer.RoadStart).sqrMagnitude < 155f * 155f;
                if (nearby && activeJob != id && sites[id] == null)
                {
                    sites[id] = new WinterRoadContracts(world, root, profile.winterJobs);
                    sites[id].BuildWorldSite(id, Done(id));
                }
                sites[id]?.ShowWorldSite(nearby && activeJob != id);
            }
        }

        internal float SnowAt(Vector3 point, WinterWeatherState weather)
        {
            float depth = 0;
            for (int id = 0; id < sites.Length; id++)
            {
                if (id == activeJob || sites[id] == null || WinterRoadContracts.Offers[id].Kind != WinterJob.Plow) continue;
                if ((point - WinterRoadContracts.Offers[id].RoadStart).sqrMagnitude > 45f * 45f) continue;
                depth = Mathf.Max(depth, sites[id].SnowAt(point));
            }
            float distance = RouteDistance(point);
            if (distance < 3.2f)
                depth = Mathf.Max(depth, Mathf.SmoothStep(.55f + weather.Gusts * .18f, 0, Mathf.InverseLerp(1.4f, 3.2f, distance)));
            return depth;
        }

        internal List<Vector3> Route(bool reverse) => reverse ? reverseRoute : forestRoute;
        internal List<Vector3> RoadRoute(bool reverse) => Done(2)
            ? reverse ? reversePackedRoute : packedRoute : reverse ? reverseDetourRoute : detourRoute;
        internal Vector3 RouteTarget(Vector3 position, bool reverse, bool forest)
        {
            var route = forest ? Route(reverse) : RoadRoute(reverse);
            int next = 1; float best = float.PositiveInfinity;
            for (int i = 1; i < route.Count; i++)
            {
                var delta = route[i] - route[i - 1];
                float t = Mathf.Clamp01(Vector3.Dot(position - route[i - 1], delta) / Mathf.Max(.01f, delta.sqrMagnitude));
                float distance = (position - route[i - 1] - delta * t).sqrMagnitude;
                if (distance < best) { best = distance; next = i; }
            }
            if ((route[next] - position).sqrMagnitude < 7 * 7 && next < route.Count - 1) next++;
            return route[next];
        }
        internal string RoutePlan(bool forest) => forest
            ? "Forest cut · ~" + Mathf.RoundToInt(forestMetres) + " m · deep snow · blue stakes" + (Done(2) ? " · timber cleared" : " · fallen pine near depot")
            : "Packed road · ~" + Mathf.RoundToInt(Done(2) ? packedMetres : detourMetres) + " m · " +
                (Done(2) ? "timber link reopened" : Done(3) ? "safe northern detour" : "northern access needs plowing");

        private void AppendRoad(List<Vector3> result, Vector3 from, Vector3 to)
        {
            var road = new List<Vector3>();
            if (world.Map.TryGetRoadRoute(from, to, road))
            {
                if (result.Count == 0 || Vector3.Distance(result[result.Count - 1], from) > 1) result.Add(from);
                result.AddRange(road);
            }
            else result.Add(from);
            result.Add(to);
        }

        private void BuildForestRoute()
        {
            // The cut stays east of the lake and joins the existing timber clearance near Pinewood.
            Vector3[] nodes = { Bay("E").Position, new(-90, 20, -242), new(-108, 22, -206), new(-128, 22, -166),
                new(-148, 22, -120), new(-164, 22, -81), Bay("B").Position };
            foreach (var node in nodes) forestRoute.Add(Ground(node));
            reverseRoute.AddRange(forestRoute); reverseRoute.Reverse();
            for (int i = 1; i < forestRoute.Count; i++)
            {
                var a = forestRoute[i - 1]; var b = forestRoute[i]; var delta = b - a;
                var side = Vector3.Cross(Vector3.up, delta).normalized;
                int count = Mathf.CeilToInt(delta.magnitude / 11f);
                for (int step = 0; step < count; step++)
                {
                    var point = Vector3.Lerp(a, b, step / (float)count);
                    Post(point + side * 2.7f, blue); Post(point - side * 2.7f, blue);
                }
                if (i == 2 || i == 4)
                {
                    var point = Ground(a + side * 4.5f);
                    Post(point, orange);
                    routeAnchors.Add(Piece("Forest cut winch eye", PrimitiveType.Sphere, point + Vector3.up * .85f, Vector3.one * .24f, orange).transform);
                    var bank = Piece("Wind drift off the maintained road", PrimitiveType.Sphere,
                        Ground(Vector3.Lerp(a, b, .5f)) + Vector3.up * .04f, new Vector3(4.6f, .06f, 2.6f), white);
                    bank.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(delta, Vector3.up));
                    windBanks.Add(bank.transform);
                }
            }
        }
        private float RouteDistance(Vector3 point)
        {
            float best = float.PositiveInfinity;
            for (int i = 1; i < forestRoute.Count; i++)
            {
                var a = forestRoute[i - 1]; var line = forestRoute[i] - a;
                float t = Mathf.Clamp01(Vector3.Dot(point - a, line) / Mathf.Max(.01f, line.sqrMagnitude));
                best = Mathf.Min(best, Vector3.Distance(point, a + line * t));
            }
            // Keep the loading rings and their road exits clear of the shortcut's additional resistance.
            if ((point - forestRoute[0]).sqrMagnitude < 12 * 12 || (point - forestRoute[forestRoute.Count - 1]).sqrMagnitude < 12 * 12) return 100;
            return best;
        }

        private void ClearForestCorridor()
        {
            // Make real access through scene vegetation, moving both trunks and their visuals.
            // The side-pull recovery also needs room to turn between its two working positions.
            var site = Array.Find(world.Map.Sites, s => s.Id == "R02");
            var exit = Vector3.ProjectOnPlane(site.ExitDirection, Vector3.up).normalized;
            var side = Vector3.Cross(Vector3.up, exit);
            var pads = new[] { site.Position - exit * 5 + side * 11, site.Position + exit * 14 };
            var visited = new HashSet<Transform>();
            foreach (var collider in UnityEngine.Object.FindObjectsByType<CapsuleCollider>(FindObjectsSortMode.None))
            {
                if (collider.gameObject.scene != world.gameObject.scene) continue;
                var tree = collider.transform;
                while (tree != null && !RecoveryOccludingTree.IsStandingTree(tree.name)) tree = tree.parent;
                if (tree == null || !visited.Add(tree)) continue;
                var position = tree.position;
                Vector3 nearest = position, normal = Vector3.zero; float best = float.PositiveInfinity;
                for (int i = 1; i < forestRoute.Count; i++)
                {
                    var line = Vector3.ProjectOnPlane(forestRoute[i] - forestRoute[i - 1], Vector3.up);
                    float t = Mathf.Clamp01(Vector3.Dot(position - forestRoute[i - 1], line) / Mathf.Max(.01f, line.sqrMagnitude));
                    var point = forestRoute[i - 1] + line * t;
                    float distance = Vector3.ProjectOnPlane(position - point, Vector3.up).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = point; normal = Vector3.Cross(Vector3.up, line).normalized; }
                }
                bool move = best <= 3.8f * 3.8f && Mathf.Abs(position.y - nearest.y) <= 5;
                float sign = Vector3.Dot(position - nearest, normal) < 0 ? -1 : 1;
                var destination = nearest + normal * (sign * 4.8f);
                foreach (var pad in pads)
                {
                    var offset = Vector3.ProjectOnPlane(position - pad, Vector3.up);
                    if (offset.sqrMagnitude > 7f * 7f || Mathf.Abs(position.y - pad.y) > 5) continue;
                    move = true; destination = pad + (offset.sqrMagnitude > .01f ? offset.normalized : side) * 8.4f;
                    break;
                }
                if (!move) continue;
                relocatedTrees.Add((tree, position)); tree.position = Ground(destination);
            }
        }

        private GameObject BuildStation()
        {
            var p = ServicePosition;
            foreach (int x in new[] { -3, 3 })
            foreach (int z in new[] { -3, 3 })
                Piece("Regional service canopy post", PrimitiveType.Cube, Ground(p + new Vector3(x, 0, z)) + Vector3.up * 1.6f, new Vector3(.18f, 3.2f, .18f), timber);
            Piece("Regional service canopy", PrimitiveType.Cube, p + Vector3.up * 3.3f, new Vector3(7, .25f, 7), metal);
            Piece("Snow on service canopy", PrimitiveType.Cube, p + Vector3.up * 3.47f, new Vector3(7.05f, .12f, 7.05f), white);
            Piece("Station generator", PrimitiveType.Cube, p + new Vector3(4, .65f, 0), new Vector3(1.4f, 1.2f, 1), metal);
            var lit = new GameObject("Pinewood power restored"); lit.transform.SetParent(root, false);
            for (int i = -1; i <= 1; i += 2)
            {
                var lamp = Piece("Warm service light", PrimitiveType.Cube, p + new Vector3(i * 2, 3.08f, 0), new Vector3(.7f, .09f, .25f), glow);
                lamp.transform.SetParent(lit.transform, true);
                var light = lamp.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .65f, .3f);
                light.range = 6; light.intensity = 1.8f; light.shadows = LightShadows.None;
            }
            BuildSteam(lit.transform, p + new Vector3(4, 1.5f, 0));
            foreach (int x in new[] { -3, 3 }) Post(p + new Vector3(x, 0, -4), orange);
            return lit;
        }
        private void BuildSteam(Transform parent, Vector3 point)
        {
            var smoke = new GameObject("Generator exhaust").AddComponent<ParticleSystem>();
            smoke.transform.SetParent(parent, false); smoke.transform.position = point;
            var main = smoke.main; main.startLifetime = 2.8f; main.startSpeed = .4f; main.startSize = .22f;
            main.startColor = new Color(.67f, .7f, .73f, .28f); main.maxParticles = 24; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = smoke.emission; emission.rateOverTime = 4;
            var shape = smoke.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 12; shape.radius = .06f;
            smoke.transform.rotation = Quaternion.LookRotation(Vector3.up);
            var size = smoke.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 3));
            var render = smoke.GetComponent<ParticleSystemRenderer>();
            render.sharedMaterial = world.Drivers[0].GetComponent<RecoveryVehicleEffects>()?.VaporMaterial;
            render.shadowCastingMode = ShadowCastingMode.Off;
            var fade = smoke.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(0, 1) }); fade.color = gradient;
        }
        private void BuildDelivery(int id, WinterRoadContracts.Offer offer)
        {
            var p = Ground(Bay(offer.TargetBay).Position + new Vector3(9 + id % 3 * 3.5f, 0, -8 - id % 2 * 3));
            var item = new GameObject(offer.Name + " · delivered"); item.transform.SetParent(root, false); deliveries[id] = item;
            if (offer.Kind == WinterJob.Recovery)
            {
                var car = UnityEngine.Object.Instantiate(world.Casualty, item.transform);
                car.SetModel(offer.Model); car.SetPaint(2); car.SetSimulation(false);
                car.SetSpawn(p + Vector3.up * .65f, Quaternion.Euler(0, 90, 0)); car.Body.isKinematic = true;
                foreach (var collider in car.GetComponentsInChildren<Collider>()) collider.enabled = false;
                car.gameObject.SetActive(true);
            }
            else for (int i = 0; i < offer.CargoUnits; i++)
            {
                var box = Piece("Delivered supplies", PrimitiveType.Cube, p + new Vector3(i * .8f, .4f, 0), new Vector3(.7f, .8f, .7f), blue);
                box.transform.SetParent(item.transform, true);
            }
        }
        private void BuildIce()
        {
            for (int patch = 0; patch < WinterWeatherRules.IceCenters.Length; patch++)
            {
                var center = WinterWeatherRules.IceCenters[patch]; var along = WinterWeatherRules.IceDirections[patch];
                var side = Vector3.Cross(Vector3.up, along);
                var vertices = new Vector3[25]; var triangles = new int[72]; vertices[0] = Ground(center) + Vector3.up * .035f;
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2 / 24;
                    float outline = .93f + .07f * Mathf.Sin(i * 2.7f);
                    vertices[i + 1] = Ground(center + along * (Mathf.Cos(angle) * 7 * outline) + side * (Mathf.Sin(angle) * 1.45f * outline)) + Vector3.up * .035f;
                    triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % 24 + 1;
                }
                var mesh = new Mesh { name = "Exposed icy road patch", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); meshes.Add(mesh);
                var go = new GameObject("Ice during freeze", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(root, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = ice;
                icePatches.Add(go); go.SetActive(false);
            }
        }
        private void Post(Vector3 point, Material cap)
        {
            point = Ground(point);
            Piece("Route stake", PrimitiveType.Cylinder, point + Vector3.up * .45f, new Vector3(.075f, .48f, .075f), timber);
            Piece("Route reflector", PrimitiveType.Cylinder, point + Vector3.up * .88f, new Vector3(.095f, .09f, .095f), cap);
        }
        private GameObject Piece(string name, PrimitiveType kind, Vector3 position, Vector3 scale, Material material)
        {
            var item = GameObject.CreatePrimitive(kind); item.name = name; item.transform.SetParent(root, false);
            item.transform.position = position; item.transform.localScale = scale; item.GetComponent<Renderer>().sharedMaterial = material;
            var collider = item.GetComponent<Collider>(); collider.enabled = false; UnityEngine.Object.Destroy(collider); return item;
        }
        private static Material Material(string name, Color color) => new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        private static Vector3 Ground(Vector3 p) => Physics.Raycast(p + Vector3.up * 6, Vector3.down, out var hit, 36, 1 << 8, QueryTriggerInteraction.Ignore) ? hit.point : p;
        private static float Length(List<Vector3> points) { float sum = 0; for (int i = 1; i < points.Count; i++) sum += Vector3.Distance(points[i - 1], points[i]); return sum; }
        public void Dispose()
        {
            foreach (var site in sites) site?.Dispose();
            foreach (var tree in relocatedTrees) if (tree.Tree != null) tree.Tree.position = tree.Position;
            UnityEngine.Object.Destroy(root.gameObject);
            foreach (var mesh in meshes) UnityEngine.Object.Destroy(mesh);
            foreach (var material in new[] { timber, metal, blue, orange, white, glow, ice }) UnityEngine.Object.Destroy(material);
        }
    }
}
