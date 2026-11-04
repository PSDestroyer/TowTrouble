using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    // Painted terrain trees have no per-instance Renderer. Keep their original terrain
    // and tree colliders intact; switch tree drawing to private data only on first hide.
    internal sealed class RecoveryTerrainTrees
    {
        private const float CellSize = 32f;
        private readonly Terrain source;
        private readonly TerrainData originalData;
        private readonly TreeInstance[] originals;
        private readonly TreePrototype[] prototypes;
        private readonly Dictionary<Vector2Int, List<Tree>> cells = new Dictionary<Vector2Int, List<Tree>>();
        private readonly List<Tree> hidden = new List<Tree>();
        private Vector2 padding;
        private TerrainData visualData;
        private GameObject visuals;
        private float originalDistance;
        private bool ownsDistance;

        private sealed class Tree
        {
            internal int Index;
            internal Bounds Bounds;
            internal RecoveryOccludingTree.Shape Shape;
            internal float RestoreAt;
            internal bool WantHidden;
            internal float FadeProgress;
            internal GameObject StandIn;
            internal RecoveryTreeFade Fade;
        }

        private RecoveryTerrainTrees(Terrain terrain)
        {
            source = terrain;
            originalData = terrain.terrainData;
            originals = originalData.treeInstances;
            prototypes = originalData.treePrototypes;
        }

        internal static RecoveryTerrainTrees TryCreate(Terrain terrain)
        {
            if (terrain == null || !terrain.isActiveAndEnabled || !terrain.drawTreesAndFoliage ||
                terrain.treeDistance <= 0f || terrain.terrainData == null ||
                terrain.terrainData.treeInstanceCount == 0 || terrain.gameObject.hideFlags == HideFlags.HideAndDontSave)
                return null;

            var owner = new RecoveryTerrainTrees(terrain);
            owner.IndexTrees();
            return owner.cells.Count > 0 ? owner : null;
        }

        private void IndexTrees()
        {
            var localBounds = new Bounds[prototypes.Length];
            for (int i = 0; i < prototypes.Length; i++)
            {
                var prefab = prototypes[i].prefab;
                if (prefab != null && RecoveryOccludingTree.IsStandingTree(prefab.name))
                    localBounds[i] = PrototypeBounds(prefab);
            }

            var origin = source.transform.position;
            for (int i = 0; i < originals.Length; i++)
            {
                TreeInstance instance = originals[i];
                int prototype = instance.prototypeIndex;
                if (prototype < 0 || prototype >= prototypes.Length || localBounds[prototype].size.y <= 0f ||
                    instance.widthScale <= 0f || instance.heightScale <= 0f) continue;
                var basePoint = origin + Vector3.Scale(originalData.size, instance.position);
                var scale = new Vector3(instance.widthScale, instance.heightScale, instance.widthScale);
                var pose = Matrix4x4.TRS(basePoint, Quaternion.Euler(0, instance.rotation * Mathf.Rad2Deg, 0), scale);
                var bounds = TransformBounds(localBounds[prototype], pose);
                var entry = new Tree { Index = i, Bounds = bounds,
                    Shape = new RecoveryOccludingTree.Shape(prototypes[prototype].prefab.name, bounds, basePoint) };
                padding.x = Mathf.Max(padding.x, bounds.extents.x);
                padding.y = Mathf.Max(padding.y, bounds.extents.z);
                var cell = Cell(bounds.center.x, bounds.center.z);
                if (!cells.TryGetValue(cell, out var entries))
                    cells.Add(cell, entries = new List<Tree>());
                entries.Add(entry);
            }
        }

        private static Bounds PrototypeBounds(GameObject prefab)
        {
            Bounds result = default;
            bool first = true;
            var root = prefab.transform;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                // Terrain instantiates from the asset origin with its authored rotation and scale.
                var toPrototype = Matrix4x4.Translate(-root.position) * filter.transform.localToWorldMatrix;
                var b = TransformBounds(filter.sharedMesh.bounds, toPrototype);
                if (first) result = b;
                else result.Encapsulate(b);
                first = false;
            }
            return result;
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 e = bounds.extents;
            Vector3 extents = new Vector3(
                Mathf.Abs(matrix.m00) * e.x + Mathf.Abs(matrix.m01) * e.y + Mathf.Abs(matrix.m02) * e.z,
                Mathf.Abs(matrix.m10) * e.x + Mathf.Abs(matrix.m11) * e.y + Mathf.Abs(matrix.m12) * e.z,
                Mathf.Abs(matrix.m20) * e.x + Mathf.Abs(matrix.m21) * e.y + Mathf.Abs(matrix.m22) * e.z);
            return new Bounds(matrix.MultiplyPoint3x4(bounds.center), extents * 2f);
        }

        private static Vector2Int Cell(float x, float z) =>
            new Vector2Int(Mathf.FloorToInt(x / CellSize), Mathf.FloorToInt(z / CellSize));

        internal void UpdateVisibility(Vector3 eye, Vector3[] sightPoints, int count, Bounds sightBounds, float now, float restoreDelay)
        {
            if (source == null || !source.isActiveAndEnabled || !source.drawTreesAndFoliage || source.terrainData != originalData)
            {
                ReleaseVisuals();
                return;
            }

            if (count > 0)
            {
                Vector2Int min = Cell(sightBounds.min.x - padding.x, sightBounds.min.z - padding.y);
                Vector2Int max = Cell(sightBounds.max.x + padding.x, sightBounds.max.z + padding.y);
                for (int x = min.x; x <= max.x; x++)
                    for (int z = min.y; z <= max.y; z++)
                        if (cells.TryGetValue(new Vector2Int(x, z), out var trees))
                            foreach (var tree in trees)
                            {
                                if (!tree.Bounds.Intersects(sightBounds)) continue;
                                for (int p = 0; p < count; p++)
                                {
                                    if (!tree.Shape.Blocks(eye, sightPoints[p])) continue;
                                    tree.RestoreAt = now + restoreDelay;
                                    if (tree.StandIn == null) BeginFade(tree);
                                    tree.WantHidden = true;
                                    break;
                                }
                            }
            }
            foreach (var tree in hidden)
                if (now >= tree.RestoreAt) tree.WantHidden = false;
        }

        internal void AdvanceFades(float dt)
        {
            if (hidden.Count > 0 && (source == null || visualData == null || visuals == null))
            {
                ReleaseVisuals();
                return;
            }
            for (int i = hidden.Count - 1; i >= 0; i--)
            {
                var tree = hidden[i];
                float next = RecoveryTreeFade.Advance(tree.FadeProgress, tree.WantHidden, dt);
                if (next != tree.FadeProgress)
                {
                    tree.FadeProgress = next;
                    tree.Fade.Apply(next);
                }
                if (tree.WantHidden || next > 0f) continue;
                // Switch back only after the full-size stand-in is completely opaque.
                visualData.SetTreeInstance(tree.Index, originals[tree.Index]);
                EndFade(tree);
                hidden.RemoveAt(i);
            }
        }

        private void BeginFade(Tree tree)
        {
            if (visualData == null)
            {
                if (source.treeDistance <= 0f) return;
                CreateVisuals();
            }
            var instance = originals[tree.Index];
            var prefab = prototypes[instance.prototypeIndex].prefab;
            var standIn = new GameObject(prefab.name + " (camera fade)") { hideFlags = HideFlags.HideAndDontSave };
            standIn.SetActive(false);
            try
            {
                standIn.transform.SetParent(visuals.transform, false);
                standIn.transform.position = source.transform.position + Vector3.Scale(originalData.size, instance.position);
                standIn.transform.rotation = Quaternion.Euler(0, instance.rotation * Mathf.Rad2Deg, 0) * prefab.transform.rotation;
                standIn.transform.localScale = Vector3.Scale(prefab.transform.localScale,
                    new Vector3(instance.widthScale, instance.heightScale, instance.widthScale));

                // Only duplicate rendering components, keeping each authored submesh, LOD and its transforms.
                var copies = new Dictionary<Renderer, Renderer>();
                CopyTreeGeometry(prefab.transform, standIn.transform, copies, instance.color);
                var renderers = new Renderer[copies.Count];
                copies.Values.CopyTo(renderers, 0);
                tree.Fade = new RecoveryTreeFade(renderers);
                tree.FadeProgress = 0f;
                var hiddenInstance = instance;
                hiddenInstance.widthScale = hiddenInstance.heightScale = .00001f;
                visualData.SetTreeInstance(tree.Index, hiddenInstance);
                tree.StandIn = standIn;
                standIn.SetActive(true);
                hidden.Add(tree);
            }
            catch
            {
                if (tree.Fade != null) tree.Fade.Restore();
                visualData.SetTreeInstance(tree.Index, instance);
                standIn.SetActive(false);
                DestroyOwned(standIn);
                tree.StandIn = null;
                tree.Fade = null;
                throw;
            }
        }

        private void CopyTreeGeometry(Transform prototype, Transform copy, Dictionary<Renderer, Renderer> copies, Color32 tint)
        {
            copy.gameObject.layer = source.preserveTreePrototypeLayers ? prototype.gameObject.layer : source.gameObject.layer;
            Renderer original = null;
            Renderer renderer = null;
            if (prototype.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null &&
                prototype.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                original = meshRenderer;
                renderer = copy.gameObject.AddComponent<MeshRenderer>();
            }
            else if (prototype.TryGetComponent<BillboardRenderer>(out var billboard))
            {
                var billboardCopy = copy.gameObject.AddComponent<BillboardRenderer>();
                billboardCopy.billboard = billboard.billboard;
                original = billboard;
                renderer = billboardCopy;
            }
            if (renderer != null)
            {
                renderer.sharedMaterials = original.sharedMaterials;
                renderer.enabled = original.enabled;
                renderer.forceRenderingOff = original.forceRenderingOff;
                renderer.shadowCastingMode = source.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off
                    ? UnityEngine.Rendering.ShadowCastingMode.Off : original.shadowCastingMode;
                renderer.receiveShadows = original.receiveShadows;
                renderer.lightProbeUsage = original.lightProbeUsage;
                renderer.reflectionProbeUsage = source.reflectionProbeUsage;
                renderer.renderingLayerMask = source.renderingLayerMask;
                var properties = new MaterialPropertyBlock();
                original.GetPropertyBlock(properties);
                properties.SetColor("_TreeInstanceColor", tint);
                renderer.SetPropertyBlock(properties);
                copies.Add(original, renderer);
            }

            foreach (Transform child in prototype)
            {
                var branch = new GameObject(child.name) { hideFlags = HideFlags.HideAndDontSave };
                branch.transform.SetParent(copy, false);
                branch.transform.localPosition = child.localPosition;
                branch.transform.localRotation = child.localRotation;
                branch.transform.localScale = child.localScale;
                branch.SetActive(child.gameObject.activeSelf);
                CopyTreeGeometry(child, branch.transform, copies, tint);
            }
            if (!prototype.TryGetComponent<LODGroup>(out var originalGroup)) return;
            var group = copy.gameObject.AddComponent<LODGroup>();
            var lods = originalGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                var mapped = new List<Renderer>(lods[i].renderers.Length);
                foreach (var lodRenderer in lods[i].renderers)
                    if (lodRenderer != null && copies.TryGetValue(lodRenderer, out var lodCopy)) mapped.Add(lodCopy);
                lods[i].renderers = mapped.ToArray();
            }
            group.SetLODs(lods);
            group.localReferencePoint = originalGroup.localReferencePoint;
            group.size = originalGroup.size;
            group.fadeMode = originalGroup.fadeMode;
            group.animateCrossFading = originalGroup.animateCrossFading;
            group.enabled = originalGroup.enabled;
        }

        private static void EndFade(Tree tree)
        {
            tree.Fade.Restore();
            if (tree.StandIn != null) tree.StandIn.SetActive(false);
            DestroyOwned(tree.StandIn);
            tree.StandIn = null;
            tree.Fade = null;
            tree.FadeProgress = 0f;
            tree.WantHidden = false;
        }

        private void CreateVisuals()
        {
            try
            {
                visualData = new TerrainData { name = "Camera tree instances", hideFlags = HideFlags.HideAndDontSave,
                    heightmapResolution = 33, size = originalData.size, treePrototypes = prototypes };
                visualData.SetTreeInstances(originals, false);
                visuals = new GameObject("Camera tree visuals") { hideFlags = HideFlags.HideAndDontSave, layer = source.gameObject.layer };
                visuals.SetActive(false);
                visuals.transform.SetParent(source.transform, false);
                var terrain = visuals.AddComponent<Terrain>();
                terrain.allowAutoConnect = false;
                terrain.groupingID = source.GetInstanceID();
                terrain.drawHeightmap = false;
                terrain.materialTemplate = source.materialTemplate;
                terrain.drawInstanced = source.drawInstanced;
                terrain.ignoreQualitySettings = source.ignoreQualitySettings;
                terrain.shadowCastingMode = source.shadowCastingMode;
                terrain.reflectionProbeUsage = source.reflectionProbeUsage;
                terrain.renderingLayerMask = source.renderingLayerMask;
                terrain.preserveTreePrototypeLayers = source.preserveTreePrototypeLayers;
                terrain.bakeLightProbesForTrees = source.bakeLightProbesForTrees;
                terrain.deringLightProbesForTrees = source.deringLightProbesForTrees;
                terrain.treeDistance = originalDistance = source.treeDistance;
                terrain.treeBillboardDistance = source.treeBillboardDistance;
                terrain.treeCrossFadeLength = source.treeCrossFadeLength;
                terrain.treeMaximumFullLODCount = source.treeMaximumFullLODCount;
                terrain.treeLODBiasMultiplier = source.treeLODBiasMultiplier;
                terrain.treeMotionVectorModeOverride = source.treeMotionVectorModeOverride;
                terrain.terrainData = visualData;
                visuals.SetActive(true);
                source.treeDistance = 0f;
                ownsDistance = true;
            }
            catch
            {
                ReleaseVisuals();
                throw;
            }
        }

        internal void Dispose() => ReleaseVisuals();

        private void ReleaseVisuals()
        {
            if (ownsDistance && source != null) source.treeDistance = originalDistance;
            ownsDistance = false;
            foreach (var tree in hidden) EndFade(tree);
            hidden.Clear();
            if (visuals != null) visuals.SetActive(false);
            DestroyOwned(visuals);
            DestroyOwned(visualData);
            visuals = null;
            visualData = null;
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
