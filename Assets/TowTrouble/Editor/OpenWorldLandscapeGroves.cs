using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string LandscapeGroveGroup = "Composed woodlands and meadow clearings";
        private const string LandscapeGroveSourceSuffix = " [grove source]";

        private sealed class LandscapeGrove
        {
            public readonly string Name;
            public readonly Vector3 Center;
            public readonly Vector2 Extent;
            public readonly Quaternion Rotation;
            public readonly int Additions;
            public readonly bool Conifer;
            public Transform Parent;
            public int Redistributed;
            public int Added;

            public LandscapeGrove(string name, float x, float z, float width, float depth, float yaw, int additions, bool conifer = false)
            {
                Name = name; Center = Ground(new Vector3(x, 0, z)); Extent = new Vector2(width, depth);
                Rotation = Quaternion.Euler(0, yaw, 0); Additions = additions; Conifer = conifer;
            }

            public float NormalizedDistance(Vector3 p)
            {
                var d = Quaternion.Inverse(Rotation) * (p - Center);
                return d.x * d.x / (Extent.x * Extent.x) + d.z * d.z / (Extent.y * Extent.y);
            }
        }

        private sealed class LandscapeTreeSource
        {
            public GameObject Object;
            public string Prefab;
            public float Height;
            public Vector3 Position;
        }

        private sealed class LandscapeTreeGrid
        {
            private struct Tree
            {
                public Vector3 CrownCenter;
                public Vector3 Trunk;
                public float Radius;
            }

            private readonly Dictionary<Vector2Int, List<Tree>> cells = new Dictionary<Vector2Int, List<Tree>>();
            private float largest;

            public void Add(Vector3 trunk, Bounds bounds)
            {
                float radius = Mathf.Max(.7f, Mathf.Max(bounds.extents.x, bounds.extents.z));
                largest = Mathf.Max(largest, radius + Flat(bounds.center - trunk).magnitude);
                var key = Cell(trunk);
                if (!cells.TryGetValue(key, out var list)) cells.Add(key, list = new List<Tree>());
                list.Add(new Tree { Trunk = trunk, CrownCenter = bounds.center, Radius = radius });
            }

            public bool Clear(Vector3 p, float radius, bool understory = false)
            {
                var cell = Cell(p);
                int reach = Mathf.CeilToInt((largest + radius + 1) / 20);
                for (int z = -reach; z <= reach; z++)
                for (int x = -reach; x <= reach; x++)
                {
                    if (!cells.TryGetValue(cell + new Vector2Int(x, z), out var list)) continue;
                    foreach (var tree in list)
                    {
                        // Modest interlocking foliage makes woodland; physical trunks remain separated.
                        float gap = understory ? radius + .65f : Mathf.Max(2.6f, (radius + tree.Radius) * .77f);
                        if (Flat(p - (understory ? tree.Trunk : tree.CrownCenter)).sqrMagnitude < gap * gap) return false;
                    }
                }
                return true;
            }

            private static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / 20), Mathf.FloorToInt(p.z / 20));
        }

        private sealed class LandscapeGroveContext
        {
            public readonly System.Random Random = new System.Random(481203);
            public readonly Dictionary<Mesh, Vector3[]> Vertices = new Dictionary<Mesh, Vector3[]>();
            public readonly Dictionary<Material, Material> Materials = new Dictionary<Material, Material>();
            public readonly LandscapeTreeGrid Trees = new LandscapeTreeGrid();
            public readonly float[,,] Paint = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);
        }

        private static void ComposeLandscapeGroves(Transform root)
        {
            var old = root.Find(LandscapeGroveGroup);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            RestoreLandscapeGroveSources();
            var parent = PolishGroup(root, LandscapeGroveGroup);
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "Compose landscape woodlands");
            var context = new LandscapeGroveContext();
            var groves = LandscapeGrovePlans();
            var sources = LandscapeGroveSources(groves);
            foreach (var tree in environment.GetComponentsInChildren<Transform>())
                if (tree.gameObject.activeInHierarchy && MountainStandingTree(tree.name) &&
                    (tree.parent == null || !MountainStandingTree(tree.parent.name))) context.Trees.Add(tree.position, ScenicBounds(tree.gameObject));
            Physics.SyncTransforms();

            int redistributed = 0, additions = 0, details = 0, sourcesTried = 0;
            // Originals retain their hierarchy/position. A source is hidden only after its equivalent replacement succeeds.
            foreach (var source in sources)
            {
                if (redistributed >= 160 || sourcesTried++ >= 260) break;
                var nearest = new List<LandscapeGrove>(groves);
                nearest.Sort((a, b) => Flat(a.Center - source.Position).sqrMagnitude.CompareTo(Flat(b.Center - source.Position).sqrMagnitude));
                int grovesTried = 0;
                foreach (var grove in nearest)
                {
                    if (grove.Center.y > 118 || grove.Redistributed >= 20 || Flat(grove.Center - source.Position).sqrMagnitude > 160 * 160) continue;
                    if (grovesTried++ >= 4) break;
                    if (!PlaceLandscapeTree(grove, parent, context, source.Prefab, source.Height, 70)) continue;
                    Undo.RecordObject(source.Object, "Redistribute isolated tree into woodland");
                    source.Object.name += LandscapeGroveSourceSuffix;
                    source.Object.SetActive(false);
                    grove.Redistributed++; redistributed++;
                    break;
                }
            }

            // Additions are bounded independently of redistribution: at most 140 more active trees across the whole pass.
            foreach (var grove in groves)
            {
                for (int i = 0; i < grove.Additions && additions < 140; i++)
                {
                    if (!PlaceLandscapeTree(grove, parent, context, null, 0, 90)) continue;
                    grove.Added++; additions++;
                }
                if (grove.Parent == null) continue;
                for (int i = 0; i < 2 && details < 40; i++)
                    if (PlaceLandscapeUnderstory(grove, context)) details++;
            }
            Debug.Log($"Landscape woodlands: {redistributed} legacy trees redistributed, {additions}/140 additional trees, {details}/40 understory props. " +
                "Roads, recoveries, water margins and the workshop clearing protected.");
        }

        private static LandscapeGrove[] LandscapeGrovePlans()
        {
            // World-space anchors follow the forest spurs, inland lake rim and outer workshop slopes seen in the regional review.
            // Ellipses leave the central workshop meadow, river bridges and long lake views open.
            return new[]
            {
                new LandscapeGrove("Western woodland", -385, 95, 40, 28, -20, 9, true),
                new LandscapeGrove("Forester upper woods", -343, 190, 32, 24, 40, 8, true),
                new LandscapeGrove("Western hollow", -414, -4, 31, 24, 18, 8),
                new LandscapeGrove("Forest fork woodland", -303, 4, 29, 20, -18, 8),
                new LandscapeGrove("River west hillside", -241, 109, 30, 24, 25, 8),
                new LandscapeGrove("Pine bank", -241, -22, 25, 19, -25, 7, true),
                new LandscapeGrove("Lake head woodland", -295, -109, 31, 20, 15, 8),
                new LandscapeGrove("Lake western setback", -429, -187, 27, 17, -10, 7),
                new LandscapeGrove("Lake eastern setback", -133, -165, 27, 19, 30, 7),
                new LandscapeGrove("Upper meadow edge", -128, 157, 31, 20, -20, 8),
                new LandscapeGrove("Workshop western edge", -82, -55, 21, 17, 40, 5),
                new LandscapeGrove("Workshop rear hillside", 24, 77, 24, 18, -35, 7),
                new LandscapeGrove("Workshop eastern slope", 111, 65, 30, 23, 16, 7),
                new LandscapeGrove("Eastern meadow woodland", 149, 154, 29, 22, 30, 7),
                new LandscapeGrove("Western foothill", -215, 224, 25, 18, 15, 6, true),
                new LandscapeGrove("Eastern foothill", 190, 260, 27, 18, -25, 6, true),
                new LandscapeGrove("Western treeline pocket", -165, 294, 23, 16, 35, 6, true),
                new LandscapeGrove("Middle treeline pocket", 78, 308, 22, 15, -20, 6, true),
                new LandscapeGrove("Eastern treeline pocket", 148, 337, 21, 14, 20, 6, true),
                new LandscapeGrove("Upper treeline pocket", -95, 321, 20, 13, -30, 6, true)
            };
        }

        private static void RestoreLandscapeGroveSources()
        {
            foreach (var t in environment.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.EndsWith(LandscapeGroveSourceSuffix, StringComparison.Ordinal)) continue;
                Undo.RecordObject(t.gameObject, "Restore woodland source for recomposition");
                t.name = t.name.Substring(0, t.name.Length - LandscapeGroveSourceSuffix.Length);
                t.gameObject.SetActive(true);
            }
        }

        private static List<LandscapeTreeSource> LandscapeGroveSources(LandscapeGrove[] groves)
        {
            var result = new List<LandscapeTreeSource>();
            // Only old blanket-scatter groups donate trees; hand-authored landmarks, new mountains and existing groves remain.
            foreach (string groupName in new[] { "Authored biome groves and rock formations", "Scenic biome details", "Biome dressing - So Stylized" })
            {
                var group = environment.Find(groupName);
                if (group == null) continue;
                foreach (var t in group.GetComponentsInChildren<Transform>())
                {
                    if (!t.gameObject.activeInHierarchy || !MountainStandingTree(t.name) || (t.parent != null && MountainStandingTree(t.parent.name))) continue;
                    var p = Ground(t.position);
                    if (p.x < -445 || p.x > 180 || p.z < -205 || p.z > 210 || p.y < 16 || p.y > 110) continue;
                    var bounds = ScenicBounds(t.gameObject);
                    float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                    if (!CompositionClear(p, radius) || ShoreDistance(p) < 20 + radius || RiverDistance(p) < 16 + radius) continue;
                    bool insideWoodland = false;
                    foreach (var grove in groves) if (grove.NormalizedDistance(p) < 1.2f) { insideWoodland = true; break; }
                    if (insideWoodland) continue;
                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                    string prefab = string.IsNullOrEmpty(path) ? null : Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrEmpty(prefab) || !MountainStandingTree(prefab)) continue;
                    result.Add(new LandscapeTreeSource { Object = t.gameObject, Position = t.position, Prefab = prefab, Height = bounds.size.y });
                }
            }
            result.Sort((a, b) =>
            {
                int hash = LandscapeSourceOrder(a.Position).CompareTo(LandscapeSourceOrder(b.Position));
                if (hash != 0) return hash;
                int x = a.Position.x.CompareTo(b.Position.x);
                return x != 0 ? x : a.Position.z.CompareTo(b.Position.z);
            });
            return result;
        }

        private static uint LandscapeSourceOrder(Vector3 p)
        {
            // Stable spatial shuffle spreads the bounded redistribution budget across the whole valley.
            unchecked
            {
                uint key = (uint)Mathf.RoundToInt(p.x * 10) * 73856093u ^ (uint)Mathf.RoundToInt(p.z * 10) * 19349663u;
                key ^= key >> 16; key *= 2246822519u; key ^= key >> 13;
                return key;
            }
        }

        private static Vector3 LandscapeGrovePoint(LandscapeGrove grove, System.Random random)
        {
            float angle = MountainRandom(random, 0, Mathf.PI * 2);
            float radius = Mathf.Sqrt(MountainRandom(random, 0, 1));
            // A gentle lobe in the boundary avoids both a regular disk and a dense uniform rectangle.
            float lobe = .87f + .13f * Mathf.Sin(angle * 3 + grove.Center.x * .03f);
            return Ground(grove.Center + grove.Rotation * new Vector3(Mathf.Cos(angle) * grove.Extent.x, 0, Mathf.Sin(angle) * grove.Extent.y) * (radius * lobe));
        }

        private static bool LandscapeGroveClear(Vector3 p, float radius, LandscapeGroveContext context)
        {
            p = Ground(p);
            if (p.y < 15 || p.y > 165 || DesertAt(p) || ShoreDistance(p) < 17 + radius || !CompositionClear(p, radius)) return false;
            if (MountainVegetationSlope(p) > (p.y > 128 ? 25 : 29)) return false;
            if (p.z > 185 && p.y > 55 && !MountainPlacementClear(p, radius)) return false;
            return LandscapePaintWeight(p, context, 5) < .7f;
        }

        private static float LandscapePaintWeight(Vector3 p, LandscapeGroveContext context, int layer)
        {
            if (layer >= context.Paint.GetLength(2)) return 0;
            var local = p - terrain.transform.position;
            int x = Mathf.Clamp(Mathf.RoundToInt(local.x / terrain.terrainData.size.x * (context.Paint.GetLength(1) - 1)), 0, context.Paint.GetLength(1) - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(local.z / terrain.terrainData.size.z * (context.Paint.GetLength(0) - 1)), 0, context.Paint.GetLength(0) - 1);
            return context.Paint[z, x, layer];
        }

        private static bool PlaceLandscapeTree(LandscapeGrove grove, Transform parent, LandscapeGroveContext context, string sourcePrefab, float sourceHeight, int attempts)
        {
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                var p = LandscapeGrovePoint(grove, context.Random);
                if (sourcePrefab != null && p.y > 118) continue;
                LandscapeTreeChoice(grove, p, context, out string prefab, out float height);
                if (sourcePrefab != null) { prefab = sourcePrefab; height = sourceHeight; }
                float estimate = Mathf.Max(1, height * .22f);
                if (!context.Trees.Clear(p, estimate) || !LandscapeGroveClear(p, estimate, context) || MountainTrunkObstructed(p, height)) continue;
                if (grove.Parent == null) grove.Parent = PolishGroup(parent, grove.Name);
                var go = ScenicPrefab(prefab, p, height, MountainRandom(context.Random, 0, 360), grove.Parent, false);
                if (go == null) throw new InvalidOperationException("Missing landscape grove prefab: " + prefab);
                StripMountainPrefabScripts(go);
                ApplyMountainVegetationMaterials(go, context.Materials);
                SettleMountainVegetation(go, context.Vertices, true, height * .007f);
                var bounds = ScenicBounds(go);
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                if (!LandscapeGroveClear(bounds.center, radius, context) || !context.Trees.Clear(bounds.center, radius))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    continue;
                }
                AddMountainTrunk(go, p, height);
                ConfigureMountainVegetationLod(go, 420);
                context.Trees.Add(p, bounds);
                return true;
            }
            return false;
        }

        private static void LandscapeTreeChoice(LandscapeGrove grove, Vector3 p, LandscapeGroveContext context, out string prefab, out float height)
        {
            var random = context.Random;
            float snow = LandscapePaintWeight(p, context, 4);
            if (p.y > 118 || snow > .25f)
            {
                // Mixed green/snow-covered conifers follow the actual painted edge rather than a single altitude cutoff.
                bool snowy = MountainRandom(random, 0, 1) < Mathf.Clamp01(snow * .85f + Mathf.InverseLerp(125, 157, p.y) * .3f);
                prefab = "P_Pine0" + (random.Next(3) + 1) + (snowy ? "_Snowy" : "");
                float size = Mathf.Lerp(10.5f, 4.5f, Mathf.InverseLerp(118, 165, p.y));
                height = size * MountainRandom(random, .8f, 1.08f);
            }
            else
            {
                int choice = random.Next(10);
                bool fir = grove.Conifer ? choice < 8 : p.y > 65 ? choice < 6 : choice < 3;
                prefab = fir ? (choice % 2 == 0 ? "P_FirTree4" : "P_FirTree2") : choice == 9 ? "P_OakTreeLarge1" : choice % 2 == 0 ? "P_BirchTree1" : "P_BirchTree3";
                height = MountainRandom(random, fir ? 10 : 8, fir ? 15 : 12);
            }
        }

        private static bool PlaceLandscapeUnderstory(LandscapeGrove grove, LandscapeGroveContext context)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                var p = LandscapeGrovePoint(grove, context.Random);
                if (!LandscapeGroveClear(p, 1.4f, context) || !context.Trees.Clear(p, .5f, true)) continue;
                bool snow = LandscapePaintWeight(p, context, 4) > .45f;
                string prefab = snow ? "P_BushSnowDead02" : context.Random.Next(2) == 0 ? "P_Fern2" : "P_Bush1_Light";
                var go = ScenicPrefab(prefab, p, MountainRandom(context.Random, .45f, .9f), MountainRandom(context.Random, 0, 360), grove.Parent, false);
                if (go == null) throw new InvalidOperationException("Missing woodland understory prefab: " + prefab);
                StripMountainPrefabScripts(go);
                ApplyMountainVegetationMaterials(go, context.Materials);
                SettleMountainVegetation(go, context.Vertices, false, .07f);
                var bounds = ScenicBounds(go);
                if (!LandscapeGroveClear(bounds.center, Mathf.Max(bounds.extents.x, bounds.extents.z), context))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    continue;
                }
                ConfigureMountainVegetationLod(go, 140);
                return true;
            }
            return false;
        }
    }
}
