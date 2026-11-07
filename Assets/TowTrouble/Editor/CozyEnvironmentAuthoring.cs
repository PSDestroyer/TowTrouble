using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DistantLands.Cozy;
using DistantLands.Cozy.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static class CozyEnvironmentAuthoring
    {
        private const string Root="Assets/TowTrouble/Weather";
        private const string ResourcesRoot="Packages/com.distantlands.cozy.core/Content/Resources/";
        [MenuItem("Tow Trouble/Weather/Integrate COZY and Night Headlights")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
            EnsureRequiredTag();
            DisableLegacySky();
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.GetRootGameObjects().Any(g=>g.GetComponent<RecoveryCozyEnvironment>()!=null))throw new InvalidOperationException("Weather already integrated");
            Directory.CreateDirectory(Root+"/Profiles");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Particles");AssetDatabase.Refresh();
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();
            var session=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DemoSession>(true)).Single();
            var tracks=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWheelTracks>(true)).Single();
            var map=(RecoveryGroundMap)new SerializedObject(tracks).FindProperty("surfaceMap").objectReferenceValue;
            AuthorHeadlampPorts();
            var root=new GameObject("Tow Trouble - COZY Weather");root.SetActive(false);Undo.RegisterCreatedObjectUndo(root,"Integrate COZY weather");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesRoot+"Cozy Prefabs/Cozy Weather Sphere.prefab");
            var sphere=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);PrefabUtility.UnpackPrefabInstance(sphere,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);sphere.name="County sky and weather";
            foreach(var renderer in sphere.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)if(materials[i]!=null)materials[i]=Clone(materials[i],Root+"/Materials/"+Safe(materials[i].name)+".mat");
                renderer.sharedMaterials=materials;
            }
            foreach(var module in sphere.GetComponentsInChildren<CozyAmbienceModule>(true))UnityEngine.Object.DestroyImmediate(module);
            var cozy=sphere.GetComponent<CozyWeather>();cozy.dontDestroyOnLoad=false;cozy.lockToCamera=CozyWeather.LockToCameraStyle.useMainCamera;cozy.cozyCamera=Camera.main;
            cozy.skyStyle=CozyWeather.SkyStyle.desktop;cozy.cloudStyle=CozyWeather.CloudStyle.cozyMobile;cozy.fogStyle=CozyWeather.FogStyle.unity;
            cozy.timeModule=sphere.GetComponentInChildren<CozyTimeModule>(true);cozy.weatherModule=sphere.GetComponentInChildren<CozyWeatherModule>(true);
            cozy.atmosphereModule=sphere.GetComponentInChildren<CozyAtmosphereModule>(true);cozy.climateModule=sphere.GetComponentInChildren<CozyClimateModule>(true);cozy.windModule=sphere.GetComponentInChildren<CozyWindModule>(true);
            cozy.timeModule.perennialProfile=Clone(cozy.timeModule.perennialProfile,Root+"/Profiles/County clock.asset");
            cozy.timeModule.perennialProfile.pauseTime=true;cozy.timeModule.perennialProfile.resetTimeOnStart=false;cozy.timeModule.perennialProfile.progressDay=false;cozy.timeModule.currentTime=RecoveryWeatherRules.StartDay;
            EditorUtility.SetDirty(cozy.timeModule.perennialProfile);AssetDatabase.SaveAssetIfDirty(cozy.timeModule.perennialProfile);
            cozy.atmosphereModule.atmosphereProfile=Clone(cozy.atmosphereModule.atmosphereProfile,Root+"/Profiles/County atmosphere.asset");
            OwnSatelliteProfiles(cozy);
            var names=new[]{"Mostly Clear","Partly Cloudy","Overcast","Light Precipitation"};var profiles=new WeatherProfile[4];
            for(int i=0;i<4;i++)
            {
                var original=AssetDatabase.LoadAssetAtPath<WeatherProfile>(ResourcesRoot+"Profiles/Weather Profiles/"+names[i]+".asset");
                profiles[i]=Clone(original,Root+"/Profiles/County "+names[i]+".asset");
                profiles[i].FX=original.FX.Where(f=>f is CloudFX||f is FilterFX||f is WindFX).Select(f=>CloneFx(f)).ToArray();
                EditorUtility.SetDirty(profiles[i]);AssetDatabase.SaveAssetIfDirty(profiles[i]);
            }
            var forecast=Clone(cozy.weatherModule.ecosystem.forecastProfile,Root+"/Profiles/County forecast.asset");forecast.profilesToForecast=profiles.ToList();forecast.initialProfile=profiles[1];forecast.startWeatherWith=ForecastProfile.StartWeatherWith.InitialProfile;
            EditorUtility.SetDirty(forecast);AssetDatabase.SaveAssetIfDirty(forecast);
            cozy.weatherModule.ecosystem.forecastProfile=forecast;cozy.weatherModule.ecosystem.weatherSelectionMode=CozyEcosystem.EcosystemStyle.manual;
            cozy.weatherModule.ecosystem.currentWeather=cozy.weatherModule.ecosystem.weatherChangeCheck=profiles[1];
            cozy.weatherModule.ecosystem.weightedWeatherProfiles=new List<WeatherRelation>{new WeatherRelation{profile=profiles[1],weight=1}};
            cozy.weatherModule.currentWeatherProfiles=new List<WeatherRelation>{new WeatherRelation{profile=profiles[1],weight=1}};
            var rain=Precipitation("Light Rain Particle FX",450);var snow=Precipitation("Light Snow Particle FX",320);
            var rainAudio=Clone(AssetDatabase.LoadAssetAtPath<AudioFX>(ResourcesRoot+"Profiles/FX/Audio FX/Light Rain Atmosphere SFX.asset"),Root+"/Profiles/County rainfall audio.asset");
            rainAudio.maximumVolume=.24f;var bank=Resources.Load<RecoveryAudioBank>("RecoveryAudioBank");if(bank!=null)rainAudio.audioMixer=bank.Ambience;EditorUtility.SetDirty(rainAudio);AssetDatabase.SaveAssetIfDirty(rainAudio);
            var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
            var fill=lights.FirstOrDefault(l=>l.name=="Soft sky fill");
            foreach(var light in lights)if(light.type==LightType.Directional&&light!=fill&&!light.transform.IsChildOf(sphere.transform)){Undo.RecordObject(light,"Replace static sun with COZY day/night");light.enabled=false;EditorUtility.SetDirty(light);}
            cozy.SetupReferences();cozy.sunLight.intensity=1;cozy.moonlightShadows=LightShadows.None;
            var director=root.AddComponent<RecoveryCozyEnvironment>();director.Configure(world,session,cozy,map,profiles,rain,snow,rainAudio,fill);
            root.AddComponent<RecoveryCozyVisibility>().Configure(director);
            foreach(var ps in sphere.GetComponentsInChildren<ParticleSystem>(true)){var main=ps.main;main.maxParticles=Mathf.Min(main.maxParticles,400);}
            root.SetActive(true);cozy.SetupReferences();cozy.ResetModules();cozy.ResetQuality();
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        private static string Safe(string name)=>name.Replace('/','-').Replace(':','-');
        public static void DisableLegacySky()
        {
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!renderer.sharedMaterials.Any(m=>m!=null&&(m.shader.name=="Shader Graphs/S_StylizedSky"||m.shader.name=="Shader Graphs/S_StylizedClouds")))continue;
                Undo.RecordObject(renderer,"Replace static sky with COZY");renderer.enabled=false;EditorUtility.SetDirty(renderer);
            }
        }
        public static void EnsureRequiredTag()
        {
            var asset=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];var serialized=new SerializedObject(asset);var tags=serialized.FindProperty("tags");
            for(int i=0;i<tags.arraySize;i++)if(tags.GetArrayElementAtIndex(i).stringValue=="FX Block Zone")return;
            tags.InsertArrayElementAtIndex(tags.arraySize);tags.GetArrayElementAtIndex(tags.arraySize-1).stringValue="FX Block Zone";
            serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(asset);
        }
        public static void OwnSatelliteProfiles(CozyWeather cozy)
        {
            var module=cozy.GetComponentInChildren<CozySatelliteModule>(true);if(module==null)return;
            for(int i=0;i<module.satellites.Length;i++)
            {
                var p=Clone(module.satellites[i],Root+"/Profiles/County moon "+i+".asset");p.orbitRef=null;p.moonRef=null;p.lightRef=null;
                EditorUtility.SetDirty(p);AssetDatabase.SaveAssetIfDirty(p);module.satellites[i]=p;
            }
            EditorUtility.SetDirty(module);module.UpdateSatellites();
        }
        private static T Clone<T>(T source,string path) where T:UnityEngine.Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing!=null)return existing;
            if(source==null)throw new InvalidOperationException("Missing COZY source for "+path);
            var value=UnityEngine.Object.Instantiate(source);value.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(value,path);return value;
        }
        private static FXProfile CloneFx(FXProfile source)
        {return Clone(source,Root+"/Profiles/"+Safe(source.name)+".asset");}
        private static ParticleFX Precipitation(string sourceName,int cap)
        {
            var fx=Clone(AssetDatabase.LoadAssetAtPath<ParticleFX>(ResourcesRoot+"Profiles/FX/Particle FX/"+sourceName+".asset"),Root+"/Profiles/County "+sourceName+".asset");
            string path=Root+"/Particles/"+sourceName+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)
            {
                var clone=UnityEngine.Object.Instantiate(fx.particleSystem.gameObject);clone.name=sourceName;
                try
                {
                    foreach(var ps in clone.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main=ps.main;main.maxParticles=Mathf.Min(main.maxParticles,cap);main.useUnscaledTime=false;
                        if(sourceName.Contains("Snow"))main.startSizeMultiplier*=1.7f;
                        var collision=ps.collision;collision.enabled=false;
                        var renderer=ps.GetComponent<ParticleSystemRenderer>();var materials=renderer.sharedMaterials;
                        for(int i=0;i<materials.Length;i++)if(materials[i]!=null)materials[i]=Clone(materials[i],Root+"/Materials/"+Safe(materials[i].name)+".mat");renderer.sharedMaterials=materials;
                    }
                    prefab=PrefabUtility.SaveAsPrefabAsset(clone,path);
                }
                finally{UnityEngine.Object.DestroyImmediate(clone);}
            }
            fx.particleSystem=prefab.GetComponent<CozyParticles>();fx.autoScale=false;EditorUtility.SetDirty(fx);AssetDatabase.SaveAssetIfDirty(fx);return fx;
        }
        public static void AuthorHeadlampPorts()
        {
            var profile=AssetDatabase.LoadAssetAtPath<RecoveryVehicleEffectsProfile>("Assets/TowTrouble/Art/VehicleEffects/Hot Rod vehicle effects.asset");
            var seen=new HashSet<Mesh>();var result=new List<VehicleHeadlampPorts>();
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/TowTrouble/Garage"}).Select(AssetDatabase.GUIDToAssetPath))
            foreach(var filter in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;if(mesh==null||!mesh.name.StartsWith("Bonnet")||!seen.Add(mesh))continue;
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null)continue;
                var vertices=mesh.vertices;var uv=mesh.uv;if(uv.Length!=vertices.Length)continue;var points=new List<Vector3>();var materials=renderer.sharedMaterials;
                for(int s=0;s<mesh.subMeshCount&&s<materials.Length;s++)
                {
                    var material=materials[s];if(material==null||!material.HasProperty("_EmissionMap"))continue;
                    var atlas=profile.Atlases.FirstOrDefault(a=>a.Source==material.GetTexture("_EmissionMap"));if(atlas==null)continue;
                    var texture=(Texture2D)atlas.Running;var triangles=mesh.GetTriangles(s);
                    for(int t=0;t<triangles.Length;t+=3)
                    {
                        var center=(uv[triangles[t]]+uv[triangles[t+1]]+uv[triangles[t+2]])/3;var c=texture.GetPixelBilinear(center.x,center.y);
                        if(c.g>.08f&&c.g>c.r*.5f)points.Add((vertices[triangles[t]]+vertices[triangles[t+1]]+vertices[triangles[t+2]])/3);
                    }
                }
                if(points.Count==0)continue;var ports=new List<Vector3>();
                foreach(bool right in new[]{false,true}){var side=points.Where(p=>(p.x>=0)==right).ToArray();if(side.Length>0)ports.Add(side.Aggregate(Vector3.zero,(a,b)=>a+b)/side.Length);}
                result.Add(new VehicleHeadlampPorts{Mesh=mesh,Positions=ports.ToArray()});
            }
            Undo.RecordObject(profile,"Measure visible bonnet headlamps");profile.Headlights=result.ToArray();EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        }
    }
}
