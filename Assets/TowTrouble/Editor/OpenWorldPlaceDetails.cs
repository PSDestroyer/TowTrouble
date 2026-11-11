using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string PlaceDetailRoot = "County place details - sandstone and homesteads";
        private static List<CompositionStoryRoad> placeRoads;
        private static RecoveryMap placeMap;

        [MenuItem("Tow Trouble/Art Integration/Refine Desert Houses and Workshop Edges")]
        public static void RefineCountyPlaceDetails()
        {
            VisualContext(); ScenicPaths();
            placeRoads = CompositionStoryRoads();
            placeMap = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>().Map;
            var scene = EditorSceneManager.GetActiveScene();
            Directory.CreateDirectory("/private/tmp/TowPlaceDetailsBackup");
            if (!File.Exists("/private/tmp/TowPlaceDetailsBackup/Before.unity"))
                EditorSceneManager.SaveScene(scene, "/private/tmp/TowPlaceDetailsBackup/Before.unity", true);
            var mapBefore = EditorJsonUtility.ToJson(placeMap);
            var existing = environment.GetComponentsInChildren<Collider>(true)
                .Where(c => !c.transform.GetComponentsInParent<Transform>().Any(t => t.name == PlaceDetailRoot))
                .ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Refine sandstone and lived-in places");
            try
            {
                var old = environment.Find(PlaceDetailRoot);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                var root = PolishGroup(environment, PlaceDetailRoot);
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Refine county places");
                ComposePlaceSandstone(PolishGroup(root, "Eroded sandstone shelves"));
                ComposePlaceHomes(PolishGroup(root, "House gardens and working corners"));
                ComposePlaceForest(PolishGroup(root, "Workshop woodland transition"));
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.lightProbeUsage = LightProbeUsage.Off;
                    r.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
                int triangles = root.GetComponentsInChildren<MeshFilter>()
                    .Where(f => f.GetComponent<Renderer>() != null && f.GetComponent<Renderer>().enabled)
                    .Sum(f => f.sharedMesh.triangles.Length / 3);
                int renderers = root.GetComponentsInChildren<Renderer>().Count(r => r.enabled);
                if (renderers > 320 || triangles > 85000) throw new InvalidOperationException("Place detail budget exceeded.");
                if (mapBefore != EditorJsonUtility.ToJson(placeMap) || existing.Any(p => p.Key == null || EditorJsonUtility.ToJson(p.Key) != p.Value))
                    throw new InvalidOperationException("Existing gameplay/colliders changed.");
                Directory.CreateDirectory("Docs/Validation/PlaceDetails");
                File.WriteAllText("Docs/Validation/PlaceDetails/Validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    renderers, triangles, existingCollidersUnchanged = existing.Count, mapUnchanged = true,
                    addedColliders = root.GetComponentsInChildren<Collider>().Length,
                    addedLights = root.GetComponentsInChildren<Light>().Length,
                    runtimeBehaviours = root.GetComponentsInChildren<MonoBehaviour>().Length,
                    groups = root.Cast<Transform>().Select(t => new { t.name, props = t.childCount }).ToArray()
                }, Newtonsoft.Json.Formatting.Indented));
                SaveVisualScene(); Undo.CollapseUndoOperations(undo);
                Debug.Log($"County place details saved: {renderers} renderers, {triangles} triangles. Existing gameplay retained.");
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
        }

        private static bool PlaceDetailClear(Vector3 p, float radius)
        {
            if (Ground(p).y < 12 || Mathf.Abs(p.x) > 460 || Mathf.Abs(p.z) > 440) return false;
            foreach (var road in placeRoads)
            {
                var delta = Flat(road.B - road.A);
                float t = Mathf.Clamp01(Vector3.Dot(Flat(p - road.A), delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                float width = Vector3.Distance(Vector3.Lerp(road.LeftA, road.LeftB, t), Vector3.Lerp(road.RightA, road.RightB, t)) * .5f;
                if (Flat(p - Vector3.Lerp(road.A, road.B, t)).magnitude < width + radius + 1.5f) return false;
            }
            if (placeMap.Sites.Any(s => Flat(p - s.Position).magnitude < 13 + radius)) return false;
            if (placeMap.Deliveries.Any(s => Flat(p - s.Position).magnitude < 13 + radius)) return false;
            return ClearRecoveryAccess(p, radius);
        }

        private static GameObject PlaceDetailProp(string prefab, Vector3 p, float size, float yaw, Transform parent, bool rock = false, bool tree = false)
        {
            if (!PlaceDetailClear(p, tree ? 1.4f : size * .55f)) return null;
            var go = ForestProp(prefab, p, size, yaw, parent, false, tree);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
            foreach (var group in go.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                if (lods.Length > 0)
                {
                    var chosen = lods[Mathf.Min(1, lods.Length - 1)].renderers;
                    if (chosen.Length == 0) chosen = lods.SelectMany(l => l.renderers).Where(r => r != null).Distinct().ToArray();
                    if (chosen.Length == 0) chosen = group.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in group.GetComponentsInChildren<Renderer>(true)) r.enabled = chosen.Contains(r);
                }
                UnityEngine.Object.DestroyImmediate(group);
            }
            foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
            if (rock)
            {
                if (prefab.Contains("Cliff"))
                    go.transform.localScale = Vector3.Scale(go.transform.localScale, new Vector3(1, .42f, 1));
                var b = ScenarioBounds(go); float low = Ground(p).y;
                foreach (float x in new[] { -.7f, 0, .7f }) foreach (float z in new[] { -.7f, 0, .7f })
                    low = Mathf.Min(low, Ground(b.center + new Vector3(x * b.extents.x, 0, z * b.extents.z)).y);
                go.transform.position += Vector3.up * (low - b.min.y - Mathf.Min(1.2f, size * .04f));
                // Only substantial rock formations block driving; decorative chips remain passable.
                if (size > 4)
                    foreach (var f in go.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>().enabled))
                    { f.gameObject.layer = 8; f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh; }
            }
            var visible = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            var lod = go.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.01f, visible) }); lod.RecalculateBounds();
            float distance = size > 12 ? 450 : tree ? 240 : size > 3 ? 200 : 95;
            lod.SetLODs(new[] { new LOD(Mathf.Clamp(lod.size / (distance * 1.1547f), .001f, .15f), visible) });
            return go;
        }

        private static void ComposePlaceSandstone(Transform root)
        {
            var anchors = new[] { new Vector2(438,100), new Vector2(449,13), new Vector2(433,-78),
                new Vector2(446,-164), new Vector2(436,-259), new Vector2(442,-342) };
            for (int i = 0; i < anchors.Length; i++)
            {
                var c = new Vector3(anchors[i].x, 0, anchors[i].y);
                var cluster = PolishGroup(root, "Sandstone alcove " + (i + 1));
                // Unequal overlapping masses, then a broken apron on the exposed side.
                for (int j = 0; j < 3; j++)
                {
                    var p = c + new Vector3(j == 1 ? 6 : -5, 0, j * 12 - 12);
                    PlaceDetailProp(j == 1 ? "P_RockDesert_Clump09" : "P_RockDesert_Clump01", p,
                        j == 1 ? 40 + i % 3 * 3 : 25 + (i + j) % 3 * 4, 27 + i * 43 + j * 17, cluster, true);
                }
                for (int j = 0; j < 3; j++)
                    PlaceDetailProp("P_RockDesert_Clump01", c + new Vector3(-13 - j % 2 * 3, 0, j * 10 - 10),
                        9 + j * 2, i * 41 + j * 127, cluster, true);
                for (int j = 0; j < 13; j++)
                {
                    float angle = (145 + j * 17) * Mathf.Deg2Rad;
                    var p = c + new Vector3(Mathf.Cos(angle) * (18 + j % 3 * 2), 0, Mathf.Sin(angle) * (15 + j % 4));
                    string name = j < 5 ? "P_RockDesert_Clump01" : j % 3 == 0 ? "P_DesertBush02" : "P_DesertBush01";
                    PlaceDetailProp(name, p, j < 5 ? 2 + j * .55f : .65f + j % 3 * .35f, i * 51 + j * 137, cluster, j < 5);
                }
            }
        }

        private static void ComposePlaceHomes(Transform root)
        {
            var homes = environment.GetComponentsInChildren<Transform>()
                .Where(t => t.name == "Forest depot lodge" || t.name == "Lakeside service house").ToArray();
            foreach (var home in homes)
            {
                var group = PolishGroup(root, home.name + " - settled garden");
                var center = home.position;
                for (int i = 0; i < 22; i++)
                {
                    // Side/back foundations only: leave the front doorway and its access open.
                    float side = i % 2 == 0 ? -1 : 1;
                    var p = center + home.right * (6.5f + i % 3 * .8f) * side + home.forward * (-4 + i / 2 * .65f);
                    PlaceDetailProp(i % 5 == 0 ? "P_RockSpire_RockClump01" : i % 3 == 0 ? "P_Bush1_Light" : "P_Fern2_Yellow",
                        p, i % 5 == 0 ? 1.2f : i % 3 == 0 ? 1.25f : .6f, i * 137, group, i % 5 == 0);
                }
                var corner = center - home.right * 7.5f - home.forward * 3;
                if (!PlaceDetailClear(corner, 2)) corner = center + home.right * 7.5f - home.forward * 3;
                if (PlaceDetailClear(corner, 2))
                {
                    var props = PolishGroup(group, "Firewood and rest bench"); props.position = Ground(corner) - Vector3.up * .08f; props.rotation = home.rotation;
                    using (var shapes = new GarageFacadeShapes())
                    {
                        ReceivingFirewood(shapes, new Vector3(0,0,-1), WorkshopFinish("Timber"), WorkshopFinish("WallLight"));
                        ReceivingBench(shapes, new Vector3(0,0,1.6f), WorkshopFinish("Timber"), WorkshopFinish("Metal"));
                        shapes.Finish(props, "Place details " + home.name);
                    }
                    ConfigureDetailLod(props.gameObject, 120);
                }
            }
        }

        private static void ComposePlaceForest(Transform root)
        {
            // The open yard and tutorial camera sightline stay empty. These islands join
            // the workshop boundary to the existing woodland instead of random scatter.
            var islands = new[] { new Vector2(-60,-23), new Vector2(-79,16), new Vector2(-97,38),
                new Vector2(-137,65), new Vector2(-132,104), new Vector2(-96,105), new Vector2(-53,47) };
            for (int i = 0; i < islands.Length; i++)
            {
                var center = new Vector3(islands[i].x, 0, islands[i].y);
                var group = PolishGroup(root, "Woodland island " + (i + 1));
                PlaceDetailProp("P_RockSpire_RockClump01", center, 2.8f, i * 47, group, true);
                if (i % 2 == 0) PlaceDetailProp("P_FirStump1", center + new Vector3(2,0,1), 1.3f, i * 81, group);
                for (int j = 0; j < 11; j++)
                {
                    float angle = j * 2.4f + i;
                    var p = center + new Vector3(Mathf.Cos(angle) * (2 + j % 3), 0, Mathf.Sin(angle) * (1.6f + j % 3));
                    PlaceDetailProp(j % 4 == 0 ? "P_Bush1_Light" : j % 2 == 0 ? "P_Fern2" : "P_Grass2", p,
                        j % 4 == 0 ? 1.35f : .6f + j % 3 * .12f, j * 137 + i * 31, group);
                }
            }
        }
    }
}
