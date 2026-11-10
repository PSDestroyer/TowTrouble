using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    /// <summary>Session-owned Switch rendering budget. Physics and gameplay objects stay active.</summary>
    [DefaultExecutionOrder(500)]
    public sealed class RecoverySwitchSceneRendering : MonoBehaviour
    {
        private sealed class Item
        {
            public Renderer[] renderers;
            public bool[] originalHidden;
            public LODGroup lodGroup;
            public Bounds bounds;
            public int distanceBand;
            public bool distant, occluded, batched, originalLodEnabled;
            public LOD[] lods;
            public RockPart[][] parts;
            public Vector3 referencePoint;
            public float worldSize;
            public int cell;
        }
        private sealed class Cell
        {
            public Bounds bounds;
            public readonly List<int> items = new List<int>();
            public bool occluded;
            public Vector3 checkedFrom;
        }
        private sealed class Batch
        {
            public Mesh mesh;
            public int submesh;
            public RenderParams parameters;
            public Matrix4x4[] matrices;
            public int count, capacity;
            public Bounds bounds;
        }
        private struct RockPart { public int batch; public Matrix4x4 matrix; public Bounds bounds; }
        private Camera lens;
        private CullingGroup culling;
        private readonly List<Item> items = new List<Item>();
        private readonly List<Cell> cells = new List<Cell>();
        private readonly List<Batch> batches = new List<Batch>();
        private readonly Dictionary<(int, int), int> cellLookup = new Dictionary<(int, int), int>();
        private readonly Dictionary<(int, Mesh, Material, int, int, uint, ShadowCastingMode, bool, LightProbeUsage, ReflectionProbeUsage), int> batchLookup
            = new Dictionary<(int, Mesh, Material, int, int, uint, ShadowCastingMode, bool, LightProbeUsage, ReflectionProbeUsage), int>();
        private TerrainCollider[] terrainColliders;
        private readonly Vector3[] occlusionPoints = new Vector3[9];
        private readonly Plane[] planes = new Plane[6];
        private int nextCell;
        private bool initialized;
        public int ManagedGroups => items.Count;
        public int InstancedGroups { get; private set; }
        public int ManagedRenderers { get; private set; }
        public int LastHiddenGroups { get; private set; }
        public int LastInstanceDraws { get; private set; }
        public int LastVisibleInstances { get; private set; }

        public void Initialize(Camera camera)
        {
            if (initialized || !RecoveryPlatform.UsesSwitchRendering || camera == null) return;
            initialized = true;
            lens = camera;
            var catalog = Resources.Load<RecoverySwitchInstancingCatalog>("SwitchRockInstancing");
            var grouped = new Dictionary<UnityEngine.Object, List<Renderer>>();
            foreach (var root in camera.gameObject.scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.gameObject.scene != camera.gameObject.scene || !renderer.enabled ||
                    renderer.GetComponentInParent<Rigidbody>() != null || renderer.GetComponentInParent<RecoveryVehicle>() != null)
                    continue;
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0 || !IsEnvironment(materials)) continue;
                var lod = renderer.GetComponentInParent<LODGroup>();
                UnityEngine.Object key = lod != null ? (UnityEngine.Object)lod : renderer;
                if (!grouped.TryGetValue(key, out var group)) grouped.Add(key, group = new List<Renderer>());
                group.Add(renderer);
            }
            foreach (var pair in grouped)
            {
                var renderers = pair.Value.ToArray();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                // Vertex wind and LOD silhouettes must fit the culling volume.
                bounds.Expand(6);
                var item = new Item { renderers = renderers, originalHidden = new bool[renderers.Length],
                    bounds = bounds, lodGroup = pair.Key as LODGroup, distanceBand = DistanceBand(renderers) };
                for (int i = 0; i < renderers.Length; i++) item.originalHidden[i] = renderers[i].forceRenderingOff;
                item.cell = GetCell(bounds);
                cells[item.cell].items.Add(items.Count);
                items.Add(item);
                ManagedRenderers += renderers.Length;
                if (SystemInfo.supportsInstancing && catalog != null && TryBatchRock(item, catalog))
                {
                    item.batched = true;
                    item.originalLodEnabled = item.lodGroup.enabled;
                    item.lodGroup.enabled = false;
                    InstancedGroups++;
                }
            }
            foreach (var batch in batches) batch.matrices = new Matrix4x4[batch.capacity];
            var spheres = new BoundingSphere[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                spheres[i] = new BoundingSphere(item.bounds.center, item.bounds.extents.magnitude);
                float edgeDistance = Mathf.Max(0, Vector3.Distance(camera.transform.position, spheres[i].position) - spheres[i].radius);
                item.distant = edgeDistance >= Ranges[item.distanceBand];
                ApplyVisibility(item);
            }
            culling = new CullingGroup { targetCamera = lens };
            culling.SetBoundingSpheres(spheres);
            culling.SetBoundingSphereCount(spheres.Length);
            culling.SetBoundingDistances(Ranges);
            culling.SetDistanceReferencePoint(lens.transform);
            culling.onStateChanged = OnCull;
            var terrains = new List<TerrainCollider>();
            foreach (var terrain in Terrain.activeTerrains)
                if (terrain.gameObject.scene == camera.gameObject.scene && terrain.drawHeightmap &&
                    terrain.TryGetComponent<TerrainCollider>(out var collider) && collider.enabled)
                    terrains.Add(collider);
            terrainColliders = terrains.ToArray();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Tow Trouble Switch scene] groups={ManagedGroups} renderers={ManagedRenderers} instancedRockGroups={InstancedGroups} gpuInstancing={SystemInfo.supportsInstancing}");
