using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Visuals/Dress Workshop Activity and Edges")]
        public static void DressWorkshopActivityAndEdges()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Workshop dressing requires Edit Mode.");
            VisualContext(); ScenicPaths(); CachePolishRoads();
            var root = environment.Find("Workshop service base");
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            if (root == null || world == null) throw new InvalidOperationException("Compose the workshop service base first.");
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Dress workshop activity and planted edges");
            foreach (var asset in AssetDatabase.FindAssets("", new[] { GarageServiceFolder, SuntailExteriorFolder + "/Materials" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath))
                if (asset is Mesh || asset is Material) Undo.RegisterCompleteObjectUndo(asset, "Dress workshop assets");
            try
            {
                BuildGarageLivedIn(root, world.GaragePosition);
                BuildGarageCourtWear(root, world.GaragePosition);
                Physics.SyncTransforms();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save workshop dressing.");
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static void BuildGarageLivedIn(Transform workshop, Vector3 garage)
        {
            var old = workshop.Find("Workshop activity and planted edges");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var root = PolishGroup(workshop, "Workshop activity and planted edges");
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Add workshop activity clusters");
            BuildGarageActivity(root, garage);
            BuildGaragePlantedEdges(root, garage);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mesh = renderer.GetComponent<MeshFilter>();
                if (mesh == null || mesh.sharedMesh == null || renderer.bounds.size.sqrMagnitude < .0001f)
                    throw new InvalidOperationException("Workshop dressing has missing or empty geometry: " + renderer.name);
            }

            // Re-seat the already-authored quiet parking lines too; Ground includes
            // an 8 cm placement allowance intended for props, not ground paint.
            foreach (var mark in workshop.GetComponentsInChildren<Transform>().Where(t => t.name == "Quiet parking guide"))
            {
                Undo.RecordObject(mark, "Seat parking paint on the courtyard");
                mark.position = Ground(mark.position) - Vector3.up * .055f;
            }
        }

        private static void BuildGaragePlantedEdges(Transform root, Vector3 garage)
        {
            Vector3 At(float x, float z) => Ground(garage + new Vector3(x + 9, 0, z - 9));
            var wood = AssetDatabase.LoadAssetAtPath<Material>(PolishFolder + "/Weathered wood.mat");
            var boundary = PolishGroup(root, "Connected southern working boundary");
            // The open west end is the tested towing exit. Enclose the back of
            // the work stations rather than placing obstacles around the receiving lane.
            foreach (float x in new[] { -29f, -25f, -21f, -17f, -13f, -9f }) GarageYardFence(boundary, At(x, -34), 0, wood);
            foreach (float x in new[] { -36f, -32f }) GarageYardFence(boundary, At(x, 6), 0, wood);
            BatchGarageYardGeometry(boundary, "Workshop connected boundary");
            ConfigureDetailLod(boundary.gameObject, 230);

            var planting = PolishGroup(root, "Layered workshop border planting");
            var trees = new[] { new Vector3(-57, 0, -30), new Vector3(-27, 0, -40), new Vector3(-3, 0, -38) };
            for (int i = 0; i < trees.Length; i++)
            {
                var p = At(trees[i].x, trees[i].z);
                if (PolishRoadNear(p, 3)) continue;
                var tree = ScenicPrefab(i == 1 ? "P_OakTree2" : "P_BirchTree1", p, i == 1 ? 10 : 8.5f, 31 + i * 71, planting, true);
                if (tree == null) throw new InvalidOperationException("Workshop border tree asset missing.");
                foreach (var script in tree.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                ConfigureMountainVegetationLod(tree, 290);
            }

            var clusters = new[] { new Vector3(-48,0,-37), new Vector3(-38,0,-37), new Vector3(-19,0,-37), new Vector3(-10,0,-37),
                new Vector3(-43,0,3), new Vector3(-40,0,4), new Vector3(-39,0,2), new Vector3(-54,0,-31), new Vector3(-1,0,-32) };
            for (int i = 0; i < clusters.Length; i++)
            {
                var center = At(clusters[i].x, clusters[i].z);
                for (int j = 0; j < 4; j++)
                {
                    float angle = (i * 37 + j * 127) * Mathf.Deg2Rad;
                    var p = Ground(center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (j == 0 ? 0 : .65f + .24f * j));
                    if (PolishRoadNear(p, 1.1f) || Flat(p - garage).magnitude < 15 || (p.z > -2 && p.z < 1)) continue;
                    string prefab = j == 0 ? "P_Bush1_Light" : j % 2 == 0 ? "P_Fern2_Yellow" : "P_Grass2";
                    var prop = ScenicPrefab(prefab, p, j == 0 ? .95f : .4f + j * .05f, i * 47 + j * 71, planting, false);
                    if (prop == null) throw new InvalidOperationException("Workshop planting asset missing: " + prefab);
                    foreach (var script in prop.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                    ConfigureMountainVegetationLod(prop, j == 0 ? 170 : 105);
                }
            }
            // Low planting and its native terrain paint break up the broad pale court.
            PaintGaragePlantingBeds(garage);
        }

        private static void PaintGaragePlantingBeds(Vector3 garage)
        {
            var data = terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(data, "Blend workshop planted ground");
            var paint = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            var centers = new[] { garage + new Vector3(-33, 0, -6), garage + new Vector3(-18, 0, -49), garage + new Vector3(6, 0, -47) };
            for (int z = 0; z < data.alphamapHeight; z++) for (int x = 0; x < data.alphamapWidth; x++)
            {
                var p = terrain.transform.position + new Vector3(x / (float)(data.alphamapWidth - 1) * data.size.x, 0, z / (float)(data.alphamapHeight - 1) * data.size.z);
                float target = 0;
                for (int i = 0; i < centers.Length; i++)
                {
                    var d = p - centers[i];
                    float distance = Mathf.Sqrt(d.x * d.x / 25 + d.z * d.z / 9);
                    target = Mathf.Max(target, .88f * (1 - Smooth01(.35f, 1.1f + .12f * Mathf.PerlinNoise(p.x, p.z), distance)));
                }
                float previous = paint[z, x, 0];
                if (target <= previous || previous >= .9999f) continue;
                for (int l = 1; l < data.alphamapLayers; l++) paint[z, x, l] *= (1 - target) / (1 - previous);
                paint[z, x, 0] = target;
            }
            data.SetAlphamaps(0, 0, paint); data.SetBaseMapDirty(); terrain.Flush(); EditorUtility.SetDirty(data);
            RefreshGarageTrackSurfaces(garage);
        }
    }
}
