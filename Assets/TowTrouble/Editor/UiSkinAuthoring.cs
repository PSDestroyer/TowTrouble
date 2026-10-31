using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.SceneManagement;
namespace TowTrouble.Editor
{
    public static class UiSkinAuthoring
    {
        public const string Root="Assets/TowTrouble/Art/UI/Theme";
        [MenuItem("Tow Trouble/Apply Finished UI Skin")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before changing the UI skin.");
            AssetDatabase.Refresh();var body=FontAsset("TowText",72,2048);var display=FontAsset("TowDisplay",90,2048);
            if(!display.fallbackFontAssetTable.Contains(body))display.fallbackFontAssetTable.Insert(0,body);EditorUtility.SetDirty(display);
            var scene=SceneManager.GetSceneByPath(DemoBuilder.ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(DemoBuilder.ScenePath,OpenSceneMode.Additive);
            var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryHud>(true)).Single();hud.ConfigureTypography(body,display);hud.ConfigureImages(CaptureContracts(scene));EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void BindExisting(RecoveryHud hud)
        {
            var body=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/TowText SDF.asset");var display=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/TowDisplay SDF.asset");if(body!=null&&display!=null)hud.ConfigureTypography(body,display);
            var images=Enumerable.Range(1,3).Select(i=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Contracts/Recovery-"+i.ToString("00")+".png")).ToArray();if(images.All(t=>t!=null))hud.ConfigureImages(images);
        }
        public static GameObject PreviewEnvironment(Scene original,Scene preview)
        {
            var container=new GameObject("World for static UI preview");SceneManager.MoveGameObjectToScene(container,preview);
            var world=original.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();var data=new SerializedObject(world);var environment=(GameObject)data.FindProperty("environment").objectReferenceValue;
            foreach(var terrain in environment.GetComponentsInChildren<Terrain>(true))
            {
                if(!terrain.gameObject.activeInHierarchy)continue;var go=new GameObject("Terrain preview");go.transform.SetParent(container.transform,false);go.transform.position=terrain.transform.position;var copy=go.AddComponent<Terrain>();copy.terrainData=terrain.terrainData;copy.materialTemplate=terrain.materialTemplate;copy.treeDistance=terrain.treeDistance;copy.detailObjectDistance=80;copy.drawInstanced=terrain.drawInstanced;
            }
            var allowed=new System.Collections.Generic.HashSet<Renderer>();foreach(var group in environment.GetComponentsInChildren<LODGroup>()){var lod=group.GetLODs();if(lod.Length>0)foreach(var renderer in lod[0].renderers)allowed.Add(renderer);}
            foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null||!renderer.enabled)continue;var lod=renderer.GetComponentInParent<LODGroup>();if(lod!=null&&!allowed.Contains(renderer))continue;
                var go=new GameObject(renderer.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(container.transform,false);go.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);go.transform.localScale=renderer.transform.lossyScale;go.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var copy=go.GetComponent<MeshRenderer>();copy.sharedMaterials=renderer.sharedMaterials;copy.shadowCastingMode=renderer.shadowCastingMode;copy.receiveShadows=renderer.receiveShadows;
            }
            return container;
        }
        public static GameObject PreviewVehicle(RecoveryVehicle source,int model,Transform parent,Vector3 position)
        {
            var data=new SerializedObject(source);var template=(GameObject)data.FindProperty("models").GetArrayElementAtIndex(model).FindPropertyRelative("Root").objectReferenceValue;var visual=UnityEngine.Object.Instantiate(template,parent,false);visual.SetActive(true);visual.transform.SetPositionAndRotation(position,Quaternion.identity);return visual;
        }
        private static Texture2D[] CaptureContracts(Scene scene)
        {
            Directory.CreateDirectory(Root+"/Contracts");var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoveryWorld>(true)).Single();var results=new Texture2D[3];
            var preview=EditorSceneManager.NewPreviewScene();var root=PreviewEnvironment(scene,preview);var rt=new RenderTexture(1200,600,24);var pixels=new Texture2D(1200,600,TextureFormat.RGB24,false);var lineMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));lineMaterial.SetColor("_BaseColor",new Color(.055f,.065f,.075f));
            try
            {
                var cameraGo=new GameObject("Contract photo camera");cameraGo.transform.SetParent(root.transform,false);var camera=cameraGo.AddComponent<Camera>();camera.scene=preview;camera.targetTexture=rt;camera.fieldOfView=46;camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=900;camera.nearClipPlane=.2f;
                var sunGo=new GameObject("Contract photo sun");sunGo.transform.SetParent(root.transform,false);var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.color=new Color(1,.93f,.78f);sun.shadows=LightShadows.Soft;sunGo.transform.rotation=Quaternion.Euler(42,-28,0);
                for(int i=0;i<3;i++)
                {
                    var contract=RecoveryContract.Get(i);var point=contract.Target;var target=PreviewVehicle(world.Casualty,0,root.transform,point);var driverPoint=point+new Vector3(1,0,8);
                    if(Physics.Raycast(driverPoint+Vector3.up*20,Vector3.down,out var ground,40,1<<8,QueryTriggerInteraction.Ignore))driverPoint.y=ground.point.y+.05f;
                    var driver=PreviewVehicle(world.Drivers[0],0,root.transform,driverPoint);var cable=new GameObject("Static tow cable").AddComponent<LineRenderer>();cable.transform.SetParent(root.transform,false);cable.sharedMaterial=lineMaterial;cable.positionCount=12;cable.startWidth=cable.endWidth=.045f;
                    var a=point+new Vector3(0,.66f,2.25f);var b=driverPoint+new Vector3(0,.66f,-2.15f);for(int j=0;j<12;j++){float t=j/11f;cable.SetPosition(j,Vector3.Lerp(a,b,t)-Vector3.up*(.22f*4*t*(1-t)));}
                    GameObject mud=null;if(i==2)
                    {
                        var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).FirstOrDefault(f=>f.name=="Contract mud - Living Mud");
                        if(source!=null){mud=new GameObject("Mud preview",typeof(MeshFilter),typeof(MeshRenderer));mud.transform.SetParent(root.transform,false);mud.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);mud.transform.localScale=source.transform.lossyScale;mud.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;mud.GetComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;}
                    }
                    camera.transform.position=point+new Vector3(15,8,-14);camera.transform.LookAt(point+new Vector3(0,1.1f,4));camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1200,600),0,0);pixels.Apply();RenderTexture.active=previous;
                    string path=Root+"/Contracts/Recovery-"+(i+1).ToString("00")+".png";File.WriteAllBytes(path,pixels.EncodeToPNG());AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.SaveAndReimport();results[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(driver);UnityEngine.Object.DestroyImmediate(cable.gameObject);if(mud!=null)UnityEngine.Object.DestroyImmediate(mud);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(lineMaterial);UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(preview);}
            return results;
        }
        private static TMP_FontAsset FontAsset(string name,int size,int resolution)
        {
            string path=Root+"/Fonts/"+name+" SDF.asset";var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(asset!=null&&asset.material!=null&&asset.atlasTextures.Length>0&&asset.atlasTextures[0]!=null&&asset.characterTable.Count>0){if(asset.fallbackFontAssetTable==null)asset.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();return asset;}
            var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/"+name+".ttf");if(source==null)throw new InvalidOperationException("Missing UI font: "+name);
            asset=TMP_FontAsset.CreateFontAsset(source,size,8,GlyphRenderMode.SDFAA,resolution,resolution,AtlasPopulationMode.Dynamic);asset.name=name+" SDF";
            asset.hideFlags=HideFlags.None;asset.material.hideFlags=HideFlags.None;asset.atlasTextures[0].hideFlags=HideFlags.None;AssetDatabase.CreateAsset(asset,path);asset.atlasTextures[0].name=name+" Atlas";AssetDatabase.AddObjectToAsset(asset.atlasTextures[0],asset);AssetDatabase.AddObjectToAsset(asset.material,asset);
            string chars=string.Concat(Enumerable.Range(32,95).Select(c=>(char)c))+"ĂăÂâÎîȘșȚțŞşŢţéÉèàüöä–—•…→←×‹›©";
            asset.TryAddCharacters(chars,out var missing);
            File.WriteAllText(Root+"/Fonts/"+name+"-Fallback.txt",string.IsNullOrEmpty(missing)?"All requested glyphs are present.":"Optional symbols use the fallback font: "+missing);
            if(asset.fallbackFontAssetTable==null)asset.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
            asset.fallbackFontAssetTable.Add(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"));
            if(name=="TowDisplay")
            {
                asset.material.SetFloat(ShaderUtilities.ID_FaceDilate,.015f);asset.material.SetFloat(ShaderUtilities.ID_OutlineWidth,.045f);asset.material.SetColor(ShaderUtilities.ID_OutlineColor,new Color(.075f,.085f,.11f,.8f));
            }
            EditorUtility.SetDirty(asset.atlasTextures[0]);EditorUtility.SetDirty(asset.material);EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();return asset;
        }
    }
}
