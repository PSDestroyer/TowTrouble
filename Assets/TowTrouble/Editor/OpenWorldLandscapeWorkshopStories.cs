using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private struct CompositionStoryRoad
        {
            internal Vector3 A, B, LeftA, LeftB, RightA, RightB;
        }

        // Called after the landscape is shaped. These surfaces are visual only:
        // the authored recovery support, wheel positions and obstacles retain ownership of physics.
        private static void ComposeLandscapeWorkshopAndStories(Transform root)
        {
            var world = UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            if (world == null || world.Map == null) throw new InvalidOperationException("Recovery world is missing.");
            RefineIncidentGroundMaterials();
            var roads = CompositionStoryRoads();
            ComposeWorkshopCourtyard(root, world, roads);
            var stories = PolishGroup(root, "Road to recovery incident stories");
            int authored = 0;
            foreach (var site in world.Map.Sites)
            {
                // The original wheel rut is already the tested, readable tutorial scene.
                if (site.Id == "R01") continue;
                if (!CompositionClosestRoad(roads, site.Position, out var edge, out var tangent)) continue;
                var forward = Quaternion.Euler(0, site.Heading, 0) * Vector3.forward;
                var approach = Vector3.Dot(forward, edge - site.Position) >= 0 ? forward : -forward;
                var end = Ground(site.Position + approach * 3.8f);
                float length = Flat(end - edge).magnitude;
                if (length < 1.2f || length > 75) continue;

                var group = PolishGroup(stories, site.Id + " - " + site.English);
                var inward = Flat(end - edge).normalized;
                float turn = Mathf.Min(length * .28f, 8);
                // Enter from the actual shoulder, then settle into the vehicle's direction.
                var start = Ground(edge - inward * .55f);
                var control1 = start + inward * turn + tangent * Mathf.Clamp(site.Tier * .28f, 0, 1.1f);
                var control2 = end + approach * Mathf.Min(length * .25f, 4);
                var points = new List<Vector3>();
                int rows = Mathf.Clamp(Mathf.CeilToInt(length / .32f), 8, 240);
                for (int i = 0; i <= rows; i++)
                {
                    float t = i / (float)rows, u = 1 - t;
                    points.Add(Ground(u * u * u * start + 3 * u * u * t * control1 + 3 * u * t * t * control2 + t * t * t * end));
                }
                HidePreviousIncidentMarks(site);
                ComposeIncidentWheelBands(group, site, points);
                ComposeIncidentClues(group, site, points);
                ConfigureDetailLod(group.gameObject, 175);
                authored++;
            }
            Debug.Log("[TowTrouble] Composed workshop access and " + authored + " road-linked recovery stories; original tutorial retained.");
        }

        private static void RefineIncidentGroundMaterials()
        {
            // Wet marsh used a second brown multiplier over an already brown albedo.
            // In linear lighting that made the ditch centres almost black, unlike the
            // adjacent Living Mud. Rebind only the authored marsh skins; the source
            // material, tutorial, terrain paint and physical support stay independent.
            var broad = environment.Find("Broad recovery ground zones");
            if (broad != null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(BroadFolder + "/Wet marsh.mat");
                if (source == null) throw new InvalidOperationException("Authored marsh material is missing.");
                var material = new Material(source) { name = "Incident exposed wet soil", enableInstancing = true };
                material.SetColor("_CoverTint", new Color(.95f, .83f, .67f));
                material = VisualAsset(material, "LandscapeComposition/Incident exposed wet soil.mat");
                foreach (var renderer in broad.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.name.EndsWith(" - marsh basin", StringComparison.Ordinal)) continue;
                    if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader != source.shader) continue;
                    Undo.RecordObject(renderer, "Match incident soil to surrounding earth");
                    renderer.sharedMaterial = material;
                    EditorUtility.SetDirty(renderer);
                }
            }

            // Broad-ground generation replaced the tutorial's two-submesh patch with
            // one surface, but retained its second opaque bank material. Match slots
            // to the actual mesh so that its last submesh is not submitted twice.
            var pools = environment.Find("Living Mud - forest and lake recoveries");
            if (pools == null) return;
            foreach (var filter in pools.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.name.EndsWith(" - Living Mud", StringComparison.Ordinal)
                    || filter.sharedMesh == null || filter.sharedMesh.subMeshCount != 1) continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterials.Length <= 1) continue;
                Undo.RecordObject(renderer, "Remove obsolete tutorial mud bank slot");
                renderer.sharedMaterials = new[] { renderer.sharedMaterial };
                EditorUtility.SetDirty(renderer);
            }
        }

        private static List<CompositionStoryRoad> CompositionStoryRoads()
        {
            var result = new List<CompositionStoryRoad>();
            var root = environment.Find("Road network - 24 recovery spurs");
            if (root == null) throw new InvalidOperationException("Current authored roads are missing.");
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                // Imported workshop apron meshes do not use the authored six-column road format.
                if (!filter.name.StartsWith("Road-") || filter.sharedMesh == null) continue;
                var v = filter.sharedMesh.vertices;
                if (v.Length < 12 || v.Length % 6 != 0) continue;
                for (int i = 0; i < v.Length / 6 - 1; i++)
                {
                    int a = i * 6, b = a + 6;
                    result.Add(new CompositionStoryRoad
                    {
                        A = filter.transform.TransformPoint((v[a + 2] + v[a + 3]) * .5f),
                        B = filter.transform.TransformPoint((v[b + 2] + v[b + 3]) * .5f),
                        LeftA = filter.transform.TransformPoint(v[a + 1]), LeftB = filter.transform.TransformPoint(v[b + 1]),
                        RightA = filter.transform.TransformPoint(v[a + 4]), RightB = filter.transform.TransformPoint(v[b + 4])
                    });
                }
            }
            if (result.Count == 0) throw new InvalidOperationException("No authored road segments were found.");
            return result;
        }

        private static bool CompositionClosestRoad(List<CompositionStoryRoad> roads, Vector3 point, out Vector3 edge, out Vector3 tangent)
        {
            edge = tangent = default;
            float best = float.MaxValue;
            foreach (var road in roads)
            {
                var delta = Flat(road.B - road.A);
                if (delta.sqrMagnitude < .001f) continue;
                float t = Mathf.Clamp01(Vector3.Dot(Flat(point - road.A), delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                var center = Vector3.Lerp(road.A, road.B, t);
                // Height matters on switchbacks: a road directly below is not the incident approach.
                float cost = Flat(center - point).sqrMagnitude + (center.y - point.y) * (center.y - point.y) * 2;
                if (cost >= best) continue;
                best = cost;
                var left = Vector3.Lerp(road.LeftA, road.LeftB, t);
                var right = Vector3.Lerp(road.RightA, road.RightB, t);
                edge = Flat(left - point).sqrMagnitude < Flat(right - point).sqrMagnitude ? left : right;
                tangent = delta.normalized;
            }
            return best < float.MaxValue;
        }

        private static void HidePreviousIncidentMarks(RecoverySite site)
        {
            foreach (var groupName in new[] { "County landscape polish/Recovery accident stories", "Progression damage trails", "Distinct recovery situations" })
            {
                var parent = environment.Find(groupName);
                if (parent == null) continue;
                foreach (Transform group in parent)
                {
                    if (!group.name.StartsWith(site.Id + " ")) continue;
                    foreach (var renderer in group.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (!renderer.name.Contains("accident skid and wheel rut") && renderer.name != "Approach from existing road") continue;
                        Undo.RecordObject(renderer, "Replace outdated incident surface");
                        renderer.enabled = false;
                    }
                }
            }
        }

        private static Material CompositionStoryMaterial(int layer, bool hard)
        {
            string name = "Incident tread " + layer + (hard ? " firm" : " loose");
            var material = new Material(Shader.Find("TowTrouble/Wheel Impressions")) { name = name, enableInstancing = true };
            material.SetTexture("_BaseMap", terrain.terrainData.terrainLayers[layer].diffuseTexture);
            material.SetColor("_BaseColor", layer == 4 ? new Color(.86f, .92f, .98f, .56f)
                : layer == 2 || layer == 5 ? new Color(.95f, .88f, .74f, .56f) : new Color(.83f, .74f, .61f, .48f));
            material.SetFloat("_Compaction", .9f);
            material.SetFloat("_HardSurface", hard ? .75f : .05f);
            material.SetFloat("_RutDepth", layer == 4 ? .07f : .045f);
            return VisualAsset(material, "LandscapeComposition/" + name + ".mat");
        }

        private static void ComposeIncidentWheelBands(Transform parent, RecoverySite site, List<Vector3> points)
        {
            bool snow = site.Biome == 3 || site.Id == "R12", sand = site.Biome >= 4;
            int layer = snow ? 4 : sand ? (site.Biome == 4 ? 2 : 5) : 1;
            bool hard = site.Kind == RecoverySituationKind.EngineFailure || site.Kind == RecoverySituationKind.Rocks;
            var material = CompositionStoryMaterial(layer, hard);
            var vs = new List<Vector3>(); var uv = new List<Vector2>(); var rut = new List<Vector2>();
            var ends = new List<Vector2>(); var colors = new List<Color>(); var triangles = new List<int>();
            float total = 0;
            for (int i = 1; i < points.Count; i++) total += Vector3.Distance(points[i], points[i - 1]);
            for (int lane = 0; lane < 2; lane++)
            {
                int offset = vs.Count; float traveled = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    if (i > 0) traveled += Vector3.Distance(points[i], points[i - 1]);
                    var direction = Flat(points[Mathf.Min(points.Count - 1, i + 1)] - points[Mathf.Max(0, i - 1)]).normalized;
                    var across = Vector3.Cross(Vector3.up, direction);
                    float t = i / (float)(points.Count - 1), halfWidth = .25f + site.Tier * .02f * Mathf.SmoothStep(0, 1, t);
                    for (int c = 0; c < 7; c++)
                    {
                        float u = c / 6f;
                        var p = Ground(points[i] + across * ((lane == 0 ? -.82f : .82f) + (u * 2 - 1) * halfWidth));
                        float bank = hard ? 0 : (snow ? .045f : .027f) * Mathf.Pow(Mathf.Abs(u * 2 - 1), 2) * Mathf.Sin(t * Mathf.PI);
                        p.y += .014f + bank;
                        vs.Add(parent.InverseTransformPoint(p)); uv.Add(new Vector2(p.x, p.z) * .3f);
                        rut.Add(new Vector2(u, traveled)); ends.Add(new Vector2((total - traveled) / .85f, 1));
                        // Natural short interruptions and tapered arrival keep these from reading as painted rails.
                        float broken = .55f + .45f * Smooth01(.1f, .65f, Mathf.PerlinNoise(traveled * .67f + lane * 3, site.Tier + .6f));
                        float alpha = broken * Mathf.SmoothStep(0, 1, traveled / 1.5f) * Mathf.SmoothStep(0, 1, (total - traveled) / 1.2f);
                        var local = p - terrain.transform.position;
                        float slope = terrain.terrainData.GetSteepness(local.x / terrain.terrainData.size.x, local.z / terrain.terrainData.size.z);
                        alpha *= 1 - Smooth01(52, 68, slope);
                        if (site.Kind == RecoverySituationKind.Water && p.y < 10.9f) alpha = 0;
                        colors.Add(new Color(1, 1, 1, alpha));
                        if (i < points.Count - 1 && c < 6)
                        {
                            int a = offset + i * 7 + c;
                            triangles.AddRange(new[] { a, a + 7, a + 1, a + 1, a + 7, a + 8 });
                        }
                    }
                }
            }
            CompositionSurface(parent, site.Id + " shoulder to accident treads", vs, uv, colors, triangles, material, rut, ends);
        }

        private static void ComposeIncidentClues(Transform parent, RecoverySite site, List<Vector3> points)
        {
            // The existing smoke, deep wheel pockets and towable obstacles remain the main story.
            // Only small off-route debris is added; never a second barrier or a sign naming an accident.
            if (site.Tier < 1 || points.Count < 8 || site.Kind == RecoverySituationKind.EngineFailure) return;
            int i = points.Count / 3;
            var across = Vector3.Cross(Vector3.up, Flat(points[i + 1] - points[i - 1]).normalized);
            for (int n = 0; n < Mathf.Min(2, site.Tier); n++)
            {
                var p = Ground(points[i] + across * (n == 0 ? 2.3f : -2.6f));
                if (!CompositionClear(p, .45f) || Flat(p - site.Position).magnitude < 7) continue;
                string prefab = site.Biome >= 4 ? "P_RockDesert_Clump04" : site.Biome == 3 || site.Biome == 2 ? "P_RockClumpClassic2" : "P_Fern2";
                var go = PolishProp(prefab, p, site.Biome <= 1 ? .38f : .23f, 31 + n * 63, parent, 115);
                go.name = site.Biome <= 1 ? "Bent verge fern" : "Dislodged shoulder gravel";
                if (site.Biome <= 1)
                {
                    go.transform.rotation *= Quaternion.Euler(0, 0, n == 0 ? 28 : -23);
                    go.transform.position += Vector3.up * (Ground(p).y - ScenicBounds(go).min.y - .03f);
                }
            }
        }

        private static void ComposeWorkshopCourtyard(Transform root, RecoveryWorld world, List<CompositionStoryRoad> roads)
        {
            var parent = PolishGroup(root, "Workshop connected service court");
            var yard = environment.Find("Mountain cliffs and garage grounds/Garage working yard");
            if (yard == null) throw new InvalidOperationException("Existing garage working yard is missing.");
            var material = CompositionCourtyardMaterial();
            var entrance = Ground(yard.TransformPoint(new Vector3(2.5f, 0, 8)));
            CompositionClosestRoad(roads, entrance, out var road, out _);
            ComposeCourtSurface(parent, "Salvage yard open entrance", Ground(road), entrance, 3.8f, material);
            // Broaden the receiving surface at the real delivery position, without moving its trigger.
            var delivery = world.Map.Deliveries.FirstOrDefault(d => d.Id == "A");
            if (delivery != null)
            {
                CompositionClosestRoad(roads, delivery.Position, out _, out var tangent);
                ComposeCourtSurface(parent, "Workshop receiving apron", Ground(delivery.Position - tangent * 12), Ground(delivery.Position + tangent * 12), 7.2f, material);
            }

            var storage = yard.Find("Workshop storage and work area");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(PolishFolder + "/Weathered wood.mat");
            var metal = AssetDatabase.LoadAssetAtPath<Material>(PolishFolder + "/Oxidised metal.mat");
            var trim = AssetDatabase.LoadAssetAtPath<Material>(PolishFolder + "/Faded parking paint.mat");
            if (wood == null || metal == null || trim == null) throw new InvalidOperationException("Existing yard materials are missing.");
            if (storage != null)
            {
                // Reuse the original equipment and parking, instead of adding another supply yard.
                int leg = 0;
                foreach (Transform item in storage)
                {
                    Vector3 p = item.localPosition;
                    if (item.name == "Outdoor workbench") p.x = -6;
                    else if (item.name == "Bench legs") p.x = -7.2f + leg++ * 2.4f;
                    else continue;
                    Undo.RecordObject(item, "Organize workshop service station");
                    item.localPosition = p;
                }
            }
            var canopy = PolishGroup(parent, "Open equipment shelter");
            canopy.position = Ground(yard.TransformPoint(new Vector3(-6, 0, 5)));
            foreach (float x in new[] { -4.3f, 4.3f }) foreach (float z in new[] { -1.7f, 1.7f })
            {
                var footing = Ground(canopy.TransformPoint(new Vector3(x, 0, z)));
                float h = Mathf.Max(2.1f, canopy.position.y + 2.6f - footing.y);
                PolishBox(canopy, "Timber shelter upright", canopy.InverseTransformPoint(footing) + Vector3.up * h * .5f, new Vector3(.16f, h, .16f), wood);
            }
            PolishBox(canopy, "Shallow service canopy", new Vector3(0, 2.68f, 0), new Vector3(9.2f, .13f, 4.2f), metal, new Vector3(5, 0, 0));
            foreach (float z in new[] { -1.65f, 1.65f }) PolishBox(canopy, "Shelter cross beam", new Vector3(0, 2.48f, z), new Vector3(8.9f, .18f, .18f), wood);
            ConfigureDetailLod(canopy.gameObject, 230);

            // A pair of guide marks describes the opening; it does not close the towing turn.
            foreach (float x in new[] { -1.5f, 6.5f })
            {
                var p = Ground(yard.TransformPoint(new Vector3(x, 0, 9)));
                PolishBox(parent, "Low yard entrance paint", parent.InverseTransformPoint(p) + Vector3.up * .025f, new Vector3(.12f, .015f, 1.6f), trim);
            }
            ConfigureDetailLod(parent.gameObject, 240);
        }

        private static Material CompositionCourtyardMaterial()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(CliffYardFolder + "/Garage weathered gravel.mat");
            if (source == null) throw new InvalidOperationException("Existing workshop ground blend is missing.");
            var material = new Material(source) { name = "Connected workshop gravel", enableInstancing = true };
            material.SetColor("_CoverTint", new Color(.82f, .79f, .68f));
            return VisualAsset(material, "LandscapeComposition/Connected workshop gravel.mat");
        }

        private static void ComposeCourtSurface(Transform parent, string name, Vector3 start, Vector3 end, float halfWidth, Material material)
        {
            var vs = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var triangles = new List<int>();
            var direction = Flat(end - start).normalized; var side = Vector3.Cross(Vector3.up, direction);
            int rows = Mathf.Max(8, Mathf.CeilToInt(Vector3.Distance(start, end) / .6f));
            const int columns = 19;
            for (int row = 0; row <= rows; row++) for (int column = 0; column < columns; column++)
            {
                float t = row / (float)rows, u = column / (float)(columns - 1) * 2 - 1;
                var center = Vector3.Lerp(start, end, t);
                var p = Ground(center + side * (u * halfWidth)); p.y += .016f;
                float feather = 1 - Smooth01(.55f, 1, Mathf.Abs(u));
                float caps = Smooth01(0, .14f, t) * (1 - Smooth01(.86f, 1, t));
                float alpha = feather * caps * (.42f + .12f * Mathf.PerlinNoise(p.x * .3f, p.z * .3f));
                vs.Add(parent.InverseTransformPoint(p)); uv.Add(new Vector2(p.x, p.z) * .2f); colors.Add(new Color(1, 1, 1, alpha));
                if (row < rows && column < columns - 1)
                {
                    int a = row * columns + column;
                    triangles.AddRange(new[] { a, a + columns, a + 1, a + 1, a + columns, a + columns + 1 });
                }
            }
            CompositionSurface(parent, name, vs, uv, colors, triangles, material);
        }

        private static void CompositionSurface(Transform parent, string name, List<Vector3> vertices, List<Vector2> uv,
            List<Color> colors, List<int> triangles, Material material, List<Vector2> rut = null, List<Vector2> ends = null)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            if (rut != null) mesh.SetUVs(1, rut);
            if (ends != null) mesh.SetUVs(2, ends);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.layer = 8; go.isStatic = true;
            go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "LandscapeComposition/" + name + ".asset");
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            ConfigureDetailLod(go, 200);
        }
    }
}
