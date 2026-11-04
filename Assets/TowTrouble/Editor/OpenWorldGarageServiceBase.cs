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
        private const string GarageServiceFolder = VisualRoot + "/GarageServiceBase";

        [MenuItem("Tow Trouble/Visuals/Compose Workshop Service Base")]
        public static void ComposeWorkshopServiceBase()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before composing the workshop.");
            VisualContext(); ScenicPaths(); CachePolishRoads();
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            if (world == null || world.Map == null) throw new InvalidOperationException("Open-world recovery scene required.");
            Directory.CreateDirectory(GarageServiceFolder);
            AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Compose workshop service base");
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Blend workshop court");
            Undo.RegisterCompleteObjectUndo(world.Map, "Move workshop receiving bay");
            foreach (var asset in AssetDatabase.FindAssets("", new[] { GarageServiceFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath))
                if (asset is Mesh || asset is Material) Undo.RegisterCompleteObjectUndo(asset, "Refine workshop assets");
            try
            {
                BuildGarageServiceBase(world);
                Physics.SyncTransforms();
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                if (!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()))
                    throw new InvalidOperationException("Could not save workshop scene.");
                Undo.CollapseUndoOperations(undo);
                Debug.Log("[TowTrouble] Workshop service base saved: shared court, pull-through receiving, exterior workshop, salvage parking and rest corner.");
            }
            catch { Undo.RevertAllDownToGroup(undo); AssetDatabase.SaveAssets(); throw; }
        }

        private static void BuildGarageServiceBase(RecoveryWorld world)
        {
            Directory.CreateDirectory(GarageServiceFolder);
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Blend workshop court");
            Undo.RegisterCompleteObjectUndo(world.Map, "Move workshop receiving bay");
            foreach (var asset in AssetDatabase.FindAssets("", new[] { GarageServiceFolder })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath))
                if (asset is Mesh || asset is Material) Undo.RegisterCompleteObjectUndo(asset, "Compose workshop assets");
            var previous = environment.Find("Workshop service base");
            if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);
            var root = PolishGroup(environment, "Workshop service base");
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create workshop service base");
            // Retain the original parked vehicles as reusable source art. Nothing under
            // this inactive group renders or collides; the new yard owns its clones.
            foreach (string path in new[] { "Mountain cliffs and garage grounds/Garage working yard", "County landscape composition/Workshop connected service court" })
            {
                var old = environment.Find(path);
                if (old == null) continue;
                Undo.RecordObject(old.gameObject, "Retire disconnected workshop yard"); old.gameObject.SetActive(false);
            }
            foreach (var approach in environment.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "Broken Vector - workshop approach" || t.name.StartsWith("Dropoff A -", StringComparison.Ordinal)).ToArray())
            {
                Undo.RecordObject(approach.gameObject, "Replace workshop slab and roadside receiving mark");
                approach.gameObject.SetActive(false);
            }

            GradeGarageEastApproach(world.GaragePosition);
            var roads = CompositionStoryRoads();
            BlendGarageCourt(world.GaragePosition, roads);
            SeatGarageRoadShoulders(world.GaragePosition);
            MoveGarageReceiving(world);
            BuildGarageFacade(root, world.GaragePosition);
            BuildGarageYard(root, world.GaragePosition);
            BuildGarageReceiving(root, world);
            BuildGarageAccess(world);
            BuildGarageCourtWear(root, world.GaragePosition);
            BuildGarageLivedIn(root, world.GaragePosition);
            RefreshGarageTrackSurfaces(world.GaragePosition);
        }

        private static void GradeGarageEastApproach(Vector3 garage)
        {
            var road = environment.Find("Road network - 24 recovery spurs/Road-21-g-centerEast");
            if (road == null) throw new InvalidOperationException("Workshop east road is missing.");
            var filter = road.GetComponent<MeshFilter>(); var collider = road.GetComponent<MeshCollider>();
            Mesh Grade(Mesh current, string name)
            {
                string baselinePath = GarageServiceFolder + "/" + name + " source.asset";
                var source = AssetDatabase.LoadAssetAtPath<Mesh>(baselinePath);
                if (source == null)
                {
                    source = UnityEngine.Object.Instantiate(current); source.name = name + " source";
                    AssetDatabase.CreateAsset(source, baselinePath);
                }
                var mesh = UnityEngine.Object.Instantiate(source); var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var p = road.TransformPoint(vertices[i]); float distance = Flat(p - garage).magnitude;
                    // The old east road rose beside an overlapping flat junction,
                    // leaving a ledge across the garage exit. Ease that first climb.
                    p.y = Mathf.Lerp(garage.y - .06f, p.y, Smooth01(8, 35, distance));
                    vertices[i] = road.InverseTransformPoint(p);
                }
                mesh.name = name; mesh.vertices = vertices; mesh.RecalculateBounds(); mesh.RecalculateNormals();
                return VisualAsset(mesh, "GarageServiceBase/" + name + ".asset");
            }
            Undo.RecordObject(filter, "Ease workshop road junction"); Undo.RecordObject(collider, "Ease workshop drive surface");
            filter.sharedMesh = Grade(filter.sharedMesh, "Workshop east road");
            collider.sharedMesh = Grade(collider.sharedMesh, "Workshop east drive surface");
            EditorUtility.SetDirty(filter); EditorUtility.SetDirty(collider);
        }

        private static void SeatGarageRoadShoulders(Vector3 garage)
        {
            foreach (var filter in environment.Find("Road network - 24 recovery spurs").GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.name.StartsWith("Road-") || filter.sharedMesh == null || filter.sharedMesh.vertexCount % 6 != 0) continue;
                var vertices = filter.sharedMesh.vertices;
                bool changed = false;
                for (int i = 0; i < vertices.Length; i++)
                {
                    // Only the visual feathered edge follows the new shoulder. The
                    // authored road drive collider and four inner columns stay intact.
                    if (i % 6 != 0 && i % 6 != 5) continue;
                    var p = filter.transform.TransformPoint(vertices[i]);
                    if (GarageCourtWeight(p, garage) <= 0) continue;
                    p.y = Ground(p).y - .065f;
                    vertices[i] = filter.transform.InverseTransformPoint(p); changed = true;
                }
                if (!changed) continue;
                var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh); mesh.name = filter.name + " workshop shoulder";
                mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                Undo.RecordObject(filter, "Blend road shoulder into workshop court");
                filter.sharedMesh = VisualAsset(mesh, "GarageServiceBase/" + mesh.name + ".asset");
                EditorUtility.SetDirty(filter);
            }
        }

        private static void RefreshGarageTrackSurfaces(Vector3 garage)
        {
            var map = AssetDatabase.LoadAssetAtPath<RecoveryGroundMap>(VisualRoot + "/Painted ground grip.asset");
            var tracks = environment.GetComponentInChildren<RecoveryWheelTracks>(true);
            if (map == null || tracks == null) throw new InvalidOperationException("Authored ground/track system missing.");
            Undo.RegisterCompleteObjectUndo(map, "Match workshop wheel wear to packed earth");
            var field = typeof(RecoveryGroundMap).GetField("trackKinds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var kinds = (byte[])field.GetValue(map); int n = map.Resolution;
            var data = terrain.terrainData;
            var paint = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                var p = terrain.transform.position + new Vector3(x / (float)(n - 1) * data.size.x, 0, z / (float)(n - 1) * data.size.z);
                if (GarageCourtWeight(p, garage) <= 0) continue;
                int ax = Mathf.RoundToInt(x / (float)(n - 1) * (data.alphamapWidth - 1)), az = Mathf.RoundToInt(z / (float)(n - 1) * (data.alphamapHeight - 1));
                kinds[z * n + x] = (byte)RecoveryTrackSurface.FromPaint(paint[az, ax, 0], paint[az, ax, 1] + paint[az, ax, 6],
                    paint[az, ax, 2] + paint[az, ax, 5], paint[az, ax, 3], paint[az, ax, 4] + paint[az, ax, 7]);
            }
            map.ConfigureTrackKinds(kinds); EditorUtility.SetDirty(map);
            Undo.RecordObject(tracks, "Register workshop drive access for wheel tracks");
            var serialized = new SerializedObject(tracks); var surfaces = serialized.FindProperty("roadSurfaces");
            var colliders = environment.GetComponentsInChildren<MeshCollider>().Where(c => c.name.StartsWith("Road-")).ToArray();
            surfaces.arraySize = colliders.Length;
            for (int i = 0; i < colliders.Length; i++) surfaces.GetArrayElementAtIndex(i).objectReferenceValue = colliders[i];
            serialized.ApplyModifiedProperties();
        }

        private static float GarageCourtWeight(Vector3 point, Vector3 garage)
        {
            var p = point - garage;
            // Connected asymmetric lobes: open turning area, southern parking strip,
            // façade approach. Softly joined boundaries follow the working area.
            float Court(float x, float z, float rx, float rz)
            {
                float d = Mathf.Sqrt(Mathf.Pow((p.x - x) / rx, 2) + Mathf.Pow((p.z - z) / rz, 2));
                float irregular = .025f * Mathf.PerlinNoise(point.x * .18f + 7, point.z * .18f + 19);
                return 1 - Smooth01(.78f + irregular, 1.03f + irregular, d);
            }
            return Mathf.Max(Court(-17, -15, 37, 31), Mathf.Max(Court(-31, -34, 20, 14), Court(0, 9, 17, 24)));
        }

        private static void BlendGarageCourt(Vector3 garage, List<CompositionStoryRoad> roads)
        {
            var data = terrain.terrainData;
            var baseline = AssetDatabase.LoadAssetAtPath<TerrainData>(CompositionFolder + "/Terrain baseline.asset");
            if (baseline == null || baseline.heightmapResolution != data.heightmapResolution)
                throw new InvalidOperationException("Workshop needs the preserved county terrain baseline.");
            int n = data.heightmapResolution;
            var h = data.GetHeights(0, 0, n, n);
            var original = baseline.GetHeights(0, 0, n, n);
            var origin = terrain.transform.position;
            float floor = garage.y - .08f;
            var nearby = roads.Where(r => Flat((r.A + r.B) * .5f - garage).magnitude < 95).ToArray();
            // Record planted scenery before shaping so retained perimeter vegetation
            // cannot float after the small shoulder correction.
            var props = environment.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("P_", StringComparison.Ordinal) && !t.parent.name.StartsWith("P_", StringComparison.Ordinal)
                    && GarageCourtWeight(t.position, garage) > 0).Select(t => new { t, ground = Ground(t.position).y }).ToArray();
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                var p = origin + new Vector3(x / (float)(n - 1) * data.size.x, 0, z / (float)(n - 1) * data.size.z);
                float weight = GarageCourtWeight(p, garage);
                if (weight <= 0) continue;
                float target = floor;
                float roadDistance = float.MaxValue, roadY = floor, halfWidth = 0;
                foreach (var road in nearby)
                {
                    var delta = Flat(road.B - road.A);
                    float t = Mathf.Clamp01(Vector3.Dot(Flat(p - road.A), delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                    var c = Vector3.Lerp(road.A, road.B, t);
                    float d = Flat(p - c).magnitude;
                    if (d >= roadDistance) continue;
                    roadDistance = d; roadY = c.y - .02f;
                    halfWidth = Flat(Vector3.Lerp(road.LeftA, road.LeftB, t) - Vector3.Lerp(road.RightA, road.RightB, t)).magnitude * .5f;
                }
                // Keep road elevations while easing the old abrupt edge of the pad.
                target = Mathf.Lerp(roadY, target, Smooth01(halfWidth + .5f, halfWidth + 8, roadDistance));
                float baseY = origin.y + original[z, x] * data.size.y;
                h[z, x] = (Mathf.Lerp(baseY, target, weight) - origin.y) / data.size.y;
            }
            data.SetHeights(0, 0, h); terrain.Flush();
            foreach (var prop in props)
            {
                Undo.RecordObject(prop.t, "Seat workshop verge planting");
                prop.t.position += Vector3.up * (Ground(prop.t.position).y - prop.ground);
            }

            var paint = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            var before = baseline.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            for (int z = 0; z < data.alphamapHeight; z++) for (int x = 0; x < data.alphamapWidth; x++)
            {
                var p = origin + new Vector3(x / (float)(data.alphamapWidth - 1) * data.size.x, 0, z / (float)(data.alphamapHeight - 1) * data.size.z);
                float w = GarageCourtWeight(p, garage);
                if (w <= 0) continue;
                float gravel = .12f + .05f * Mathf.PerlinNoise(p.x * .17f + 7, p.z * .17f + 19);
                for (int l = 0; l < data.alphamapLayers; l++)
                    paint[z, x, l] = Mathf.Lerp(before[z, x, l], l == 1 ? 1 - gravel : l == 3 ? gravel : 0, w);
            }
            data.SetAlphamaps(0, 0, paint); data.SetBaseMapDirty(); terrain.Flush(); EditorUtility.SetDirty(data);
        }

        private static void MoveGarageReceiving(RecoveryWorld world)
        {
            var map = world.Map;
            var bay = map.Deliveries.First(d => d.Id == "A");
            var old = bay.Position;
            var next = Ground(world.GaragePosition + new Vector3(-22, 0, -18));
            Undo.RecordObject(map, "Relocate workshop delivery off the main road");
            foreach (var site in map.Sites)
                if (Flat(site.Delivery - old).sqrMagnitude < .01f) site.Delivery = next;
            bay.Position = next;
            var serialized = new SerializedObject(map);
            var property = serialized.FindProperty("roadCheckpoints");
            var checkpoints = Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i).vector3Value).ToList();
            if (!checkpoints.Any(p => Flat(p - next).sqrMagnitude < .01f)) checkpoints.Add(next);
            map.Configure(map.Sites, map.Deliveries, checkpoints.ToArray());
            EditorUtility.SetDirty(map);
        }

        private static void BuildGarageReceiving(Transform root, RecoveryWorld world)
        {
            var bay = world.Map.Deliveries.First(d => d.Id == "A");
            var group = PolishGroup(root, "Workshop pull-through receiving");
            var trim = AssetDatabase.LoadAssetAtPath<Material>(PolishFolder + "/Faded parking paint.mat");
            var orange = AssetDatabase.LoadAssetAtPath<Material>(VisualRoot + "/Workshop safety yellow.mat");
            if (trim == null || orange == null) throw new InvalidOperationException("Workshop marking materials missing.");
            var highlights = new List<Renderer>();
            foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            {
                var corner = bay.Position + new Vector3(sx * 5.5f, 0, sz * 4);
                AddGarageGroundMark(group, "Faded receiving corner", corner - Vector3.right * sx * .8f, new Vector3(1.6f, .012f, .085f), trim);
                AddGarageGroundMark(group, "Faded receiving corner", corner - Vector3.forward * sz * .65f, new Vector3(.085f, .012f, 1.3f), trim);
            }
            foreach (int side in new[] { -1, 1 })
            {
                highlights.Add(AddGarageGroundMark(group, "Active receiving boundary", bay.Position + Vector3.forward * side * 4, new Vector3(11, .014f, .14f), orange));
                highlights.Add(AddGarageGroundMark(group, "Active receiving boundary", bay.Position + Vector3.right * side * 5.5f, new Vector3(.14f, .014f, 8), orange));
            }
            group.gameObject.AddComponent<RecoveryDeliveryHighlight>().Configure(world, "A", highlights.ToArray());
        }

        private static Renderer AddGarageGroundMark(Transform parent, string name, Vector3 p, Vector3 size, Material material)
        {
            var go = PolishBox(parent, name, parent.InverseTransformPoint(Ground(p) - Vector3.up * .055f), size, material);
            go.isStatic = true;
            var r = go.GetComponent<Renderer>(); r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        private static void BuildGarageAccess(RecoveryWorld world)
        {
            var roads = environment.Find("Road network - 24 recovery spurs");
            const string name = "Road-70-g-workshopYard";
            var old = roads.Find(name); if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var g = world.GaragePosition;
            var bay = world.Map.Deliveries.First(d => d.Id == "A").Position;
            var path = new[] { g, g + new Vector3(-4, 0, -8), g + new Vector3(-10, 0, -16), bay };
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int section = 0; section < path.Length - 1; section++)
            {
                int count = Mathf.CeilToInt(Flat(path[section + 1] - path[section]).magnitude / .6f);
                var side = Vector3.Cross(Vector3.up, Flat(path[section + 1] - path[section]).normalized);
                for (int i = 0; i <= count; i++)
                {
                    if (section > 0 && i == 0) continue;
                    var center = Vector3.Lerp(path[section], path[section + 1], i / (float)count);
                    vertices.Add(Ground(center - side * 3.5f) - Vector3.up * .075f);
                    vertices.Add(Ground(center + side * 3.5f) - Vector3.up * .075f);
                    int a = vertices.Count - 4;
                    if (a >= 0) triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }
            var mesh = new Mesh { name = "Workshop receiving access" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshCollider)); go.transform.SetParent(roads, false); go.layer = 8; go.isStatic = true;
            go.GetComponent<MeshCollider>().sharedMesh = VisualAsset(mesh, "GarageServiceBase/Workshop receiving access.asset");
            Undo.RegisterCreatedObjectUndo(go, "Connect workshop receiving navigation");
        }

    }
}