#endif
        }

        private static readonly float[] Ranges = { RecoverySwitchGraphics.MeshGrassDistance, RecoverySwitchGraphics.BushDistance,
            RecoverySwitchGraphics.RockDistance, RecoverySwitchGraphics.TreeDistance };
        private static bool IsEnvironment(Material[] materials)
        {
            foreach (var material in materials)
            {
                if (material == null || material.shader == null) return false;
                string name = material.shader.name;
                if (name != "Shader Graphs/S_Rock" && name != "Shader Graphs/S_Bark" &&
                    name != "Shader Graphs/S_Leaves" && name != "Shader Graphs/S_FoliageShader") return false;
            }
            return true;
        }
        private static int DistanceBand(Renderer[] renderers)
        {
            foreach (var renderer in renderers)
                foreach (var material in renderer.sharedMaterials)
                {
                    string name = material.shader.name;
                    if (name == "Shader Graphs/S_Bark") return 3;
                    if (name == "Shader Graphs/S_Leaves") return 1;
                }
            return renderers[0].sharedMaterial.shader.name == "Shader Graphs/S_Rock" ? 2 : 0;
        }
        private int GetCell(Bounds bounds)
        {
            var key = (Mathf.FloorToInt(bounds.center.x / 48), Mathf.FloorToInt(bounds.center.z / 48));
            if (cellLookup.TryGetValue(key, out int index)) { cells[index].bounds.Encapsulate(bounds); return index; }
            index = cells.Count;
            cells.Add(new Cell { bounds = bounds });
            cellLookup.Add(key, index);
            return index;
        }
        private void OnCull(CullingGroupEvent evt)
        {
            var item = items[evt.index];
            item.distant = evt.currentDistance > item.distanceBand;
            ApplyVisibility(item);
        }
        private static void ApplyVisibility(Item item)
        {
            for (int i = 0; i < item.renderers.Length; i++)
                if (item.renderers[i] != null)
                    item.renderers[i].forceRenderingOff = item.originalHidden[i] || item.batched || item.distant || item.occluded;
        }

        // Only fully supported static rock LODs are replaced. Other objects retain native rendering.
        private bool TryBatchRock(Item item, RecoverySwitchInstancingCatalog catalog)
        {
            if (item.lodGroup == null || !item.lodGroup.enabled || item.bounds.size.magnitude > 40) return false;
            var lods = item.lodGroup.GetLODs();
            if (lods.Length == 0) return false;
            foreach (var lod in lods)
                foreach (var renderer in lod.renderers)
                {
                    if (renderer == null || !renderer.enabled || renderer.isPartOfStaticBatch || renderer.HasPropertyBlock() || renderer.forceRenderingOff ||
                        renderer.lightmapIndex >= 0 || renderer.realtimeLightmapIndex >= 0 ||
                        !(renderer is MeshRenderer) || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
                        return false;
                    var materials = renderer.sharedMaterials;
                    if (materials.Length != filter.sharedMesh.subMeshCount) return false;
                    foreach (var material in materials)
                        if (material == null || material.shader.name != "Shader Graphs/S_Rock" || catalog.Find(material) == null) return false;
                }
            item.lods = lods;
            item.parts = new RockPart[lods.Length][];
            item.referencePoint = item.lodGroup.transform.TransformPoint(item.lodGroup.localReferencePoint);
            var scale = item.lodGroup.transform.lossyScale;
            item.worldSize = item.lodGroup.size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            for (int l = 0; l < lods.Length; l++)
            {
                var parts = new List<RockPart>();
                foreach (var renderer in lods[l].renderers)
                {
                    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var materials = renderer.sharedMaterials;
                    for (int s = 0; s < materials.Length; s++)
                    {
                        var material = catalog.Find(materials[s]);
                        var key = (item.cell, mesh, material, s, renderer.gameObject.layer, renderer.renderingLayerMask,
                            renderer.shadowCastingMode, renderer.receiveShadows, renderer.lightProbeUsage, renderer.reflectionProbeUsage);
                        if (!batchLookup.TryGetValue(key, out int b))
                        {
                            b = batches.Count; batchLookup.Add(key, b);
                            batches.Add(new Batch { mesh = mesh, submesh = s, parameters = new RenderParams(material) {
                                camera = lens, layer = renderer.gameObject.layer, renderingLayerMask = renderer.renderingLayerMask,
                                shadowCastingMode = renderer.shadowCastingMode, receiveShadows = renderer.receiveShadows,
                                lightProbeUsage = renderer.lightProbeUsage, reflectionProbeUsage = renderer.reflectionProbeUsage,
                                motionVectorMode = MotionVectorGenerationMode.Camera } });
                        }
                        batches[b].capacity++;
                        parts.Add(new RockPart { batch = b, matrix = renderer.localToWorldMatrix, bounds = renderer.bounds });
                    }
                }
                item.parts[l] = parts.ToArray();
            }
            return true;
        }
        public static int SelectLod(LOD[] lods, float relativeHeight)
        {
            for (int i = 0; i < lods.Length; i++) if (relativeHeight >= lods[i].screenRelativeTransitionHeight) return i;
            return -1;
        }
        private void LateUpdate()
        {
            if (!initialized || lens == null) return;
            UpdateTerrainOcclusion();
            GeometryUtility.CalculateFrustumPlanes(lens, planes);
            LastHiddenGroups = LastInstanceDraws = LastVisibleInstances = 0;
            foreach (var batch in batches) batch.count = 0;
            float projection = lens.orthographic ? 2 * lens.orthographicSize : 2 * Mathf.Tan(lens.fieldOfView * Mathf.Deg2Rad * .5f);
            Vector3 eye = lens.transform.position;
            float lodBias = QualitySettings.lodBias;
            foreach (var item in items)
            {
                if (item.distant || item.occluded) { LastHiddenGroups++; continue; }
                if (!item.batched || item.lodGroup == null || !item.lodGroup.gameObject.activeInHierarchy) continue;
                // Keep offscreen shadow casters close to the camera.
                if (item.bounds.SqrDistance(eye) > 1600 && !GeometryUtility.TestPlanesAABB(planes, item.bounds)) continue;
                float distance = lens.orthographic ? 1 : Mathf.Max(.01f, Vector3.Distance(eye, item.referencePoint));
                int lod = SelectLod(item.lods, item.worldSize * lodBias / (projection * distance));
                if (lod < 0) continue;
                foreach (var part in item.parts[lod])
                {
                    var batch = batches[part.batch];
                    if (batch.count == 0) batch.bounds = part.bounds; else batch.bounds.Encapsulate(part.bounds);
                    batch.matrices[batch.count++] = part.matrix;
                    LastVisibleInstances++;
                }
            }
            foreach (var batch in batches)
            {
                batch.parameters.worldBounds = batch.bounds;
                // Nonuniform scales need both object/world matrices; keep below platform instance limits.
                for (int start = 0; start < batch.count; start += 128)
                {
                    Graphics.RenderMeshInstanced(batch.parameters, batch.mesh, batch.submesh, batch.matrices,
                        Mathf.Min(128, batch.count - start), start);
                    LastInstanceDraws++;
                }
            }
        }
        private void UpdateTerrainOcclusion()
        {
            if (terrainColliders == null || terrainColliders.Length == 0 || cells.Count == 0) return;
            Vector3 eye = lens.transform.position;
            // Release old occlusion before evaluating new viewpoints. Rotation alone does not change line of sight.
            foreach (var cell in cells)
                if (cell.occluded && (eye - cell.checkedFrom).sqrMagnitude > 1) SetOccluded(cell, false);
            // Amortized, bounded work: at most eighteen terrain rays per frame per terrain.
            for (int i = 0; i < 2; i++)
            {
                var cell = cells[nextCell++ % cells.Count];
                cell.checkedFrom = eye;
                SetOccluded(cell, cell.bounds.SqrDistance(eye) > 1600 && IsTerrainOccluded(cell.bounds, eye));
            }
        }
        private bool IsTerrainOccluded(Bounds bounds, Vector3 eye)
        {
            Vector3 min = bounds.min, max = bounds.max;
            for (int i = 0; i < 8; i++) occlusionPoints[i] = new Vector3((i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
            occlusionPoints[8] = bounds.center;
            float limit = Mathf.Sqrt(bounds.SqrDistance(eye)) - 8;
            if (limit <= 0) return false;
            foreach (var point in occlusionPoints)
            {
                var ray = new Ray(eye, point - eye);
                bool blocked = false;
                foreach (var terrain in terrainColliders)
                    if (terrain != null && terrain.enabled && terrain.Raycast(ray, out _, limit)) { blocked = true; break; }
                if (!blocked) return false;
            }
            return true;
        }
        private void SetOccluded(Cell cell, bool hidden)
        {
            if (cell.occluded == hidden) return;
            cell.occluded = hidden;
            foreach (int index in cell.items) { items[index].occluded = hidden; ApplyVisibility(items[index]); }
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        public void Release()
        {
            culling?.Dispose(); culling = null;
            foreach (var item in items)
            {
                if (item.batched && item.lodGroup != null) item.lodGroup.enabled = item.originalLodEnabled;
                for (int i = 0; i < item.renderers.Length; i++)
                    if (item.renderers[i] != null) item.renderers[i].forceRenderingOff = item.originalHidden[i];
            }
            items.Clear(); cells.Clear(); batches.Clear(); cellLookup.Clear(); batchLookup.Clear();
            terrainColliders = null;
            ManagedRenderers = InstancedGroups = 0;
            initialized = false;
        }
    }
}
