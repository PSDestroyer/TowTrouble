using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace TowTrouble.Editor
{
 public static partial class OpenWorldAuthoring
 {
  [MenuItem("Tow Trouble/Art Integration/Create Natural County Boundaries")]
  public static void CreateCountyBoundaries()
  {
   VisualContext();var scene=EditorSceneManager.GetActiveScene();var data=terrain.terrainData;
   if(data.size.x!=1000||data.size.z!=1000)throw new InvalidOperationException("Review perimeter for this terrain size");
   var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
   var roads=environment.Find("Road network - 24 recovery spurs").GetComponentsInChildren<MeshFilter>();
   float roadLimit=roads.SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).Max(p=>Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.z)));
   if(roadLimit>420||world.Map.Sites.Any(s=>Mathf.Max(Mathf.Abs(s.Position.x),Mathf.Abs(s.Position.z))>420)||world.Map.Deliveries.Any(s=>Mathf.Max(Mathf.Abs(s.Position.x),Mathf.Abs(s.Position.z))>420))throw new InvalidOperationException("Perimeter overlaps playable content");
   const string evidence="Docs/Validation/Boundaries";Directory.CreateDirectory(evidence);
   const string backup="/private/tmp/TowBoundaryBackup";Directory.CreateDirectory(backup);
   if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);
   if(!File.Exists(backup+"/Terrain.asset"))File.Copy(AssetDatabase.GetAssetPath(data),backup+"/Terrain.asset");
   int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);var before=(float[,])heights.Clone();
   string baseline=evidence+"/HeightBaseline.bin";if(!File.Exists(baseline))using(var w=new BinaryWriter(File.Create(baseline))){w.Write(n);foreach(float h in heights)w.Write(h);}
   var source=new float[n,n];using(var r=new BinaryReader(File.OpenRead(baseline))){if(r.ReadInt32()!=n)throw new InvalidOperationException("Review boundary height baseline");for(int z=0;z<n;z++)for(int x=0;x<n;x++)source[z,x]=r.ReadSingle();}
   Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Natural map perimeter");Undo.RegisterCompleteObjectUndo(data,"Outer perimeter terrain");
   int changed=0;for(int z=0;z<n;z++)for(int x=0;x<n;x++)
   {
    float wx=-500+x/(float)(n-1)*1000,wz=-500+z/(float)(n-1)*1000,edge=Mathf.Max(Mathf.Abs(wx),Mathf.Abs(wz));if(edge<=440)continue;
    float noise=Mathf.PerlinNoise(wx*.018f+17,wz*.018f+8);float start=444+noise*8;
    float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,487,edge));float height=wz>380?48+20*noise:wx>380?32+20*noise:wz< -380?26+16*noise:36+16*noise;
    heights[z,x]=Mathf.Clamp01(source[z,x]+height*rise/data.size.y);if(heights[z,x]!=before[z,x])changed++;
   }
   data.SetHeights(0,0,heights);
   // Paint only the new outer slopes; the playable area and its road margins stay intact.
   int a=data.alphamapResolution;var paint=data.GetAlphamaps(0,0,a,a);var originalPaint=new float[a,a,data.alphamapLayers];
   string paintBaseline=evidence+"/PaintBaseline.bin";if(!File.Exists(paintBaseline))using(var w=new BinaryWriter(File.Create(paintBaseline))){w.Write(a);w.Write(data.alphamapLayers);foreach(float v in paint)w.Write(v);}
   using(var r=new BinaryReader(File.OpenRead(paintBaseline))){if(r.ReadInt32()!=a||r.ReadInt32()!=data.alphamapLayers)throw new InvalidOperationException("Review boundary paint baseline");for(int z=0;z<a;z++)for(int x=0;x<a;x++)for(int k=0;k<data.alphamapLayers;k++)originalPaint[z,x,k]=r.ReadSingle();}
   for(int z=0;z<a;z++)for(int x=0;x<a;x++)
   {
    float wx=-500+x/(float)(a-1)*1000,wz=-500+z/(float)(a-1)*1000,edge=Mathf.Max(Mathf.Abs(wx),Mathf.Abs(wz));if(edge<=452)continue;
    float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(452,475,edge));int layer=wz>380?4:wx>380?5:wz< -380?2:3;
    // Stable assignment at the outer edge; do not repeatedly accumulate blends on rerun.
    float rock=t*.85f;for(int k=0;k<data.alphamapLayers;k++)paint[z,x,k]=Mathf.Lerp(originalPaint[z,x,k],k==layer?1:0,rock);
   }
   data.SetAlphamaps(0,0,paint);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
   const string rootName="County limits - natural perimeter";var old=environment.Find(rootName);if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
   var root=new GameObject(rootName).transform;root.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"County perimeter");
   for(int side=0;side<4;side++)
   {
    var group=new GameObject(new[]{"North snowy ridge","East sandstone escarpment","South coastal headlands","West forest ridge"}[side]).transform;group.SetParent(root,false);
    for(int i=0;i<24;i++)
    {
     float along=-470+i*(940f/23),offset=498+Mathf.Sin(i*2.7f+side);
     var p=side==0?new Vector3(along,0,offset):side==1?new Vector3(offset,0,along):side==2?new Vector3(along,0,-offset):new Vector3(-offset,0,along);
     bool desert=side==1||(side==2&&along>150);string prefab=desert?"P_RockDesert_Clump01":"P_RockSpire_RockClump01";
     var go=ForestProp(prefab,p,26+(i%4)*4,i*137+side*41,group,false,false);go.name="Boundary formation "+i;go.transform.position-=Vector3.up*3;
     foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))renderer.enabled=true;
     foreach(var lod in go.GetComponentsInChildren<LODGroup>(true)){var levels=lod.GetLODs();if(levels.Length>0){var chosen=levels[Mathf.Min(1,levels.Length-1)].renderers;foreach(var r in lod.GetComponentsInChildren<Renderer>(true))r.enabled=chosen.Contains(r);}UnityEngine.Object.DestroyImmediate(lod);}
     var visible=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
     foreach(var r in visible){r.shadowCastingMode=ShadowCastingMode.Off;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;}
     // Seat the whole footprint into the slope, not only the centre point.
     go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(1,1.5f,1));
     var bounds=ScenarioBounds(go);float low=float.MaxValue;
     for(int sx=0;sx<3;sx++)for(int sz=0;sz<3;sz++){var sample=new Vector3(Mathf.Lerp(bounds.min.x,bounds.max.x,sx*.5f),0,Mathf.Lerp(bounds.min.z,bounds.max.z,sz*.5f));low=Mathf.Min(low,Ground(sample).y);}
     go.transform.position+=Vector3.up*(low-bounds.min.y-1);
     var distanceLod=go.AddComponent<LODGroup>();distanceLod.SetLODs(new[]{new LOD(.006f,visible)});distanceLod.RecalculateBounds();
    }
    // Four cheap static safety colliders sit behind the terrain ridge, below its back face.
    var wall=new GameObject("Hidden outer safety barrier");wall.transform.SetParent(group,false);wall.layer=8;
    wall.transform.position=side==0?new Vector3(0,250,494):side==1?new Vector3(494,250,0):side==2?new Vector3(0,250,-494):new Vector3(-494,250,0);
    var box=wall.AddComponent<BoxCollider>();box.size=side%2==0?new Vector3(1010,700,12):new Vector3(12,700,1010);
   }
   bool innerUnchanged=true;for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(Mathf.Max(Mathf.Abs(-500+x/(float)(n-1)*1000),Mathf.Abs(-500+z/(float)(n-1)*1000))<=440&&heights[z,x]!=before[z,x])innerUnchanged=false;
   if(!innerUnchanged)throw new InvalidOperationException("Playable terrain changed");
   var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();long triangles=renderers.Sum(r=>(long)r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);
   Physics.SyncTransforms();int hits=0;foreach(var box in root.GetComponentsInChildren<BoxCollider>())for(int j=-4;j<=4;j++){var c=box.transform.position;bool northSouth=box.size.x>box.size.z;var origin=northSouth?new Vector3(j*115,450,0):new Vector3(0,450,j*115);var dir=northSouth?new Vector3(0,0,Mathf.Sign(c.z)):new Vector3(Mathf.Sign(c.x),0,0);if(box.Raycast(new Ray(origin,dir),out _,510))hits++;}
   if(hits!=36)throw new InvalidOperationException("Boundary continuity failed");
   File.WriteAllText(evidence+"/Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{innerUnchanged,roadLimit,terrainSamplesChanged=changed,formations=96,renderers=renderers.Length,triangles,colliders=4,boundaryRayHits=hits,newLights=0,newRuntimeComponents=0},Newtonsoft.Json.Formatting.Indented));
   EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
  }
 }
}
