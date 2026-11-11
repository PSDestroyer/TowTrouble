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
  private const string ShowcaseFolder="Assets/TowTrouble/OpenWorld/Visuals/R01 Showcase";
  [Serializable] private sealed class R01PaintBaseline { public int X,Z,W,H,Layers;public float[] Values; }
  [MenuItem("Tow Trouble/Art Integration/Polish R01 Showcase Only")]
  public static void PolishR01Showcase()
  {
   VisualContext();ScenicPaths();var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();
   var site=world.Map.Sites.Single(s=>s.Id=="R01");var scene=EditorSceneManager.GetActiveScene();
   var forest=environment.Find("Workshop forest - finished clearing");var rut=environment.Find("R01 visible wheel rut");
   if(forest==null||rut==null)throw new InvalidOperationException("R01 forest/extraction setup is required.");
   const string backup="/private/tmp/TowR01ShowcaseBackup";Directory.CreateDirectory(backup);
   if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);
   string terrainPath=AssetDatabase.GetAssetPath(terrain.terrainData);
   if(!File.Exists(backup+"/Terrain.asset"))File.Copy(terrainPath,backup+"/Terrain.asset");
   if(!AssetDatabase.IsValidFolder(ShowcaseFolder))AssetDatabase.CreateFolder(VisualRoot,"R01 Showcase");
   Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Polish R01 showcase locally");
   var exit=Vector3.ProjectOnPlane(site.ExitDirection,Vector3.up).normalized;var right=Vector3.Cross(Vector3.up,exit);
   Vector3 Point(float x,float z)=>site.Position+right*x+exit*z;
   bool Clear(Vector3 p){var d=p-site.Position;float x=Vector3.Dot(d,right),z=Vector3.Dot(d,exit);return !(Mathf.Abs(x)<3.8f&&z>-7&&z<23)&&ScenicDistance(p,out _,out var road)>road.Width+1.2f;}
   R01BlendGround(site,exit,right);
   // Existing canopy, rocks, fence and log remain. Replace scattered small plants
   // with fewer grouped instances, all outside the towing and road corridors.
   foreach(Transform child in forest)
    if(child.name.StartsWith("P_Grass")||child.name.StartsWith("P_Fern")||child.name.StartsWith("P_Bush")||child.name.StartsWith("P_Flower"))
    {Undo.RecordObject(child.gameObject,"Replace scattered forest understory");child.gameObject.SetActive(false);}
   var previous=environment.Find("R01 showcase - clustered understory");if(previous!=null)Undo.DestroyObjectImmediate(previous.gameObject);
   var root=new GameObject("R01 showcase - clustered understory").transform;root.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(root.gameObject,"R01 showcase plants");
   var islands=new[]{new Vector2(-6,-4),new Vector2(6,-6),new Vector2(-8,5),new Vector2(8,8),new Vector2(-3,-13)};
   var random=new System.Random(11011);int count=0;
   foreach(var island in islands)
   {
    var cluster=new GameObject("Forest floor island").transform;cluster.SetParent(root,false);
    for(int i=0;i<14;i++)
    {
     float angle=(float)random.NextDouble()*Mathf.PI*2,radius=Mathf.Sqrt((float)random.NextDouble())*2.1f;
     var p=Point(island.x+Mathf.Cos(angle)*radius,island.y+Mathf.Sin(angle)*radius);if(!Clear(p))continue;
     string prefab=i%6==0?"P_BushLeafy01":i%3==0?"P_Fern2":"P_Grass2";
     var go=ForestProp(prefab,p,i%6==0?1.25f:i%3==0?.85f:.48f+(float)random.NextDouble()*.2f,random.Next(360),cluster,false,false);
     Undo.RegisterCreatedObjectUndo(go,"Cluster forest groundcover");
     // One medium mesh set per plant; avoid nested LODGroups re-enabling all
     // source LOD meshes when the cluster's distance culling runs.
     foreach(var sourceLod in go.GetComponentsInChildren<LODGroup>(true))
     {
      var levels=sourceLod.GetLODs();if(levels.Length==0)continue;
      var chosen=levels[Mathf.Min(1,levels.Length-1)].renderers;if(chosen.Length==0)chosen=levels[0].renderers;
      foreach(var r in sourceLod.GetComponentsInChildren<Renderer>(true))r.enabled=chosen.Contains(r);
      UnityEngine.Object.DestroyImmediate(sourceLod);
     }
     count++;
    }
    var renderers=cluster.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
    if(renderers.Length>0){var lod=Undo.AddComponent<LODGroup>(cluster.gameObject);lod.SetLODs(new[]{new LOD(.035f,renderers)});lod.RecalculateBounds();}
   }
   // Bring two existing canopy trees into the background, instead of adding trees.
   var canopy=forest.Cast<Transform>().Where(t=>t.name.StartsWith("P_Birch")||t.name.StartsWith("P_FirTree")).ToArray();
   var treePoints=new[]{new Vector2(-6.5f,-10),new Vector2(7,-12)};
   for(int i=0;i<treePoints.Length&&canopy.Length>i+3;i++)
   {
    var p=Point(treePoints[i].x,treePoints[i].y);if(!Clear(p))continue;var tree=canopy[i+3];
    Undo.RecordObject(tree,"Frame R01 with existing canopy");var b=ScenarioBounds(tree.gameObject);
    tree.position+=Ground(p)-new Vector3(b.center.x,b.min.y+.08f,b.center.z);
   }
   foreach(var spot in new[]{new Vector2(-5.8f,-7),new Vector2(6.2f,-9)})
   {
    var p=Point(spot.x,spot.y);if(!Clear(p))continue;
    var rock=ForestProp("P_RockSpire_RockClump01",p,2.4f,spot.x*31,root,false,false);Undo.RegisterCreatedObjectUndo(rock,"R01 backdrop rocks");
   }
   // Local copies keep changes out of other recovery sites and shared shaders.
   foreach(var renderer in rut.GetComponentsInChildren<MeshRenderer>())
   {
    if(renderer.sharedMaterial==null)continue;
    var source=renderer.sharedMaterial;string path=ShowcaseFolder+"/"+source.name+".mat";
    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,path);}else Undo.RecordObject(material,"R01 soil material");
    bool bed=renderer.name=="Dark rut bed",lip=renderer.name=="Visible exit lip";
    if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",bed?new Color(.43f,.34f,.24f,.92f):lip?new Color(.58f,.46f,.32f,.6f):new Color(.61f,.48f,.32f,.85f));
    if(material.HasProperty("_EdgeBreakup"))material.SetFloat("_EdgeBreakup",.45f);
    if(material.HasProperty("_RutDepth"))material.SetFloat("_RutDepth",bed?.055f:.025f);
    if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.08f);
    material.enableInstancing=true;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
    Undo.RecordObject(renderer,"Assign R01 earth palette");renderer.sharedMaterial=material;
    var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null||renderer.name.StartsWith("Loose earth"))continue;
    string meshPath=ShowcaseFolder+"/"+renderer.name+" organic.asset";
    // Always derive from the original extraction mesh, not a previous generated copy.
    var original=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/TowTrouble/OpenWorld/Visuals/Extraction/"+renderer.name+".asset");
    if(original==null)original=filter.sharedMesh;
    var mesh=UnityEngine.Object.Instantiate(original);mesh.name=renderer.name+" organic";var verts=mesh.vertices;var colors=mesh.colors;
    for(int i=0;i<verts.Length;i++)
    {
     var worldPoint=filter.transform.TransformPoint(verts[i]);
     float jitter=Mathf.PerlinNoise(worldPoint.x*2.3f,worldPoint.z*2.3f)-.5f;
     worldPoint+=right*jitter*.075f;
     // Reduce the straight raised berm, retaining the terrain's actual depression.
     float ground=terrain.SampleHeight(worldPoint)+terrain.transform.position.y;
     worldPoint.y=ground+Mathf.Max(.014f,(worldPoint.y-ground)*.55f);
     verts[i]=filter.transform.InverseTransformPoint(worldPoint);
     if(colors.Length==verts.Length)colors[i].a*=.65f+.35f*Mathf.PerlinNoise(worldPoint.x*3,worldPoint.z*3);
    }
    mesh.vertices=verts;if(colors.Length==verts.Length)mesh.colors=colors;mesh.RecalculateNormals();mesh.RecalculateBounds();
    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else{Undo.RecordObject(saved,"R01 organic soil mesh");EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
    AssetDatabase.SaveAssetIfDirty(saved);Undo.RecordObject(filter,"R01 soil mesh binding");filter.sharedMesh=saved;
   }
   // Three shallow small-rock groups ground the surrounding props without blocking towing.
   foreach(var island in new[]{new Vector2(-5.5f,-5),new Vector2(6.5f,-8),new Vector2(-7,7)})
   for(int i=0;i<3;i++)
   {var p=Point(island.x+(i-1)*.55f,island.y+Mathf.Sin(i)*.6f);if(Clear(p)){var go=ForestProp("P_RockSpire_Rock01",p,.25f+i*.14f,i*113,root,false,false);Undo.RegisterCreatedObjectUndo(go,"R01 stone cluster");}}
   Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(group);
   Directory.CreateDirectory("Docs/Validation/R01Showcase");
   File.WriteAllText("Docs/Validation/R01Showcase/Authoring.json",JsonUtility.ToJson(new R01ShowcaseReport{Plants=count,Renderers=root.GetComponentsInChildren<Renderer>().Length,Colliders=root.GetComponentsInChildren<Collider>().Length,SiteUnchanged=JsonUtility.ToJson(site)},true));
   // Scene saving is left to the caller after validation, preserving reviewability.
  }
  [Serializable] private sealed class R01ShowcaseReport{public int Plants,Renderers,Colliders;public string SiteUnchanged;}
  private static void R01BlendGround(RecoverySite site,Vector3 exit,Vector3 right)
  {
   var data=terrain.terrainData;int n=data.alphamapResolution,layers=data.alphamapLayers;
   int x=Mathf.Clamp(Mathf.FloorToInt((site.Position.x-terrain.transform.position.x-23)/data.size.x*n),0,n-1);
   int z=Mathf.Clamp(Mathf.FloorToInt((site.Position.z-terrain.transform.position.z-23)/data.size.z*n),0,n-1);
   int w=Mathf.Min(n-x,Mathf.CeilToInt(46/data.size.x*n)+2),h=Mathf.Min(n-z,Mathf.CeilToInt(46/data.size.z*n)+2);
   string path="Docs/Validation/R01Showcase/GroundBaseline.json";Directory.CreateDirectory(Path.GetDirectoryName(path));R01PaintBaseline baseline;
   if(File.Exists(path))baseline=JsonUtility.FromJson<R01PaintBaseline>(File.ReadAllText(path));
   else{baseline=new R01PaintBaseline{X=x,Z=z,W=w,H=h,Layers=layers,Values=new float[w*h*layers]};var old=data.GetAlphamaps(x,z,w,h);int j=0;foreach(float value in old)baseline.Values[j++]=value;File.WriteAllText(path,JsonUtility.ToJson(baseline));}
   if(baseline.X!=x||baseline.Z!=z||baseline.W!=w||baseline.H!=h||baseline.Layers!=layers)throw new InvalidOperationException("R01 terrain baseline changed; review before re-authoring.");
   var paint=new float[h,w,layers];int index=0;for(int a=0;a<h;a++)for(int b=0;b<w;b++)for(int k=0;k<layers;k++)paint[a,b,k]=baseline.Values[index++];
   for(int a=0;a<h;a++)for(int b=0;b<w;b++)
   {
    var p=terrain.transform.position+new Vector3((x+b)/(float)(n-1)*data.size.x,0,(z+a)/(float)(n-1)*data.size.z);var d=p-site.Position;
    float side=Vector3.Dot(d,right),along=Vector3.Dot(d,exit);
    float oval=Mathf.Sqrt(side*side/169+along*along/289),noise=Mathf.PerlinNoise(p.x*.24f,p.z*.24f);
    float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1.25f,oval+(noise-.5f)*.25f)))*.65f;
    float road=ScenicDistance(p,out _,out var route);if(road<route.Width+1.5f||blend<=0)continue;
    float earth=.25f+noise*.25f,soil=.28f+(1-noise)*.22f;
    for(int k=0;k<layers;k++)paint[a,b,k]=Mathf.Lerp(paint[a,b,k],k==0?1-earth-soil:k==1?earth:k==6?soil:0,blend);
   }
   Undo.RegisterCompleteObjectUndo(data,"R01 blended forest floor");data.SetAlphamaps(x,z,paint);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
  }
 }
}
