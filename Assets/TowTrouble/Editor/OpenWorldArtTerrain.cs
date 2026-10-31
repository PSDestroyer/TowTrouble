using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static float Smooth01(float a, float b, float x) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, x));
        private static float ArtNoise(float x, float z, float scale) => Mathf.PerlinNoise(x * scale + 57.3f, z * scale + 19.1f);
        private static bool DesertAt(Vector3 p) => p.x > 165 && p.z < 160;
        private static float ShoreDistance(Vector3 p) => LakeDistance(new Vector2(p.x / 900 + .5f, .5f - p.z / 900)) - (ArtNoise(p.x, p.z, .035f) - .5f) * 12;
        private static float Bilinear(float[,] a, float x, float z)
        {
            int n = a.GetLength(0), ix = Mathf.Min((int)x, n - 2), iz = Mathf.Min((int)z, n - 2);
            return Mathf.Lerp(Mathf.Lerp(a[iz, ix], a[iz, ix + 1], x - ix), Mathf.Lerp(a[iz + 1, ix], a[iz + 1, ix + 1], x - ix), z - iz);
        }
        [MenuItem("Tow Trouble/Art Integration/1 - Continuous Landforms and Roads")]
        public static void IntegrateLandforms()
        {
            VisualContext(); ScenicPaths(); EnsureRecoveryOffsets(); ScenicRoadField(1025);
            var data = terrain.terrainData;
            string snapshotPath = VisualRoot + "/Art integration source terrain.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(snapshotPath) == null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data), snapshotPath);
            var source = AssetDatabase.LoadAssetAtPath<TerrainData>(snapshotPath);
            // Solve the height field around fixed road/pad constraints. Interpolation between
            // neighbouring routes replaces the old nearest-road terraces with continuous slopes.
            const int low = 257;
            var h = new float[low, low]; var next = new float[low, low];
            var target = new float[low, low]; var weight = new float[low, low];
            var pads = plan.Nodes.Where(n => n.Id == "g" || n.Id.Length == 1 || n.Id.StartsWith("R")).ToArray();
            for (int z = 0; z < low; z++) for (int x = 0; x < low; x++)
            {
                float wx = x / 256f * 1000 - 500, wz = z / 256f * 1000 - 500;
                float elevation = source.GetInterpolatedHeight(x / 256f, z / 256f) - 50;
                h[z, x] = target[z, x] = elevation;
                float d = roadDistance[z * 4, x * 4], w = roadWidth[z * 4, x * 4];
                if (d < w + 2.3f) { target[z, x] = roadHeight[z * 4, x * 4]; weight[z, x] = 1 - Smooth01(w + .25f, w + 2.3f, d); }
                foreach (var pad in pads)
                {
                    float radius = pad.Id == "g" ? 32 : pad.Id.Length == 1 ? 14 : 5;
                    float dist = Vector2.Distance(new Vector2(wx, wz), new Vector2(pad.Point.x, pad.Point.z));
                    if (dist < radius) { target[z, x] = pad.Height; weight[z, x] = 1; }
                }
                // Fix the water basin independently from the graded road system.
                float shore = ShoreDistance(new Vector3(wx, 0, wz));
                if (shore < 4 && d > w + 26) { target[z, x] = 8 + 3.2f * Smooth01(-14, 8, shore); weight[z, x] = 1; }
                if (x == 0 || z == 0 || x == low - 1 || z == low - 1) weight[z, x] = 1;
            }
            for (int pass = 0; pass < 460; pass++)
            {
                for (int z = 1; z < low - 1; z++) for (int x = 1; x < low - 1; x++)
                {
                    float average = (h[z, x - 1] + h[z, x + 1] + h[z - 1, x] + h[z + 1, x]) * .25f;
                    next[z, x] = Mathf.Lerp(average, target[z, x], weight[z, x]);
                }
                for (int i = 0; i < low; i++) { next[0, i] = target[0, i]; next[low - 1, i] = target[low - 1, i]; next[i, 0] = target[i, 0]; next[i, low - 1] = target[i, low - 1]; }
                var swap = h; h = next; next = swap;
            }
            var heights = new float[1025, 1025];
            for (int z = 0; z < 1025; z++) for (int x = 0; x < 1025; x++)
            {
                float wx = x / 1024f * 1000 - 500, wz = z / 1024f * 1000 - 500;
                float d = roadDistance[z, x], width = roadWidth[z, x];
                float elevation = Bilinear(h, x * .25f, z * .25f);
                // Small rolling detail outside the drive corridor, never sharp displacement under wheels.
                elevation += (ArtNoise(wx, wz, .045f) - .5f) * 2.4f * Smooth01(width + 4, 26, d);
                float shore = ShoreDistance(new Vector3(wx, 0, wz));
                if (shore < 16)
                    elevation = Mathf.Lerp(elevation, 8 + 5.5f * Smooth01(-17, 20, shore), (1-Smooth01(6,16,shore))*Smooth01(width+2,width+30,d));
                foreach (var pad in pads)
                {
                    float radius = pad.Id == "g" ? 32 : pad.Id.Length == 1 ? 14 : 5;
                    float dist = Vector2.Distance(new Vector2(wx, wz), new Vector2(pad.Point.x, pad.Point.z));
                    if (dist < radius + 16) elevation = Mathf.Lerp(pad.Height, elevation, Smooth01(radius, radius + 16, dist));
                }
                elevation = Mathf.Lerp(roadHeight[z, x], elevation, Smooth01(width + .65f, width + 4, d));
                
                heights[z, x] = (elevation + 50) / 350;
            }
            // Smooth the transition without projecting the nearest road height across watershed
            // boundaries between roads at different elevations (which creates vertical terraces).
            var relaxed=(float[,])heights.Clone();
            for(int pass=0;pass<220;pass++)
            {
                for(int z=1;z<1024;z++)for(int x=1;x<1024;x++)
                    relaxed[z,x]=roadDistance[z,x]<roadWidth[z,x]+.7f?heights[z,x]:(heights[z,x]*4+heights[z-1,x]+heights[z+1,x]+heights[z,x-1]+heights[z,x+1])/8;
                var swap=heights;heights=relaxed;relaxed=swap;
            }
            Undo.RegisterCompleteObjectUndo(data, "Integrate Tow County landforms");
            data.SetHeights(0, 0, heights);
            GradeRecoveryPullouts();
            VisualAsset(UnityEngine.Object.Instantiate(data), "Road graded terrain snapshot.asset");
            BuildScenicRoadMeshes(); PolishWaterAndBridges(); PaintFinishedGround();
            RegroundExistingDressing(); BuildIrregularMud(); BuildRoadWear(); UpdateRecoveryPullouts();
            var map = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RecoveryMap>()).Single();
            map.Configure(map.Sites, map.Deliveries, scenicRoads.Where(r => !r.Edge.Trail).SelectMany(r => r.Points.Where((p, i) => i % 8 == 0 || i == r.Points.Length - 1).Select(p => p + Vector3.up * .14f)).ToArray());
            EditorUtility.SetDirty(map); EditorUtility.SetDirty(data); SaveVisualScene();
        }
        private static void PaintFinishedGround()
        {
            var data = terrain.terrainData; var layers = data.terrainLayers;
            var tints = new[] { new Color(.80f,.84f,.63f), new Color(.86f,.76f,.58f), new Color(.93f,.86f,.68f), new Color(.71f,.73f,.68f), new Color(.95f,.98f,1), new Color(.99f,.83f,.62f), new Color(.45f,.43f,.31f), new Color(.64f,.76f,.86f) };
            for (int i = 0; i < layers.Length; i++) { layers[i].diffuseRemapMax = tints[i]; layers[i].tileSize = Vector2.one * (i == 3 ? 12 : i == 4 ? 10 : i == 1 ? 5 : 8); layers[i].normalScale = .7f; EditorUtility.SetDirty(layers[i]); }
            const int n = 1024; data.alphamapResolution = n; var alpha = new float[n, n, 8];
            var mudSites = plan.Sites.Where(s => s.Id == "R02" || s.Id == "R03" || s.Biome == 1).Select(s => RecoveryPoint(s)).ToArray();
            for (int z = 0; z < n; z++) for (int x = 0; x < n; x++)
            {
                float u = x / (float)(n - 1), v = z / (float)(n - 1), wx = u * 1000 - 500, wz = v * 1000 - 500;
                var p = new Vector3(wx, data.GetInterpolatedHeight(u, v) - 50, wz);
                float noise = ArtNoise(wx, wz, .035f), fine = ArtNoise(wx, wz, .16f);
                float slope = data.GetSteepness(u, v), rock = Smooth01(24, 46, slope);
                float snow = Smooth01(116 + noise * 12, 143 + noise * 9, p.y) * (1 - rock * .8f);
                float desert = Smooth01(145, 235, wx + (noise - .5f) * 24) * (1 - Smooth01(120, 190, wz));
                float shore = ShoreDistance(p), sand = (1 - Smooth01(6, 28 + noise * 14, shore));
                sand = Mathf.Max(sand, (1 - Smooth01(-405, -320, wz)) * (1 - Smooth01(45, 155, wx)));
                float woodland = Mathf.Clamp01((noise - .30f) * 1.5f);
                alpha[z, x, 0] = 1 - woodland; alpha[z, x, 6] = woodland;
                BlendLayer(alpha, z, x, 3, rock); BlendLayer(alpha, z, x, 2, sand); BlendLayer(alpha, z, x, 5, desert); BlendLayer(alpha, z, x, 4, snow);
                int rx = Mathf.RoundToInt(u * 1024), rz = Mathf.RoundToInt(v * 1024);
                float d = roadDistance[rz, rx], w = roadWidth[rz, rx];
                float road = (1 - Smooth01(w - 1.2f + (fine - .5f) * .7f, w + 1.8f + noise * 1.5f, d)) * (1-Smooth01(26,48,slope));
                int roadLayer = snow > .45f ? 7 : desert > .5f ? 5 : 1;
                BlendLayer(alpha, z, x, roadLayer, road * .92f);
                foreach(var site in plan.Sites)
                {
                    var entry=nodes[site.Node].Point;var end=RecoveryPoint(site);
                    float access=Distance(new Vector2(wx,wz),new Vector2(entry.x,entry.z),new Vector2(end.x,end.z),out _);
                    if(access<5)BlendLayer(alpha,z,x,roadLayer,(1-Smooth01(2.2f,5,access))*.68f*(1-Smooth01(26,48,slope)));
                }
                // Gravel and soil collect along the shoulder. This band has a broken organic edge.
                float shoulder = (1 - Smooth01(.6f, 2.4f, Mathf.Abs(d - w - .5f))) * (.18f + fine * .3f) * (1-Smooth01(26,48,slope));
                if (snow < .4f && desert < .5f && sand < .8f) BlendLayer(alpha, z, x, 6, shoulder);
                foreach (var mud in mudSites)
                {
                    float dx = (wx - mud.x) / 10.5f, dz = (wz - mud.z) / 12;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    if (distance < 1.65f) BlendLayer(alpha, z, x, 6, (1 - Smooth01(.75f, 1.55f + (noise - .5f) * .2f, distance)) * .96f);
                }
                float yard = Vector2.Distance(new Vector2(wx, wz), new Vector2(nodes["g"].Point.x, nodes["g"].Point.z + 8));
                if (yard < 31) BlendLayer(alpha, z, x, 1, (1 - Smooth01(20, 31, yard)) * .93f);
            }
            data.SetAlphamaps(0, 0, alpha);
        }
        private static void BuildIrregularMud()
        {
            foreach (var filter in environment.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name.EndsWith(" - Living Mud")))
            {
                var center = RecoveryPoint(plan.Sites.Single(s=>s.Id==filter.name.Substring(0,3)));
                const int n = 48; const float extent = 22;
                var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
                Func<float, float, float> shape = (x, z) => Mathf.Sqrt(x * x / 81 + z * z / 100) + (ArtNoise(center.x + x, center.z + z, .20f) - .5f) * .18f;
                for (int z = 0; z <= n; z++) for (int x = 0; x <= n; x++)
                {
                    float dx = (x / (float)n - .5f) * extent, dz = (z / (float)n - .5f) * extent;
                    var p = Ground(center + new Vector3(dx, 0, dz));
                    p.y += .025f - Smooth01(.78f, 1, shape(dx, dz)) * .19f;
                    vertices.Add(filter.transform.InverseTransformPoint(p)); uv.Add(new Vector2(x / (float)n, z / (float)n));
                    if (x < n && z < n && shape(dx + extent / n * .5f, dz + extent / n * .5f) < 1.03f)
                    { int i = z * (n + 1) + x; tri.AddRange(new[] { i, i + n + 1, i + 1, i + 1, i + n + 1, i + n + 2 }); }
                }
                var mesh = new Mesh { name = filter.name + " organic shoreline" };
                mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                filter.sharedMesh = VisualAsset(mesh, filter.name.Substring(0, 3) + " organic mud.asset");
                var collider = filter.GetComponent<MeshCollider>(); collider.sharedMesh = null; collider.sharedMesh = filter.sharedMesh;
            }
            // The terrain, rather than the mesh edge, now defines the waterline.
            var lakeFilter = environment.GetComponentsInChildren<MeshFilter>().First(f => f.name.StartsWith("Lake -"));
            var points = ArtLakeOutline();
            var water = new Mesh { name = "Water beneath irregular banks" }; water.SetVertices(points); water.SetTriangles(ShoreTriangles(points), 0); water.SetUVs(0, points.Select(p => new Vector2(p.x * .02f, p.z * .02f)).ToList()); water.RecalculateNormals(); water.RecalculateBounds();
            lakeFilter.sharedMesh = VisualAsset(water, "Art lake water.asset");
        }
        private static void BuildRoadWear()
        {
            var previous = environment.Find("Road wheel tracks and gravel"); if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var parent = ScenicGroup("Road wheel tracks and gravel");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(VisualRoot + "/RoadWear.shader");
            if (shader == null) throw new InvalidOperationException("RoadWear shader must be imported first.");
            var dirt = ScenicMaterial("Road worn wheel paths", new Color(.42f,.32f,.19f,.48f), shader);
            dirt.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SoStylized/Environment/Landscape/Textures/T_Dirt_BC.png"));
            var snow = VisualAsset(new Material(dirt) { name = "Snow compacted tire paths" }, "Snow compacted tire paths.mat"); snow.SetColor("_BaseColor", new Color(.34f,.49f,.63f,.35f));
            foreach (var path in scenicRoads)
            {
                var vs = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var tr = new List<int>();
                foreach (int side in new[] { -1, 1 })
                {
                    int start = vs.Count;
                    for (int i = 0; i < path.Points.Length; i++)
                    {
                        var p = path.Points[i]; var direction = Flat(path.Points[Mathf.Min(i + 1, path.Points.Length - 1)] - path.Points[Mathf.Max(0, i - 1)]).normalized;
                        var right = Vector3.Cross(Vector3.up, direction);
                        float drift = Mathf.Sin(i * .045f) * .18f;
                        for (int k = 0; k < 3; k++) { var q = Ground(p + right * (side * .85f + drift + (k - 1) * .37f)); q.y -= .025f; vs.Add(q); uv.Add(new Vector2(q.x * .33f, q.z * .33f)); colors.Add(new Color(1, 1, 1, k == 1 ? .7f + ArtNoise(q.x, q.z, .5f) * .3f : 0)); }
                        if (i < path.Points.Length - 1 && !OnRiverBridge(path.Points[i],5)) for (int k = 0; k < 2; k++) { int a = start + i * 3 + k; tr.AddRange(new[] { a, a + 3, a + 1, a + 1, a + 3, a + 4 }); }
                    }
                }
                var mesh = new Mesh { name = path.Name + " tire wear" }; mesh.SetVertices(vs); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(tr, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var go = new GameObject(path.Name + " wheel wear", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
                go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, path.Name + "-wear.asset");
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = path.Points[path.Points.Length / 2].y > 125 ? snow : dirt; renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            EditorUtility.SetDirty(dirt); EditorUtility.SetDirty(snow);
        }
    }
}
