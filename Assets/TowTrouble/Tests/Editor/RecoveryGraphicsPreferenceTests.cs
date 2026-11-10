using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TowTrouble.Tests
{
    public sealed class RecoveryGraphicsPreferenceTests
    {
        [Test] public void SwitchQualityToggleNeverSelectsPcOrMutatesAuthoredPipelines()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Switch_RPAsset.asset");
            var pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            string before = EditorJsonUtility.ToJson(pipeline), pcBefore = EditorJsonUtility.ToJson(pc);
            var preference = new RecoveryGraphicsPreference();

            try
            {
                preference.Apply("Switch", false);
                Assert.That(QualitySettings.names[QualitySettings.GetQualityLevel()], Is.EqualTo("Switch"));
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(pipeline));
                for (int i = 0; i < 8; i++)
                {
                    preference.Apply("Switch", true);
                    var current = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                    Assert.That(current, Is.SameAs(pipeline));
                    Assert.That(current, Is.Not.SameAs(pc));

                    Assert.That(current.renderScale, Is.EqualTo(pipeline.renderScale));
                    Assert.That(current.shadowDistance, Is.EqualTo(pipeline.shadowDistance));
                    Assert.That(current.supportsCameraDepthTexture, Is.True);
                    Assert.That(QualitySettings.names[QualitySettings.GetQualityLevel()], Is.EqualTo("Switch"));
                    preference.Apply("Switch", false);
                    Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(pipeline));
                }
                Assert.That(EditorJsonUtility.ToJson(pipeline), Is.EqualTo(before));
                Assert.That(EditorJsonUtility.ToJson(pc), Is.EqualTo(pcBefore));
            }
            finally { preference.Dispose(); }
        }

        [Test] public void DesktopSelectsPcByNameEvenWhenSwitchIsTheLastQualityLevel()
        {
            var pc = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            using (var preference = new RecoveryGraphicsPreference())
            {
                preference.Apply("PC", false);
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(pc));
                Assert.That(QualitySettings.names[QualitySettings.GetQualityLevel()], Is.EqualTo("PC"));
                preference.Apply("PC", true);
                var current = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                Assert.That(current.renderScale, Is.EqualTo(.85f).Within(.0001f));
                Assert.That(current.shadowDistance, Is.EqualTo(35));
                preference.Apply("PC", false);
                Assert.That(GraphicsSettings.currentRenderPipeline, Is.SameAs(pc));
            }
        }

        [Test] public void SessionDisposalRestoresEntryQualityPipelineAndLod()
        {
            int level = QualitySettings.GetQualityLevel();
            var pipeline = QualitySettings.renderPipeline;
            float lod = QualitySettings.lodBias;
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            string before = EditorJsonUtility.ToJson(settings);
            for (int i = 0; i < 3; i++)
            {
                var preference = new RecoveryGraphicsPreference();
                try { preference.Apply("Switch", i % 2 == 0); }
                finally { preference.Dispose(); preference.Dispose(); }
                Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(level));
                Assert.That(QualitySettings.renderPipeline, Is.SameAs(pipeline));
                Assert.That(QualitySettings.lodBias, Is.EqualTo(lod));
                Assert.That(EditorJsonUtility.ToJson(settings), Is.EqualTo(before));
            }
        }

        [Test] public void SwitchBuildIncludesOnlyItsDedicatedQualityAndKeepsDepthForWater()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
            var levels = settings.FindProperty("m_QualitySettings");
            int switchIndex = Array.IndexOf(QualitySettings.names, "Switch");
            Assert.That(switchIndex, Is.GreaterThanOrEqualTo(0));
            for (int i = 0; i < levels.arraySize; i++)
            {
                var excluded = levels.GetArrayElementAtIndex(i).FindPropertyRelative("excludedTargetPlatforms");
                bool excludesSwitch = false;
                for (int j = 0; j < excluded.arraySize; j++) excludesSwitch |= excluded.GetArrayElementAtIndex(j).stringValue == "Nintendo Switch";
                Assert.That(excludesSwitch, Is.EqualTo(i != switchIndex));
            }
            var defaults = settings.FindProperty("m_PerPlatformDefaultQuality"); bool found = false;
            for (int i = 0; i < defaults.arraySize; i++)
            {
                var pair = defaults.GetArrayElementAtIndex(i);
                if (pair.FindPropertyRelative("first").stringValue != "Nintendo Switch") continue;
                found = true; Assert.That(pair.FindPropertyRelative("second").intValue, Is.EqualTo(switchIndex));
            }
            Assert.That(found, Is.True);
            var pipeline = levels.GetArrayElementAtIndex(switchIndex).FindPropertyRelative("customRenderPipeline").objectReferenceValue as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null);
            Assert.That(pipeline.supportsCameraDepthTexture, Is.True);
            Assert.That(pipeline.supportsAdditionalLightShadows, Is.False);
            Assert.That(pipeline.supportsSoftShadows, Is.False);
            Assert.That(pipeline.shadowCascadeCount, Is.EqualTo(1));
            var renderer = new SerializedObject(pipeline).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue as UniversalRendererData;
            Assert.That(AssetDatabase.GetAssetPath(renderer), Is.EqualTo("Assets/Settings/Switch_Renderer.asset"));
            Assert.That((int)renderer.renderingMode, Is.Zero);
            Assert.That(renderer.rendererFeatures.Count, Is.GreaterThan(0));
            foreach (var feature in renderer.rendererFeatures) { Assert.That(feature, Is.Not.Null); Assert.That(feature.isActive, Is.False); }
        }

        [Test] public void SwitchPresetCutsGpuCostWhileDesktopKeepsItsAuthoredFeatures()
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Switch_RPAsset.asset");
            var pc=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            Assert.That(pipeline.renderScale,Is.EqualTo(RecoverySwitchGraphics.RenderScale));
            Assert.That(pipeline.supportsHDR,Is.False);Assert.That(pipeline.supportsCameraDepthTexture,Is.True);
            Assert.That(pipeline.mainLightShadowmapResolution,Is.EqualTo(512));Assert.That(pipeline.maxAdditionalLightsCount,Is.EqualTo(2));
            Assert.That(pipeline.upscalingFilter,Is.EqualTo(UpscalingFilterSelection.FSR));
            Assert.That(pc.supportsHDR,Is.True);Assert.That(pc.renderScale,Is.EqualTo(1));
        }

        [Test] public void VegetationOverridesAreConfinedToTheSwitchQualityLevel()
        {
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);var levels=settings.FindProperty("m_QualitySettings");
            for(int i=0;i<levels.arraySize;i++)
            {
                var level=levels.GetArrayElementAtIndex(i);bool isSwitch=level.FindPropertyRelative("name").stringValue=="Switch";
                Assert.That(level.FindPropertyRelative("terrainQualityOverrides").intValue,Is.EqualTo(isSwitch?255:0));
                if(!isSwitch)continue;
                Assert.That(level.FindPropertyRelative("terrainDetailDistance").floatValue,Is.EqualTo(RecoverySwitchGraphics.GrassDistance));
                Assert.That(level.FindPropertyRelative("terrainDetailDensityScale").floatValue,Is.EqualTo(RecoverySwitchGraphics.GrassDensity));
                Assert.That(level.FindPropertyRelative("terrainTreeDistance").floatValue,Is.EqualTo(RecoverySwitchGraphics.TreeDistance));
                Assert.That(level.FindPropertyRelative("terrainPixelError").floatValue,Is.EqualTo(RecoverySwitchGraphics.TerrainPixelError));
            }
            using(var preference=new RecoveryGraphicsPreference())
            {
                preference.Apply("Switch",false);
                Assert.That(QualitySettings.terrainDetailDistance,Is.EqualTo(RecoverySwitchGraphics.GrassDistance));
                Assert.That(QualitySettings.terrainDetailDensityScale,Is.EqualTo(RecoverySwitchGraphics.GrassDensity));
                Assert.That(QualitySettings.terrainTreeDistance,Is.EqualTo(RecoverySwitchGraphics.TreeDistance));
                Assert.That(QualitySettings.lodBias,Is.EqualTo(RecoverySwitchGraphics.LodBias));
                preference.Apply("Switch",true);Assert.That(QualitySettings.lodBias,Is.EqualTo(RecoverySwitchGraphics.LodBias));
            }
        }

        [Test] public void SwitchTerrainBudgetIsPersistedInTheNativeSettingsFile()
        {
            var copies=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(Application.dataPath+"/../ProjectSettings/QualitySettings.asset");
            try
            {
                var file=new SerializedObject(copies[0]);var levels=file.FindProperty("m_QualitySettings");bool found=false;
                for(int i=0;i<levels.arraySize;i++)
                {
                    var level=levels.GetArrayElementAtIndex(i);if(level.FindPropertyRelative("name").stringValue!="Switch")continue;found=true;
                    Assert.That(level.FindPropertyRelative("terrainQualityOverrides").intValue,Is.EqualTo(255));
                    Assert.That(level.FindPropertyRelative("terrainDetailDistance").floatValue,Is.EqualTo(RecoverySwitchGraphics.GrassDistance));
                    Assert.That(level.FindPropertyRelative("terrainDetailDensityScale").floatValue,Is.EqualTo(RecoverySwitchGraphics.GrassDensity));
                    Assert.That(level.FindPropertyRelative("terrainTreeDistance").floatValue,Is.EqualTo(RecoverySwitchGraphics.TreeDistance));
                }
                Assert.That(found,Is.True);
            }
            finally{foreach(var copy in copies)UnityEngine.Object.DestroyImmediate(copy);}
        }

        [Test] public void SwitchGraphicsPreviewIsIndependentOfTheOfflinePreviewAndRestores()
        {
            bool original=RecoveryPlatform.PreviewSwitchRendering;
            bool multiplayer=RecoveryPlatform.MultiplayerAvailable,offline=RecoveryPlatform.OfflineOnly;
            try
            {
                RecoveryPlatform.PreviewSwitchRendering=true;
                Assert.That(RecoveryPlatform.UsesSwitchRendering,Is.True);Assert.That(RecoveryPlatform.GraphicsQuality,Is.EqualTo("Switch"));
                Assert.That(RecoveryPlatform.FrameRateLimit,Is.EqualTo(30));
                Assert.That(RecoveryPlatform.MultiplayerAvailable,Is.EqualTo(multiplayer));Assert.That(RecoveryPlatform.OfflineOnly,Is.EqualTo(offline));
                RecoveryPlatform.PreviewSwitchRendering=false;
#if !UNITY_SWITCH
                Assert.That(RecoveryPlatform.UsesSwitchRendering,Is.False);Assert.That(RecoveryPlatform.GraphicsQuality,Is.EqualTo("PC"));
                Assert.That(RecoveryPlatform.FrameRateLimit,Is.EqualTo(60));
#endif
            }
            finally{RecoveryPlatform.PreviewSwitchRendering=original;}
        }

        [Test] public void CameraRangeIsUnchangedOnPcAndRestoredAfterSwitchScope()
        {
            bool preview=RecoveryPlatform.PreviewSwitchRendering;
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Inactive Switch camera scope");root.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            try
            {
                var lens=root.AddComponent<Camera>();lens.farClipPlane=2300;
                var rig=root.AddComponent<RecoveryCamera>();var session=root.AddComponent<DemoSession>();
                typeof(DemoSession).GetField("recoveryCamera",flags).SetValue(session,rig);
                var apply=typeof(DemoSession).GetMethod("ApplySwitchCameraBudget",flags);var release=typeof(DemoSession).GetMethod("RestoreGraphicsPreference",flags);
#if !UNITY_SWITCH
                RecoveryPlatform.PreviewSwitchRendering=false;apply.Invoke(session,null);Assert.That(lens.farClipPlane,Is.EqualTo(2300));
#endif
                RecoveryPlatform.PreviewSwitchRendering=true;apply.Invoke(session,null);Assert.That(lens.farClipPlane,Is.EqualTo(RecoverySwitchGraphics.CameraDistance));
                apply.Invoke(session,null);release.Invoke(session,null);Assert.That(lens.farClipPlane,Is.EqualTo(2300));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);RecoveryPlatform.PreviewSwitchRendering=preview;}
        }

        [Test] public void GraphicsPolicyUsesBuildTargetWithoutFollowingMultiplayerPreview()
        {
            bool preview = RecoveryPlatform.PreviewSinglePlayerOnly;
            try
            {
                string quality = RecoveryPlatform.GraphicsQuality; int rate = RecoveryPlatform.FrameRateLimit;
                RecoveryPlatform.PreviewSinglePlayerOnly = true;
                Assert.That(RecoveryPlatform.GraphicsQuality, Is.EqualTo(quality));
                Assert.That(RecoveryPlatform.FrameRateLimit, Is.EqualTo(rate));
#if UNITY_SWITCH
                Assert.That(quality, Is.EqualTo("Switch")); Assert.That(rate, Is.EqualTo(30));
#else
                Assert.That(quality, Is.EqualTo("PC")); Assert.That(rate, Is.EqualTo(60));
#endif
            }
            finally { RecoveryPlatform.PreviewSinglePlayerOnly = preview; }
        }
    }
}
