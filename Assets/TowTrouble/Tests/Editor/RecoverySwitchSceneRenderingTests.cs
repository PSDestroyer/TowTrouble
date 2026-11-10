using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoverySwitchSceneRenderingTests
    {
        [Test] public void ManualRockLodSelectsOneLevelAndHonoursFinalCullThreshold()
        {
            var lods = new[] { new LOD(.6f, Array.Empty<Renderer>()), new LOD(.25f, Array.Empty<Renderer>()), new LOD(.03f, Array.Empty<Renderer>()) };
            Assert.That(RecoverySwitchSceneRendering.SelectLod(lods, 1), Is.EqualTo(0));
            Assert.That(RecoverySwitchSceneRendering.SelectLod(lods, .6f), Is.EqualTo(0));
            Assert.That(RecoverySwitchSceneRendering.SelectLod(lods, .4f), Is.EqualTo(1));
            Assert.That(RecoverySwitchSceneRendering.SelectLod(lods, .04f), Is.EqualTo(2));
            Assert.That(RecoverySwitchSceneRendering.SelectLod(lods, .01f), Is.EqualTo(-1));
        }
        [Test] public void RockInstancingRestoresSourceLodAndVisibilityWithoutChangingMaterials()
        {
            bool preview = RecoveryPlatform.PreviewSwitchRendering;
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Isolated rock render validation");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                RecoveryPlatform.PreviewSwitchRendering = true;
                var camera = root.AddComponent<Camera>();
                var budget = root.AddComponent<RecoverySwitchSceneRendering>();
                var catalog = Resources.Load<RecoverySwitchInstancingCatalog>("SwitchRockInstancing");
                Assert.That(catalog, Is.Not.Null); Assert.That(catalog.entries.Length, Is.GreaterThan(0));
                var rock = new GameObject("Rock test"); rock.transform.SetParent(root.transform);
                var lod = rock.AddComponent<LODGroup>();
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.transform.SetParent(rock.transform);
                var renderer = cube.GetComponent<MeshRenderer>(); renderer.sharedMaterial = catalog.entries[0].source;
                lod.SetLODs(new[] { new LOD(.02f, new Renderer[] { renderer }) }); lod.RecalculateBounds();
                budget.Initialize(camera);
                Assert.That(budget.ManagedGroups, Is.EqualTo(1));
                Assert.That(budget.InstancedGroups, Is.EqualTo(SystemInfo.supportsInstancing ? 1 : 0));
                if (SystemInfo.supportsInstancing) { Assert.That(lod.enabled, Is.False); Assert.That(renderer.forceRenderingOff, Is.True); }
                budget.Release(); budget.Release();
                Assert.That(lod.enabled, Is.True); Assert.That(renderer.forceRenderingOff, Is.False);
                Assert.That(renderer.sharedMaterial, Is.SameAs(catalog.entries[0].source));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); RecoveryPlatform.PreviewSwitchRendering = preview; }
        }
        [Test] public void DynamicObjectsAndPerObjectMaterialOverridesKeepNativeRendering()
        {
            bool preview = RecoveryPlatform.PreviewSwitchRendering;
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("Isolated rendering fallback validation");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                RecoveryPlatform.PreviewSwitchRendering = true;
                var camera = root.AddComponent<Camera>(); var budget = root.AddComponent<RecoverySwitchSceneRendering>();
                var catalog = Resources.Load<RecoverySwitchInstancingCatalog>("SwitchRockInstancing");
                var dynamic = GameObject.CreatePrimitive(PrimitiveType.Cube); dynamic.transform.SetParent(root.transform);
                dynamic.GetComponent<MeshRenderer>().sharedMaterial = catalog.entries[0].source;
                dynamic.AddComponent<Rigidbody>();
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube); rock.transform.SetParent(root.transform);
                var renderer = rock.GetComponent<MeshRenderer>(); renderer.sharedMaterial = catalog.entries[0].source;
                var block = new MaterialPropertyBlock(); block.SetFloat("_TowTestOverride", 1); renderer.SetPropertyBlock(block);
                var lod = rock.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.02f, new Renderer[] { renderer }) }); lod.RecalculateBounds();
                budget.Initialize(camera);
                Assert.That(budget.ManagedGroups, Is.EqualTo(1)); Assert.That(budget.InstancedGroups, Is.Zero);
                Assert.That(lod.enabled, Is.True); Assert.That(renderer.forceRenderingOff, Is.False);
                Assert.That(dynamic.GetComponent<MeshRenderer>().forceRenderingOff, Is.False);
                budget.Release(); Assert.That(renderer.HasPropertyBlock(), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); RecoveryPlatform.PreviewSwitchRendering = preview; }
        }
        [Test] public void PcDoesNotIndexOrAlterEnvironmentRenderers()
        {
#if !UNITY_SWITCH
            bool preview = RecoveryPlatform.PreviewSwitchRendering;
            var scene = EditorSceneManager.NewPreviewScene(); var root = new GameObject("PC rendering isolation");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                RecoveryPlatform.PreviewSwitchRendering = false;
                var camera = root.AddComponent<Camera>(); var budget = root.AddComponent<RecoverySwitchSceneRendering>();
                budget.Initialize(camera); Assert.That(budget.ManagedGroups, Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); RecoveryPlatform.PreviewSwitchRendering = preview; }
#endif
        }
        [Test] public void TerrainOcclusionRequiresCoverageOfTheWholeExpandedBounds()
        {
            var scene = EditorSceneManager.NewPreviewScene(); var root = new GameObject("Terrain visibility test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(32, 20, 64) };
            try
            {
                var heights = new float[33, 33];
                for (int z = 8; z <= 16; z++) for (int x = 0; x < 33; x++) heights[z, x] = .5f;
                data.SetHeights(0, 0, heights);
                var terrain = Terrain.CreateTerrainGameObject(data); terrain.transform.SetParent(root.transform);
                var collider = terrain.GetComponent<TerrainCollider>();
                var budget = root.AddComponent<RecoverySwitchSceneRendering>();
                typeof(RecoverySwitchSceneRendering).GetField("terrainColliders", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(budget, new[] { collider });
                var test = typeof(RecoverySwitchSceneRendering).GetMethod("IsTerrainOccluded", BindingFlags.NonPublic | BindingFlags.Instance);
                var eye = new Vector3(16, 2, 2);
                Assert.That((bool)test.Invoke(budget, new object[] { new Bounds(new Vector3(16, 1, 50), Vector3.one * 2), eye }), Is.True);
                Assert.That((bool)test.Invoke(budget, new object[] { new Bounds(new Vector3(16, 24, 50), new Vector3(2, 32, 2)), eye }), Is.False);
                Assert.That((bool)test.Invoke(budget, new object[] { new Bounds(new Vector3(16, 2, 8), Vector3.one * 2), eye }), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(data); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
