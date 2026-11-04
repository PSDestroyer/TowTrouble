using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string MountainLandscapeFolder = VisualRoot + "/MountainLandscape";

        [MenuItem("Tow Trouble/Visuals/Refine Mountain Landscape")]
        public static void RefineMountainLandscape()
        {
            VisualContext();
            ScenicPaths();
            polishMap = UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            CachePolishRoads();
            Directory.CreateDirectory(MountainLandscapeFolder);
            AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Refine mountain landscape");
            var existingAssets = AssetDatabase.FindAssets("", new[] { MountainLandscapeFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath)
                .Where(asset => asset is Mesh || asset is Material).ToArray();
            foreach (var asset in existingAssets)
                Undo.RegisterCompleteObjectUndo(asset, "Rebuild mountain geometry and materials");
            try
            {
                var previous = environment.Find("Mountain landscape");
                if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
                var root = ScenicGroup("Mountain landscape");
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Create mountain landscape");
                RetireMountainSlabs();
                var bare = MountainRockMaterial("Mountain exposed stone", false);
                var snowy = MountainRockMaterial("Mountain snow shelves", true);
                BuildMountainWalls(root, bare, snowy);
                BuildMountainCrests(root, snowy);
                DressMountainLandscape(root);
                Physics.SyncTransforms();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Undo.CollapseUndoOperations(undo);
                Debug.Log("[TowTrouble] Mountain landscape rebuilt with connected cliff faces, summit crags and conifer groves.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        private static bool MountainPlacementClear(Vector3 p, float radius)
        {
            if (!PolishClear(p, radius) || RiverDistance(p) < 8f + radius) return false;
            if (Flat(p - AlpineShelter).magnitude < 10f + radius ||
                Flat(p - MeadowShelter).magnitude < 10f + radius) return false;
            return Distance(new Vector2(p.x, p.z), new Vector2(-27f, 265.5f),
                new Vector2(AlpineDelivery.x, AlpineDelivery.z), out _) >= 6f + radius;
        }

        private static Vector3 MountainNormal(Vector3 p)
        {
            var local = p - terrain.transform.position;
            return terrain.terrainData.GetInterpolatedNormal(local.x / terrain.terrainData.size.x,
                local.z / terrain.terrainData.size.z);
        }

        private static bool MountainFootprintClear(Vector3 center, Vector3 side, Vector3 outward, float halfWidth, float halfDepth)
        {
            for (int row = -2; row <= 2; row++)
                for (int col = -2; col <= 2; col++)
                    if (!MountainPlacementClear(center + side * (col * halfWidth * .5f) +
                        outward * (row * halfDepth * .5f), 1.3f)) return false;
            return true;
        }

        private static void RetireMountainSlabs()
        {
            foreach (string path in new[] {
                "Mountain cliffs and garage grounds/Continuous mountain cliff belts",
                "Mountain approaches - refuges and meadow/Layered lower mountain escarpment"
            })
            {
                var group = environment.Find(path);
                if (group == null || !group.gameObject.activeSelf) continue;
                Undo.RecordObject(group.gameObject, "Replace mountain slab group");
                group.gameObject.SetActive(false);
            }

            var geology = environment.Find("Authored gorge and mountain geology");
            if (geology == null) return;
            foreach (Transform child in geology)
            {
                if (!child.name.StartsWith("Massif", StringComparison.Ordinal)) continue;
                var b = ScenicBounds(child.gameObject);
                // Retain the lower river walls that support the existing waterfall landings.
                if (b.center.x < -90f || b.center.z < 275f) continue;
                Undo.RecordObject(child.gameObject, "Replace detached upper massif");
                child.gameObject.SetActive(false);
            }
        }

        private static Material MountainRockMaterial(string name, bool snow)
        {
            var shader = Shader.Find("TowTrouble/Mountain Rock");
            if (shader == null) throw new InvalidOperationException("Mountain Rock shader has not imported.");
            var material = new Material(shader) { name = name };
            material.SetTexture("_Rock_Texture", AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/SoStylized/Environment/Rocks/Textures/Classic/T_RockClassic_BC.PNG"));
            material.SetTexture("_Rock_Normal_Texture", AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/SoStylized/Environment/Rocks/Textures/Classic/T_RockClassicCliffs_N.PNG"));
            material.SetTexture("_Snow_Texture", terrain.terrainData.terrainLayers[4].diffuseTexture);
            material.SetColor("_Rock_Tint", new Color(.86f, .88f, .91f));
            material.SetColor("_Snow_Tint", new Color(.94f, .97f, 1f));
            material.SetFloat("_TopSnow", snow ? 1f : 0f);
            material.SetFloat("_Rock_Scale", 10f);
            material.SetFloat("_NormalStrength", .7f);
            material.enableInstancing = true;
            return VisualAsset(material, "MountainLandscape/" + name + ".mat");
        }

        private static void BuildMountainWalls(Transform root, Material bare, Material snowy)
        {
            var group = PolishGroup(root, "Layered cliff faces");
            float[] elevations = { 78f, 102f, 126f, 148f, 167f };
            int count = 0;
            for (int band = 0; band < elevations.Length; band++)
            {
                var strip = new List<Vector3>();
                for (int section = 0; section <= 58; section++)
                {
                    float x = -225f + section * 8f;
                    Vector3 point = default;
                    bool found = false;
                    var previous = Ground(new Vector3(x, 0f, 180f));
                    for (float z = 181.5f; z < 444f; z += 1.5f)
                    {
                        var sample = Ground(new Vector3(x, 0f, z));
                        if (previous.y < elevations[band] && sample.y >= elevations[band])
                        {
                            point = Vector3.Lerp(previous, sample, Mathf.InverseLerp(previous.y, sample.y, elevations[band]));
                            found = true;
                            break;
                        }
                        previous = sample;
                    }
                    var normal = found ? MountainNormal(point) : Vector3.up;
                    var outward = Flat(normal).normalized;
                    bool clear = found && normal.y < .95f && MountainFootprintClear(point,
                        Vector3.Cross(Vector3.up, outward), outward, 5f, 8f);
                    if (!clear || (strip.Count > 0 && Flat(point - strip[strip.Count - 1]).magnitude > 17f))
                    {
                        if (strip.Count >= 2) MountainWallStrip(strip, band, count++, elevations[band] > 132 ? snowy : bare, group);
                        strip.Clear();
                    }
                    if (clear) strip.Add(point);
                }
                if (strip.Count >= 2) MountainWallStrip(strip, band, count++, elevations[band] > 132 ? snowy : bare, group);
            }
            Debug.Log("[TowTrouble] Connected mountain wall sections: " + count);
        }

        private static void MountainWallStrip(List<Vector3> points, int band, int number, Material material, Transform parent)
        {
            var sampled = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                int steps = Mathf.CeilToInt(Flat(points[i + 1] - points[i]).magnitude / 2f);
                for (int step = 0; step < steps; step++)
                    sampled.Add(Ground(Vector3.Lerp(points[i], points[i + 1], step / (float)steps)));
            }
            sampled.Add(points[points.Count - 1]);
            points = sampled;
            const int columns = 5;
            var vertices = new Vector3[points.Count * columns];
            var triangles = new List<int>();
            for (int row = 0; row < points.Count; row++)
            {
                var p = points[row];
                var outward = Flat(MountainNormal(p)).normalized;
                float edge = row == 0 || row == points.Count - 1 ? 0f : Mathf.Clamp01(Mathf.Sin(Mathf.PI * row / (points.Count - 1)));
                float width = 4.5f + Mathf.PerlinNoise(p.x * .11f, band) * 1.8f;
                var back = Ground(p - outward * 6f);
                var lip = Ground(p + outward * width);
                float crest = Mathf.Lerp(lip.y - .4f, back.y + Mathf.Sin(p.x * .27f) * .7f, Mathf.Sqrt(edge));
                vertices[row * columns] = back - Vector3.up * 1.2f;
                vertices[row * columns + 1] = new Vector3(p.x, crest + .25f, p.z);
                vertices[row * columns + 2] = new Vector3(lip.x, crest, lip.z);
                var fracture = lip + outward * (.7f + Mathf.PerlinNoise(p.x * .31f, band) * 1.5f);
                fracture.y = Mathf.Lerp(crest, Ground(fracture).y, .48f);
                vertices[row * columns + 3] = fracture;
                vertices[row * columns + 4] = Ground(lip + outward * 2.2f) - Vector3.up * 1.7f;
                if (row == points.Count - 1) continue;
                for (int col = 0; col < columns - 1; col++)
                {
                    int i = row * columns + col;
                    triangles.AddRange(new[] { i, i + columns, i + 1, i + 1, i + columns, i + columns + 1 });
                }
            }
            // Seal both ends below the slope, including the entire cliff footprint.
            foreach (int row in new[] { 0, points.Count - 1 })
                for (int col = 1; col < columns - 1; col++)
                {
                    int i = row * columns;
                    if (row == 0) triangles.AddRange(new[] { i, i + col, i + col + 1 });
                    else triangles.AddRange(new[] { i, i + col + 1, i + col });
                }
            var mesh = new Mesh { vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            MountainMesh("Mountain wall " + band + "-" + number, mesh, material, parent);
        }

        private static float MountainRidgeWeight(Vector3 p)
        {
            float ridge = 0f;
            var peaks = new[] {
                new Vector4(-170f, 396f, 50f, 40f), new Vector4(-128f, 424f, 55f, 55f),
                new Vector4(-80f, 445f, 44f, 32f), new Vector4(-30f, 443f, 43f, 29f),
                new Vector4(72f, 414f, 38f, 26f), new Vector4(112f, 391f, 34f, 20f)
            };
            foreach (var peak in peaks)
            {
                float x = (p.x - peak.x) / peak.z;
                float z = (p.z - peak.y) / (peak.z * .76f);
                float shape = Mathf.Max(0f, 1f - Mathf.Sqrt(x * x + z * z));
                ridge = Mathf.Max(ridge, peak.w * Mathf.Pow(shape, .82f));
            }
            return ridge;
        }

        private static void BuildMountainCrests(Transform root, Material snowy)
        {
            var parent = PolishGroup(root, "Connected summit ridges");
            const float spacing = 1.5f;
            const int columns = 247, rows = 95;
            var vertices = new Vector3[columns * rows];
            var terrainNormals = new Vector3[vertices.Length];
            var joinWeights = new float[vertices.Length];
            var visible = new bool[columns * rows];
            var triangles = new List<int>();
            for (int z = 0; z < rows; z++)
                for (int x = 0; x < columns; x++)
                {
                    var p = Ground(new Vector3(-221f + x * spacing, 0f, 320f + z * spacing));
                    float height = MountainRidgeWeight(p);
                    bool clear = MountainPlacementClear(p, 6f);
                    float safety = 0f;
                    if (clear)
                    {
                        for (int r = 7; r <= 19; r++)
                        {
                            if (!MountainPlacementClear(p, r)) break;
                            safety += 1f / 13f;
                        }
                        safety = Mathf.SmoothStep(0f, 1f, safety);
                    }
                    float roughness = 1f + .15f * (Mathf.PerlinNoise(p.x * .055f, p.z * .055f) - .5f);
                    float rise = height * safety * roughness - 2.4f;
                    p.y += rise;
                    int i = z * columns + x;
                    vertices[i] = p;
                    terrainNormals[i] = MountainNormal(p);
                    joinWeights[i] = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rise / 6f));
                    visible[i] = clear && height > .5f;
                }
            for (int z = 0; z < rows - 1; z++)
                for (int x = 0; x < columns - 1; x++)
                {
                    int i = z * columns + x;
                    // Do not leave a buried collision sheet under existing roads or recovery pockets.
                    if (!visible[i] || !visible[i + 1] || !visible[i + columns] || !visible[i + columns + 1]) continue;
                    triangles.AddRange(new[] { i, i + columns, i + 1, i + 1, i + columns, i + columns + 1 });
                }
            var mesh = new Mesh { vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            // Match lighting and snow coverage where the new ridge enters the original slope.
            var normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
                normals[i] = Vector3.Lerp(terrainNormals[i], normals[i], joinWeights[i]).normalized;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            MountainMesh("Mountain crest continuous", mesh, snowy, parent);
        }

        private static void MountainMesh(string name, Mesh mesh, Material material, Transform parent)
        {
            foreach (var vertex in mesh.vertices)
                if (!float.IsFinite(vertex.x) || !float.IsFinite(vertex.y) || !float.IsFinite(vertex.z))
                    throw new InvalidOperationException("Invalid mountain mesh vertex in " + name);
            mesh.name = name;
            var asset = VisualAsset(mesh, "MountainLandscape/" + name + ".asset");
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);
            go.layer = 8;
            go.isStatic = true;
            go.GetComponent<MeshFilter>().sharedMesh = asset;
            go.GetComponent<MeshCollider>().sharedMesh = asset;
            go.GetComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, asset.subMeshCount).ToArray();
            ConfigureDetailLod(go, 800f);
        }
    }
}
