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
        private const string ReceivingFolder = VisualRoot + "/ReceivingPlaces";

        [MenuItem("Tow Trouble/Visuals/Naturalize Parking and Delivery Areas")]
        public static void NaturalizeParkingAndDeliveryAreas()
        {
            VisualContext();
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            if (world == null || world.Map == null) throw new InvalidOperationException("Open-world workshop required.");
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Natural parking and receiving places");
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Blend receiving ground");
            Undo.RegisterCompleteObjectUndo(world.Map, "Move roadside receiving places");
            foreach (string folder in new[] { ReceivingFolder, GarageServiceFolder })
                if (AssetDatabase.IsValidFolder(folder))
                    foreach (var path in AssetDatabase.FindAssets("", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath))
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(path);
                        if (asset is Mesh || asset is Material) Undo.RegisterCompleteObjectUndo(asset, "Update receiving art");
                    }
            try
            {
                BuildNaturalReceivingPlaces(world);
                BuildNaturalGarageParking(environment.Find("Workshop service base"), world.GaragePosition);
                Physics.SyncTransforms();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save receiving places.");
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); AssetDatabase.SaveAssets(); throw; }
        }

        // Fixed, surveyed locations make this final pass repeatable after older
        // landscape tools. Keep the authored mission IDs/order and destination names.
        private static Vector3 ReceivingPosition(string id, RecoveryWorld world)
        {
            switch (id)
            {
                case "A": return Ground(world.GaragePosition + new Vector3(-22, 0, -18));
                case "B": return Ground(new Vector3(-184.5f, 0, -45));
                case "C": return Ground(new Vector3(54, 0, 144));
                case "D": return Ground(new Vector3(-11, 0, 254));
                case "E": return Ground(new Vector3(-80, 0, -276));
                case "F": return Ground(new Vector3(-2, 0, -349));
                case "G": return Ground(new Vector3(242, 0, -117));
                case "H": return Ground(new Vector3(359, 0, -269));
                default: throw new ArgumentException("Unknown receiving place: " + id);
            }
        }

        private static float ReceivingYaw(string id) => id == "A" ? 270 : id == "C" || id == "D" || id == "E" ? 90 : 0;

        private static void BuildNaturalReceivingPlaces(RecoveryWorld world)
        {
            Directory.CreateDirectory(ReceivingFolder);
            // This helper also runs inside the workshop/county rebuilds. Those
            // callers must be able to undo mutations to our existing road meshes.
            if (AssetDatabase.IsValidFolder(ReceivingFolder))
                foreach (var asset in AssetDatabase.FindAssets("t:Mesh", new[] { ReceivingFolder })
                    .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Mesh>))
                    if (asset != null) Undo.RegisterCompleteObjectUndo(asset, "Update receiving access meshes");
            var map = world.Map;
            Undo.RegisterCompleteObjectUndo(map, "Align receiving destinations");
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Paint natural receiving access");
            var previous = environment.Find("Natural receiving places");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var root = PolishGroup(environment, "Natural receiving places");
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create natural receiving places");
            // Preserve the old authoring anchors/sign hierarchy for legacy tools,
            // but none of their raised paint or oversized signs remains visible.
            foreach (var old in environment.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Dropoff ", StringComparison.Ordinal)).ToArray())
            { Undo.RecordObject(old.gameObject, "Retire prototype receiving outline"); old.gameObject.SetActive(false); }
            var oldWorkshop = environment.Find("Workshop service base/Workshop pull-through receiving");
            if (oldWorkshop != null) Undo.DestroyObjectImmediate(oldWorkshop.gameObject);

            var roadRoot = environment.Find("Road network - 24 recovery spurs");
            foreach (var old in roadRoot.Cast<Transform>().Where(t => t.name.StartsWith("Road-") && t.name.Contains("-receive")).ToArray())
                Undo.DestroyObjectImmediate(old.gameObject);
            var originalRoads = roadRoot.GetComponentsInChildren<MeshCollider>();
            var checkpointsProperty = new SerializedObject(map).FindProperty("roadCheckpoints");
            var checkpoints = Enumerable.Range(0, checkpointsProperty.arraySize)
                .Select(i => checkpointsProperty.GetArrayElementAtIndex(i).vector3Value).ToList();
            var paint = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);
            var wear = GarageWearMaterial("Receiving place tire impressions", .29f);
            foreach (var bay in map.Deliveries)
            {
                var center = ReceivingPosition(bay.Id, world);
                float yaw = ReceivingYaw(bay.Id);
                var forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                var right = Vector3.Cross(Vector3.up, forward);
                bay.Position = center;
                foreach (var authored in plan.Sites.Where(s => s.Delivery == bay.Id))
                    map.Sites.Single(s => s.Id == authored.Id).Delivery = center;
                var group = PolishGroup(root, bay.Id + " - " + bay.English);
                group.position = center;
                Vector3[] path;
                if (bay.Id == "A") path = new[] { center - forward * 13, center, center + forward * 13 };
                else if (bay.Id == "C") path = new[] { center + new Vector3(-12, 0, 12), center, center + forward * 10 };
                else
                {
                    // Start at the existing named graph junction, never at an
                    // unconnected mid-span point on a nearby mountain switchback.
                    var start = Ground(nodes[bay.Id].Point);
                    path = new[] { start, Vector3.Lerp(start, center, .55f) - forward * 3, center, center + forward * 11 };
                    BuildReceivingAccess(roadRoot, bay.Id, path.Take(3).ToArray(), originalRoads, checkpoints);
                }
                if (bay.Id != "A")
                {
                    PaintReceivingGround(paint, bay.Id, center, yaw, path);
                    GarageWheelPath(group, bay.Id + " receiving approach impressions", wear, .28f, originalRoads, path);
                    // A second faint track makes the cleared place read as used
                    // ground without drawing a perimeter or a solid coloured patch.
                    GarageWheelPath(group, bay.Id + " receiving old parking impressions", wear, .24f, originalRoads,
                        new[] { center - forward * 7 - right * 2.4f, center - right * 2.1f, center + forward * 7 - right * 2.1f });
                }
                BuildNaturalReceivingDetails(group, bay.Id, bay.English, center, yaw);
                if (!checkpoints.Any(p => Flat(p - center).sqrMagnitude < .01f)) checkpoints.Add(center);
            }
            terrain.terrainData.SetAlphamaps(0, 0, paint);
            terrain.terrainData.SetBaseMapDirty(); terrain.Flush(); EditorUtility.SetDirty(terrain.terrainData);
            map.Configure(map.Sites, map.Deliveries, checkpoints.ToArray()); EditorUtility.SetDirty(map);
        }

        private static void BuildReceivingAccess(Transform root, string id, Vector3[] spine, MeshCollider[] roads, List<Vector3> checkpoints)
        {
            var points = new List<Vector3>();
            for (int section = 0; section < spine.Length - 1; section++)
            {
                int count = Mathf.Max(4, Mathf.CeilToInt(Flat(spine[section + 1] - spine[section]).magnitude / .6f));
                for (int i = section == 0 ? 0 : 1; i <= count; i++) points.Add(Vector3.Lerp(spine[section], spine[section + 1], i / (float)count));
            }
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int i = 0; i < points.Count; i++)
            {
                var tangent = Flat(points[Mathf.Min(points.Count - 1, i + 1)] - points[Mathf.Max(0, i - 1)]).normalized;
                var across = Vector3.Cross(Vector3.up, tangent);
                foreach (int side in new[] { -1, 1 })
                {
                    var p = Ground(points[i] + across * side * 3.6f) - Vector3.up * .075f;
                    var ray = new Ray(p + Vector3.up * 3, Vector3.down);
                    foreach (var road in roads) if (road.Raycast(ray, out var hit, 4) && hit.point.y > p.y) p.y = hit.point.y;
                    vertices.Add(p);
                }
                if (i > 0) { int a = i * 2 - 2; triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); }
                if (i % 6 == 0 && !checkpoints.Any(p => Flat(p - points[i]).sqrMagnitude < .04f)) checkpoints.Add(Ground(points[i]));
            }
            var mesh = new Mesh { name = id + " receiving access" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            var go = new GameObject("Road-" + (80 + id[0] - 'A') + "-" + id + "-receive" + id, typeof(MeshCollider));
            go.transform.SetParent(root, false); go.layer = 8; go.isStatic = true;
            go.GetComponent<MeshCollider>().sharedMesh = VisualAsset(mesh, "ReceivingPlaces/" + mesh.name + ".asset");
            Undo.RegisterCreatedObjectUndo(go, "Connect receiving navigation");
        }

        private static void PaintReceivingGround(float[,,] paint, string id, Vector3 center, float yaw, Vector3[] path)
        {
            var data = terrain.terrainData;
            var bounds = new Bounds(center, new Vector3(42, 1, 42));
            foreach (var p in path) bounds.Encapsulate(p);
            var origin = terrain.transform.position;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - origin.x) / data.size.x * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((bounds.max.x - origin.x) / data.size.x * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((bounds.min.z - origin.z) / data.size.z * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((bounds.max.z - origin.z) / data.size.z * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
            var rotation = Quaternion.Euler(0, -yaw, 0);
            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
            {
                var p = origin + new Vector3(x / (float)(data.alphamapWidth - 1) * data.size.x, 0, z / (float)(data.alphamapHeight - 1) * data.size.z);
                var local = rotation * Flat(p - center);
                float noise = Mathf.PerlinNoise(p.x * .11f + 5, p.z * .11f + 23);
                float radius = Mathf.Sqrt(Mathf.Pow(local.x / 10.5f, 2) + Mathf.Pow(local.z / (id == "C" || id == "D" ? 11f : 16f), 2));
                float pad = 1 - Smooth01(.68f, 1.04f + .035f * noise, radius);
                float distance = float.MaxValue;
                for (int i = 1; i < path.Length; i++) distance = Mathf.Min(distance, Distance(new Vector2(p.x, p.z), new Vector2(path[i - 1].x, path[i - 1].z), new Vector2(path[i].x, path[i].z), out _));
                float access = 1 - Smooth01(2.4f, 5.0f + .3f * noise, distance);
                // Keep the native sand palette. On grassy shoulders, blend the
                // existing soil layer into a connected softly worn apron.
                float strength = id == "G" || id == "H" ? .15f : id == "F" ? .53f : .78f;
                float target = Mathf.Max(pad, access) * strength * (.88f + .12f * noise);
                float old = paint[z, x, 1];
                if (target <= old + .0001f || old >= .9999f) continue;
                float keep = (1 - target) / (1 - old);
                for (int layer = 0; layer < paint.GetLength(2); layer++) paint[z, x, layer] = layer == 1 ? target : paint[z, x, layer] * keep;
            }
        }
    }
}
