using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TowTrouble.Editor
{
    public static class SwitchGraphicsAuthoring
    {
        [MenuItem("Tow Trouble/Platform/Configure Switch Rendering Budget")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before configuring Switch graphics.");
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Switch_RPAsset.asset");
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Switch_Renderer.asset");
            if(pipeline==null||renderer==null)throw new InvalidOperationException("Dedicated Switch pipeline/renderer is missing.");
            pipeline.renderScale=RecoverySwitchGraphics.RenderScale;
            pipeline.upscalingFilter=UpscalingFilterSelection.Linear;
            pipeline.supportsHDR=false;pipeline.msaaSampleCount=1;
            // Water intersections and other authored surface shaders still sample scene depth.
            pipeline.supportsCameraDepthTexture=true;pipeline.supportsCameraOpaqueTexture=false;
            pipeline.mainLightShadowmapResolution=512;pipeline.shadowDistance=RecoverySwitchGraphics.ShadowDistance;
            pipeline.shadowCascadeCount=1;
            // Retain per-pixel headlights, with a smaller per-object light budget.
            pipeline.maxAdditionalLightsCount=2;
            pipeline.colorGradingLutSize=16;
            var serialized=new SerializedObject(pipeline);
            foreach(string field in new[]{"m_SoftShadowsSupported","m_AdditionalLightShadowsSupported","m_EnableLODCrossFade","m_ReflectionProbeBlending","m_ReflectionProbeBoxProjection","m_ReflectionProbeAtlas","m_SupportsLightCookies","m_SupportDataDrivenLensFlare","m_SupportScreenSpaceLensFlare"})
                serialized.FindProperty(field).boolValue=false;
            serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);AssetDatabase.SaveAssetIfDirty(pipeline);
            foreach(var feature in renderer.rendererFeatures)
                if(feature!=null&&feature.GetType().FullName.Contains("BetterFog"))
                {feature.SetActive(false);EditorUtility.SetDirty(feature);AssetDatabase.SaveAssetIfDirty(feature);}
            EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssetIfDirty(renderer);
            var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            var quality=new SerializedObject(settings);var levels=quality.FindProperty("m_QualitySettings");
            int index=Array.IndexOf(QualitySettings.names,"Switch");
            if(index<0)throw new InvalidOperationException("Switch quality level is missing.");
            var level=levels.GetArrayElementAtIndex(index);
            void Number(string name,float value){var p=level.FindPropertyRelative(name);if(p.propertyType==SerializedPropertyType.Integer)p.intValue=(int)value;else p.floatValue=value;}
            var overrides=TerrainQualityOverrides.PixelError|TerrainQualityOverrides.BasemapDistance|TerrainQualityOverrides.DetailDensity|TerrainQualityOverrides.DetailDistance|TerrainQualityOverrides.TreeDistance|TerrainQualityOverrides.BillboardStart|TerrainQualityOverrides.FadeLength|TerrainQualityOverrides.MaxTrees;
            Number("terrainQualityOverrides",(int)overrides);
            Number("terrainPixelError",RecoverySwitchGraphics.TerrainPixelError);
            Number("terrainDetailDensityScale",RecoverySwitchGraphics.GrassDensity);
            Number("terrainDetailDistance",RecoverySwitchGraphics.GrassDistance);
            Number("terrainTreeDistance",RecoverySwitchGraphics.TreeDistance);
            Number("terrainBillboardStart",RecoverySwitchGraphics.BillboardDistance);
            Number("terrainBasemapDistance",RecoverySwitchGraphics.BasemapDistance);
            Number("terrainFadeLength",5);Number("terrainMaxTrees",24);
            Number("lodBias",RecoverySwitchGraphics.LodBias);Number("shadowDistance",RecoverySwitchGraphics.ShadowDistance);
            level.FindPropertyRelative("enableLODCrossFade").boolValue=false;
            quality.ApplyModifiedPropertiesWithoutUndo();
            // Save only QualitySettings. SaveAssetIfDirty does not persist this game-manager object;
            // the native file writer requires a non-persistent object, so round-trip through a detached copy.
            string path=System.IO.Path.GetFullPath(Application.dataPath+"/../ProjectSettings/QualitySettings.asset");
            var copies=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(path);
            try
            {
                if(copies.Length!=1||!(copies[0] is QualitySettings))throw new InvalidOperationException("Unexpected QualitySettings file.");
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(settings),copies[0]);
                UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(copies,path,true);
            }
            finally{foreach(var copy in copies)UnityEngine.Object.DestroyImmediate(copy);}
        }
    }
}
