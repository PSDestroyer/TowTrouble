using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DistantLands.Cozy;
using INab.BetterFog.URP;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TowTrouble.Editor
{
    /// <summary>Offline photographs of the real sites; owns only PNG/importer changes, never the map or live casualty.</summary>
    public static class RecoveryPhotoAuthoring
    {
        private const string Folder="Assets/TowTrouble/OpenWorld/Photos";
        private const int Width=1280,Height=512,PhotoLayer=30;
        private static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        [MenuItem("Tow Trouble/Recovery/Regenerate Cinematic Recovery Photos")]
        public static void BakeAll()=>Capture(0,int.MaxValue,true);
        public static void Capture(int start,int count,bool writeAssets=true)
            =>CaptureWhere((site,index)=>index>=start&&index-start<count,writeAssets);
        public static bool HasStableBindings()
        {
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var hud=UnityEngine.Object.FindFirstObjectByType<RecoveryHud>();
            if(world==null||world.Map==null||hud==null)return false;
            var images=new SerializedObject(hud).FindProperty("contractImages");
            if(images.arraySize!=world.Map.Sites.Length)return false;
            for(int i=0;i<images.arraySize;i++)if(AssetDatabase.GetAssetPath(images.GetArrayElementAtIndex(i).objectReferenceValue)!=Folder+"/"+world.Map.Sites[i].Id+".png")return false;
            return true;
        }
        public static void CaptureWhere(Func<RecoverySite,int,bool> include,bool writeAssets=true)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before photographing recoveries.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=OpenWorldAuthoring.ScenePath)throw new InvalidOperationException("Open the recovery map first.");
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
            var hud=UnityEngine.Object.FindFirstObjectByType<RecoveryHud>();
            if(world==null||world.Map==null||hud==null)throw new InvalidOperationException("Map, casualty and recovery UI are required.");
            var bindings=new SerializedObject(hud).FindProperty("contractImages");
            if(bindings.arraySize!=world.Map.Sites.Length)throw new InvalidOperationException("Review existing photo bindings before capture.");
            for(int i=0;i<bindings.arraySize;i++)if(AssetDatabase.GetAssetPath(bindings.GetArrayElementAtIndex(i).objectReferenceValue)!=Folder+"/"+world.Map.Sites[i].Id+".png")
                throw new InvalidOperationException("Photo bindings do not match stable site IDs.");
            Directory.CreateDirectory("/private/tmp/TowRecoveryPhotos");
            var reports=new List<object>();bool dirty=scene.isDirty;
            string beforeMap=EditorJsonUtility.ToJson(world.Map),beforeCar=EditorJsonUtility.ToJson(world.Casualty),beforeHud=EditorJsonUtility.ToJson(hud);
            Physics.SyncTransforms();
            using(var graphics=new RecoveryGraphicsPreference())
            using(var studio=new PhotoStudio(scene))
            {
                graphics.Apply("PC",false);
                for(int index=0;index<world.Map.Sites.Length;index++)
                {
                    var site=world.Map.Sites[index];if(!include(site,index))continue;
                    var holder=new GameObject("Posed recovery vehicle");holder.SetActive(false);holder.transform.SetParent(studio.root.transform,false);
                    try
                    {
                        var car=Vehicle(world.Casualty,site,holder.transform);
                        holder.SetActive(true);
                        var renderers=car.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                        if(renderers.Length==0)throw new InvalidOperationException("No visible vehicle geometry for "+site.Id);
                        Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                        var points=new List<Vector3>();
                        foreach(var renderer in renderers)
                        {
                            var local=renderer.localBounds;
                            for(int corner=0;corner<8;corner++)points.Add(renderer.localToWorldMatrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
                        }
                        var selected=Frame(studio.camera,bounds,points.ToArray(),site,studio.trees);
                        Smoke(world.Casualty,site,car.transform,(uint)(117+index));
                        studio.PrepareSky();studio.camera.Render();
                        Graphics.Blit(studio.large,studio.small);
                        var active=RenderTexture.active;
                        try{RenderTexture.active=studio.small;studio.pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);studio.pixels.Apply();}
                        finally{RenderTexture.active=active;}
                        string path=writeAssets?Folder+"/"+site.Id+".png":"/private/tmp/TowRecoveryPhotos/"+site.Id+"-Preview.png";
                        File.WriteAllBytes(path,studio.pixels.EncodeToPNG());
                        if(writeAssets)
                        {
                            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
                            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.isReadable=false;
                            importer.alphaSource=TextureImporterAlphaSource.None;importer.wrapMode=TextureWrapMode.Clamp;
                            importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;
                            importer.textureCompression=TextureImporterCompression.CompressedHQ;
                            var sw=importer.GetPlatformTextureSettings("Switch");sw.name="Switch";sw.overridden=true;
                            sw.maxTextureSize=1024;sw.format=TextureImporterFormat.ASTC_6x6;sw.compressionQuality=75;
                            importer.SetPlatformTextureSettings(sw);importer.SaveAndReimport();
                        }
                        reports.Add(new{site.Id,site.English,site.TargetModel,site.TargetPaint,kind=site.Kind.ToString(),path,
                            selected.Score,position=new[]{studio.camera.transform.position.x,studio.camera.transform.position.y,studio.camera.transform.position.z},
                            viewport=new[]{selected.Rect.xMin,selected.Rect.yMin,selected.Rect.xMax,selected.Rect.yMax},width=Width,height=Height});
                    }
                    finally{UnityEngine.Object.DestroyImmediate(holder);}
                }
            }
            if(reports.Count==0)return;
            if(beforeMap!=EditorJsonUtility.ToJson(world.Map)||beforeCar!=EditorJsonUtility.ToJson(world.Casualty)||beforeHud!=EditorJsonUtility.ToJson(hud))
                throw new InvalidOperationException("Source data changed during photo capture.");
            File.WriteAllText("/private/tmp/TowRecoveryPhotos/Capture-"+startToken()+".json",Newtonsoft.Json.JsonConvert.SerializeObject(new{reports,scenePreserved=scene==SceneManager.GetActiveScene(),dirtyPreserved=dirty==scene.isDirty,sourceDataUnchanged=true},Newtonsoft.Json.Formatting.Indented));
            string startToken()=>world.Map.Sites.First(s=>include(s,Array.IndexOf(world.Map.Sites,s))).Id+(writeAssets?"-Final":"-Preview");
        }
        private static GameObject Vehicle(RecoveryVehicle source,RecoverySite site,Transform parent)
        {
            var model=source.ModelAt(site.TargetModel);
            var car=UnityEngine.Object.Instantiate(model.Root,parent,false);car.SetActive(false);
            foreach(var behaviour in car.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behaviour);
            foreach(var audio in car.GetComponentsInChildren<AudioSource>(true))UnityEngine.Object.DestroyImmediate(audio);
            foreach(var collider in car.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            foreach(var body in car.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
            foreach(var particles in car.GetComponentsInChildren<ParticleSystem>(true)){particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);particles.gameObject.SetActive(false);}
            foreach(var child in car.GetComponentsInChildren<Transform>(true))child.gameObject.layer=PhotoLayer;
            foreach(var r in car.GetComponentsInChildren<Renderer>(true))r.forceRenderingOff=false;
            var slots=new SerializedObject(source).FindProperty("paintSlots");
            for(int i=0;i<slots.arraySize;i++)
            {
                var entry=slots.GetArrayElementAtIndex(i);var original=entry.FindPropertyRelative("Renderer").objectReferenceValue as Renderer;
                if(original==null||!original.transform.IsChildOf(model.Root.transform))continue;
                string path=AnimationUtility.CalculateTransformPath(original.transform,model.Root.transform);
                var target=path.Length==0?car.transform:car.transform.Find(path);if(target==null)continue;
                var renderer=target.GetComponent<Renderer>();if(renderer==null)continue;
                var colors=entry.FindPropertyRelative("Colors");int slot=entry.FindPropertyRelative("Slot").intValue;
                if(site.TargetPaint<0||site.TargetPaint>=colors.arraySize||slot>=renderer.sharedMaterials.Length)continue;
                var materials=renderer.sharedMaterials;materials[slot]=colors.GetArrayElementAtIndex(site.TargetPaint).objectReferenceValue as Material;renderer.sharedMaterials=materials;
            }
            car.transform.SetPositionAndRotation(site.Position-Vector3.up*site.Depth,Quaternion.Euler(site.Pitch,site.Heading,site.Roll));
            var wheels=new SerializedObject(source).FindProperty("wheels");
            for(int i=0;i<4;i++)
            {
                var wheel=wheels.GetArrayElementAtIndex(i).objectReferenceValue as WheelCollider;
                var visual=car.transform.Find(AnimationUtility.CalculateTransformPath(model.Wheels[i],model.Root.transform));
                Vector3 anchor=car.transform.position+car.transform.rotation*model.Anchors[i],up=car.transform.up;
                if(visual!=null&&wheel!=null&&Physics.Raycast(anchor+up*RecoveryWheelPose.ProbeLift,-up,out var hit,model.Radius+wheel.suspensionDistance+RecoveryWheelPose.ProbeLift,1<<8,QueryTriggerInteraction.Ignore))
                    visual.position=RecoveryWheelPose.Position(anchor,up,RecoveryWheelPose.SuspensionOffset(hit.distance,model.Radius,wheel.suspensionDistance));
            }
            car.SetActive(true);return car;
        }
        private static void Smoke(RecoveryVehicle source,RecoverySite site,Transform car,uint seed)
        {
            if(site.Kind!=RecoverySituationKind.EngineFailure)return;
            var visual=source.GetComponent<RecoveryDistressVisuals>();if(visual==null)return;
            var original=new SerializedObject(visual).FindProperty("engineSmoke").objectReferenceValue as ParticleSystem;if(original==null)return;
            var copy=UnityEngine.Object.Instantiate(original.gameObject,car,false);copy.SetActive(true);
            copy.transform.localPosition=source.transform.InverseTransformPoint(original.transform.position);copy.layer=PhotoLayer;
            var ps=copy.GetComponent<ParticleSystem>();ps.useAutoRandomSeed=false;ps.randomSeed=seed;
            var main=ps.main;main.playOnAwake=false;main.maxParticles=48;
            var emission=ps.emission;emission.enabled=true;emission.rateOverTime=8;
            ps.Simulate(1.8f,true,true,true);ps.Pause(true);
        }
        private struct Shot { public float Score;public Rect Rect; }
        private static Shot Frame(Camera camera,Bounds bounds,Vector3[] corners,RecoverySite site,Renderer[] trees)
        {
            Vector3 focus=bounds.center;float best=float.MaxValue;Vector3 chosen=Vector3.zero,chosenAim=focus;
            var localTrees=trees.Where(r=>r!=null&&r.bounds.SqrDistance(focus)<900).ToArray();
            foreach(float yaw in new[]{-52f,52,-75,75,-120,120,0,180})foreach(float elevation in new[]{18f,25,33})
            {
                float a=(site.Heading+yaw)*Mathf.Deg2Rad,e=elevation*Mathf.Deg2Rad;
                var direction=new Vector3(Mathf.Sin(a)*Mathf.Cos(e),Mathf.Sin(e),Mathf.Cos(a)*Mathf.Cos(e));
                float distance=10;Vector3 aim=focus;Rect rect=default;
                for(int fit=0;fit<6;fit++)
                {
                    Vector3 position=focus+direction*distance;
                    if(Physics.Raycast(position+Vector3.up*100,Vector3.down,out var ground,200,1<<8,QueryTriggerInteraction.Ignore))position.y=Mathf.Max(position.y,ground.point.y+1.25f);
                    camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(aim-position,Vector3.up));
                    rect=Projected(camera,corners);
                    float ratio=Mathf.Max(rect.height/.49f,rect.width/.52f);
                    if(Mathf.Abs(ratio-1)<.02f&&(rect.center-Vector2.one*.5f).sqrMagnitude<.0001f)break;
                    float span=2*distance*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
                    aim+=camera.transform.right*(rect.center.x-.5f)*span*camera.aspect+camera.transform.up*(rect.center.y-.5f)*span;
                    distance*=Mathf.Clamp(ratio,.7f,1.5f);
                }
                float score=Mathf.Abs(rect.height-.49f)*50+Mathf.Abs(rect.center.x-.5f)*15+Mathf.Abs(rect.center.y-.5f)*15;
                if(rect.yMin<.235f||rect.yMax>.765f)score+=100;
                var probes=new[]{focus,focus+Vector3.up*.65f,focus+Vector3.up*1.1f,focus+Vector3.right*.55f,focus-Vector3.right*.55f};
                foreach(var probe in probes)if(Physics.Linecast(camera.transform.position,probe,1<<8,QueryTriggerInteraction.Ignore))score+=100;
                var ray=new Ray(camera.transform.position,focus-camera.transform.position);float length=Vector3.Distance(camera.transform.position,focus);
                foreach(var tree in localTrees)if(tree.bounds.IntersectRay(ray,out float hit)&&hit<length-1.5f)score+=35;
                score+=Mathf.Abs(Mathf.Abs(yaw)-52)*.01f+(elevation-18)*.08f;
                if(score>=best)continue;best=score;chosen=camera.transform.position;chosenAim=aim;
            }
            camera.transform.SetPositionAndRotation(chosen,Quaternion.LookRotation(chosenAim-chosen,Vector3.up));
            return new Shot{Score=best,Rect=Projected(camera,corners)};
        }
        private static Rect Projected(Camera camera,Vector3[] corners)
        {
            Vector2 min=Vector2.one*100,max=Vector2.one*-100;
            foreach(var p in corners){var v=camera.WorldToViewportPoint(p);min=Vector2.Min(min,v);max=Vector2.Max(max,v);}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private sealed class PhotoStudio:IDisposable
        {
            public readonly GameObject root;
            public readonly Camera camera;
            public readonly RenderTexture large,small;
            public readonly Texture2D pixels;
            public readonly Renderer[] trees;
            private readonly CozyWeather sky;
            private readonly Light photoFill;
            private readonly Camera oldSkyCamera;
            private readonly Vector3 oldSkyPosition,oldDomeScale;
            private readonly bool oldFog=RenderSettings.fog;
            private readonly FogMode oldFogMode=RenderSettings.fogMode;
            private readonly float oldFogDensity=RenderSettings.fogDensity;
            private readonly Color oldFogColor=RenderSettings.fogColor,oldAmbientSky=RenderSettings.ambientSkyColor,oldAmbientEquator=RenderSettings.ambientEquatorColor,oldAmbientGround=RenderSettings.ambientGroundColor;
            private readonly Light oldSun=RenderSettings.sun;
            private readonly Color oldSunColor;
            private readonly float oldSunIntensity;
            private readonly Quaternion oldSunRotation;
            private readonly Volume volume;
            private readonly VolumeProfile previousProfile,photoProfile;
            private readonly FieldInfo internalProfile=typeof(Volume).GetField("m_InternalProfile",Hidden);
            public PhotoStudio(Scene scene)
            {
                if(oldSun!=null){oldSunColor=oldSun.color;oldSunIntensity=oldSun.intensity;oldSunRotation=oldSun.transform.rotation;}
                root=new GameObject("Temporary cinematic recovery photography"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(root,scene);
                var go=new GameObject("Photo camera",typeof(Camera),typeof(UniversalAdditionalCameraData));go.transform.SetParent(root.transform,false);camera=go.GetComponent<Camera>();
                camera.enabled=false;camera.fieldOfView=39;camera.aspect=Width/(float)Height;camera.farClipPlane=700;camera.nearClipPlane=.15f;camera.allowHDR=true;
                camera.cullingMask=~(1<<5);var data=go.GetComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.requiresDepthTexture=true;data.requiresColorTexture=true;
                data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
                large=new RenderTexture(Width*2,Height*2,24);small=new RenderTexture(Width,Height,0);large.Create();small.Create();camera.targetTexture=large;pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);
                var fillObject=new GameObject("Photo vehicle fill",typeof(Light));fillObject.transform.SetParent(root.transform,false);var fill=photoFill=fillObject.GetComponent<Light>();
                fill.type=LightType.Directional;fill.intensity=.18f;fill.color=new Color(1,.95f,.87f);fill.shadows=LightShadows.None;fill.cullingMask=1<<PhotoLayer;fill.transform.rotation=Quaternion.Euler(40,135,0);
                sky=UnityEngine.Object.FindFirstObjectByType<CozyWeather>();
                if(sky!=null){oldSkyCamera=sky.cozyCamera;oldSkyPosition=sky.transform.position;oldDomeScale=sky.transform.GetChild(0).localScale;sky.cozyCamera=camera;}
                volume=UnityEngine.Object.FindFirstObjectByType<Volume>();
                if(volume!=null)
                {
                    previousProfile=(VolumeProfile)internalProfile.GetValue(volume);
                    var source=previousProfile!=null?previousProfile:volume.sharedProfile;
                    photoProfile=ScriptableObject.CreateInstance<VolumeProfile>();photoProfile.hideFlags=HideFlags.HideAndDontSave;
                    foreach(var component in source.components)photoProfile.components.Add(UnityEngine.Object.Instantiate(component));
                    if(photoProfile.TryGet<ColorAdjustments>(out var grade)){grade.saturation.Override(15);grade.contrast.Override(12);grade.postExposure.Override(.08f);}
                    if(photoProfile.TryGet<BetterFogVolumeComponent>(out var fog)){fog._UseNoise.value=false;fog._FogIntensity.value=.45f;fog._DistanceFogOffset.value=-120;}
                    if(photoProfile.TryGet<Bloom>(out var bloom))bloom.intensity.Override(.05f);
                    var dof=photoProfile.TryGet<DepthOfField>(out var existing)?existing:photoProfile.Add<DepthOfField>();
                    dof.mode.Override(DepthOfFieldMode.Gaussian);dof.gaussianStart.Override(18);dof.gaussianEnd.Override(55);dof.gaussianMaxRadius.Override(.65f);dof.highQualitySampling.Override(true);
                    volume.profile=photoProfile;
                }
                trees=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene&&r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.y>2&&
                    (r.name.Contains("Tree")||r.name.Contains("Birch")||r.name.Contains("Fir")||r.name.Contains("Oak"))).Cast<Renderer>().ToArray();
            }
            public void PrepareSky(){photoFill.transform.rotation=camera.transform.rotation*Quaternion.Euler(18,-20,0);if(sky!=null){sky.UpdateSkydomePositionAndScale();sky.UpdateShaderVariables();}}
            public void Dispose()
            {
                if(sky!=null){sky.cozyCamera=oldSkyCamera;sky.transform.position=oldSkyPosition;sky.transform.GetChild(0).localScale=oldDomeScale;sky.UpdateShaderVariables();}
                RenderSettings.fog=oldFog;RenderSettings.fogMode=oldFogMode;RenderSettings.fogDensity=oldFogDensity;RenderSettings.fogColor=oldFogColor;
                RenderSettings.ambientSkyColor=oldAmbientSky;RenderSettings.ambientEquatorColor=oldAmbientEquator;RenderSettings.ambientGroundColor=oldAmbientGround;
                if(oldSun!=null){oldSun.color=oldSunColor;oldSun.intensity=oldSunIntensity;oldSun.transform.rotation=oldSunRotation;}
                if(volume!=null)volume.profile=previousProfile;
                if(photoProfile!=null){foreach(var c in photoProfile.components)UnityEngine.Object.DestroyImmediate(c);UnityEngine.Object.DestroyImmediate(photoProfile);}
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(pixels);large.Release();small.Release();UnityEngine.Object.DestroyImmediate(large);UnityEngine.Object.DestroyImmediate(small);
            }
        }
    }
}
