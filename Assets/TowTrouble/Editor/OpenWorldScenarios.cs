using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace TowTrouble.Editor
{
 public static partial class OpenWorldAuthoring
 {
  [MenuItem("Tow Trouble/Art Integration/11 - Dress Recovery Scenarios")]
  public static void DressRecoveryScenarios()
  {
   VisualContext();var map=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
   string backup="/Users/gheorghe/Desktop/TowTrublw/Backups/RecoveryScenarios-20261031";Directory.CreateDirectory(backup);
   string terrainPath=AssetDatabase.GetAssetPath(terrain.terrainData);string terrainBackup=backup+"/Terrain-before.asset";if(!File.Exists(terrainBackup))File.Copy(terrainPath,terrainBackup);
   var old=environment.Find("Recovery scenario dressing");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
   var parent=new GameObject("Recovery scenario dressing").transform;parent.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(parent.gameObject,"Dress recovery scenes");
   Undo.RecordObject(map,"Recovery situations");Undo.RecordObject(world,"Stage tow obstacles");Undo.RegisterCompleteObjectUndo(terrain.terrainData,"Recovery pockets");
   var data=terrain.terrainData;int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);var origin=terrain.transform.position;var size=data.size;
   string[] situations={"Roți îngropate în făgașe; trage progresiv spre potecă.","Mașina este în mâl; caută sprijin pe malul tare.","Mașina este înclinată între stânci; aliniază cablul înainte să urci.","Mașina este prinsă în șanțul de zăpadă; evită tracțiunea laterală.","Punte afundată în nisip; păstrează un impuls constant.","Mașina a alunecat în vadul uscat; extrage-o spre rampa liberă."};
   string[] english={"Wheels buried in ruts; pull progressively toward the trail.","Car sunk in silt; pull from firm bank ground.","Car tilted among rocks; align the cable before climbing.","Car trapped in a snow ditch; avoid sideways pulling.","Axle sunk in sand; keep a steady pull.","Car slipped into a dry wash; recover toward the clear ramp."};
   var logs=new List<RecoveryTowObstacle>();var report=new List<object>();
   for(int index=0;index<map.Sites.Length;index++)
   {
    var s=map.Sites[index];var source=plan.Sites.Single(p=>p.Id==s.Id);var toward=Flat(nodes[source.Node].Point-s.Position).normalized;var side=Vector3.Cross(Vector3.up,toward);int variation=index%4;
    s.Depth=(s.Biome==2?.12f:.16f)+s.Tier*.105f+(variation==2?.10f:0);s.Pitch=variation==1?-7:variation==2?8:2;s.Roll=(variation%2==0?1:-1)*(s.Biome==2?9+s.Tier:3+s.Tier);s.Resistance=350+s.Tier*390+(s.Biome==1?350:0);s.ExitDirection=toward;
    s.Heading=Quaternion.LookRotation(toward).eulerAngles.y+new[]{0,-18,24,-32}[variation];s.Situation=situations[s.Biome];s.EnglishSituation=english[s.Biome];
    float baseHeight=s.Position.y-.12f;
    // Local smooth bowl with a longer escape ramp. Uses a fixed authored height for repeatability.
    int cx=Mathf.RoundToInt((s.Position.x-origin.x)/size.x*(n-1)),cz=Mathf.RoundToInt((s.Position.z-origin.z)/size.z*(n-1));int radius=Mathf.CeilToInt(10/size.x*(n-1));
    for(int z=Mathf.Max(0,cz-radius);z<=Mathf.Min(n-1,cz+radius);z++)for(int x=Mathf.Max(0,cx-radius);x<=Mathf.Min(n-1,cx+radius);x++)
    {
     var delta=new Vector3(origin.x+x/(float)(n-1)*size.x-s.Position.x,0,origin.z+z/(float)(n-1)*size.z-s.Position.z);float lateral=Vector3.Dot(delta,side),longitudinal=Vector3.Dot(delta,toward);
     float r=Mathf.Sqrt(lateral*lateral/16+longitudinal*longitudinal/(longitudinal>0?64:25));if(r>=1)continue;
     float desired=(baseHeight-s.Depth*(1-Mathf.SmoothStep(0,1,r))-origin.y)/size.y;
     heights[z,x]=Mathf.Min(heights[z,x],desired);
    }
    var group=new GameObject(s.Id+" - "+s.Name).transform;group.SetParent(parent,false);
    string rock=s.Biome==5?"P_RockDesert_Rock01":"P_RockSpire_Rock01";
    // Side clusters frame each site while preserving a central 5 m extraction corridor.
    for(int j=0;j<7+variation*2;j++)
    {
     float sign=j%2==0?1:-1;var point=s.Position+side*sign*(4.2f+(j%3)*1.05f)+toward*((j/2-2)*2.1f);
     if(Flat(map.NearestRoad(point)-point).magnitude<7)continue;
     PlaceScenarioProp(rock,point,(j<2?1.8f:.55f)+(s.Tier*.12f),index*23+j*71,group,j<2);
    }
    string accent=s.Biome==3?"P_OakFallen1_SnowCover":s.Biome==4?"P_PalmStump01":s.Biome==5?"P_RockDesert_Clump01":"P_FirStump1";
    PlaceScenarioProp(accent,s.Position-side*5.6f-toward*4,1.2f+variation*.13f,index*37,group,true);
    bool log=index==1||index==3||index==5||index==13||index==17;
    if(log)
    {
     var p=s.Position+toward*6.5f;var root=new GameObject(s.Id+" - tow fallen tree",typeof(Rigidbody),typeof(BoxCollider),typeof(RecoveryTowObstacle));root.transform.SetParent(group,false);root.transform.position=Ground(p)+Vector3.up*.15f;
     string native=s.Biome==3?"P_OakFallen1_SnowCover":s.Biome==4?"P_PalmFallen01":"P_FirFallen1";
     var visual=PlaceScenarioProp(native,root.transform.position,5.7f,s.Heading+90,root.transform,false);
     if(visual==null)throw new InvalidOperationException("Missing native fallen tree");
     var bounds=ScenarioBounds(visual);var shift=root.transform.position+Vector3.up*.3f-bounds.center;visual.transform.position+=shift;bounds=ScenarioBounds(visual);
     var box=root.GetComponent<BoxCollider>();box.center=root.transform.InverseTransformPoint(bounds.center);box.size=new Vector3(Mathf.Max(.3f,bounds.size.x),Mathf.Min(.65f,bounds.size.y),Mathf.Max(.3f,bounds.size.z));
     root.layer=8;var body=root.GetComponent<Rigidbody>();body.isKinematic=true;body.mass=310+s.Tier*90;body.linearDamping=1.6f;body.angularDamping=3;body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;
     var obstacle=root.GetComponent<RecoveryTowObstacle>();obstacle.SiteIndex=index;obstacle.Body=body;obstacle.Home=root.transform.position;obstacle.HomeRotation=root.transform.rotation;obstacle.Side=side;logs.Add(obstacle);
     s.Situation="1. Trage copacul căzut în lateral pentru a elibera accesul. 2. "+s.Situation;s.EnglishSituation="1. Pull the fallen tree aside to clear access. 2. "+s.EnglishSituation;
    }
    report.Add(new{id=s.Id,s.Name,s.Biome,s.Tier,s.Depth,s.Pitch,s.Roll,s.Resistance,logFirst=log,exit=new{x=toward.x,y=toward.y,z=toward.z},props=group.childCount});
   }
   data.SetHeights(0,0,heights);EditorUtility.SetDirty(data);FitScenarioMud();world.ConfigureObstacles(logs.ToArray());
   // Invalidate cached contracts after the serialized scene data changes.
   var serialized=new SerializedObject(map);var roads=serialized.FindProperty("roadCheckpoints");var checkpoints=new Vector3[roads.arraySize];for(int i=0;i<checkpoints.Length;i++)checkpoints[i]=roads.GetArrayElementAtIndex(i).vector3Value;map.Configure(map.Sites,map.Deliveries,checkpoints);
   EditorUtility.SetDirty(map);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());Physics.SyncTransforms();
   Directory.CreateDirectory("Docs/Validation/RecoveryScenarios");File.WriteAllText("Docs/Validation/RecoveryScenarios/Locations.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
  }
  public static void FitScenarioMud()
  {
   VisualContext();
   foreach(var f in environment.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.EndsWith(" - Living Mud")))
   {
    var mesh=f.sharedMesh;string path=AssetDatabase.GetAssetPath(mesh);string backup="/Users/gheorghe/Desktop/TowTrublw/Backups/RecoveryScenarios-20261031/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);
    Undo.RegisterCompleteObjectUndo(mesh,"Fit mud to recovery pit");var vs=mesh.vertices;for(int i=0;i<vs.Length;i++){var p=f.transform.TransformPoint(vs[i]);p.y=Ground(p).y+.07f;vs[i]=f.transform.InverseTransformPoint(p);}mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateBounds();f.GetComponent<MeshCollider>().sharedMesh=null;f.GetComponent<MeshCollider>().sharedMesh=mesh;EditorUtility.SetDirty(mesh);
   }
   AssetDatabase.SaveAssets();
  }
  private static Bounds ScenarioBounds(GameObject go)
  {var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
  private static GameObject PlaceScenarioProp(string name,Vector3 position,float width,float yaw,Transform parent,bool collide)
  {
   string path=PrefabPath(name);if(path==null)return null;var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
   go.transform.rotation=Quaternion.Euler(0,yaw,0);var b=ScenarioBounds(go);go.transform.localScale*=width/Mathf.Max(b.size.x,b.size.z);b=ScenarioBounds(go);go.transform.position+=Ground(position)-new Vector3(b.center.x,b.min.y+.08f,b.center.z);
   foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
   if(collide){var box=go.AddComponent<BoxCollider>();b=ScenarioBounds(go);box.center=go.transform.InverseTransformPoint(b.center);box.size=new Vector3(b.size.x/go.transform.lossyScale.x,b.size.y/go.transform.lossyScale.y,b.size.z/go.transform.lossyScale.z)*.8f;go.layer=8;}
   return go;
  }
 }
}
