using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Editor
{
    // Deliberately local: does not run any world, forest or scenario generator.
    public static class ExtractionPrototypeAuthoring
    {
        private const string Evidence = "Docs/Validation/ExtractionPrototype";
        [Serializable] private sealed class PocketBaseline
        {
            public string TerrainGuid, SiteBefore;
            public int X, Z, Width, Height, Resolution;
            public float[] Heights;
            public Vector3 Wheel, Exit;
        }

        [MenuItem("Tow Trouble/Recovery/Apply R01 Extraction Prototype")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/TowTrouble/Scenes/OpenWorldRecovery.unity") throw new InvalidOperationException("Open OpenWorldRecovery first.");
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var map = UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            if (world == null || map == null) throw new InvalidOperationException("Missing recovery world/map.");
            var site = map.Sites.Single(s => s.Id == "R01");
            var terrain = Terrain.activeTerrains.First(t => Contains(t, site.Position));
            var data = terrain.terrainData;
            string asset = AssetDatabase.GetAssetPath(data);
            string guid = AssetDatabase.AssetPathToGUID(asset);
            var pose = Quaternion.Euler(site.Pitch, site.Heading, site.Roll);
            var wheel = site.Position - Vector3.up * site.Depth + pose * world.Casualty.CurrentModel.Anchors[0];
            var exit = Vector3.ProjectOnPlane(site.ExitDirection, Vector3.up).normalized;
            if (exit.sqrMagnitude < .9f) throw new InvalidOperationException("R01 needs an authored exit direction.");
            Directory.CreateDirectory(Evidence);
            string baselinePath = Evidence + "/R01TerrainBaseline.json";
            PocketBaseline baseline;
            if (File.Exists(baselinePath))
            {
                baseline = JsonUtility.FromJson<PocketBaseline>(File.ReadAllText(baselinePath));
                if (baseline.TerrainGuid != guid || baseline.Resolution != data.heightmapResolution
                    || Vector3.ProjectOnPlane(wheel - baseline.Wheel, Vector3.up).magnitude > .05f || Vector3.Distance(exit, baseline.Exit) > .01f)
                    throw new InvalidOperationException("R01 or its terrain changed since this prototype baseline. Review before re-authoring.");
            }
            else
            {
                int n = data.heightmapResolution;
                var relative = wheel - terrain.transform.position;
                int cx = Mathf.RoundToInt(relative.x / data.size.x * (n - 1));
                int cz = Mathf.RoundToInt(relative.z / data.size.z * (n - 1));
                int rx = Mathf.CeilToInt(4.5f / data.size.x * (n - 1));
                int rz = Mathf.CeilToInt(4.5f / data.size.z * (n - 1));
                int x = Mathf.Max(0, cx - rx), z = Mathf.Max(0, cz - rz);
                int width = Mathf.Min(n, cx + rx + 1) - x, height = Mathf.Min(n, cz + rz + 1) - z;
                var source = data.GetHeights(x, z, width, height);
                baseline = new PocketBaseline { TerrainGuid = guid, SiteBefore = JsonUtility.ToJson(site),
                    X = x, Z = z, Width = width, Height = height, Resolution = n, Wheel = wheel, Exit = exit, Heights = new float[width * height] };
                for (int j = 0; j < height; j++) for (int i = 0; i < width; i++) baseline.Heights[j * width + i] = source[j, i];
                File.WriteAllText(baselinePath, JsonUtility.ToJson(baseline, true));
            }

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("R01 arcade extraction prototype");
            Undo.RecordObject(map, "Configure R01 extraction");
            Undo.RegisterCompleteObjectUndo(data, "Author one-wheel rut");
            // Six centimetres of initial settling keeps every wheel inside the
            // existing suspension probe reach, including the wheel in the pocket.
            var originalSite = JsonUtility.FromJson<RecoverySite>(baseline.SiteBefore);
            site.Depth = originalSite.Depth + .06f;
            wheel = site.Position - Vector3.up * site.Depth + pose * world.Casualty.CurrentModel.Anchors[0];
            site.Extraction = new RecoveryExtractionSettings { Enabled = true, BlockedWheel = 0,
                Travel = 2.2f, BreakawayForce = 5600, MovingForce = 3600, MovingDrag = 4200, ClearHold = .2f, ReleaseDuration = .4f };
            site.Situation = "Roata din față-stânga este prinsă într-un șanț. Trage progresiv spre potecă.";
            site.EnglishSituation = "The front-left wheel is caught in a rut. Pull progressively toward the trail.";

            var patch = new float[baseline.Height, baseline.Width];
            var side = Vector3.Cross(Vector3.up, exit);
            int changed = 0;
            for (int z = 0; z < baseline.Height; z++) for (int x = 0; x < baseline.Width; x++)
            {
                var point = terrain.transform.position + new Vector3((baseline.X + x) / (float)(baseline.Resolution - 1) * data.size.x, 0,
                    (baseline.Z + z) / (float)(baseline.Resolution - 1) * data.size.z);
                var delta = point - wheel;
                float along = Vector3.Dot(delta, exit), across = Mathf.Abs(Vector3.Dot(delta, side));
                float width = 1 - Mathf.SmoothStep(0, 1, across / 1.05f);
                float ramp = along < 0 ? Mathf.SmoothStep(0, 1, (along + 1.1f) / 1.1f)
                    : 1 - Mathf.SmoothStep(0, 1, along / site.Extraction.Travel);
                float depth = .26f * width * ramp;
                patch[z, x] = baseline.Heights[z * baseline.Width + x] - depth / data.size.y;
                if (depth > .001f) changed++;
            }
            data.SetHeights(baseline.X, baseline.Z, patch);
            FitExistingTireMarks(terrain, wheel, exit, site.Extraction.Travel);
            // Configure invalidates only the contract cache; all other sites/bays/checkpoints stay as authored.
            var serialized = new SerializedObject(map);
            var roads = serialized.FindProperty("roadCheckpoints");
            var checkpoints = new Vector3[roads.arraySize];
            for (int i = 0; i < checkpoints.Length; i++) checkpoints[i] = roads.GetArrayElementAtIndex(i).vector3Value;
            map.Configure(map.Sites, map.Deliveries, checkpoints);
            EditorUtility.SetDirty(map); EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            ExtractionVisualAuthoring.Apply();
            Physics.SyncTransforms();
            Undo.CollapseUndoOperations(group);
            File.WriteAllText(Evidence + "/Authoring.json", JsonUtility.ToJson(new AuthoringReport
            { Scene = scene.path, Terrain = asset, ChangedHeightSamples = changed, Wheel = wheel, Exit = exit, Settings = site.Extraction }, true));
        }

        [Serializable] private sealed class AuthoringReport
        {
            public string Scene, Terrain;
            public int ChangedHeightSamples;
            public Vector3 Wheel, Exit;
            public RecoveryExtractionSettings Settings;
        }
        private static bool Contains(Terrain terrain, Vector3 point)
        {
            var p = point - terrain.transform.position; var s = terrain.terrainData.size;
            return p.x >= 0 && p.z >= 0 && p.x <= s.x && p.z <= s.z;
        }

        private static void FitExistingTireMarks(Terrain terrain, Vector3 wheel, Vector3 exit, float travel)
        {
            var side = Vector3.Cross(Vector3.up, exit);
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                string path = AssetDatabase.GetAssetPath(filter.sharedMesh);
                if (!path.StartsWith("Assets/TowTrouble/OpenWorld/Visuals/R01 approach ruts ", StringComparison.Ordinal)) continue;
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                bool changed = false;
                Undo.RecordObject(mesh, "Fit existing R01 tire marks to rut");
                for (int i = 0; i < vertices.Length; i++)
                {
                    var p = filter.transform.TransformPoint(vertices[i]); var delta = p - wheel;
                    float along = Vector3.Dot(delta, exit), across = Mathf.Abs(Vector3.Dot(delta, side));
                    if (along <= -1.1f || along >= travel || across >= 1.05f) continue;
                    p.y = terrain.SampleHeight(p) + terrain.transform.position.y + .016f;
                    var next = filter.transform.InverseTransformPoint(p);
                    if ((next - vertices[i]).sqrMagnitude < .00000001f) continue;
                    vertices[i] = next; changed = true;
                }
                if (!changed) continue;
                mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
            }
        }
    }
}
