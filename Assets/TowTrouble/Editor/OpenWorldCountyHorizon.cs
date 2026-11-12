using System;
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
        private const string HorizonRoot = "County horizon - continuous foothills";
        private static readonly int[] HorizonLayers = { 0, 2, 3, 4, 5 };

        [MenuItem("Tow Trouble/Art Integration/Blend County Terrain into the Horizon")]
        public static void BlendCountyHorizon()
        {
            VisualContext();
            var data = terrain.terrainData;
            if (data.size.x != 1000 || data.size.z != 1000 || data.alphamapLayers < 8)
                throw new InvalidOperationException("Review horizon sampling for this county terrain.");

            var original = environment.Find("County landscape composition/County distant foothill silhouettes");
            var barriers = environment.Find("County limits - natural perimeter");
            if (original == null || barriers == null) throw new InvalidOperationException("County perimeter is missing.");
            var originalColliders = barriers.GetComponentsInChildren<BoxCollider>(true)
                .ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
            if (originalColliders.Count != 4) throw new InvalidOperationException("Expected four perimeter barriers.");

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Blend county into continuous foothills");
            try
            {
                Undo.RegisterCompleteObjectUndo(data, "Shape outer slopes");
                ShapeCountyOuterSlopes(data, barriers);
                PaintCountyOuterSlopes(data);
                var mesh = BuildCountyHorizonMesh(data);
                var material = BuildCountyHorizonMaterial(data);
                var old = environment.Find(HorizonRoot);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                Undo.RecordObject(original.gameObject, "Replace old county horizon");
                original.gameObject.SetActive(false);

                var obsoleteRows = barriers.Cast<Transform>()
                    .SelectMany(side => side.Cast<Transform>())
                    .Where(t => t.name.StartsWith("Boundary formation ", StringComparison.Ordinal)).ToArray();
                foreach (var formation in obsoleteRows)
                    Undo.DestroyObjectImmediate(formation.gameObject);

                var root = PolishGroup(environment, HorizonRoot);
                root.gameObject.isStatic = true;
                root.gameObject.AddComponent<MeshFilter>().sharedMesh =
                    VisualAsset(mesh, "CountyPolish/Continuous county foothills.asset");
                var renderer = root.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                DressCountyOuterSlopes(root);
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    Undo.RegisterCreatedObjectUndo(child.gameObject, "County foothills");

                if (originalColliders.Any(pair => pair.Key == null || EditorJsonUtility.ToJson(pair.Key) != pair.Value))
                    throw new InvalidOperationException("An existing perimeter barrier changed.");
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Undo.CollapseUndoOperations(undo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                throw;
            }
        }

        private static void ShapeCountyOuterSlopes(TerrainData data, Transform barriers)
        {
            const string baselinePath = "Docs/Validation/Boundaries/HeightBaseline.bin";
            int resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            var protectedBounds = environment.GetComponentsInChildren<Collider>()
                .Where(c => !(c is TerrainCollider) && !c.transform.IsChildOf(barriers))
                .Select(c => c.bounds).Where(b => Mathf.Max(Mathf.Abs(b.center.x), Mathf.Abs(b.center.z)) +
                    Mathf.Max(b.extents.x, b.extents.z) >= 448).ToArray();
            using var baseline = new BinaryReader(File.OpenRead(baselinePath));
            if (baseline.ReadInt32() != resolution) throw new InvalidOperationException("County slope baseline mismatch.");

            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float original = baseline.ReadSingle();
                float wx = -500 + x * (1000f / (resolution - 1));
                float wz = -500 + z * (1000f / (resolution - 1));
                float edge = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz));
                if (edge <= 454 || protectedBounds.Any(b => wx >= b.min.x - 6 && wx <= b.max.x + 6 &&
                    wz >= b.min.z - 6 && wz <= b.max.z + 6)) continue;

                float south = 1 - Smooth01(-.94f, -.67f, wz / edge);
                float west = 1 - Smooth01(-.94f, -.67f, wx / edge);
                float influence = Mathf.Max(south, west);
                if (influence <= 0) continue;

                float legacyNoise = Mathf.PerlinNoise(wx * .018f + 17, wz * .018f + 8);
                float legacyRise = Smooth01(444 + legacyNoise * 8, 487, edge);
                float legacyHeight = wz > 380 ? 48 + 20 * legacyNoise : wx > 380 ? 32 + 20 * legacyNoise :
                    wz < -380 ? 26 + 16 * legacyNoise : 36 + 16 * legacyNoise;
                float currentRise = legacyRise * legacyHeight;
                float ridgeNoise = Mathf.PerlinNoise(wx * .010f + 37, wz * .012f + 14);
                float crest = Mathf.Lerp(24, 17, south) + ridgeNoise * Mathf.Lerp(15, 11, south);
                float rise = Smooth01(449, 478 + ridgeNoise * 14, edge);
                float fall = 1 - .23f * Smooth01(487, 500, edge);
                float softened = Mathf.Lerp(currentRise, crest * rise * fall, Smooth01(454, 482, edge) * influence);
                heights[z, x] = Mathf.Clamp01(original + softened / data.size.y);
            }
            data.SetHeights(0, 0, heights);
        }

        private static void PaintCountyOuterSlopes(TerrainData data)
        {
            int resolution = data.alphamapResolution;
            int strip = Mathf.FloorToInt(46f * (resolution - 1) / 1000) + 1;

            void PaintStrip(int startX, int startZ, int width, int depth)
            {
                var paint = data.GetAlphamaps(startX, startZ, width, depth);
                for (int z = 0; z < depth; z++)
                for (int x = 0; x < width; x++)
                {
                    float wx = -500 + (startX + x) * (1000f / (resolution - 1));
                    float wz = -500 + (startZ + z) * (1000f / (resolution - 1));
                    float edge = Mathf.Max(Mathf.Abs(wx), Mathf.Abs(wz));
                    float fade = Smooth01(454, 478, edge);
                    float patch = .72f * Mathf.PerlinNoise(wx * .027f + 47, wz * .023f + 9) +
                        .28f * Mathf.PerlinNoise(wx * .08f + 8, wz * .074f + 52);
                    float coast = 1 - Smooth01(-480, -420, wz);
                    float sediment = coast * fade * Smooth01(.43f, .68f, patch) * .34f;
                    float grass = (1 - coast) * fade * Smooth01(.35f, .69f, patch) * .62f;

                    void Fill(int layer, float minimum)
                    {
                        float current = paint[z, x, layer];
                        if (current >= minimum) return;
                        float scale = (1 - minimum) / (1 - current);
                        for (int k = 0; k < data.alphamapLayers; k++) paint[z, x, k] *= scale;
                        paint[z, x, layer] = minimum;
                    }

                    Fill(0, grass);
                    Fill(3, sediment);
                }
                data.SetAlphamaps(startX, startZ, paint);
            }

            PaintStrip(0, 0, resolution, strip);
            PaintStrip(0, strip, strip, resolution - strip);
        }

        private static void DressCountyOuterSlopes(Transform root)
        {
            var slopes = PolishGroup(root, "Dune scrub and weathered rock");
            var random = new System.Random(15141);
            float Range(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
            var dunes = new[] { new Vector2(-344, -471), new Vector2(-232, -485), new Vector2(-118, -467),
                new Vector2(-44, -476), new Vector2(29, -460), new Vector2(61, -488), new Vector2(133, -472),
                new Vector2(181, -485), new Vector2(232, -457), new Vector2(307, -481), new Vector2(397, -465) };
            var woodland = new[] { new Vector2(-467, -318), new Vector2(-479, -233), new Vector2(-462, -138),
                new Vector2(-481, -59), new Vector2(-461, 9), new Vector2(-477, 77), new Vector2(-467, 132),
                new Vector2(-483, 186), new Vector2(-460, 268), new Vector2(-479, 346), new Vector2(-467, 400) };

            void Place(string prefab, Vector3 point, float size, bool tree)
            {
                float edge = Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z));
                if (edge < 452 || edge > 492 || Ground(point).y < 12) return;
                float slope = terrain.terrainData.GetSteepness((point.x + 500) / 1000, (point.z + 500) / 1000);
                if (slope > (tree ? 34 : 48)) return;
                var go = ForestProp(prefab, point, size, Range(0, 360), slopes, false, tree);
                foreach (var script in go.GetComponentsInChildren<MonoBehaviour>(true))
                    UnityEngine.Object.DestroyImmediate(script);
                if (prefab.Contains("Rock"))
                {
                    var bounds = ScenarioBounds(go);
                    float low = Ground(bounds.center).y;
                    foreach (float x in new[] { -.7f, .7f })
                    foreach (float z in new[] { -.7f, .7f })
                        low = Mathf.Min(low, Ground(bounds.center + new Vector3(x * bounds.extents.x, 0, z * bounds.extents.z)).y);
                    go.transform.position += Vector3.up * (low - bounds.min.y - .25f);
                }
                if (tree)
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            }

            for (int i = 0; i < dunes.Length; i++)
            {
                var center = new Vector3(dunes[i].x, 0, dunes[i].y);
                bool desert = center.x > 220;
                if (i % 3 != 1)
                    Place(desert ? "P_RockDesert_Rock05" : "P_RockSpire_Rock01", center, Range(2.2f, 5.1f), false);
                for (int j = 0; j < 14; j++)
                {
                    var p = center + new Vector3(Range(-16, 16), 0, Range(-9, 9));
                    string plant = j % 6 == 0 ? "P_Bush1_Dry" : "P_GrassDesert1_Paintable";
                    Place(plant, p, Range(1.15f, 2.65f), false);
                }
            }

            for (int i = 0; i < woodland.Length; i++)
            {
                var center = new Vector3(woodland[i].x, 0, woodland[i].y);
                if (i % 3 != 1)
                    Place(i % 2 == 0 ? "P_RockSpire_RockClump01" : "P_RockSpire_Rock02", center, Range(4.5f, 8f), false);
                for (int j = 0; j < 16; j++)
                {
                    var p = center + new Vector3(Range(-6, 15), 0, Range(-17, 17));
                    bool tree = j % 7 == 0;
                    string plant = tree ? "P_FirTree2" : j % 4 == 0 ? "P_Fern2" : "P_Grass2";
                    Place(plant, p, tree ? Range(4.3f, 7f) : Range(1.2f, 3.2f), tree);
                }
            }
        }

        private static Mesh BuildCountyHorizonMesh(TerrainData data)
        {
            const int sideSamples = 512;
            const int perimeterSamples = sideSamples * 4;
            float[] distances = { -3, 0, 6, 15, 28, 46, 68, 94, 125, 163, 208, 260, 324, 398, 486, 592, 720, 880, 1080, 1360 };
            int vertexCount = perimeterSamples * distances.Length;
            var positions = new Vector3[vertexCount];
            var colors = new Color[vertexCount];
            var extraWeights = new Vector2[vertexCount];
            var edgeNormals = new Vector3[perimeterSamples];
            var triangles = new int[perimeterSamples * (distances.Length - 1) * 6];
            int index = 0;

            for (int i = 0; i < perimeterSamples; i++)
            {
                float along = -500 + i % sideSamples * (1000f / sideSamples);
                Vector3 border;
                switch (i / sideSamples)
                {
                    case 0: border = new Vector3(along, 0, -500); break;
                    case 1: border = new Vector3(500, 0, along); break;
                    case 2: border = new Vector3(-along, 0, 500); break;
                    default: border = new Vector3(-500, 0, -along); break;
                }
                float edgeHeight = terrain.SampleHeight(border) + terrain.transform.position.y;
                float inwardHeight = terrain.SampleHeight(border * .996f) + terrain.transform.position.y;
                float slope = Mathf.Clamp((edgeHeight - inwardHeight) / 2, -.55f, .65f);
                edgeNormals[i] = data.GetInterpolatedNormal((border.x + 500) / 1000, (border.z + 500) / 1000);
                int alphaX = Mathf.RoundToInt((border.x + 500) * (data.alphamapWidth - 1) / 1000);
                int alphaZ = Mathf.RoundToInt((border.z + 500) * (data.alphamapHeight - 1) / 1000);
                var alpha = data.GetAlphamaps(alphaX, alphaZ, 1, 1);
                var edgeWeights = new Color(alpha[0, 0, 0] + alpha[0, 0, 1] + alpha[0, 0, 6],
                    alpha[0, 0, 2], alpha[0, 0, 3], alpha[0, 0, 4] + alpha[0, 0, 7]);
                float north = Smooth01(95, 465, border.z);
                float variation = Mathf.PerlinNoise(border.x * .0047f + 16, border.z * .0047f + 41);
                float ridgeDistance = 125 + variation * 145;
                float distantRidgeDistance = 370 + variation * 210;

                for (int ring = 0; ring < distances.Length; ring++)
                {
                    float distance = distances[ring];
                    var point = border * (1 + distance / 500);
                    float wide = Mathf.PerlinNoise(point.x * .0038f + 33, point.z * .0038f + 12);
                    float detail = Mathf.PerlinNoise(point.x * .013f + 5, point.z * .013f + 26);
                    float ridge = Mathf.Exp(-Mathf.Pow((distance - ridgeDistance) / 75, 2));
                    float distantRidge = Mathf.Exp(-Mathf.Pow((distance - distantRidgeDistance) / 126, 2));
                    float hills = 5 + north * 28 + wide * (32 + north * 26) + detail * 10
                        + ridge * (12 + wide * 40 + north * 24) + distantRidge * (8 + wide * 66 + north * 25);
                    float continuation = edgeHeight + slope * Mathf.Min(distance, 18);
                    point.y = Mathf.Lerp(continuation, hills, Smooth01(9, 126, distance));
                    if (distance < 0) point.y = terrain.SampleHeight(point) + terrain.transform.position.y - .38f;
                    if (distance == 0) point.y += .04f;

                    float localSand = 1 - Smooth01(-580, -275, point.z + (detail - .5f) * 120);
                    float desert = Smooth01(225, 590, point.x + (wide - .5f) * 155) * (1 - Smooth01(-30, 355, point.z));
                    float snow = Smooth01(91, 157, point.y) * north;
                    float rock = (.13f + detail * .18f) * (1 - localSand) * (1 - snow);
                    float drySand = localSand * (1 - desert) * (1 - snow) * (1 - rock);
                    desert *= (1 - snow) * (1 - rock);
                    var outer = new Color(Mathf.Max(0, 1 - drySand - snow - rock - desert), drySand, rock, snow);
                    float blend = Smooth01(7, 113, distance);
                    int vertex = ring * perimeterSamples + i;
                    positions[vertex] = point;
                    colors[vertex] = Color.Lerp(edgeWeights, outer, blend);
                    extraWeights[vertex] = new Vector2(Mathf.Lerp(alpha[0, 0, 5], desert, blend), 0);

                    if (ring == distances.Length - 1) continue;
                    int next = ring * perimeterSamples + (i + 1) % perimeterSamples;
                    triangles[index++] = vertex;
                    triangles[index++] = next;
                    triangles[index++] = vertex + perimeterSamples;
                    triangles[index++] = next;
                    triangles[index++] = next + perimeterSamples;
                    triangles[index++] = vertex + perimeterSamples;
                }
            }

            var mesh = new Mesh { name = "Continuous county foothills", vertices = positions, colors = colors,
                uv = extraWeights, triangles = triangles };
            mesh.RecalculateNormals();
            var normals = mesh.normals;
            for (int ring = 0; ring < distances.Length && distances[ring] < 32; ring++)
            {
                float blend = Smooth01(0, 32, distances[ring]);
                for (int i = 0; i < perimeterSamples; i++)
                {
                    int vertex = ring * perimeterSamples + i;
                    normals[vertex] = Vector3.Slerp(edgeNormals[i], normals[vertex], blend);
                }
            }
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material BuildCountyHorizonMaterial(TerrainData data)
        {
            var shader = Shader.Find("TowTrouble/County Horizon Terrain");
            if (shader == null) throw new InvalidOperationException("County horizon shader is missing.");
            var material = new Material(shader) { name = "County horizon terrain" };
            for (int i = 0; i < HorizonLayers.Length; i++)
            {
                var layer = data.terrainLayers[HorizonLayers[i]];
                material.SetTexture("_Layer" + i, layer.diffuseTexture);
                material.SetVector("_Tile" + i, new Vector4(layer.tileSize.x, layer.tileSize.y, layer.tileOffset.x, layer.tileOffset.y));
                material.SetVector("_Tint" + i, layer.diffuseRemapMax);
            }
            return VisualAsset(material, "CountyPolish/County horizon terrain.mat");
        }
    }
}
