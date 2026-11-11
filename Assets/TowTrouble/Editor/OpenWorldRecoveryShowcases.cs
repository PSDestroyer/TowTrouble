using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble.Editor
{
 public static partial class OpenWorldAuthoring
 {
  private const string CountyShowcaseEvidence="Docs/Validation/RecoveryShowcases";
  private const string CountyShowcaseRoot="Recovery showcases - biome compositions";
  private static readonly string[] ShowcaseStories={"Workshop meadow","Logging ruts","Silt hollow","Ranger ravine","Fishing bank","Reed pocket","Boulder shore","Secluded cove","Gravel turnout","Scree slide","Rocky ramp","High switchback","Snow tongue","Icy timber ditch","Wind packed snow","Summit refuge","Beach entrance","Driftwood ledge","Wet sand shelf","Dune crest","Dry wash","Deep dunes","Canyon bank","Desert breakdown"};
  [MenuItem("Tow Trouble/Art Integration/Polish Remaining Recovery Showcases")]
  public static void PolishRemainingRecoveryShowcases()
  {
   VisualContext();ScenicPaths();var scene=EditorSceneManager.GetActiveScene();
   if(scene.path!="Assets/TowTrouble/Scenes/OpenWorldRecovery.unity")throw new InvalidOperationException("OpenWorldRecovery required");
   var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;
   if(map.Sites.Length!=24)throw new InvalidOperationException("Expected the authored 24 recoveries");
   string before=EditorJsonUtility.ToJson(map);var data=terrain.terrainData;
   const string backup="/private/tmp/TowRecoveryShowcasesBackup";Directory.CreateDirectory(backup);
   if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);
   if(!File.Exists(backup+"/Terrain.asset"))File.Copy(AssetDatabase.GetAssetPath(data),backup+"/Terrain.asset");
   Directory.CreateDirectory(CountyShowcaseEvidence);
   Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Polish biome recovery locations");
   var old=environment.Find(CountyShowcaseRoot);if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
   var root=new GameObject(CountyShowcaseRoot).transform;root.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Recovery showcase compositions");
   Undo.RegisterCompleteObjectUndo(data,"Local recovery ground blending");
   var reports=new List<object>();
   for(int index=1;index<map.Sites.Length;index++)
   {
    var site=map.Sites[index];var forward=Flat(site.ExitDirection).normalized;var right=Vector3.Cross(Vector3.up,forward);
    var group=new GameObject(site.Id+" - "+ShowcaseStories[index]).transform;group.SetParent(root,false);group.position=site.Position;
    int variation=index%4;var random=new System.Random(7301+index*97);
    Vector3 Point(float side,float along)=>site.Position+right*side+forward*along;
    bool Clear(Vector3 p,float radius)
    {
     var delta=p-site.Position;float x=Vector3.Dot(delta,right),z=Vector3.Dot(delta,forward);
     if(Mathf.Abs(x)<4.2f+radius&&z>-9-radius&&z<26+radius)return false;
     if(ScenicDistance(p,out _,out var road)<road.Width+radius+1.8f)return false;
     if(map.Sites.Any(s=>s.Id!=site.Id&&Flat(s.Position-p).magnitude<12+radius))return false;
     // Native ground placement, never populate lake beds with terrestrial foliage.
     if(site.Biome==1&&Ground(p).y<11.65f)return false;
     if(data.GetSteepness(Mathf.Clamp01((p.x-terrain.transform.position.x)/data.size.x),Mathf.Clamp01((p.z-terrain.transform.position.z)/data.size.z))>48)return false;
     return true;
    }
    GameObject Prop(string prefab,float x,float z,float size,bool tree=false)
    {
     var point=Point(x,z);float radius=tree?2:size*.6f;if(!Clear(point,radius))return null;
     var go=ForestProp(prefab,point,size,random.Next(360),group,false,tree);
     // Some source rock prefabs have disabled renderers; normalize the instance before LOD selection.
     foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))renderer.enabled=true;
     // Keep a single medium native mesh set; all decorative objects are distance culled.
     foreach(var lod in go.GetComponentsInChildren<LODGroup>(true))
     {
      var levels=lod.GetLODs();if(levels.Length>0){var chosen=levels[Mathf.Min(1,levels.Length-1)].renderers;if(chosen.Length==0)chosen=levels[0].renderers;foreach(var r in lod.GetComponentsInChildren<Renderer>(true))r.enabled=chosen.Contains(r);}
      UnityEngine.Object.DestroyImmediate(lod);
     }
     foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=ShadowCastingMode.Off;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;}
     var visible=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
     if(visible.Length>0){var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(tree?.025f:size>2?.018f:.012f,visible)});lod.RecalculateBounds();}
     return go;
    }
    // Different arrangement per incident: a rear crescent, staggered banks or an open island.
    float shift=(variation-1.5f)*1.4f;
    var islands=new[]{new Vector2(-8-shift,-5-variation),new Vector2(9+shift,-9+variation),new Vector2(variation%2==0?-11:12,7+variation)};
    if(site.Kind==RecoverySituationKind.Water)islands=new[]{new Vector2(-8,9),new Vector2(9,13),new Vector2(-12,18)};
    string rock=site.Biome==5?"P_RockDesert_Rock05":"P_RockSpire_Rock01";
    string plant=site.Biome==1?"P_Marshtail03":site.Biome==3?"P_BushSnowDead01":site.Biome>=4?"P_GrassDesert1_Paintable":"P_Grass2";
    string bush=site.Biome==3?"P_Bush1_Snowy":site.Biome==5?"P_BushLeafy01_Desert":site.Biome==4?"P_Bush1_Dry":"P_Fern2";
    for(int island=0;island<islands.Length;island++)
    {
     var c=islands[island];
     if(site.Biome==3)Prop("P_SnowDrift03",c.x,c.y,2.4f+island*.5f);
     else Prop(rock,c.x,c.y,(site.Biome==2||site.Biome==5?2.7f:1.5f)+island*.3f);
     for(int j=0;j<(site.Biome==3?5:9);j++)
     {
      float a=(float)random.NextDouble()*Mathf.PI*2,r=.9f+Mathf.Sqrt((float)random.NextDouble())*2.2f;
      Prop(j%4==0?bush:plant,c.x+Mathf.Cos(a)*r,c.y+Mathf.Sin(a)*r,j%4==0?.85f:site.Biome==1?.65f:.5f);
     }
     for(int j=0;j<3;j++)Prop(rock,c.x+(j-1)*.85f,c.y+1.2f+j*.3f,.22f+j*.18f);
    }
    // Biome-specific landmarks describe the incident without new gameplay obstacles.
    if(site.Biome==0){Prop("P_FirFallen2",-8,-10,4);Prop("P_FirStump1",-10,-7,1.1f);Prop(variation%2==0?"P_BirchTree2":"P_FirTree1",9,-15,7,true);}
    if(site.Biome==1){Prop("P_OakFallen1",variation%2==0?-9:9,site.Kind==RecoverySituationKind.Water?12:-6,3.3f);Prop("P_RockSpire_RockClump01",-12,site.Kind==RecoverySituationKind.Water?18:3,3.1f);}
    if(site.Biome==2){Prop("P_RockSpire_RockClump01",-9,-11,4);Prop("P_RockSpire_RockClump01",11,-5,3);Prop("P_FirStump1",-9,8,1);}
    if(site.Biome==3){Prop("P_OakFallen1_SnowCover",-9,-12,3.8f);Prop("P_SnowDrift03",9,-14,4);}
    if(site.Biome==4){Prop("P_PalmFallen01",-9,-10,3.8f);Prop("P_PalmStump01",10,-12,1.1f);if(variation!=3)Prop("P_PalmTree03",12,-16,6.5f,true);}
    if(site.Biome==5){Prop("P_RockDesert_Clump01",-10,-12,4);Prop("P_Cactus03",10,-10,1.8f);Prop("P_CactusPricklyPear01",-11,5,1.1f);}
    BlendShowcaseGround(site,forward,right);
    var rs=group.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
    reports.Add(new{site.Id,story=ShowcaseStories[index],site.Biome,objects=group.childCount,renderers=rs.Length,triangles=rs.OfType<MeshRenderer>().Sum(r=>r.GetComponent<MeshFilter>()?.sharedMesh?.triangles.Length/3??0),colliders=group.GetComponentsInChildren<Collider>(true).Length});
   }
   if(before!=EditorJsonUtility.ToJson(map))throw new InvalidOperationException("Recovery map changed unexpectedly");
   EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
   File.WriteAllText(CountyShowcaseEvidence+"/Locations.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
  }
  private static void BlendShowcaseGround(RecoverySite site,Vector3 forward,Vector3 right)
  {
   var data=terrain.terrainData;int n=data.alphamapResolution,layers=data.alphamapLayers;
   int x=Mathf.Clamp(Mathf.FloorToInt((site.Position.x-terrain.transform.position.x-21)/data.size.x*n),0,n-1),z=Mathf.Clamp(Mathf.FloorToInt((site.Position.z-terrain.transform.position.z-21)/data.size.z*n),0,n-1);
   int w=Mathf.Min(n-x,Mathf.CeilToInt(42/data.size.x*n)+2),h=Mathf.Min(n-z,Mathf.CeilToInt(42/data.size.z*n)+2);
   string file=CountyShowcaseEvidence+"/"+site.Id+"GroundBaseline.json";R01PaintBaseline baseline;
   if(File.Exists(file))baseline=JsonUtility.FromJson<R01PaintBaseline>(File.ReadAllText(file));
   else{baseline=new R01PaintBaseline{X=x,Z=z,W=w,H=h,Layers=layers,Values=new float[w*h*layers]};int i=0;foreach(float f in data.GetAlphamaps(x,z,w,h))baseline.Values[i++]=f;File.WriteAllText(file,JsonUtility.ToJson(baseline));}
   if(baseline.X!=x||baseline.Z!=z||baseline.W!=w||baseline.H!=h||baseline.Layers!=layers)throw new InvalidOperationException("Review terrain baseline for "+site.Id);
   var paint=new float[h,w,layers];int j=0;for(int a=0;a<h;a++)for(int b=0;b<w;b++)for(int k=0;k<layers;k++)paint[a,b,k]=baseline.Values[j++];
   for(int a=0;a<h;a++)for(int b=0;b<w;b++)
   {
    var p=terrain.transform.position+new Vector3((x+b)/(float)(n-1)*data.size.x,0,(z+a)/(float)(n-1)*data.size.z);var d=p-site.Position;
    float side=Vector3.Dot(d,right),along=Vector3.Dot(d,forward),noise=Mathf.PerlinNoise(p.x*.31f,p.z*.31f),r=Mathf.Sqrt(side*side/196+along*along/256);
    float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1.12f,r+(noise-.5f)*.22f)))*.42f;
    if(blend<=0||ScenicDistance(p,out _,out var road)<road.Width+1.8f||site.Biome==1&&Ground(p).y<11.65f)continue;
    // Match the existing biome; preserve the bright snow/sand palettes and softly expose earth/rock.
    int main=site.Biome==3?4:site.Biome==4?2:site.Biome==5?5:0;
    int detail=site.Biome==0?6:site.Biome==1?1:3;
    float detailWeight=site.Biome==3?.06f:site.Biome>=4?.08f:.25f+noise*.18f;
    for(int k=0;k<layers;k++)paint[a,b,k]=Mathf.Lerp(paint[a,b,k],k==main?1-detailWeight:k==detail?detailWeight:0,blend);
   }
   data.SetAlphamaps(x,z,paint);
  }
 }
}
