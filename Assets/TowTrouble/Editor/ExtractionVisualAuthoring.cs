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
    // Owned visual derivatives only. Terrain heights, colliders and extraction
    // settings remain under the existing prototype authoring tool.
    public static class ExtractionVisualAuthoring
    {
        private const string Folder = "Assets/TowTrouble/OpenWorld/Visuals/Extraction";

        [MenuItem("Tow Trouble/Recovery/Refresh R01 Extraction Visuals")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode only.");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/TowTrouble/Scenes/OpenWorldRecovery.unity") throw new InvalidOperationException("Open OpenWorldRecovery first.");
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var site = world.Map.Sites.Single(s => s.Id == "R01");
            if (site.Extraction == null || !site.Extraction.Enabled) throw new InvalidOperationException("Apply the R01 extraction prototype first.");
            var terrain = Terrain.activeTerrains.First(t =>
            { var p = site.Position - t.transform.position; return p.x >= 0 && p.z >= 0 && p.x <= t.terrainData.size.x && p.z <= t.terrainData.size.z; });
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/TowTrouble/OpenWorld/Visuals", "Extraction");

            var pose = Quaternion.Euler(site.Pitch, site.Heading, site.Roll);
            var wheel = site.Position - Vector3.up * site.Depth + pose * world.Casualty.CurrentModel.Anchors[site.Extraction.BlockedWheel];
            var exit = Vector3.ProjectOnPlane(site.ExitDirection, Vector3.up).normalized;
            ClearR01Sightline(terrain, site, exit);
            var root = terrain.transform.parent.Find("R01 visible wheel rut");
            if (root == null)
            {
                root = new GameObject("R01 visible wheel rut").transform;
                root.SetParent(terrain.transform.parent, false);
                Undo.RegisterCreatedObjectUndo(root.gameObject, "R01 visible wheel rut");
            }
            Undo.RecordObject(root, "Position R01 rut visuals");
            root.SetPositionAndRotation(wheel, Quaternion.LookRotation(exit));
            root.localScale = Vector3.one;

            var soilTexture = terrain.terrainData.terrainLayers[1].diffuseTexture;
            var groundShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/TowTrouble/OpenWorld/Visuals/WheelImpressions.shader");
            var bed = Material("Exposed dark earth", groundShader, new Color(.49f, .32f, .16f, .97f), soilTexture);
            var rim = Material("Fresh rut edge", groundShader, new Color(.82f, .62f, .36f, .96f), soilTexture);
            var clodMaterial = Material("Broken soil clods", Shader.Find("Universal Render Pipeline/Lit"), new Color(.37f, .24f, .12f), soilTexture);
            var bankMaterial = Material("Cut soil banks", groundShader, new Color(.76f, .55f, .30f, .9f), soilTexture);
            var dustMaterial = Material("Recovery soil dust", Shader.Find("TowTrouble/Exhaust Vapor"), Color.white, null);

            float travel = site.Extraction.Travel;
            Surface("Dark rut bed", root, Patch(root, terrain, -1.05f, travel + .1f, .68f, .014f, 25, 11), bed);
            Surface("Left cut bank", root, Bank(root, terrain, travel, -1, false), bankMaterial);
            Surface("Right cut bank", root, Bank(root, terrain, travel, 1, false), bankMaterial);
            Surface("Exit soil shoulder", root, Bank(root, terrain, travel, 1, true), bankMaterial);
            // The light narrow line follows the real terrain at the exit plane.
            Surface("Visible exit lip", root, Patch(root, terrain, travel - .13f, travel + .13f, .81f, .026f, 5, 11), rim);
            var clodMesh = SaveMesh("Soil clod", Clod());
            var locations = new[] { new Vector2(-.67f,-.65f),new Vector2(.71f,-.32f),new Vector2(-.72f,.15f),new Vector2(.73f,.5f),
                new Vector2(-.66f,1.15f),new Vector2(.7f,1.45f),new Vector2(-.55f,travel),new Vector2(.5f,travel),new Vector2(.04f,travel+.1f) };
            for (int i = 0; i < locations.Length; i++)
            {
                var point = Ground(terrain, root.TransformPoint(new Vector3(locations[i].x, 0, locations[i].y)));
                var clod = Child(root, "Loose earth " + i);
                float size = .15f + (i % 3) * .035f;
                clod.SetPositionAndRotation(point + Vector3.up * size * .28f, Quaternion.Euler(i * 17, i * 137.5f, i * 13));
                clod.localScale = new Vector3(size, size * .62f, size * .8f);
                MeshRenderer(clod, clodMesh, clodMaterial);
            }
            var dust = Burst(root, "Release soil dust", dustMaterial, null);
            var clods = Burst(root, "Release soil clods", clodMaterial, clodMesh);
            var visuals = root.GetComponent<RecoveryExtractionVisuals>();
            if (visuals == null) visuals = Undo.AddComponent<RecoveryExtractionVisuals>(root.gameObject);
            Undo.RecordObject(visuals, "Bind soil release feedback");
            visuals.Configure(Array.IndexOf(world.Map.Sites, site), dust, clods);
            Undo.RecordObject(world, "Bind R01 extraction visuals");
            world.ConfigureExtractionVisuals(visuals);
            EditorUtility.SetDirty(visuals); EditorUtility.SetDirty(world);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static Material Material(string name, Shader shader, Color color, Texture texture)
        {
            if (shader == null) throw new InvalidOperationException("Missing native shader for " + name);
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
            Undo.RecordObject(material, "Configure R01 soil material");
            material.shader = shader; material.SetColor("_BaseColor", color);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Compaction")) material.SetFloat("_Compaction", .88f);
            if (material.HasProperty("_HardSurface")) material.SetFloat("_HardSurface", 0);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); return material;
        }

        private static Vector3 Ground(Terrain terrain, Vector3 point)
        { point.y = terrain.SampleHeight(point) + terrain.transform.position.y; return point; }

        [Serializable] private sealed class TreeBaseline
        { public int Sibling; public string Name; public Vector3 Position; }

        private static void ClearR01Sightline(Terrain terrain, RecoverySite site, Vector3 exit)
        {
            var forest = terrain.transform.parent.Find("Workshop forest - finished clearing");
            if (forest == null) return;
            string path = "Docs/Validation/ExtractionVisuals/TreeBaseline.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var side = Vector3.Cross(Vector3.up, exit);
            TreeBaseline baseline;
            Transform tree;
            if (File.Exists(path))
            {
                baseline = JsonUtility.FromJson<TreeBaseline>(File.ReadAllText(path));
                tree = forest.GetChild(baseline.Sibling);
                if (tree.name != baseline.Name) throw new InvalidOperationException("Review the R01 sightline tree after forest re-authoring.");
            }
            else
            {
                var expected = site.Position + side * 8 - exit * 2;
                tree = forest.Cast<Transform>().Where(t => t.name == "P_BirchTree2")
                    .OrderBy(t => Vector3.ProjectOnPlane(t.position - expected, Vector3.up).sqrMagnitude).FirstOrDefault();
                if (tree == null || Vector3.ProjectOnPlane(tree.position - expected, Vector3.up).magnitude > 2) return;
                baseline = new TreeBaseline { Sibling = tree.GetSiblingIndex(), Name = tree.name, Position = tree.position };
                File.WriteAllText(path, JsonUtility.ToJson(baseline, true));
            }
            // Move this single existing birch out of the normal recovery sightline.
            // Keep native art, scale, rotation and the original ground embed offset.
            float embed = baseline.Position.y - Ground(terrain, baseline.Position).y;
            var position = Ground(terrain, baseline.Position + side * 6 + exit * 2) + Vector3.up * embed;
            Undo.RecordObject(tree, "Clear R01 wheel sightline"); tree.position = position;
        }

        private static Mesh Patch(Transform root, Terrain terrain, float from, float to, float halfWidth, float lift, int rows, int columns)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var rut = new List<Vector2>();
            var ends = new List<Vector2>(); var colors = new List<Color>(); var triangles = new List<int>();
            for (int row = 0; row < rows; row++)
            {
                float t = row / (float)(rows - 1), along = Mathf.Lerp(from, to, t);
                for (int c = 0; c < columns; c++)
                {
                    float across = c / (float)(columns - 1);
                    float width = halfWidth * (1 + .08f * Mathf.Sin(row * 1.73f));
                    var point = Ground(terrain, root.TransformPoint(new Vector3((across * 2 - 1) * width, 0, along))) + Vector3.up * lift;
                    vertices.Add(root.InverseTransformPoint(point)); uv.Add(new Vector2(point.x, point.z) * .25f);
                    rut.Add(new Vector2(across, t * (to - from) + .65f)); ends.Add(Vector2.one);
                    float cap = Mathf.SmoothStep(0, 1, Mathf.Min(t, 1 - t) * 12);
                    colors.Add(new Color(1, 1, 1, cap));
                    if (row < rows - 1 && c < columns - 1)
                    { int a = row * columns + c; triangles.AddRange(new[] { a, a + columns, a + 1, a + 1, a + columns, a + columns + 1 }); }
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, rut); mesh.SetUVs(2, ends);
            mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh Clod()
        {
            var p = new[] {new Vector3(-.5f,-.4f,-.4f),new Vector3(.45f,-.5f,-.5f),new Vector3(.5f,-.4f,.4f),new Vector3(-.4f,-.5f,.5f),
                new Vector3(-.35f,.45f,-.3f),new Vector3(.3f,.5f,-.4f),new Vector3(.4f,.35f,.4f),new Vector3(-.5f,.4f,.3f)};
            int[] faces = {0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
            for (int i = 0; i < faces.Length; i += 3) { int swap = faces[i + 1]; faces[i + 1] = faces[i + 2]; faces[i + 2] = swap; }
            var vertices = faces.Select(i => p[i]).ToArray(); var mesh = new Mesh { name = "Soil clod" };
            mesh.vertices = vertices; mesh.uv = vertices.Select(v => new Vector2(v.x + .5f, v.z + .5f)).ToArray();
            mesh.triangles = Enumerable.Range(0, faces.Length).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh Bank(Transform root, Terrain terrain, float travel, int side, bool exitLip)
        {
            const int rows = 25, columns = 5;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var rut = new List<Vector2>(); var ends = new List<Vector2>(); var colors = new List<Color>();
            for (int row = 0; row < rows; row++)
            {
                float t = row / (float)(rows - 1);
                for (int c = 0; c < columns; c++)
                {
                    float cross = c / (float)(columns - 1);
                    float x = exitLip ? Mathf.Lerp(-.76f, .76f, t) : side * Mathf.Lerp(.43f, .87f, cross);
                    if (!exitLip) x *= 1 + .06f * Mathf.Sin(row * 1.3f);
                    float z = exitLip ? travel + Mathf.Lerp(-.14f, .17f, cross) : Mathf.Lerp(-.9f, travel - .05f, t);
                    var p = Ground(terrain, root.TransformPoint(new Vector3(x, 0, z)));
                    // Small loose-earth shoulders make the actual depression readable.
                    // They are visual dressing, at most 12 cm high, with no collider.
                    float cap = Mathf.SmoothStep(0, 1, Mathf.Min(t, 1 - t) * 10);
                    p.y += .012f + Mathf.Sin(cross * Mathf.PI) * (exitLip ? .075f : .12f) * cap;
                    vertices.Add(root.InverseTransformPoint(p)); uv.Add(new Vector2(p.x, p.z) * .3f);
                    rut.Add(new Vector2(cross, t * (exitLip ? 1.5f : travel + .85f) + .65f));
                    ends.Add(Vector2.one); colors.Add(new Color(1, 1, 1, cap));
                    if (row < rows - 1 && c < columns - 1)
                    {
                        int a = row * columns + c;
                        if (exitLip || side < 0) triangles.AddRange(new[] {a,a+1,a+columns,a+1,a+columns+1,a+columns});
                        else triangles.AddRange(new[] {a,a+columns,a+1,a+1,a+columns,a+columns+1});
                    }
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetUVs(1, rut); mesh.SetUVs(2, ends); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = Folder + "/" + name + ".asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            mesh.name = name;
            if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            Undo.RecordObject(saved, "Refresh R01 soil mesh"); EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(saved); AssetDatabase.SaveAssetIfDirty(saved); return saved;
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) { Undo.RecordObject(child, "Refresh R01 soil detail"); return child; }
            child = new GameObject(name).transform; child.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child.gameObject, "R01 soil detail"); return child;
        }

        private static void Surface(string name, Transform root, Mesh mesh, Material material)
        { var child = Child(root, name); child.localPosition = Vector3.zero; child.localRotation = Quaternion.identity; child.localScale = Vector3.one; MeshRenderer(child, SaveMesh(name, mesh), material); }

        private static void MeshRenderer(Transform child, Mesh mesh, Material material)
        {
            var filter = child.GetComponent<MeshFilter>(); if (filter == null) filter = Undo.AddComponent<MeshFilter>(child.gameObject);
            var renderer = child.GetComponent<UnityEngine.MeshRenderer>(); if (renderer == null) renderer = Undo.AddComponent<UnityEngine.MeshRenderer>(child.gameObject);
            Undo.RecordObject(filter, "Bind R01 soil geometry"); Undo.RecordObject(renderer, "Bind R01 soil material");
            filter.sharedMesh = mesh; renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            EditorUtility.SetDirty(filter); EditorUtility.SetDirty(renderer);
        }

        private static ParticleSystem Burst(Transform root, string name, Material material, Mesh mesh)
        {
            var child = Child(root, name); var ps = child.GetComponent<ParticleSystem>();
            if (ps == null) { child.gameObject.SetActive(false); ps = Undo.AddComponent<ParticleSystem>(child.gameObject); }
            Undo.RecordObject(ps, "Configure bounded soil release");
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = mesh == null ? 18 : 14;
            main.startLifetime = mesh == null ? .75f : .45f; main.startSpeed = 0; main.startSize = mesh == null ? .45f : .12f;
            main.gravityModifier = mesh == null ? -.02f : 1; main.startRotation3D = mesh != null;
            main.startColor = mesh == null ? new Color(.57f,.39f,.21f,.42f) : Color.white;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var size = ps.sizeOverLifetime; size.enabled = mesh == null;
            if (mesh == null) size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .7f, 1, 1.8f));
            var color = ps.colorOverLifetime; color.enabled = mesh == null;
            if (mesh == null)
            {
                var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                    new[] {new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(0,1)}); color.color = gradient;
            }
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); Undo.RecordObject(renderer, "Bind soil particles");
            renderer.renderMode = mesh == null ? ParticleSystemRenderMode.Billboard : ParticleSystemRenderMode.Mesh;
            if (mesh != null) renderer.mesh = mesh;
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); child.gameObject.SetActive(true);
            EditorUtility.SetDirty(ps); EditorUtility.SetDirty(renderer); return ps;
        }
    }
}
