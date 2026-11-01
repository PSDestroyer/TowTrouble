using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace TowTrouble.Editor
{
    public static class GarageThumbnailAuthoring
    {
        private const string Folder="Assets/TowTrouble/Garage/Thumbnails";
        private const int Width=248,Height=144,Pad=4,Columns=8,Rows=13,Sheet=2048;
        private struct Placement { public string Key;public int Page,Cell; }
        // Native assemblies face +Z; wheel rim faces point toward -X. Back/side
        // accessories need their exposed surface, rather than the same world camera.
        private static Vector3 ViewDirection(GaragePart part)
        {
            switch(part.Slot)
            {
                case GarageSlot.Wheels:return new Vector3(-2.4f,.55f,1f);
                case GarageSlot.RearBumper:return new Vector3(-1.1f,.65f,-2.5f);
                case GarageSlot.FrontBumper:return new Vector3(-1.1f,.65f,2.5f);
                case GarageSlot.Fenders:
                    return new Vector3(part.Prefabs[0].name.Contains("_Right")?2f:-2f,1.2f,1.5f);
                case GarageSlot.Exhaust:
                    return new Vector3(part.Prefabs[0].name.Contains("_Right")?2f:-2f,.7f,1.2f);
                case GarageSlot.Roof:return new Vector3(-1.4f,1.45f,1.8f);
                case GarageSlot.Chassis:return new Vector3(-1.4f,1.35f,1.8f);
                case GarageSlot.Intake:return new Vector3(-1.4f,1.1f,1.8f);
                default:return new Vector3(-1.4f,.8f,1.8f);
            }
        }
        [MenuItem("Tow Trouble/Garage/Bake Part Thumbnails")]
        public static void Bake()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only");
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>(GarageAuthoring.CatalogPath);
            var groups=catalog.Parts.Where(p=>p.Prefabs!=null&&p.Prefabs.Length>0&&p.Prefabs[0]!=null).GroupBy(p=>AssetDatabase.GetAssetPath(p.Prefabs[0])).OrderBy(g=>g.Key,StringComparer.Ordinal).ToArray();
            if(groups.Any(g=>g.Select(p=>p.Slot).Distinct().Count()>1))throw new InvalidOperationException("A shared thumbnail needs a separate category view.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Isolated garage thumbnail studio");SceneManager.MoveGameObjectToScene(root,scene);
            var cameraGo=new GameObject("Thumbnail camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData));cameraGo.transform.SetParent(root.transform,false);
            var camera=cameraGo.GetComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.01f;camera.farClipPlane=100;camera.allowHDR=false;
            var lightGo=new GameObject("Thumbnail key light",typeof(Light));lightGo.transform.SetParent(root.transform,false);var light=lightGo.GetComponent<Light>();light.type=LightType.Directional;light.intensity=2f;light.transform.rotation=Quaternion.Euler(38,-35,0);
            var fillGo=new GameObject("Thumbnail fill",typeof(Light));fillGo.transform.SetParent(root.transform,false);var fill=fillGo.GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=.9f;fill.transform.rotation=Quaternion.Euler(25,145,0);
            var rt=new RenderTexture(Width,Height,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
            var pixels=new Texture2D(Width,Height,TextureFormat.RGBA32,false);var sheet=new Texture2D(Sheet,Sheet,TextureFormat.RGBA32,false);var clear=new Color32[Sheet*Sheet];var placements=new List<Placement>();var files=new List<string>();var views=new List<object>();var contactSheets=new Dictionary<GarageSlot,Texture2D>();var contactCounts=new Dictionary<GarageSlot,int>();int page=0,cell=0;
            var old=RenderTexture.active;GameObject instance=null;
            void SaveSheet(){sheet.Apply();string file=Folder+"/Parts-"+page.ToString("00")+".png";File.WriteAllBytes(file,sheet.EncodeToPNG());files.Add(file);}
            try
            {
                sheet.SetPixels32(clear);
                foreach(var group in groups)
                {
                    if(cell==Columns*Rows){SaveSheet();page++;cell=0;sheet.SetPixels32(clear);}
                    instance=(GameObject)PrefabUtility.InstantiatePrefab(group.First().Prefabs[0],scene);instance.transform.SetParent(root.transform,false);instance.transform.localPosition=Vector3.zero;instance.transform.localRotation=Quaternion.identity;
                    foreach(var behavior in instance.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(behavior);
                    foreach(var collider in instance.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                    foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                    var renderers=instance.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(renderers.Length==0)throw new InvalidOperationException("No thumbnail geometry: "+group.Key);
                    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    var part=group.First();var view=instance.transform.TransformVector(ViewDirection(part)).normalized;
                    camera.transform.position=bounds.center+view*Mathf.Max(3,bounds.size.magnitude*2);camera.transform.LookAt(bounds.center);
                    light.transform.rotation=Quaternion.LookRotation(-(Quaternion.AngleAxis(-25,Vector3.up)*view+Vector3.up*.5f));
                    fill.transform.rotation=Quaternion.LookRotation(-(Quaternion.AngleAxis(65,Vector3.up)*view+Vector3.up*.2f));
                    views.Add(new{key=group.Key,slot=part.Slot.ToString(),view=new[]{view.x,view.y,view.z},bounds=new[]{bounds.size.x,bounds.size.y,bounds.size.z}});
                    // Fit the rotated corners, including thin trim and long bumper kits.
                    float vx=0,vy=0;var ext=bounds.extents;
                    for(int corner=0;corner<8;corner++){var p=bounds.center+Vector3.Scale(ext,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));var local=camera.transform.InverseTransformPoint(p);vx=Mathf.Max(vx,Mathf.Abs(local.x));vy=Mathf.Max(vy,Mathf.Abs(local.y));}
                    camera.orthographicSize=Mathf.Max(.08f,Mathf.Max(vy,vx/(Width/(float)Height)))*1.08f;camera.farClipPlane=Mathf.Max(100,bounds.size.magnitude*5);
                    camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply();
                    if(!contactSheets.TryGetValue(part.Slot,out var contact))
                    {
                        int count=groups.Count(g=>g.First().Slot==part.Slot);contact=new Texture2D(Width*8,Height*((count+7)/8),TextureFormat.RGBA32,false);contactSheets[part.Slot]=contact;contactCounts[part.Slot]=0;
                        var background=Enumerable.Repeat(new Color32(31,37,39,255),contact.width*contact.height).ToArray();contact.SetPixels32(background);
                    }
                    int tile=contactCounts[part.Slot]++;var color=pixels.GetPixels32();
                    for(int i=0;i<color.Length;i++){float a=color[i].a/255f;color[i]=new Color32((byte)(color[i].r*a+31*(1-a)),(byte)(color[i].g*a+37*(1-a)),(byte)(color[i].b*a+39*(1-a)),255);}
                    contact.SetPixels32(tile%8*Width,contact.height-(tile/8+1)*Height,Width,Height,color);
                    int x=cell%Columns*(Width+Pad*2)+Pad,y=cell/Columns*(Height+Pad*2)+Pad;sheet.SetPixels32(x,y,Width,Height,pixels.GetPixels32());placements.Add(new Placement{Key=group.Key,Page=page,Cell=cell});cell++;
                    UnityEngine.Object.DestroyImmediate(instance);instance=null;
                }
                if(cell>0)SaveSheet();
                Directory.CreateDirectory("Docs/Validation/GarageThumbnailViews");
                foreach(var contact in contactSheets){contact.Value.Apply();File.WriteAllBytes("Docs/Validation/GarageThumbnailViews/"+contact.Key+"-After.png",contact.Value.EncodeToPNG());}
                File.WriteAllText("Docs/Validation/GarageThumbnailViews/Views.json",Newtonsoft.Json.JsonConvert.SerializeObject(views,Newtonsoft.Json.Formatting.Indented));
            }
            finally
            {RenderTexture.active=old;if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(sheet);rt.Release();UnityEngine.Object.DestroyImmediate(rt);foreach(var contact in contactSheets.Values)UnityEngine.Object.DestroyImmediate(contact);EditorSceneManager.ClosePreviewScene(scene);}
            var textures=new List<Texture2D>();
            foreach(var file in files)
            {
                AssetDatabase.ImportAsset(file);var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=Sheet;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();textures.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(file));
            }
            Undo.RecordObject(catalog,"Assign cached garage thumbnail atlases");
            foreach(var placement in placements)
            {
                var group=groups.Single(g=>g.Key==placement.Key);int x=placement.Cell%Columns*(Width+Pad*2)+Pad,y=placement.Cell/Columns*(Height+Pad*2)+Pad;
                foreach(var part in group){part.Thumbnail=textures[placement.Page];part.ThumbnailUV=new Rect(x/(float)Sheet,y/(float)Sheet,Width/(float)Sheet,Height/(float)Sheet);}
            }
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            Directory.CreateDirectory("Docs/Validation/GarageGrid");File.WriteAllText("Docs/Validation/GarageGrid/ThumbnailBudget.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{items=catalog.Parts.Length,visualItems=catalog.Parts.Count(p=>p.Thumbnail!=null),uniqueRenders=groups.Length,atlases=files.Count,atlasSize=Sheet,runtimeCameras=0},Newtonsoft.Json.Formatting.Indented));
        }
    }
}
