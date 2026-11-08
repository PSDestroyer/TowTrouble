using System;
using System.Linq;
using INab.BetterFog.Core;
using INab.BetterFog.URP;
using DistantLands.Cozy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TowTrouble.Editor
{
    public static class RecoveryVisualAuthoring
    {
        [MenuItem("Tow Trouble/Visuals/Configure Weather Lighting and Better Fog")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var weather=UnityEngine.Object.FindFirstObjectByType<RecoveryCozyEnvironment>();
            var cozy=UnityEngine.Object.FindFirstObjectByType<CozyWeather>();
            var session=UnityEngine.Object.FindFirstObjectByType<DemoSession>();
            var global=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Single(v=>v.isGlobal&&v.sharedProfile!=null&&AssetDatabase.GetAssetPath(v.sharedProfile)=="Assets/TowTrouble/OpenWorld/Visuals/County afternoon volume.asset");
            var profile=global.sharedProfile;Undo.RecordObject(profile,"Configure unified atmosphere");
            if(!profile.TryGet<BetterFogVolumeComponent>(out var fog)){fog=profile.Add<BetterFogVolumeComponent>(true);fog.name="County Better Fog";AssetDatabase.AddObjectToAsset(fog,profile);}
            Undo.RecordObject(fog,"Tune Better Fog");fog.active=true;
            fog._FogIntensity.Override(.68f);fog._EnergyLoss.Override(.025f);fog._UseGradient.Override(false);fog._FogColor.Override(new Color(.48f,.68f,.86f));
            fog._UseSunLight.Override(true);fog._SunColor.Override(new Color(1,.88f,.69f));fog._SunIntensity.Override(.1f);fog._SunPower.Override(6);
            fog._UseDistanceFog.Override(true);fog._UseRadialDistance.Override(true);fog._FogType.Override(FogMode.ExponentialSquared);fog._DistanceFogOffset.Override(-110);fog._FogDensity.Override(.00105f);
            fog._UseSkyboxHeightFog.Override(false);fog._SkyboxFill.Override(0);
            fog._UseHeightFog.Override(true);fog._Height.Override(session.World.GaragePosition.y+3);fog._HeightDensity.Override(.05f);fog._HeightFogType.Override(HeightFogType.ExponentialSquared);
            fog._UseNoise.Override(true);fog._NoiseAffect.Override(NoiseAffect.HeightOnly);fog._NoiseIntensity.Override(.12f);fog._Scale1.Override(90);fog._Lerp1.Override(.5f);fog._NoiseSpeed1.Override(new Vector3(.15f,0,.08f));fog._NoiseTimeScale1.Override(.03f);fog._NoiseDistanceEnd.Override(180);fog._NoiseEndHardness.Override(4);
            fog._UseSSMS.Override(false);
            if(profile.TryGet<Tonemapping>(out var tone)){Undo.RecordObject(tone,"Natural stylized tonemapping");tone.mode.Override(TonemappingMode.Neutral);EditorUtility.SetDirty(tone);}
            if(profile.TryGet<ColorAdjustments>(out var grade)){Undo.RecordObject(grade,"Balance world contrast");grade.saturation.Override(3);grade.contrast.Override(7);grade.colorFilter.Override(new Color(1,.995f,.985f));EditorUtility.SetDirty(grade);}
            if(profile.TryGet<Bloom>(out var bloom)){Undo.RecordObject(bloom,"Subtle light bloom");bloom.intensity.Override(.08f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.6f);EditorUtility.SetDirty(bloom);}
            if(profile.TryGet<Vignette>(out var vignette)){Undo.RecordObject(vignette,"Gentle edge framing");vignette.intensity.Override(.065f);EditorUtility.SetDirty(vignette);}
            TuneNaturalPalette(profile);
            EditorUtility.SetDirty(fog);EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
            foreach(string path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"})
            {
                var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);Undo.RecordObject(renderer,"Enable Better Fog renderer");
                var feature=renderer.rendererFeatures.OfType<BetterFog>().FirstOrDefault();
                if(feature==null){feature=ScriptableObject.CreateInstance<BetterFog>();feature.name="County Better Fog";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
                Undo.RecordObject(feature,"Configure fog pass");feature.m_Settings.Event=RenderPassEvent.BeforeRenderingPostProcessing;feature.m_Settings._UseCustomDepthTexture=false;feature.m_Settings._UseFogOffsetTexture=false;feature.SetActive(true);
                foreach(var ao in renderer.rendererFeatures.Where(f=>f.GetType().Name=="ScreenSpaceAmbientOcclusion"))
                {Undo.RecordObject(ao,"Soften contact occlusion");var serialized=new SerializedObject(ao);serialized.FindProperty("m_Settings.Intensity").floatValue=.26f;serialized.FindProperty("m_Settings.DirectLightingStrength").floatValue=.12f;serialized.ApplyModifiedProperties();EditorUtility.SetDirty(ao);}
                EditorUtility.SetDirty(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);AssetDatabase.SaveAssetIfDirty(renderer);
            }
            var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");Undo.RecordObject(mobile,"Provide depth for Better Fog");mobile.supportsCameraDepthTexture=true;EditorUtility.SetDirty(mobile);AssetDatabase.SaveAssetIfDirty(mobile);
            var fill=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.name=="Soft sky fill");
            var rig=weather.GetComponent<RecoveryAtmospherePresentation>();if(rig==null)rig=Undo.AddComponent<RecoveryAtmospherePresentation>(weather.gameObject);
            Undo.RecordObject(rig,"Wire atmosphere presentation");rig.Configure(weather,cozy,session,global,fill);EditorUtility.SetDirty(rig);
            // COZY keeps its clear blue skydome. Geometry fog is exclusively post-process.
            RenderSettings.fog=false;
            var dome=cozy.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Skydome");
            Undo.RecordObject(dome.gameObject,"Enable COZY sky");dome.gameObject.SetActive(true);
            Undo.RecordObject(cozy,"Refine cloud shading");cozy.cloudStyle=CozyWeather.CloudStyle.soft;cozy.ResetQuality();EditorUtility.SetDirty(cozy.cloudMesh.sharedMaterial);AssetDatabase.SaveAssetIfDirty(cozy.cloudMesh.sharedMaterial);EditorUtility.SetDirty(cozy);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        public static void TuneNaturalPalette(VolumeProfile profile)
        {
            var root="Assets/TowTrouble/OpenWorld/Visuals/";
            if(!profile.TryGet<UnityEngine.Rendering.Universal.ColorCurves>(out var curves)){curves=profile.Add<UnityEngine.Rendering.Universal.ColorCurves>();curves.name="Natural foliage saturation";UnityEditor.AssetDatabase.AddObjectToAsset(curves,profile);}
            UnityEditor.Undo.RecordObject(curves,"Temper foliage saturation");curves.hueVsSat.Override(new UnityEngine.Rendering.TextureCurve(new[]{new UnityEngine.Keyframe(0,.5f),new UnityEngine.Keyframe(.12f,.5f),new UnityEngine.Keyframe(.22f,.42f),new UnityEngine.Keyframe(.34f,.43f),new UnityEngine.Keyframe(.46f,.5f),new UnityEngine.Keyframe(.7f,.5f),new UnityEngine.Keyframe(1,.5f)},.5f,true,new UnityEngine.Vector2(0,1)));
            if(profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var grade)){UnityEditor.Undo.RecordObject(grade,"Natural daylight palette");grade.saturation.Override(3);grade.contrast.Override(7);UnityEditor.EditorUtility.SetDirty(grade);}
            if(profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom)){UnityEditor.Undo.RecordObject(bloom,"Reduce daylight glare");bloom.intensity.Override(.08f);UnityEditor.EditorUtility.SetDirty(bloom);}
            UnityEditor.EditorUtility.SetDirty(curves);UnityEditor.EditorUtility.SetDirty(profile);UnityEditor.AssetDatabase.SaveAssetIfDirty(profile);
            var grass=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(root+"Ground 0.terrainlayer");UnityEditor.Undo.RecordObject(grass,"Natural grass albedo");grass.diffuseRemapMax=new UnityEngine.Vector4(.62f,.64f,.53f,1);UnityEditor.EditorUtility.SetDirty(grass);UnityEditor.AssetDatabase.SaveAssetIfDirty(grass);
            var dirt=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TerrainLayer>(root+"Ground 1.terrainlayer");UnityEditor.Undo.RecordObject(dirt,"Balance road soil albedo");dirt.diffuseRemapMax=new UnityEngine.Vector4(.80f,.73f,.60f,1);UnityEditor.EditorUtility.SetDirty(dirt);UnityEditor.AssetDatabase.SaveAssetIfDirty(dirt);
            var road=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"County compacted gravel.mat");UnityEditor.Undo.RecordObject(road,"Balance pale road");road.SetColor("_BaseColor",new UnityEngine.Color(.82f,.81f,.77f));UnityEditor.EditorUtility.SetDirty(road);UnityEditor.AssetDatabase.SaveAssetIfDirty(road);
            var track=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(root+"Dynamic wheel tracks 3.mat");UnityEditor.Undo.RecordObject(track,"Grass scuffs reveal soil");track.SetColor("_BaseColor",new UnityEngine.Color(.55f,.48f,.35f,.46f));track.SetTexture("_BaseMap",dirt.diffuseTexture);track.SetFloat("_Compaction",.94f);track.SetFloat("_EdgeBreakup",.8f);track.SetFloat("_RutDepth",.004f);UnityEditor.EditorUtility.SetDirty(track);UnityEditor.AssetDatabase.SaveAssetIfDirty(track);
        }
    }
}
