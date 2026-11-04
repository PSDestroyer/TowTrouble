using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static readonly Vector4[] CompositionSummits = {
            new Vector4(-153, 414, 62, 57), new Vector4(-100, 439, 49, 34),
            new Vector4(-44, 450, 40, 24), new Vector4(89, 416, 49, 31)
        };

        private static void ShapeCompositionMountain(float[,] heights, float[,] original)
        {
            int n = heights.GetLength(0);
            var size = terrain.terrainData.size;
            for (int z = 1; z < n - 1; z++)
                for (int x = 1; x < n - 1; x++)
                {
                    var p = terrain.transform.position + new Vector3(x / (float)(n - 1) * size.x, original[z, x] * size.y, z / (float)(n - 1) * size.z);
                    if (p.z < 185 || p.z > 480 || Mathf.Abs(p.x) > 265 || p.y < 46) continue;
                    float weight = compositionWeights[z, x] * Smooth01(185, 225, p.z) * (1f - Smooth01(230, 265, Mathf.Abs(p.x)));
                    if (weight <= 0f) continue;
                    float peak = 0;
                    foreach (var summit in CompositionSummits)
                    {
                        float dx = (p.x - summit.x) / summit.z;
                        float dz = (p.z - summit.y) / (summit.z * .86f);
                        float radius = Mathf.Sqrt(dx * dx + dz * dz);
                        float ridge = Mathf.Pow(Mathf.Max(0, 1f - radius), 1.35f);
                        peak = Mathf.Max(peak, ridge * summit.w);
                    }
                    // Oblique spurs descend from the crest and break the repeated horizontal benches.
                    float westernSpur = CompositionRidge(p, new Vector2(-153, 412), new Vector2(-202, 278), 24, 18);
                    float easternSpur = CompositionRidge(p, new Vector2(90, 417), new Vector2(123, 274), 28, 16);
                    float centralSpur = CompositionRidge(p, new Vector2(-40, 410), new Vector2(24, 282), 22, 13);
                    float ravine = CompositionRidge(p, new Vector2(-111, 403), new Vector2(-79, 287), 16, 11);
                    float eastRavine = CompositionRidge(p, new Vector2(67, 397), new Vector2(83, 293), 15, 9);
                    // Incised drainage cuts across the old benches; the protected route mask keeps
                    // each road and recovery pad on its existing grade.
                    float gullies = CompositionRidge(p, new Vector2(-157, 370), new Vector2(-216, 218), 10, 22)
                        + CompositionRidge(p, new Vector2(-104, 365), new Vector2(-89, 210), 11, 24)
                        + CompositionRidge(p, new Vector2(-33, 358), new Vector2(-17, 218), 10, 23)
                        + CompositionRidge(p, new Vector2(42, 367), new Vector2(72, 218), 12, 25)
                        + CompositionRidge(p, new Vector2(115, 399), new Vector2(173, 238), 11, 22);
                    float lowerRibs = CompositionRidge(p, new Vector2(-66, 350), new Vector2(-54, 218), 19, 9)
                        + CompositionRidge(p, new Vector2(9, 365), new Vector2(29, 215), 17, 11)
                        + CompositionRidge(p, new Vector2(86, 374), new Vector2(128, 231), 19, 10);
                    float noise = (Mathf.PerlinNoise(p.x * .025f + 17, p.z * .025f + 8) - .5f) * 4.5f;
                    float average = 0;
                    for (int dz = -12; dz <= 12; dz += 6)
                        for (int dx = -12; dx <= 12; dx += 6)
                            average += original[Mathf.Clamp(z + dz, 0, n - 1), Mathf.Clamp(x + dx, 0, n - 1)];
                    float soften = Mathf.Clamp((average / 25f - original[z, x]) * size.y, -8f, 8f);
                    float change = peak + westernSpur + easternSpur + centralSpur + lowerRibs - ravine - eastRavine - gullies + noise + soften;
                    heights[z, x] = original[z, x] + change * weight / size.y;
                }
        }

        private static float CompositionRidge(Vector3 p, Vector2 start, Vector2 end, float width, float height)
        {
            float distance = Distance(new Vector2(p.x, p.z), start, end, out float t);
            float along = Mathf.Sin(t * Mathf.PI);
            return Mathf.Exp(-distance * distance / (width * width)) * Mathf.Max(0, along) * height;
        }

        private static void PaintCompositionMountain(float[,,] paint)
        {
            int nz = paint.GetLength(0), nx = paint.GetLength(1);
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    var p = Ground(terrain.transform.position + new Vector3(x / (float)(nx - 1) * terrain.terrainData.size.x, 0, z / (float)(nz - 1) * terrain.terrainData.size.z));
                    if (p.z < 185 || Mathf.Abs(p.x) > 265 || p.y < 75) continue;
                    float safe = CompositionTerrainWeight(p);
                    if (safe <= 0) continue;
                    float normal = MountainNormal(p).y;
                    float wind = (Mathf.PerlinNoise(p.x * .035f + 31, p.z * .035f) - .5f) * 18f;
                    float snow = Smooth01(124 + wind, 163 + wind, p.y) * Smooth01(.42f, .88f, normal);
                    float rock = (1 - Smooth01(.52f, .91f, normal)) * (1 - snow);
                    float soil = (1 - snow - rock) * Smooth01(95, 143, p.y) * .68f;
                    float blend = safe * Smooth01(75, 110, p.y);
                    float[] desired = { 1 - snow - rock - soil, 0f, 0f, rock, snow, 0f, soil, 0f };
                    for (int layer = 0; layer < paint.GetLength(2); layer++)
                        paint[z, x, layer] = Mathf.Lerp(paint[z, x, layer], desired[layer], blend);
                }
        }

        private static void BuildCompositionCliffs(Transform root)
        {
            var previous = root.Find("Mountain buttresses and ravines");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var group = PolishGroup(root, "Mountain buttresses and ravines");
            Undo.RegisterCreatedObjectUndo(group.gameObject, "Compose embedded mountain cliffs");
            var bare = MountainRockMaterial("Mountain exposed stone", false);
            var snowy = MountainRockMaterial("Mountain snow shelves", true);
            int clusters = BuildEmbeddedMountainFaces(group, bare, snowy);
            // The terrain already owns the summit silhouettes. Capping their tips with
            // separate, flattened meshes made the peaks look like balanced stone tables.
            Debug.Log("[TowTrouble] Mountain: " + clusters + " embedded native cliff clusters; terrain summits retained.");
        }

        private static int BuildEmbeddedMountainFaces(Transform parent, Material bare, Material snowy)
        {
            var sources = new Mesh[6];
            for (int i = 0; i < sources.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath("P_CliffClassic" + (i + 1)));
                if (prefab == null) throw new InvalidOperationException("Missing native mountain cliff prefab.");
                sources[i] = prefab.GetComponentsInChildren<MeshFilter>().First(f => f.sharedMesh != null).sharedMesh;
            }

            var candidates = new List<Vector3>();
            for (float z = 224; z < 424; z += 13)
                for (float x = -208; x < 207; x += 14)
                {
                    var p = Ground(new Vector3(x, 0, z));
                    float normalY = MountainNormal(p).y;
                    if (p.y > 64 && normalY < .76f && normalY > .19f && CompositionClear(p, 5)) candidates.Add(p);
                }
            candidates.Sort((a, b) => EmbeddedFaceScore(b).CompareTo(EmbeddedFaceScore(a)));
            var placed = new List<Vector3>();
            foreach (var p in candidates)
            {
                if (placed.Count >= 16) break;
                if (placed.Any(q => Flat(q - p).sqrMagnitude < 32f * 32f)) continue;
                var outward = Flat(MountainNormal(p)).normalized;
                var side = Vector3.Cross(Vector3.up, outward);
                float width = 25 + Mathf.PerlinNoise(p.x * .04f + 7, p.z * .03f) * 8;
                float depth = 14 + Mathf.PerlinNoise(p.x * .03f, p.z * .04f + 5) * 5;
                // The two overlapping native fragments fit inside this complete footprint.
                if (!CompositionCliffFootprintClear(p, side, outward, width * .58f, depth * .7f)) continue;
                float front = Ground(p + outward * (depth * .5f)).y;
                float back = Ground(p - outward * (depth * .5f)).y;
                if (back - front < 7 || back - front > 38) continue;

                int cluster = placed.Count;
                for (int part = 0; part < 2; part++)
                {
                    // Unequal, overlapping shoulders make one exposed formation. There
                    // is no raised disc, elliptical border or repeating row of boulders.
                    var center = Ground(p + side * (part == 0 ? -width * .13f : width * .29f)
                        - outward * (part == 0 ? 0 : depth * .12f));
                    float partWidth = width * (part == 0 ? .76f : .48f);
                    float partDepth = depth * (part == 0 ? 1 : .82f);
                    BuildEmbeddedCliffFragment(parent, sources[(cluster * 3 + part * 2) % sources.Length],
                        center, side, outward, partWidth, partDepth, cluster * 2 + part,
                        center.y > 143 ? snowy : bare);
                }
                placed.Add(p);
            }
            return placed.Count;
        }

        private static void BuildEmbeddedCliffFragment(Transform parent, Mesh source, Vector3 center,
            Vector3 side, Vector3 outward, float width, float depth, int index, Material material)
        {
            var mesh = UnityEngine.Object.Instantiate(source);
            var bounds = source.bounds;
            var vertices = source.vertices;
            float front = Ground(center + outward * (depth * .5f)).y;
            float back = Ground(center - outward * (depth * .5f)).y;
            float bottom = front - 3.2f;
            float height = Mathf.Clamp(back - bottom - .8f, 8, 39);
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                float x = (v.x - bounds.center.x) / bounds.size.x;
                float z = (v.z - bounds.center.z) / bounds.size.z;
                float level = Mathf.InverseLerp(bounds.min.y, bounds.max.y, v.y);
                // Keep the native cliff's angular silhouette and overhangs. A slight
                // shear and broken upper planes prevent a flat lid when viewed above.
                float shear = (level - .5f) * .06f * (index % 2 == 0 ? 1 : -1);
                var point = center + side * ((x + shear) * width) + outward * (z * depth);
                float fracture = EmbeddedCliffFracture(x, index) * Mathf.Min(4.2f, height * .18f);
                float crown = height + fracture - (z + .5f) * depth * .16f;
                point.y = bottom + level * crown;
                float ground = Ground(point).y;
                // Sink the entire rear wall and the basal vertices. The visible face
                // protrudes from the hillside; neither a rear gap nor a floating foot
                // remains, even where the terrain varies across the formation.
                if (z < -.18f) point.y = Mathf.Min(point.y, ground - .75f);
                if (level < .24f) point.y = Mathf.Min(point.y, ground - 1.6f);
                vertices[i] = point - center;
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "Mountain slope face " + index.ToString("00");
            var asset = VisualAsset(mesh, "LandscapeComposition/" + mesh.name + ".asset");
            var go = new GameObject(asset.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = 8;
            go.isStatic = true;
            go.GetComponent<MeshFilter>().sharedMesh = asset;
            go.GetComponent<MeshCollider>().sharedMesh = asset;
            go.GetComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, asset.subMeshCount).ToArray();
            ConfigureDetailLod(go, 800);
        }

        private static float EmbeddedCliffFracture(float x, int index)
        {
            // Piecewise linear breaks, rather than a sinusoidal mound or a horizontal
            // tabletop. Vary the fracture spacing/height between adjoining fragments.
            float t = Mathf.Clamp01(x + .5f) * 4;
            int segment = Mathf.Min(3, Mathf.FloorToInt(t));
            float a = Mathf.PerlinNoise(index * 1.37f + 4, segment * 2.31f + 7);
            float b = Mathf.PerlinNoise(index * 1.37f + 4, (segment + 1) * 2.31f + 7);
            return (Mathf.Lerp(a, b, t - segment) - .5f) * 2;
        }

        private static float EmbeddedFaceScore(Vector3 p) => (1 - MountainNormal(p).y) * 70f
            + (1 - Smooth01(280, 405, p.z)) * 24f + Mathf.PerlinNoise(p.x * .035f + 3, p.z * .035f) * 8f;

        private static bool CompositionCliffFootprintClear(Vector3 p, Vector3 side, Vector3 outward, float width, float depth)
        {
            for (int z = -2; z <= 2; z++)
                for (int x = -2; x <= 2; x++)
                    if (!CompositionClear(p + side * (x * width / 2) + outward * (z * depth / 2), 2f)) return false;
            return true;
        }

        private static void BuildCompositionHorizon(Transform root)
        {
            var horizon = environment.Find("County horizon");
            if (horizon == null) throw new InvalidOperationException("County horizon is missing.");
            var old = horizon.Find("Foothills below mountain silhouettes") ?? horizon.Find("Surrounding landscape - visual only");
            if (old == null) throw new InvalidOperationException("The existing distant terrain apron is missing.");
            Undo.RecordObject(old.gameObject, "Replace smooth distant terrain apron");
            old.gameObject.SetActive(false);
            // The old enlarged desert rocks form narrow towers above the playable mountain.
            // Seat them lower and keep a broad mesa silhouette in the distant desert instead.
            foreach (var child in horizon.Cast<Transform>())
            {
                if (!child.name.StartsWith("P_RockDesert_Cliff", StringComparison.Ordinal)) continue;
                var bounds = ScenicBounds(child.gameObject);
                float height = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * .3f, 35, 65);
                Undo.RecordObject(child, "Lower distant desert mesas");
                var scale = child.localScale;
                scale.y *= height / Mathf.Max(.01f, bounds.size.y);
                child.localScale = scale;
                bounds = ScenicBounds(child.gameObject);
                child.position += Vector3.up * (-18f - bounds.min.y);
            }
            const int segments = 240;
            float[] rings = { 0, 35, 90, 160, 260, 420, 650, 1000 };
            var vertices = new Vector3[segments * rings.Length];
            var triangles = new List<int>();
            for (int ring = 0; ring < rings.Length; ring++)
                for (int i = 0; i < segments; i++)
                {
                    float angle = i / (float)segments * Mathf.PI * 2;
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float border = 500 / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
                    float radius = border + rings[ring];
                    var edge = Ground(new Vector3(dir.x * border, 0, dir.y * border));
                    float distant = Smooth01(0, 160, rings[ring]);
                    float north = Smooth01(-.1f, .65f, dir.y);
                    float ridge = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 3f + ring * .63f)), 2) * (16 + north * 14);
                    float variation = (Mathf.PerlinNoise(dir.x * 4 + ring * .73f + 15, dir.y * 4 + 11) - .4f) * 15;
                    float height = Mathf.Lerp(edge.y - .35f, edge.y * .16f + ridge + variation - ring * 2, distant);
                    vertices[ring * segments + i] = new Vector3(dir.x * radius, height, dir.y * radius);
                    if (ring == rings.Length - 1) continue;
                    int a = ring * segments + i, b = ring * segments + (i + 1) % segments;
                    triangles.AddRange(new[] { a, b, a + segments, b, b + segments, a + segments });
                }
            var mesh = new Mesh { name = "County distant foothills", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject("County distant foothill silhouettes", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root, false); go.isStatic = true;
            go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "LandscapeComposition/County distant foothills.asset");
            go.GetComponent<MeshRenderer>().sharedMaterial = old.GetComponent<MeshRenderer>().sharedMaterial;
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
