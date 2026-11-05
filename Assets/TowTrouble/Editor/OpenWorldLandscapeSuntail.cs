using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private const string SuntailExteriorFolder = CompositionFolder + "/SuntailExteriors";
        private const string SuntailModules = "Assets/Suntail Village/Models/Building Modules/";
        private static readonly Dictionary<Material, Material> suntailExteriorMaterials = new Dictionary<Material, Material>();
        private static readonly List<object> suntailExteriorAudits = new List<object>();

        private static void ComposeLandscapeSuntail(Transform root)
        {
            Directory.CreateDirectory(SuntailExteriorFolder + "/Meshes");
            Directory.CreateDirectory(SuntailExteriorFolder + "/Materials");
            AssetDatabase.Refresh();
            suntailExteriorMaterials.Clear();
            suntailExteriorAudits.Clear();
            var group = PolishGroup(root, "Suntail exterior service landmarks");
            var forest = BuildSuntailExteriorPrefab("Forest depot lodge", "Assets/Suntail Village/Prefabs/Buildings/House_1.prefab", true);
            var lake = BuildSuntailExteriorPrefab("Lakeside service house", "Assets/Suntail Village/Prefabs/Buildings/House_6.prefab", true);
            var canopy = BuildSuntailExteriorPrefab("Open timber canopy", "Assets/Suntail Village/Prefabs/Building Modules/Decor/Wooden_Canopy_1.prefab", false);
            // Buildings belong to their surveyed roadside plots. Receiving bays
            // can move off-road without shifting the buildings on every rebuild.
            var forestPlot = Ground(nodes["B"].Point);
            var lakePlot = Ground(nodes["E"].Point);
            PlaceSuntailExterior(forest, forestPlot + new Vector3(-32, 0, 20), forestPlot, group, .88f);
            var lakeInstance = PlaceSuntailExterior(lake, lakePlot + new Vector3(28, 0, 20), lakePlot, group, .88f);
            var canopyReference = lakeInstance.transform.position + lakeInstance.transform.right * 13;
            PlaceSuntailExterior(canopy, canopyReference, lakePlot, group, 1.1f);
            PaintSuntailExteriorGround(root);
            Debug.Log("[TowTrouble] Added exactly three Suntail exterior landmarks: two closed service buildings and one open canopy; no interiors, furniture, lights or behaviours.");
        }

        private static void PaintSuntailExteriorGround(Transform root)
        {
            var group = root.name == "Suntail exterior service landmarks" ? root : root.Find("Suntail exterior service landmarks");
            if (group == null || group.childCount != 3) throw new InvalidOperationException("Expected three existing Suntail exterior landmarks.");
            var map = UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            var roads = CompositionStoryRoads();
            var data = terrain.terrainData;
            var weights = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            Undo.RegisterCompleteObjectUndo(data, "Blend Suntail exterior courtyards into terrain");
            int changed = 0;
            foreach (Transform building in group)
            {
                var bounds = SuntailLocalBounds(building.GetComponentsInChildren<MeshFilter>(true), building);
                float scale = building.lossyScale.x;
                var center = building.TransformPoint(bounds.center); center.y = Ground(center).y;
                var door = building.Find("Exterior doorway");
                var localFront = door == null ? Vector3.forward : Flat(door.localPosition - bounds.center).normalized;
                if (localFront.sqrMagnitude < .01f) localFront = Vector3.forward;
                var front = building.TransformDirection(localFront).normalized;
                var side = Vector3.Cross(Vector3.up, front);
                float depth = (Mathf.Abs(localFront.x) * bounds.extents.x + Mathf.Abs(localFront.z) * bounds.extents.z) * scale + 1;
                float width = (Mathf.Abs(localFront.z) * bounds.extents.x + Mathf.Abs(localFront.x) * bounds.extents.z) * scale + 1;
                var porch = Ground(center + front * depth);
                CompositionClosestRoad(roads, porch, out var roadEdge, out _);
                var path = new List<Vector3> { porch };
                // The doorway can face away from the road. Follow the outside perimeter,
                // rather than painting a line through the closed house.
                if (Vector3.Dot(Flat(roadEdge - center), front) < 0)
                {
                    float sign = Vector3.Dot(Flat(roadEdge - center), side) >= 0 ? 1 : -1;
                    path.Add(Ground(center + front * depth + side * width * sign));
                    path.Add(Ground(center - front * depth + side * width * sign));
                }
                if (Flat(roadEdge - porch).magnitude < 75) path.Add(Ground(roadEdge));
                var area = new Bounds(center, new Vector3(bounds.size.x * scale + 5, 1, bounds.size.z * scale + 5));
                foreach (var p in path) area.Encapsulate(p);
                area.Expand(4);
                int x0 = Mathf.Clamp(Mathf.FloorToInt((area.min.x - terrain.transform.position.x) / data.size.x * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((area.max.x - terrain.transform.position.x) / data.size.x * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
                int z0 = Mathf.Clamp(Mathf.FloorToInt((area.min.z - terrain.transform.position.z) / data.size.z * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
                int z1 = Mathf.Clamp(Mathf.CeilToInt((area.max.z - terrain.transform.position.z) / data.size.z * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    var p = Ground(terrain.transform.position + new Vector3(x / (float)(data.alphamapWidth - 1) * data.size.x, 0, z / (float)(data.alphamapHeight - 1) * data.size.z));
                    if (!SuntailGroundPaintAllowed(p, map, roads)) continue;
                    var local = building.InverseTransformPoint(p) - bounds.center;
                    float dx = Mathf.Max(0, Mathf.Abs(local.x) - bounds.extents.x) * scale;
                    float dz = Mathf.Max(0, Mathf.Abs(local.z) - bounds.extents.z) * scale;
                    float edge = new Vector2(dx, dz).magnitude;
                    float variation = Mathf.PerlinNoise(p.x * .48f + 11, p.z * .48f + 17);
                    float courtyard = (door == null ? .58f : .68f) * (1 - Smooth01(.15f + variation * .35f, 1.5f + variation * .5f, edge));
                    float trackDistance = float.MaxValue;
                    for (int i = 1; i < path.Count; i++)
                        trackDistance = Mathf.Min(trackDistance, Distance(new Vector2(p.x, p.z), new Vector2(path[i - 1].x, path[i - 1].z), new Vector2(path[i].x, path[i].z), out _));
                    float access = .55f * (1 - Smooth01(.35f, 1.0f + variation * .18f, trackDistance));
                    float target = Mathf.Max(courtyard, access) * (.86f + variation * .14f);
                    float old = weights[z, x, 1];
                    // A fixed minimum dirt weight makes the correction safe to run repeatedly.
                    if (target <= old + .0001f || old >= .9999f) continue;
                    float keep = (1 - target) / (1 - old);
                    for (int layer = 0; layer < data.alphamapLayers; layer++) weights[z, x, layer] = layer == 1 ? target : weights[z, x, layer] * keep;
                    changed++;
                }
            }
            if (changed > 0)
            {
                data.SetAlphamaps(0, 0, weights); data.SetBaseMapDirty(); terrain.Flush(); EditorUtility.SetDirty(data);
            }
            Debug.Log("[TowTrouble] Suntail compact courts and foot access: " + changed + " soil texels blended; terrain heights and gameplay surfaces unchanged.");
        }

        private static bool SuntailGroundPaintAllowed(Vector3 p, RecoveryMap map, List<CompositionStoryRoad> roads)
        {
            if (p.y < 12.2f || MountainVegetationSlope(p) > 18) return false;
            if (map.Sites.Any(s => Flat(s.Position - p).magnitude < (s.Id == "R01" ? 39 : 18)) || map.Deliveries.Any(d => Flat(d.Position - p).magnitude < 9.5f)) return false;
            foreach (var road in roads)
            {
                var delta = Flat(road.B - road.A);
                float t = Mathf.Clamp01(Vector3.Dot(Flat(p - road.A), delta) / Mathf.Max(.001f, delta.sqrMagnitude));
                var center = Vector3.Lerp(road.A, road.B, t);
                if (Mathf.Abs(center.y - p.y) > 3) continue;
                float halfWidth = Vector3.Distance(Vector3.Lerp(road.LeftA, road.LeftB, t), Vector3.Lerp(road.RightA, road.RightB, t)) * .5f;
                if (Flat(p - center).sqrMagnitude < (halfWidth + 1.2f) * (halfWidth + 1.2f)) return false;
            }
            return true;
        }

        private static bool SuntailExteriorMesh(MeshFilter filter, Transform sourceRoot)
        {
            if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) return false;
            bool exteriorAccess = false;
            for (var t = filter.transform; t != null && t != sourceRoot; t = t.parent)
            {
                string n = t.name;
                exteriorAccess |= n == "Foundation" || n == "Fountation" || n == "Exterior";
                if (n.IndexOf("Interior", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Indoor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Lighting", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            }
            // Positive source whitelist: only individual exterior construction modules.
            // In particular, no furnished house mesh, indoor floor, hearth, prop or foliage is copied.
            string path = AssetDatabase.GetAssetPath(filter.sharedMesh);
            if (!path.StartsWith(SuntailModules, StringComparison.Ordinal)) return false;
            string relative = path.Substring(SuntailModules.Length);
            if (relative.StartsWith("Indoor Modules/", StringComparison.Ordinal)) return false;
            if (relative.StartsWith("Frame Modules/", StringComparison.Ordinal) ||
                relative.StartsWith("Stone Modules/", StringComparison.Ordinal) ||
                relative.StartsWith("Roofs/", StringComparison.Ordinal) ||
                relative.StartsWith("Doors And Windows/", StringComparison.Ordinal)) return true;
            // House_1 places its internal staircase under Ground Floor/Modules, not Interior.
            // Only the separately authored foundation steps and outside balcony rails are exterior access.
            if (relative.StartsWith("Stairs/", StringComparison.Ordinal)) return exteriorAccess;
            string module = Path.GetFileNameWithoutExtension(path);
            return relative.StartsWith("Decor/", StringComparison.Ordinal) &&
                (module.StartsWith("Support_", StringComparison.Ordinal) || module.StartsWith("Crossbar_", StringComparison.Ordinal) ||
                 module.StartsWith("Cornice_Cover_", StringComparison.Ordinal) || module.StartsWith("Decor_Peaks_", StringComparison.Ordinal) ||
                 module == "Ridge" || module == "Wooden_Canopy_1");
        }

        private static GameObject BuildSuntailExteriorPrefab(string name, string sourcePath, bool closedBuilding)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) throw new InvalidOperationException("Missing Suntail exterior source " + sourcePath);
            var allFilters = source.GetComponentsInChildren<MeshFilter>(true);
            var allowed = allFilters.Where(f => SuntailExteriorMesh(f, source.transform)).ToArray();
            if (allowed.Length == 0) throw new InvalidOperationException("No audited exterior modules in " + sourcePath);
            var near = new HashSet<Renderer>(); var far = new HashSet<Renderer>(); var inLod = new HashSet<Renderer>();
            foreach (var lod in source.GetComponentsInChildren<LODGroup>(true))
            {
                var levels = lod.GetLODs();
                if (levels.Length == 0) continue;
                foreach (var level in levels) foreach (var r in level.renderers) if (r != null) inLod.Add(r);
                foreach (var r in levels[0].renderers) if (r != null) near.Add(r);
                foreach (var r in levels[levels.Length - 1].renderers) if (r != null) far.Add(r);
            }
            foreach (var filter in allowed)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (!inLod.Contains(renderer)) { near.Add(renderer); far.Add(renderer); }
            }

            var preview = EditorSceneManager.NewPreviewScene();
            var readable = new Dictionary<Mesh, Mesh>();
            try
            {
                // Do not instantiate the vendor prefab: its interior objects and scripts never enter the scene.
                var staged = new GameObject(name);
                SceneManager.MoveGameObjectToScene(staged, preview);
                staged.layer = 8;
                var nativeBounds = SuntailLocalBounds(allowed, source.transform);
                var recenter = Matrix4x4.Translate(new Vector3(-nativeBounds.center.x, -nativeBounds.min.y, -nativeBounds.center.z));
                var levels = new List<LOD>();
                for (int level = 0; level < 2; level++)
                {
                    var membership = level == 0 ? near : far;
                    var batches = new Dictionary<Material, List<CombineInstance>>();
                    foreach (var filter in allowed)
                    {
                        var renderer = filter.GetComponent<MeshRenderer>();
                        if (!membership.Contains(renderer)) continue;
                        var mesh = SuntailReadableMesh(filter.sharedMesh, readable);
                        var materials = renderer.sharedMaterials;
                        if (materials.Length != mesh.subMeshCount || materials.Any(m => m == null))
                            throw new InvalidOperationException("Invalid Suntail module materials: " + filter.name);
                        for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        {
                            var material = SuntailExteriorMaterial(materials[sub]);
                            if (!batches.TryGetValue(material, out var batch)) { batch = new List<CombineInstance>(); batches.Add(material, batch); }
                            batch.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = recenter * source.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                        }
                    }
                    var renderers = new List<Renderer>();
                    foreach (var pair in batches.OrderBy(p => p.Key.name, StringComparer.Ordinal))
                    {
                        var mesh = new Mesh { name = name + " LOD" + level + " " + pair.Key.name, indexFormat = IndexFormat.UInt32 };
                        mesh.CombineMeshes(pair.Value.ToArray(), true, true, false);
                        mesh.RecalculateBounds();
                        if (mesh.vertexCount == 0) throw new InvalidOperationException("Empty exterior batch " + mesh.name);
                        var go = new GameObject("LOD" + level + " " + pair.Key.name, typeof(MeshFilter), typeof(MeshRenderer));
                        go.transform.SetParent(staged.transform, false); go.layer = 8; go.isStatic = true;
                        go.GetComponent<MeshFilter>().sharedMesh = VisualAsset(mesh, "LandscapeComposition/SuntailExteriors/Meshes/" + mesh.name + ".asset");
                        var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = pair.Key;
                        renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                        renderers.Add(renderer);
                    }
                    if (renderers.Count > 16) throw new InvalidOperationException("Exterior exceeds 16 material batches per LOD: " + name);
                    levels.Add(new LOD(level == 0 ? .085f : .012f, renderers.ToArray()));
                }
                var lodGroup = staged.AddComponent<LODGroup>(); lodGroup.SetLODs(levels.ToArray()); lodGroup.RecalculateBounds(); lodGroup.fadeMode = LODFadeMode.None;
                if (closedBuilding)
                {
                    var walls = allowed.Where(f => AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/Stone Modules/") || AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/Frame Modules/")).ToArray();
                    var b = SuntailLocalBounds(walls, source.transform);
                    var center = recenter.MultiplyPoint3x4(b.center);
                    var collider = staged.AddComponent<BoxCollider>(); collider.center = center; collider.size = b.size;
                    // Closed external doors are scenery. There is no traversable interior or invisible furniture collision.
                    var doors = allowed.Where(f => Path.GetFileName(AssetDatabase.GetAssetPath(f.sharedMesh)).StartsWith("Wooden_Door", StringComparison.Ordinal)).ToArray();
                    if (doors.Length > 0)
                    {
                        var door = doors.OrderBy(f => source.transform.InverseTransformPoint(f.transform.position).y).First();
                        var marker = new GameObject("Exterior doorway").transform; marker.SetParent(staged.transform, false);
                        marker.localPosition = recenter.MultiplyPoint3x4(source.transform.InverseTransformPoint(door.transform.position));
                    }
                }
                if (staged.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 || staged.GetComponentsInChildren<Light>(true).Length != 0)
                    throw new InvalidOperationException("Unexpected behaviour or light in stripped exterior " + name);
                WriteSuntailExteriorAudit(name, source, allFilters, allowed, staged, levels);
                string path = SuntailExteriorFolder + "/" + name + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(staged, path, out bool saved);
                if (!saved || prefab == null) throw new InvalidOperationException("Could not save stripped exterior " + path);
                Debug.Log("[TowTrouble] " + name + ": " + allowed.Length + "/" + allFilters.Length + " exterior mesh components retained as " + levels.Sum(l => l.renderers.Length) + " LOD batches; no original hierarchy copied.");
                return prefab;
            }
            finally
            {
                foreach (var mesh in readable.Values) UnityEngine.Object.DestroyImmediate(mesh);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void WriteSuntailExteriorAudit(string name, GameObject source, MeshFilter[] all, MeshFilter[] allowed, GameObject generated, List<LOD> lods)
        {
            var retained = new HashSet<MeshFilter>(allowed);
            var modules = all.Select(filter =>
            {
                var names = new List<string>();
                for (var t = filter.transform; t != null && t != source.transform; t = t.parent) names.Add(t.name);
                names.Reverse();
                string hierarchy = string.Join("/", names);
                string model = filter.sharedMesh == null ? "" : AssetDatabase.GetAssetPath(filter.sharedMesh);
                bool keep = retained.Contains(filter);
                bool interiorHierarchy = names.Any(n => n.IndexOf("Interior", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Indoor", StringComparison.OrdinalIgnoreCase) >= 0);
                bool internalStair = model.Contains("/Stairs/") && !names.Any(n => n == "Foundation" || n == "Fountation" || n == "Exterior");
                var moduleBounds = filter.sharedMesh == null ? new Bounds() : SuntailLocalBounds(new[] { filter }, source.transform);
                bool groundWall = hierarchy.StartsWith("Ground Floor/Modules/", StringComparison.Ordinal) &&
                    Path.GetFileName(model).StartsWith("Stone_Wall", StringComparison.Ordinal);
                // Audited from native module centers, not merely from their names. House_6 has
                // an L-shaped outside corner at x=1 / z=1; those two facade faces are retained.
                var c = moduleBounds.center; var size = moduleBounds.size;
                bool onPerimeter = !groundWall || (source.name == "House_1"
                    ? size.x < .4f && Mathf.Abs(Mathf.Abs(c.x) - 3) < .04f || size.z < .4f && Mathf.Abs(Mathf.Abs(c.z) - 2) < .04f
                    : source.name == "House_6" && (size.x < .4f && (Mathf.Abs(Mathf.Abs(c.x) - 3) < .04f || Mathf.Abs(c.x - 1) < .04f && c.z >= 1 && c.z <= 3.2f) ||
                        size.z < .4f && (Mathf.Abs(Mathf.Abs(c.z) - 3) < .04f || Mathf.Abs(c.z - 1) < .04f && c.x >= 1 && c.x <= 3.2f)));
                string reason = keep ? "Audited exterior construction module" : interiorHierarchy ? "Interior hierarchy removed" :
                    model.Contains("/Indoor Modules/") ? "Indoor floor, wall or hearth removed" : internalStair ? "Internal stairs/platform under Modules removed" :
                    model.StartsWith(SuntailModules, StringComparison.Ordinal) ? "Optional exterior decoration omitted" : "Furniture, prop, effect or other non-structural model omitted";
                if (keep && (interiorHierarchy || internalStair || model.Contains("/Indoor Modules/") || !model.StartsWith(SuntailModules, StringComparison.Ordinal)))
                    throw new InvalidOperationException("Interior geometry entered exterior mesh batch: " + hierarchy);
                if (keep && !onPerimeter) throw new InvalidOperationException("Ground floor wall is not on the audited outside perimeter: " + hierarchy);
                return new { hierarchy, sourceModel = model, sourceMesh = filter.sharedMesh == null ? null : filter.sharedMesh.name, retained = keep, reason,
                    localBoundsCenter = new[] { c.x, c.y, c.z }, localBoundsSize = new[] { size.x, size.y, size.z }, groundWall, onAuditedOutsidePerimeter = onPerimeter,
                    sourceVertices = filter.sharedMesh == null ? 0 : filter.sharedMesh.vertexCount,
                    sourceMaterials = filter.GetComponent<MeshRenderer>() == null ? Array.Empty<string>() : filter.GetComponent<MeshRenderer>().sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray() };
            }).ToArray();
            var levels = lods.Select((lod, index) => new
            {
                level = index,
                renderers = lod.renderers.Length,
                materials = lod.renderers.SelectMany(r => r.sharedMaterials).Distinct().Select(AssetDatabase.GetAssetPath).ToArray(),
                vertices = lod.renderers.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.vertexCount),
                triangles = lod.renderers.Sum(r => Enumerable.Range(0, r.GetComponent<MeshFilter>().sharedMesh.subMeshCount).Sum(i => (long)r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(i) / 3))
            }).ToArray();
            suntailExteriorAudits.Add(new
            {
                prefab = name, sourcePrefab = AssetDatabase.GetAssetPath(source),
                sourceMeshComponents = all.Length, retainedExteriorMeshComponents = allowed.Length, rejectedMeshComponents = all.Length - allowed.Length,
                internalStairMeshesRejected = modules.Count(m => !m.retained && m.reason == "Internal stairs/platform under Modules removed"),
                originalLightsNotCopied = source.GetComponentsInChildren<Light>(true).Length,
                originalBehavioursNotCopied = source.GetComponentsInChildren<MonoBehaviour>(true).Length,
                generatedLights = generated.GetComponentsInChildren<Light>(true).Length,
                generatedBehaviours = generated.GetComponentsInChildren<MonoBehaviour>(true).Length,
                generatedColliders = generated.GetComponentsInChildren<Collider>(true).Length,
                sourceHierarchyInstantiated = false, levels, modules
            });
            const string folder = "/private/tmp/TowSuntail";
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder + "/ExteriorAudit.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { complete = suntailExteriorAudits.Count == 3, prefabs = suntailExteriorAudits }, Newtonsoft.Json.Formatting.Indented));
        }

        private static Bounds SuntailLocalBounds(IEnumerable<MeshFilter> filters, Transform root)
        {
            bool first = true; var bounds = new Bounds();
            foreach (var filter in filters)
            {
                var b = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var p = root.InverseTransformPoint(filter.transform.TransformPoint(b.center + Vector3.Scale(b.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1))));
                    if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p);
                }
            }
            if (first) throw new InvalidOperationException("Exterior bounds require at least one mesh.");
            return bounds;
        }

        private static Mesh SuntailReadableMesh(Mesh source, Dictionary<Mesh, Mesh> cache)
        {
            if (cache.TryGetValue(source, out var result)) return result;
            result = new Mesh { name = source.name + " exterior authoring copy", indexFormat = source.indexFormat };
            using (var data = MeshUtility.AcquireReadOnlyMeshData(source))
            {
                var mesh = data[0]; int count = mesh.vertexCount;
                using (var buffer = new NativeArray<Vector3>(count, Allocator.Temp))
                {
                    mesh.GetVertices(buffer); result.SetVertices(buffer);
                    if (mesh.HasVertexAttribute(VertexAttribute.Normal)) { mesh.GetNormals(buffer); result.SetNormals(buffer); }
                }
                using (var buffer = new NativeArray<Vector2>(count, Allocator.Temp))
                    if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0)) { mesh.GetUVs(0, buffer); result.SetUVs(0, buffer); }
                using (var buffer = new NativeArray<Vector4>(count, Allocator.Temp))
                    if (mesh.HasVertexAttribute(VertexAttribute.Tangent)) { mesh.GetTangents(buffer); result.SetTangents(buffer); }
                result.subMeshCount = mesh.subMeshCount;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var descriptor = mesh.GetSubMesh(sub);
                    using (var indices = new NativeArray<int>(descriptor.indexCount, Allocator.Temp))
                    {
                        mesh.GetIndices(indices, sub, true);
                        result.SetIndices(indices, descriptor.topology, sub, false);
                    }
                }
            }
            result.RecalculateBounds(); cache.Add(source, result); return result;
        }

        private static Material SuntailExteriorMaterial(Material source)
        {
            if (suntailExteriorMaterials.TryGetValue(source, out var cached)) return cached;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            string id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string name = source.name + " " + id.Substring(0, Math.Min(8, id.Length));
            var material = new Material(shader) { name = name, enableInstancing = true };
            var color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white; color.a = 1;
            material.SetColor("_BaseColor", color);
            if (source.HasProperty("_Albedo")) material.SetTexture("_BaseMap", source.GetTexture("_Albedo"));
            float tiling = source.HasProperty("_Tiling") ? source.GetFloat("_Tiling") : 1;
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            if (source.HasProperty("_Normal") && source.GetTexture("_Normal") != null)
            {
                material.SetTexture("_BumpMap", source.GetTexture("_Normal")); material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_BumpScale", source.HasProperty("_NormalScale") ? source.GetFloat("_NormalScale") : 1);
            }
            material.SetFloat("_Smoothness", source.HasProperty("_SurfaceSmoothness") ? Mathf.Min(.45f, source.GetFloat("_SurfaceSmoothness")) : .15f);
            if (source.HasProperty("_SmoothnessSource") && source.GetFloat("_SmoothnessSource") > .5f)
            {
                material.SetFloat("_SmoothnessTextureChannel", 1);
                material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            }
            material.SetFloat("_Metallic", source.HasProperty("_Metallic") ? source.GetFloat("_Metallic") : 0);
            if (source.HasProperty("_MetallicSmoothness") && source.GetTexture("_MetallicSmoothness") != null)
            {
                material.SetTexture("_MetallicGlossMap", source.GetTexture("_MetallicSmoothness")); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            material.SetColor("_EmissionColor", Color.black); material.DisableKeyword("_EMISSION");
            // Exterior glass stays opaque: there is deliberately no furnished room behind it.
            material.SetFloat("_Surface", 0); material.SetFloat("_Cull", 2); material.SetFloat("_ZWrite", 1);
            cached = VisualAsset(material, "LandscapeComposition/SuntailExteriors/Materials/" + name + ".mat");
            suntailExteriorMaterials.Add(source, cached); return cached;
        }

        private static GameObject PlaceSuntailExterior(GameObject prefab, Vector3 reference, Vector3 destination, Transform parent, float scale)
        {
            var bounds = SuntailLocalBounds(prefab.GetComponentsInChildren<MeshFilter>(true), prefab.transform);
            float radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude * scale + 1.8f;
            Vector3 chosen = default; Quaternion chosenRotation = Quaternion.identity; float best = float.MaxValue;
            var door = prefab.transform.Find("Exterior doorway");
            var front = door == null ? Vector3.forward : Flat(door.localPosition - bounds.center).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            for (int z = -48; z <= 48; z += 4) for (int x = -48; x <= 48; x += 4)
            {
                var p = Ground(reference + new Vector3(x, 0, z));
                if (p.y < 13 || !CompositionClear(p, radius) || MountainVegetationSlope(p) > 8) continue;
                var facing = Flat(destination - p); if (facing.sqrMagnitude < .1f) continue;
                var rotation = Quaternion.LookRotation(facing) * Quaternion.Inverse(Quaternion.LookRotation(front));
                float low = p.y, high = p.y;
                foreach (var corner in new[] { new Vector3(-bounds.extents.x, 0, -bounds.extents.z), new Vector3(bounds.extents.x, 0, -bounds.extents.z), new Vector3(-bounds.extents.x, 0, bounds.extents.z), new Vector3(bounds.extents.x, 0, bounds.extents.z) })
                { float h = Ground(p + rotation * corner * scale).y; low = Mathf.Min(low, h); high = Mathf.Max(high, h); }
                if (high - low > .55f) continue;
                var volumeCenter = p + rotation * (bounds.center * scale);
                var obstacles = Physics.OverlapBox(volumeCenter, bounds.extents * scale + new Vector3(1, 0, 1), rotation, ~0, QueryTriggerInteraction.Ignore);
                if (obstacles.Any(c => !(c is TerrainCollider) && c.enabled && c.gameObject.activeInHierarchy)) continue;
                float cost = Flat(p - reference).sqrMagnitude + (high - low) * 80;
                if (cost >= best) continue;
                best = cost; chosen = p; chosen.y = low - .10f; chosenRotation = rotation;
            }
            if (best == float.MaxValue) throw new InvalidOperationException("No clear, level footprint for Suntail exterior " + prefab.name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(chosen, chosenRotation); instance.transform.localScale = Vector3.one * scale;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = 8; t.gameObject.isStatic = true; }
            Physics.SyncTransforms();
            return instance;
        }
    }
}
