using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace TowTrouble.Editor
{
 public static partial class OpenWorldAuthoring
 {
  [MenuItem("Tow Trouble/Art Integration/12 - Finish Workshop Forest Clearing")]
  public static void FinishWorkshopForest()
  {
   VisualContext();ScenicPaths();var map=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();var s=map.Sites.Single(p=>p.Id=="R01");
   string backup="/Users/gheorghe/Desktop/TowTrublw/Backups/ForestClearing-20261101";Directory.CreateDirectory(backup);
   var scene=EditorSceneManager.GetActiveScene();if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);
   var data=terrain.terrainData;string baselinePath=VisualRoot+"/Forest clearing source terrain.asset";
   if(AssetDatabase.LoadAssetAtPath<TerrainData>(baselinePath)==null)AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data),baselinePath);
   var baseline=AssetDatabase.LoadAssetAtPath<TerrainData>(baselinePath);
   var old=environment.Find("Workshop forest - finished clearing");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
   var root=new GameObject("Workshop forest - finished clearing").transform;root.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"Finish workshop forest");
   var generic=environment.Find("Recovery scenario dressing/"+s.Id+" - "+s.Name);if(generic!=null){Undo.RecordObject(generic.gameObject,"Replace generic R01 dressing");generic.gameObject.SetActive(false);}
   Vector3 forward=s.ExitDirection.normalized,right=Vector3.Cross(Vector3.up,forward),center=s.Position;
   Func<float,float,Vector3> point=(x,z)=>center+right*x+forward*z;
   Func<Vector3,bool> roadClear=p=>ScenicDistance(p,out _,out var road)>road.Width+4;
   Undo.RegisterCompleteObjectUndo(data,"Forest clearing local ground finish");
   int n=data.alphamapResolution,layers=data.alphamapLayers;int x0=Mathf.Clamp(Mathf.FloorToInt((center.x-terrain.transform.position.x-35)/data.size.x*n),0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt((center.z-terrain.transform.position.z-35)/data.size.z*n),0,n-1);int span=Mathf.CeilToInt(70/data.size.x*n)+2;int w=Mathf.Min(span,n-x0),h=Mathf.Min(span,n-z0);
   // Native woodland soil blends into the existing grass; retain through-road paint exactly.
   var paint=data.GetAlphamaps(x0,z0,w,h);var sourcePaint=baseline.GetAlphamaps(x0,z0,w,h);
   for(int z=0;z<h;z++)for(int x=0;x<w;x++)
   {
    var p=new Vector3(terrain.transform.position.x+(x+x0)/(float)(n-1)*data.size.x,0,terrain.transform.position.z+(z+z0)/(float)(n-1)*data.size.z);var d=p-center;float a=Vector3.Dot(d,right),b=Vector3.Dot(d,forward);
    float oval=Mathf.Sqrt(a*a/400+b*b/625);float border=1-Smooth01(.67f,1.25f,oval+(ArtNoise(p.x,p.z,.12f)-.5f)*.36f);if(border<=0||!roadClear(p))continue;
    float path=(1-Smooth01(2.5f,5.4f,Mathf.Abs(a-Mathf.Sin(b*.18f)*.65f)))*(1-Smooth01(17,24,b));
    float soil=border*Mathf.Lerp(.32f,.70f,ArtNoise(p.x,p.z,.20f));soil=Mathf.Lerp(soil,.08f,path);
    float earth=border*path*.65f;float grass=1-soil-earth;
    for(int k=0;k<layers;k++)paint[z,x,k]=Mathf.Lerp(sourcePaint[z,x,k],k==0?grass:k==1?earth:k==6?soil:0,border);
   }
   data.SetAlphamaps(x0,z0,paint);EditorUtility.SetDirty(data);
   // Three uneven canopy islands leave an open sightline from the approach.
   var trees=new[]{new Vector2(-11,-9),new Vector2(-17,-14),new Vector2(-8,-18),new Vector2(1,-21),new Vector2(9,-17),new Vector2(14,-11),new Vector2(16,-2),new Vector2(-15,1),new Vector2(-12,10),new Vector2(12,12),new Vector2(-20,-4),new Vector2(20,-16),new Vector2(-7,-5),new Vector2(8,-2),new Vector2(-7,7),new Vector2(6,-11)};
   for(int i=0;i<trees.Length;i++)
   {
    var p=point(trees[i].x,trees[i].y);if(!roadClear(p))continue;var name=i%3==0?"P_BirchTreeB1":i%3==1?"P_BirchTree2":"P_FirTree1";if(PrefabPath(name)==null)name="P_BirchTree1";
    ForestProp(name,p,8.5f+i%4*1.1f,i*137.5f,root,true,true);
   }
   var rockCenters=new[]{new Vector2(-7,-5),new Vector2(8,-9),new Vector2(-10,8)};
   var random=new System.Random(417);
   foreach(var c in rockCenters)
   {
    ForestProp("P_RockSpire_RockClump01",point(c.x,c.y),3.5f+(float)random.NextDouble(),random.Next(360),root,true,false);
    for(int i=0;i<5;i++)ForestProp("P_RockSpire_Rock01",point(c.x+(float)random.NextDouble()*3-1.5f,c.y+(float)random.NextDouble()*3-1.5f),.22f+(float)random.NextDouble()*.55f,random.Next(360),root,false,false);
   }
   ForestProp("P_FirFallen2",point(7,-5),4.3f,s.Heading+54,root,true,false);ForestProp("P_FirStump1",point(9.3f,-7.2f),1.15f,17,root,true,false);
   // Clustered understory instead of evenly spaced individual plants.
   var islands=new[]{new Vector2(-7,-10),new Vector2(10,-6),new Vector2(-11,3),new Vector2(13,5),new Vector2(2,-17),new Vector2(-5,-18),new Vector2(-8,13)};
   foreach(var c in islands)for(int i=0;i<24;i++)
   {
    float a=(float)random.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt((float)random.NextDouble())*4;float x=c.x+Mathf.Cos(a)*r,z=c.y+Mathf.Sin(a)*r;var p=point(x,z);
    if(!roadClear(p)||Mathf.Abs(x)<4.4f&&z>-5&&z<25)continue;
    string name=i%5==0?"P_BushLeafy01":i%3==0?"P_Fern2":i%7==0?"P_FlowerBush01":"P_Grass2";
    ForestProp(name,p,i%5==0?1.9f:i%3==0?1.25f:.45f+(float)random.NextDouble()*.45f,random.Next(360),root,false,false);
   }
   // A short broken boundary tells the story without blocking the recovery corridor.
   var timber=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Weathered timber.mat");
   if(timber==null)timber=AssetDatabase.LoadAssetAtPath<Material>("Assets/TowTrouble/Materials/Weathered timber.mat");
   for(int i=0;i<3;i++)
   {
    var p=Ground(point(4.5f+i*.7f,5+i*2.2f));var post=Cube("Weathered fence post",p+Vector3.up*.55f,new Vector3(.17f,1.2f,.17f),timber,root,true);post.transform.rotation=Quaternion.Euler(i==0?23:0,s.Heading, i==0?-12:4);
    if(i>0){var rail=Cube("Split timber rail",p-forward*1.05f+Vector3.up*.8f,new Vector3(.12f,.16f,2.3f),timber,root,false);rail.transform.rotation=Quaternion.Euler(0,s.Heading,0);}
   }
   var broken=Cube("Fallen fence rail",Ground(point(4.1f,3.8f))+Vector3.up*.10f,new Vector3(.13f,.15f,2),timber,root,false);broken.transform.rotation=Quaternion.Euler(3,s.Heading+37,0);
   // Reclassify visual marks locally after painting; traction and contract data stay as authored.
   var grip=AssetDatabase.LoadAssetAtPath<RecoveryGroundMap>(VisualRoot+"/Painted ground grip.asset");Undo.RecordObject(grip,"Refresh forest visual track surface");BakeWheelTrackKinds(grip);ForestApproachRuts(center,forward,right,root,grip);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Physics.SyncTransforms();
   CaptureMapPhotosWhere(p=>Flat(p-center).magnitude<2);
  }
  private static void ForestApproachRuts(Vector3 center,Vector3 forward,Vector3 right,Transform parent,RecoveryGroundMap map)
  {
   foreach(var kind in new[]{RecoveryTrackKind.Dirt,RecoveryTrackKind.Grass})
   {
    var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var rut=new System.Collections.Generic.List<Vector2>();var caps=new System.Collections.Generic.List<Vector2>();var colors=new System.Collections.Generic.List<Color>();var triangles=new System.Collections.Generic.List<int>();
    const int rows=65,columns=7;
    for(int wheel=0;wheel<2;wheel++)for(int row=0;row<rows;row++)
    {
     float t=row/(float)(rows-1),along=Mathf.Lerp(20,-1,t);float drift=Mathf.Sin(t*Mathf.PI)*.65f;
     for(int c=0;c<columns;c++)
     {
      var p=Ground(center+forward*along+right*((wheel==0?-.78f:.78f)+drift+(c/6f*2-1)*.22f))+Vector3.up*.016f;
      map.TryTrackBlend(p,kind,out float weight,out _);vertices.Add(parent.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*(kind==RecoveryTrackKind.Dirt?.2f:.125f));rut.Add(new Vector2(c/6f,t*21));caps.Add(new Vector2((1-t)*21/.65f,1));colors.Add(new Color(1,1,1,c==0||c==6?0:weight*.58f));
      if(row<rows-1&&c<columns-1){int a=wheel*rows*columns+row*columns+c;triangles.AddRange(new[]{a,a+1,a+columns,a+1,a+columns+1,a+columns});}
     }
    }
    var mesh=new Mesh{name="R01 old tire path "+kind};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,rut);mesh.SetUVs(2,caps);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();string path=VisualRoot+"/R01 approach ruts "+kind+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
    var go=new GameObject("Old tire approach - "+kind,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Dynamic wheel tracks "+(int)kind+".mat");renderer.shadowCastingMode=ShadowCastingMode.Off;
   }
  }
  private static GameObject ForestProp(string name,Vector3 position,float size,float yaw,Transform parent,bool collision,bool tree)
  {
   string path=PrefabPath(name);if(path==null)throw new InvalidOperationException("Missing forest asset "+name);
   var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);go.transform.rotation=Quaternion.Euler(0,yaw,0);var b=ScenarioBounds(go);go.transform.localScale*=size/Mathf.Max(.01f,tree?b.size.y:Mathf.Max(b.size.x,b.size.z));b=ScenarioBounds(go);go.transform.position+=Ground(position)-new Vector3(b.center.x,b.min.y+(tree?.08f:.10f),b.center.z);
   foreach(var collider in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
   b=ScenarioBounds(go);
   if(collision)
   {
    go.layer=8;
    if(tree){var capsule=go.AddComponent<CapsuleCollider>();capsule.center=go.transform.InverseTransformPoint(new Vector3(b.center.x,Ground(position).y+size*.40f,b.center.z));capsule.radius=.22f/go.transform.lossyScale.x;capsule.height=size*.8f/go.transform.lossyScale.y;}
    else{var box=go.AddComponent<BoxCollider>();b=ScenarioBounds(go);box.center=go.transform.InverseTransformPoint(b.center);box.size=new Vector3(b.size.x/go.transform.lossyScale.x,b.size.y/go.transform.lossyScale.y,b.size.z/go.transform.lossyScale.z)*.65f;}
   }
   else foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=ShadowCastingMode.Off;
   return go;
  }
 }
}
