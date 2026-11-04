using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string CompositionFolder = VisualRoot + "/LandscapeComposition";
        private static float[,] compositionWeights;
        private static Vector3 compositionGarage;

        [MenuItem("Tow Trouble/Visuals/Compose County Landscape")]
        public static void ComposeCountyLandscape()
        {
            VisualContext();
            ScenicPaths();
            polishMap = UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            compositionGarage = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>().GaragePosition;
            CachePolishRoads();
            Directory.CreateDirectory(CompositionFolder);
            AssetDatabase.Refresh();
            string baselinePath = CompositionFolder + "/Terrain baseline.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(baselinePath) == null)
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(terrain.terrainData), baselinePath))
                    throw new InvalidOperationException("Could not preserve the county terrain baseline.");
            var baseline = AssetDatabase.LoadAssetAtPath<TerrainData>(baselinePath);
            if (baseline.heightmapResolution != terrain.terrainData.heightmapResolution)
                throw new InvalidOperationException("County landscape baseline resolution mismatch.");

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Compose county landscape");
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Shape county landscape");
            foreach (var asset in AssetDatabase.FindAssets("", new[] { CompositionFolder, MountainLandscapeFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath))
                if (asset is Mesh || asset is Material) Undo.RegisterCompleteObjectUndo(asset, "Compose landscape assets");
            try
            {
                var previous = environment.Find("County landscape composition");
                if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
                var oldMountain = environment.Find("Mountain landscape");
                if (oldMountain != null) Undo.DestroyObjectImmediate(oldMountain.gameObject);
                RestoreLandscapeGroveSources();
                var root = ScenicGroup("County landscape composition");
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Create county landscape composition");
                RetireMountainSlabs();
                BuildCompositionProtection();

                int n = baseline.heightmapResolution;
                var oldHeights = terrain.terrainData.GetHeights(0, 0, n, n);
                var original = baseline.GetHeights(0, 0, n, n);
                var heights = (float[,])original.Clone();
                ShapeCompositionMountain(heights, original);
                ShapeLandscapeWaterAndSand(heights, original);
                terrain.terrainData.SetHeights(0, 0, heights);
                terrain.Flush();
                ReseatCompositionProps(oldHeights, heights);

                var paint = baseline.GetAlphamaps(0, 0, baseline.alphamapWidth, baseline.alphamapHeight);
                PaintCompositionMountain(paint);
                PaintLandscapeWaterAndSand(paint);
                terrain.terrainData.SetAlphamaps(0, 0, paint);
                terrain.terrainData.SetBaseMapDirty();
                EditorUtility.SetDirty(terrain.terrainData);
                MatchCompositionGround(paint);
                BuildCompositionCliffs(root);
                BuildCompositionHorizon(root);
                Physics.SyncTransforms();
                DressMountainLandscape(root);
                ComposeLandscapeGroves(root);
                ComposeLandscapeWaterAndSand(root);
                ComposeLandscapeWorkshopAndStories(root);
                ComposeLandscapeSuntail(root);
                BuildGarageServiceBase(UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>());
                Physics.SyncTransforms();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()))
                    throw new InvalidOperationException("Could not save the county landscape.");
                Undo.CollapseUndoOperations(undo);
                Debug.Log("[TowTrouble] County landscape composed and saved.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        private static bool CompositionClear(Vector3 p, float radius)
        {
            // Placement callers check their own waterline; protection is a horizontal footprint.
            p.y = Mathf.Max(11f, p.y);
            return MountainPlacementClear(p, radius) && Flat(p - compositionGarage).magnitude >= 43f + radius;
        }

        private static float CompositionTerrainWeight(Vector3 p)
        {
            if (compositionWeights == null) return 0f;
            int n = compositionWeights.GetLength(0);
            var local = p - terrain.transform.position;
            int x = Mathf.RoundToInt(local.x / terrain.terrainData.size.x * (n - 1));
            int z = Mathf.RoundToInt(local.z / terrain.terrainData.size.z * (n - 1));
            if (x < 0 || z < 0 || x >= n || z >= n) return 0f;
            return compositionWeights[z, x];
        }

        private static void BuildCompositionProtection()
        {
            int n = terrain.terrainData.heightmapResolution;
            var distance = new float[n, n];
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++) distance[z, x] = 1000f;
            foreach (var cell in polishRoads.Values)
                foreach (var p in cell) StampCompositionProtection(distance, p, 9f);
            foreach (var site in polishMap.Sites)
            {
                StampCompositionProtection(distance, site.Position, site.Id == "R01" ? 39f : 23f);
                for (float d = 0; d < 41; d += 3)
                    StampCompositionProtection(distance, site.Position + site.ExitDirection * d, 10f);
            }
            foreach (var bay in polishMap.Deliveries) StampCompositionProtection(distance, bay.Position, 25f);
            StampCompositionProtection(distance, compositionGarage, 62f);
            StampCompositionProtection(distance, AlpineShelter, 19f);
            StampCompositionProtection(distance, MeadowShelter, 19f);
            foreach (var p in RiverPlan()) StampCompositionProtection(distance, p, p.z > 195 ? 15f : 9f);
            for (float t = 0; t <= 1; t += .08f)
                StampCompositionProtection(distance, Vector3.Lerp(new Vector3(-27, 0, 265.5f), AlpineDelivery, t), 10f);

            float diagonal = Mathf.Sqrt(2f);
            for (int z = 1; z < n; z++)
                for (int x = 1; x < n - 1; x++)
                    distance[z, x] = Mathf.Min(distance[z, x], Mathf.Min(distance[z - 1, x] + 1,
                        Mathf.Min(distance[z, x - 1] + 1, Mathf.Min(distance[z - 1, x - 1], distance[z - 1, x + 1]) + diagonal)));
            for (int z = n - 2; z >= 0; z--)
                for (int x = n - 2; x > 0; x--)
                    distance[z, x] = Mathf.Min(distance[z, x], Mathf.Min(distance[z + 1, x] + 1,
                        Mathf.Min(distance[z, x + 1] + 1, Mathf.Min(distance[z + 1, x - 1], distance[z + 1, x + 1]) + diagonal)));
            float metres = terrain.terrainData.size.x / (n - 1);
            compositionWeights = distance;
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                    compositionWeights[z, x] = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance[z, x] * metres - 2f) / 22f));
        }

        private static void StampCompositionProtection(float[,] grid, Vector3 point, float radius)
        {
            int n = grid.GetLength(0);
            float scale = (n - 1) / terrain.terrainData.size.x;
            var p = (point - terrain.transform.position) * scale;
            float r = radius * scale;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(p.x - r)), x1 = Mathf.Min(n - 1, Mathf.CeilToInt(p.x + r));
            int z0 = Mathf.Max(0, Mathf.FloorToInt(p.z - r)), z1 = Mathf.Min(n - 1, Mathf.CeilToInt(p.z + r));
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                    if ((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z) <= r * r) grid[z, x] = 0f;
        }

        private static void ReseatCompositionProps(float[,] before, float[,] after)
        {
            int n = before.GetLength(0);
            int moved = 0;
            foreach (var t in environment.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("P_", StringComparison.Ordinal) ||
                    (t.parent != null && t.parent.name.StartsWith("P_", StringComparison.Ordinal))) continue;
                var local = t.position - terrain.transform.position;
                int x = Mathf.Clamp(Mathf.RoundToInt(local.x / terrain.terrainData.size.x * (n - 1)), 0, n - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt(local.z / terrain.terrainData.size.z * (n - 1)), 0, n - 1);
                float delta = (after[z, x] - before[z, x]) * terrain.terrainData.size.y;
                if (Mathf.Abs(delta) < .015f) continue;
                Undo.RecordObject(t, "Seat scenery on composed terrain");
                t.position += Vector3.up * delta;
                moved++;
            }
            Debug.Log("[TowTrouble] Reseated existing landscape props: " + moved);
        }

        private static void MatchCompositionGround(float[,,] paint)
        {
            var map = AssetDatabase.LoadAssetAtPath<RecoveryGroundMap>(VisualRoot + "/Painted ground grip.asset");
            if (map == null) return;
            Undo.RegisterCompleteObjectUndo(map, "Match composed terrain surface");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var cells = (byte[])typeof(RecoveryGroundMap).GetField("cells", flags).GetValue(map);
            var tracks = (byte[])typeof(RecoveryGroundMap).GetField("trackKinds", flags).GetValue(map);
            var impressions = (byte[])typeof(RecoveryGroundMap).GetField("wheelImpressions", flags).GetValue(map);
            int n = map.Resolution;
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    var p = terrain.transform.position + new Vector3(x / (float)(n - 1) * terrain.terrainData.size.x, 0, z / (float)(n - 1) * terrain.terrainData.size.z);
                    if (CompositionTerrainWeight(p) < .01f) continue;
                    int ax = Mathf.RoundToInt(x / (float)(n - 1) * (paint.GetLength(1) - 1));
                    int az = Mathf.RoundToInt(z / (float)(n - 1) * (paint.GetLength(0) - 1));
                    float sand = paint[az, ax, 2] + paint[az, ax, 5], snow = paint[az, ax, 4] + paint[az, ax, 7];
                    cells[z * n + x] = (byte)(snow > .45f ? RecoveryGroundKind.Snow : sand > .5f ? RecoveryGroundKind.Sand : RecoveryGroundKind.Rock);
                    // Protected routes/pads are already excluded above. Bare rock may now be snow or sand;
                    // only explicit wooden surfaces keep their authored track override.
                    if (tracks.Length != n * n || tracks[z * n + x] == (byte)RecoveryTrackKind.Wood) continue;
                    tracks[z * n + x] = (byte)RecoveryTrackSurface.FromPaint(paint[az, ax, 0], paint[az, ax, 1], sand, paint[az, ax, 3] + paint[az, ax, 6], snow);
                    if (impressions.Length == n * n) impressions[z * n + x] = (byte)(sand + snow > .4f ? 1 : impressions[z * n + x]);
                }
            EditorUtility.SetDirty(map);
        }
    }
}
