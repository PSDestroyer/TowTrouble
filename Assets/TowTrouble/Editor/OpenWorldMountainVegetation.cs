using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string MountainVegetationGroup = "Mountain conifer groves and ground transitions";
        private static readonly string[] MountainStandingTreeFamilies =
        {
            "P_FirTree", "P_FirSapling", "P_Pine", "P_ChinaPine", "P_BirchTree", "P_BirchSapling",
            "P_OakTree", "P_OakSapling", "P_RedMaple", "P_KnotwoodTree", "P_Bamboo", "P_BananaTree",
            "P_PalmTree", "P_CoconutPalm", "P_DesertPalm", "P_DesertCoconutPalm", "P_TreeWindswept", "P_YuccaTree"
        };
        private static readonly string[] MountainGroundTreeParts = { "Fallen", "Stump", "Roots", "Branch", "Bush", "Sprout" };

        // A one-time authoring cache: adjacent crowns may meet, but trunks and duplicate trees cannot stack.
        private sealed class MountainTreeSpacing
        {
            private struct Footprint
            {
                public Vector3 TrunkPosition;
                public Vector3 CrownPosition;
                public float Crown;
                public float Trunk;
            }

            private readonly Dictionary<Vector2Int, List<Footprint>> cells = new Dictionary<Vector2Int, List<Footprint>>();
            private float largestCrown;

            public void Add(Vector3 position, Bounds bounds)
            {
                float crown = Mathf.Max(.7f, Mathf.Max(bounds.extents.x, bounds.extents.z));
                // Include any asymmetric canopy offset in the cell search extent.
                float reach = crown + Flat(bounds.center - position).magnitude;
                largestCrown = Mathf.Max(largestCrown, reach);
                Vector2Int cell = Cell(position);
                if (!cells.TryGetValue(cell, out var items)) cells.Add(cell, items = new List<Footprint>());
                items.Add(new Footprint { TrunkPosition = position, CrownPosition = bounds.center, Crown = crown, Trunk = Mathf.Max(.3f, bounds.size.y * .023f) });
            }

            public bool Clear(Vector3 position, float radius, bool crowns)
            {
                int reach = Mathf.CeilToInt((radius + largestCrown + .5f) / 16);
                Vector2Int center = Cell(position);
                for (int z = -reach; z <= reach; z++)
                for (int x = -reach; x <= reach; x++)
                {
                    if (!cells.TryGetValue(center + new Vector2Int(x, z), out var items)) continue;
                    foreach (var item in items)
                    {
                        float separation = radius + (crowns ? item.Crown : item.Trunk) + .3f;
                        if (Flat(position - (crowns ? item.CrownPosition : item.TrunkPosition)).sqrMagnitude < separation * separation) return false;
                    }
                }
                return true;
            }

            private static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / 16), Mathf.FloorToInt(p.z / 16));
        }

        private sealed class MountainGrove
        {
            public Vector3 Center;
            public Transform Parent;
            public float Radius;
            public int Trees;
        }

        // Caller supplies current terrain/map/road caches and creates cliffs before dressing their feet.
        private static void DressMountainLandscape(Transform root)
        {
            var previous = root.Find(MountainVegetationGroup);
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var spacing = CacheMountainTreeSpacing();
            var parent = PolishGroup(root, MountainVegetationGroup);
            var random = new System.Random(742619);
            var vertices = new Dictionary<Mesh, Vector3[]>();
            var materials = new Dictionary<Material, Material>();
            var groves = new List<MountainGrove>();
            var centers = new List<Vector3>();
            var budgets = new[] { 90, 104, 46, 10 };
            var planted = new int[4];
            Physics.SyncTransforms();

            // Jittered grove centers leave wide meadow/snow gaps; individual trees are never laid out on a grid.
            for (float z = 205; z < 425; z += 32)
            for (float x = -217; x < 230; x += 36)
            {
                var p = Ground(new Vector3(x + MountainRandom(random, -12, 12), 0, z + MountainRandom(random, -11, 11)));
                if (!MountainVegetationArea(p) || Mathf.PerlinNoise(p.x * .018f + 37, p.z * .018f + 19) < .37f) continue;
                centers.Add(p);
            }
            for (int i = centers.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                var swap = centers[i]; centers[i] = centers[other]; centers[other] = swap;
            }

            foreach (var center in centers)
            {
                int band = MountainVegetationBand(center.y);
                if (planted[band] >= budgets[band]) continue;
                float radius = band < 2 ? MountainRandom(random, 19, 27) : MountainRandom(random, 13, 20);
                var grove = new MountainGrove { Center = center, Radius = radius };
                int quota = Math.Min(budgets[band] - planted[band], band == 0 ? 14 : band == 1 ? 12 : band == 2 ? 7 : 3);
                float stretch = MountainRandom(random, .55f, .85f);
                var rotation = Quaternion.Euler(0, MountainRandom(random, 0, 360), 0);

                for (int attempt = 0; attempt < 130 && grove.Trees < quota; attempt++)
                {
                    float angle = MountainRandom(random, 0, Mathf.PI * 2);
                    float spread = Mathf.Sqrt(MountainRandom(random, 0, 1)) * radius;
                    var offset = rotation * new Vector3(Mathf.Cos(angle) * spread, 0, Mathf.Sin(angle) * spread * stretch);
                    var p = Ground(center + offset);
                    if (!MountainVegetationArea(p) || MountainVegetationBand(p.y) != band || MountainVegetationSlope(p) > (band < 2 ? 29 : 25)) continue;
                    MountainTreeChoice(random, band, out string prefab, out float height);
                    float estimate = height * .27f;
                    if (!MountainPlacementClear(p, estimate) || !spacing.Clear(p, estimate, true) || MountainTrunkObstructed(p, height)) continue;

                    if (grove.Parent == null) grove.Parent = PolishGroup(parent, "Conifer grove " + (groves.Count + 1).ToString("00"));
                    // ScenicPrefab's solid path treats snowy pine names as rocks; all trees get an explicit trunk capsule below.
                    var go = ScenicPrefab(prefab, p, height, MountainRandom(random, 0, 360), grove.Parent, false);
                    if (go == null) throw new InvalidOperationException("Missing mountain tree prefab: " + prefab);
                    StripMountainPrefabScripts(go);
                    ApplyMountainVegetationMaterials(go, materials);
                    SettleMountainVegetation(go, vertices, true, height * .007f);
                    var bounds = ScenicBounds(go);
                    float actualRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                    if (!MountainPlacementClear(bounds.center, actualRadius) || !spacing.Clear(bounds.center, actualRadius, true))
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                        continue;
                    }
                    AddMountainTrunk(go, p, height);
                    ConfigureMountainVegetationLod(go, band < 2 ? 420 : 360);
                    spacing.Add(p, bounds);
                    grove.Trees++;
                    planted[band]++;
                }
                if (grove.Trees > 0) groves.Add(grove);
                else if (grove.Parent != null) UnityEngine.Object.DestroyImmediate(grove.Parent.gameObject);
            }

            var groundDetails = PolishGroup(parent, "Cliff foot talus and grove understory");
            int details = DressMountainCliffFeet(groundDetails, spacing, random, vertices, materials);
            foreach (var grove in groves)
            {
                if (details >= 90) break;
                int band = MountainVegetationBand(grove.Center.y);
                int quota = band < 2 ? 3 : band == 2 ? 2 : 1;
                int accepted = 0;
                for (int attempt = 0; attempt < 24 && accepted < quota && details < 90; attempt++)
                {
                    float angle = MountainRandom(random, 0, Mathf.PI * 2);
                    float distance = MountainRandom(random, .45f, 1.1f) * grove.Radius;
                    var p = Ground(grove.Center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance);
                    bool snow = p.y > 132;
                    int choice = random.Next(7);
                    string prefab = choice == 0 ? (snow ? "P_PineStump1_SnowCover" : "P_FirStump1") :
                        choice == 1 && !snow ? "P_FirBranch1" : choice == 2 ? "P_RockClumpClassic3" :
                        snow ? (choice % 2 == 0 ? "P_BushSnowDead02" : "P_Bush2_Snowy") :
                        choice % 2 == 0 ? "P_Fern2" : "P_Bush1_Light";
                    float height = choice == 1 && !snow ? MountainRandom(random, .17f, .28f) : MountainRandom(random, .4f, snow ? .85f : 1.15f);
                    if (!PlaceMountainGroundDetail(prefab, p, height, groundDetails, spacing, random, vertices, materials)) continue;
                    accepted++; details++;
                }
            }
            int totalTrees = planted[0] + planted[1] + planted[2] + planted[3];
            Debug.Log($"Mountain dressing: {totalTrees}/250 trees in {groves.Count} groves " +
                $"(lower {planted[0]}, middle {planted[1]}, treeline {planted[2]}, summit {planted[3]}), {details}/90 ground details. " +
                "Existing trees, roads and recovery access retained.");
        }

        private static MountainTreeSpacing CacheMountainTreeSpacing()
        {
            var spacing = new MountainTreeSpacing();
            foreach (var t in environment.GetComponentsInChildren<Transform>())
            {
                if (!t.gameObject.activeInHierarchy || !MountainStandingTree(t.name) || (t.parent != null && MountainStandingTree(t.parent.name))) continue;
                var bounds = ScenicBounds(t.gameObject);
                if (bounds.size.y > .5f && bounds.max.z > 170 && bounds.min.x < 255 && bounds.max.x > -255)
                    spacing.Add(t.position, bounds);
            }
            return spacing;
        }

        private static bool MountainStandingTree(string name)
        {
            bool family = false;
            foreach (var prefix in MountainStandingTreeFamilies)
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { family = true; break; }
            if (!family) return false;
            foreach (var part in MountainGroundTreeParts)
                if (name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return false;
            return true;
        }

        private static bool MountainVegetationArea(Vector3 p) => p.z > 185 && p.z < 438 && p.y > 55 && p.x > -235 && p.x < 235;
        private static int MountainVegetationBand(float height) => height < 105 ? 0 : height < 133 ? 1 : height < 161 ? 2 : 3;
        private static float MountainRandom(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        private static float MountainVegetationSlope(Vector3 p)
        {
            var local = p - terrain.transform.position;
            var size = terrain.terrainData.size;
            return terrain.terrainData.GetSteepness(local.x / size.x, local.z / size.z);
        }

        private static void MountainTreeChoice(System.Random random, int band, out string prefab, out float height)
        {
            if (band == 0)
            {
                prefab = random.Next(5) == 0 ? "P_FirSapling1" : random.Next(2) == 0 ? "P_FirTree4" : "P_FirTree2";
                height = prefab == "P_FirSapling1" ? MountainRandom(random, 3.8f, 6) : MountainRandom(random, 10, 16);
            }
            else if (band == 1)
            {
                prefab = random.Next(3) == 0 ? "P_Pine02" : random.Next(2) == 0 ? "P_FirTree4" : "P_FirTree5";
                height = MountainRandom(random, 8, 13);
            }
            else
            {
                prefab = band == 3 && random.Next(4) == 0 ? "P_PineDead02_Snowy" : "P_Pine0" + (random.Next(3) + 1) + "_Snowy";
                height = band == 2 ? MountainRandom(random, 6, 10) : MountainRandom(random, 4, 6.5f);
            }
        }

        private static bool MountainTrunkObstructed(Vector3 p, float height)
        {
            // Reject cliff faces and other solid props, while allowing the supporting TerrainCollider.
            if (Physics.Raycast(p + Vector3.up * (height + 40), Vector3.down, out var hit, height + 42, 1 << 8, QueryTriggerInteraction.Ignore) &&
                !(hit.collider is TerrainCollider) && hit.point.y > p.y + .4f) return true;
            foreach (var collider in Physics.OverlapCapsule(p + Vector3.up * .65f, p + Vector3.up * Mathf.Min(4, height * .6f), .4f, 1 << 8, QueryTriggerInteraction.Ignore))
                if (!(collider is TerrainCollider)) return true;
            return false;
        }

        private static void AddMountainTrunk(GameObject go, Vector3 ground, float height)
        {
            var capsule = go.AddComponent<CapsuleCollider>();
            float trunkHeight = Mathf.Max(1.7f, height * .64f);
            capsule.center = go.transform.InverseTransformPoint(ground + Vector3.up * (trunkHeight * .5f));
            capsule.height = trunkHeight / Mathf.Abs(go.transform.lossyScale.y);
            capsule.radius = Mathf.Max(.16f, height * .018f) / Mathf.Max(Mathf.Abs(go.transform.lossyScale.x), Mathf.Abs(go.transform.lossyScale.z));
        }

        private static int DressMountainCliffFeet(Transform parent, MountainTreeSpacing spacing, System.Random random,
            Dictionary<Mesh, Vector3[]> vertices, Dictionary<Material, Material> materials)
        {
            int count = 0;
            var filters = environment.GetComponentsInChildren<MeshFilter>();
            // New mountain walls get the detail budget first; retained river/gorge faces are only a fallback.
            for (int pass = 0; pass < 2 && count < 30; pass++)
            {
                var faces = new List<Bounds>();
                foreach (var filter in filters)
                {
                    string name = filter.name;
                    bool current = name.StartsWith("Mountain wall ", StringComparison.Ordinal) || name.StartsWith("Mountain crest ", StringComparison.Ordinal) || name.StartsWith("Mountain buttress ", StringComparison.Ordinal) || name.StartsWith("Mountain slope face ", StringComparison.Ordinal);
                    bool legacy = name.StartsWith("Ridge cliff", StringComparison.Ordinal) || name.StartsWith("Massif", StringComparison.Ordinal) || name.StartsWith("Embedded escarpment", StringComparison.Ordinal);
                    if (!filter.gameObject.activeInHierarchy || (pass == 0 ? !current : current || !legacy)) continue;
                    var renderer = filter.GetComponent<Renderer>();
                    if (renderer != null && MountainVegetationArea(Ground(renderer.bounds.center))) faces.Add(renderer.bounds);
                }
                for (int i = faces.Count - 1; i > 0; i--)
                {
                    int other = random.Next(i + 1);
                    var swap = faces[i]; faces[i] = faces[other]; faces[other] = swap;
                }
                foreach (var bounds in faces)
                {
                    var local = bounds.center - terrain.transform.position;
                    var normal = terrain.terrainData.GetInterpolatedNormal(local.x / terrain.terrainData.size.x, local.z / terrain.terrainData.size.z);
                    var outward = Flat(normal).normalized;
                    if (outward.sqrMagnitude < .1f) outward = Vector3.back;
                    var side = Vector3.Cross(Vector3.up, outward);
                    float depth = Mathf.Abs(outward.x) * bounds.extents.x + Mathf.Abs(outward.z) * bounds.extents.z;
                    for (int i = 0; i < 4 && count < 30; i++)
                    {
                        var p = Ground(bounds.center + outward * (depth + MountainRandom(random, 1, 5)) + side * MountainRandom(random, -7, 7));
                        string prefab = i == 3 ? (p.y > 132 ? "P_BushSnowDead01" : "P_Bush1_Light") : "P_RockClumpClassic" + (random.Next(10) + 1);
                        if (PlaceMountainGroundDetail(prefab, p, MountainRandom(random, .35f, 1.1f), parent, spacing, random, vertices, materials)) count++;
                    }
                    if (count >= 30) break;
                }
            }
            return count;
        }

        private static bool PlaceMountainGroundDetail(string prefab, Vector3 p, float height, Transform parent, MountainTreeSpacing spacing,
            System.Random random, Dictionary<Mesh, Vector3[]> vertices, Dictionary<Material, Material> materials)
        {
            if (!MountainVegetationArea(p) || MountainVegetationSlope(p) > 29 || !MountainPlacementClear(p, 1.5f) || !spacing.Clear(p, .55f, false)) return false;
            var go = ScenicPrefab(prefab, p, height, MountainRandom(random, 0, 360), parent, false);
            if (go == null) throw new InvalidOperationException("Missing mountain ground prefab: " + prefab);
            StripMountainPrefabScripts(go);
            ApplyMountainVegetationMaterials(go, materials);
            var local = p - terrain.transform.position;
            var normal = terrain.terrainData.GetInterpolatedNormal(local.x / terrain.terrainData.size.x, local.z / terrain.terrainData.size.z);
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, normal, .8f)) * go.transform.rotation;
            SettleMountainVegetation(go, vertices, false, height * .1f);
            var bounds = ScenicBounds(go);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            if (!MountainPlacementClear(bounds.center, radius) || !spacing.Clear(p, Mathf.Min(radius, .8f), false))
            {
                UnityEngine.Object.DestroyImmediate(go);
                return false;
            }
            ConfigureMountainVegetationLod(go, prefab.StartsWith("P_Rock", StringComparison.Ordinal) ? 190 : 130);
            return true;
        }

        private static void StripMountainPrefabScripts(GameObject go)
        {
            // Vendor interaction/demo scripts are unnecessary for static dressing; trunk capsules are authored separately.
            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null) UnityEngine.Object.DestroyImmediate(behaviour);
        }

        private static void ApplyMountainVegetationMaterials(GameObject go, Dictionary<Material, Material> cache)
        {
            // Share the existing county grade across every LOD; missing counterparts keep their native material.
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    var source = slots[i];
                    if (source == null) continue;
                    if (!cache.TryGetValue(source, out var graded))
                    {
                        graded = AssetDatabase.LoadAssetAtPath<Material>(VisualRoot + "/Native " + source.name + ".mat") ?? source;
                        cache.Add(source, graded);
                    }
                    if (graded == source) continue;
                    slots[i] = graded;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = slots;
            }
        }

        private static void SettleMountainVegetation(GameObject go, Dictionary<Mesh, Vector3[]> vertices, bool tree, float embed)
        {
            // Settle the actual mesh feet, not merely a pivot/AABB corner that can float on a hillside.
            var bounds = ScenicBounds(go);
            float footTop = bounds.min.y + bounds.size.y * (tree ? .045f : .3f);
            float shift = float.PositiveInfinity;
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                // This authoring method runs in Edit Mode, where imported meshes remain readable without changing import settings.
                if (mesh == null) continue;
                if (!vertices.TryGetValue(mesh, out var points)) vertices.Add(mesh, points = mesh.vertices);
                foreach (var vertex in points)
                {
                    var p = filter.transform.TransformPoint(vertex);
                    if (p.y > footTop) continue;
                    shift = Mathf.Min(shift, Ground(p).y - p.y);
                }
            }
            if (!float.IsInfinity(shift)) go.transform.position += Vector3.up * (shift - embed);
            else go.transform.position -= Vector3.up * embed;
        }

        private static void ConfigureMountainVegetationLod(GameObject go, float distance)
        {
            // Keep native meshes, materials and intermediate LOD transitions; only adjust their final culling distance.
            var groups = go.GetComponentsInChildren<LODGroup>();
            if (groups.Length == 0) { ConfigureDetailLod(go, distance); return; }
            foreach (var group in groups)
            {
                group.RecalculateBounds();
                var lods = group.GetLODs();
                if (lods.Length == 0) continue;
                float scale = Mathf.Max(Mathf.Abs(group.transform.lossyScale.x), Mathf.Max(Mathf.Abs(group.transform.lossyScale.y), Mathf.Abs(group.transform.lossyScale.z)));
                float cutoff = Mathf.Clamp(group.size * scale / (distance * 1.1547f), .001f, .08f);
                if (lods.Length > 1) cutoff = Mathf.Min(cutoff, lods[lods.Length - 2].screenRelativeTransitionHeight * .8f);
                lods[lods.Length - 1].screenRelativeTransitionHeight = cutoff;
                group.SetLODs(lods);
            }
        }
    }
}
