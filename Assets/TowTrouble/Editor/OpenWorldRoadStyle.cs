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
  private const string RoadStyleFolder="Assets/TowTrouble/OpenWorld/Visuals/Road Style";
  private const string RoadStyleDocs="Docs/Validation/RoadStyle";
  private sealed class RoadEdgePixel {public float Distance,Width;public Vector3 Center;}
  [MenuItem("Tow Trouble/Art Integration/Natural Road Wear and Shoulders")]
  public static void StyleCountyRoads()
  {
   VisualContext();var scene=EditorSceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new InvalidOperationException("Recovery world required");
   var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;var data=terrain.terrainData;
   var roads=environment.Find("Road network - 24 recovery spurs");var wear=environment.Find("Road wheel tracks and gravel");var verge=environment.Find("County landscape polish/Weathered road shoulders and hazard markers");
   if(roads==null||wear==null||verge==null)throw new InvalidOperationException("Authored road layers required");
   var shader=AssetDatabase.LoadAssetAtPath<Shader>(RoadStyleFolder+"/SoftRoadWear.shader");var shoulderShader=AssetDatabase.LoadAssetAtPath<Shader>(ShowcaseFolder+"/StylizedSoil.shader");if(shader==null||shoulderShader==null)throw new InvalidOperationException("Road style shaders missing");
   const string backup="/private/tmp/TowRoadStyleBackup";Directory.CreateDirectory(backup);if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);if(!File.Exists(backup+"/Terrain.asset"))File.Copy(AssetDatabase.GetAssetPath(data),backup+"/Terrain.asset");
   Directory.CreateDirectory(RoadStyleDocs);Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Natural road shoulder and wear style");
   int n=data.alphamapResolution,layers=data.alphamapLayers;var paint=data.GetAlphamaps(0,0,n,n);var baseline=new float[n,n,layers];string file=RoadStyleDocs+"/PaintBaseline.bin";
   if(!File.Exists(file)){using(var writer=new BinaryWriter(File.Create(file))){writer.Write(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data)));writer.Write(n);writer.Write(layers);foreach(float value in paint)writer.Write(value);}}
   using(var reader=new BinaryReader(File.OpenRead(file))){if(reader.ReadString()!=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data))||reader.ReadInt32()!=n||reader.ReadInt32()!=layers)throw new InvalidOperationException("Review road paint baseline");for(int z=0;z<n;z++)for(int x=0;x<n;x++)for(int k=0;k<layers;k++)baseline[z,x,k]=reader.ReadSingle();}
   var pixels=new Dictionary<int,RoadEdgePixel>();float cell=data.size.x/(n-1);var origin=terrain.transform.position;
   foreach(var f in roads.GetComponentsInChildren<MeshFilter>())
   {
    var v=f.sharedMesh.vertices;for(int row=0;row<v.Length/6;row++)
    {
     var center=f.transform.TransformPoint((v[row*6+2]+v[row*6+3])*.5f);float width=Flat(f.transform.TransformPoint(v[row*6+4])-f.transform.TransformPoint(v[row*6+1])).magnitude*.5f;
     int cx=Mathf.RoundToInt((center.x-origin.x)/data.size.x*(n-1)),cz=Mathf.RoundToInt((center.z-origin.z)/data.size.z*(n-1)),radius=Mathf.CeilToInt((width+3.5f)/cell);
     for(int z=Mathf.Max(0,cz-radius);z<=Mathf.Min(n-1,cz+radius);z++)for(int x=Mathf.Max(0,cx-radius);x<=Mathf.Min(n-1,cx+radius);x++)
     {
      var p=origin+new Vector3(x/(float)(n-1)*data.size.x,0,z/(float)(n-1)*data.size.z);float d=Flat(p-center).magnitude;if(d>width+3.5f)continue;int key=z*n+x;
      if(!pixels.TryGetValue(key,out var prior)||d/width<prior.Distance/prior.Width)pixels[key]=new RoadEdgePixel{Center=center,Distance=d,Width=width};
     }
    }
   }
   int painted=0;
   foreach(var pair in pixels)
   {
    int z=pair.Key/n,x=pair.Key%n;var edge=pair.Value;float d=edge.Distance,w=edge.Width;
    if(d<w-.7f||d>w+2.8f)continue;
    var p=origin+new Vector3(x/(float)(n-1)*data.size.x,0,z/(float)(n-1)*data.size.z);
    if(Ground(p).y<11.8f||OnRiverBridge(p,8)||map.Sites.Any(s=>Flat(p-s.Position).sqrMagnitude<35*35)||map.Deliveries.Any(s=>Flat(p-s.Position).sqrMagnitude<24*24)||Flat(p-world.GaragePosition).sqrMagnitude<60*60)continue;
    var outward=Flat(p-edge.Center).normalized;var sample=edge.Center+outward*(w+5.2f);int sx=Mathf.Clamp(Mathf.RoundToInt((sample.x-origin.x)/data.size.x*(n-1)),0,n-1),sz=Mathf.Clamp(Mathf.RoundToInt((sample.z-origin.z)/data.size.z*(n-1)),0,n-1);
    float noise=Mathf.PerlinNoise(p.x*.17f+3,p.z*.17f+7);float band=Mathf.Sin(Mathf.Clamp01((d-(w-.7f))/3.5f)*Mathf.PI);float blend=band*(.12f+.38f*noise);
    for(int k=0;k<layers;k++)paint[z,x,k]=Mathf.Lerp(baseline[z,x,k],baseline[sz,sx,k],blend);painted++;
   }
   Undo.RegisterCompleteObjectUndo(data,"Irregular terrain road margins");data.SetAlphamaps(0,0,paint);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
   var mats=new Dictionary<string,Material>();int meshes=0;long triangles=0;
   foreach(var group in new[]{wear,verge})foreach(var f in group.GetComponentsInChildren<MeshFilter>())
   {
    bool shoulder=group==verge;if(shoulder&&!f.name.EndsWith("weathered verge"))continue;
    var renderer=f.GetComponent<MeshRenderer>();if(renderer==null||f.sharedMesh==null)continue;
    string originalPath=AssetDatabase.GetAssetPath(f.sharedMesh);if(originalPath.StartsWith(RoadStyleFolder+"/"))originalPath=AssetDatabase.GUIDToAssetPath(Path.GetFileNameWithoutExtension(originalPath).Substring(5));
    var original=AssetDatabase.LoadAssetAtPath<Mesh>(originalPath);if(original==null)throw new InvalidOperationException("Missing original road overlay");
    var p=f.transform.TransformPoint(original.bounds.center);int ax=Mathf.Clamp(Mathf.RoundToInt((p.x-origin.x)/data.size.x*(n-1)),0,n-1),az=Mathf.Clamp(Mathf.RoundToInt((p.z-origin.z)/data.size.z*(n-1)),0,n-1);
    int biome=p.y>130?3:baseline[az,ax,5]>.35f?5:baseline[az,ax,2]>.35f?4:p.y>55?2:p.y<18?1:0;
    string key=(shoulder?"Shoulder ":"Wheel wear ")+biome;
    if(!mats.TryGetValue(key,out var mat))
    {
     string path=RoadStyleFolder+"/"+key+".mat";mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(mat,path);}else Undo.RecordObject(mat,"Road material style");
     mat.shader=shoulder?shoulderShader:shader;
     var palette=new[]{new Color(.45f,.37f,.26f,.40f),new Color(.40f,.35f,.26f,.42f),new Color(.49f,.44f,.36f,.36f),new Color(.46f,.58f,.68f,.27f),new Color(.63f,.53f,.38f,.30f),new Color(.65f,.46f,.29f,.32f)};var tint=palette[biome];if(shoulder){tint.a*=.75f;mat.SetFloat("_TextureDetail",.3f);mat.SetFloat("_ShadowStrength",.78f);mat.SetFloat("_RutDepth",.008f);mat.SetFloat("_EdgeBreakup",0);mat.SetFloat("_Compaction",1);}
     mat.SetColor("_BaseColor",tint);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);mats.Add(key,mat);
    }
    var mesh=UnityEngine.Object.Instantiate(original);mesh.name=original.name+" natural wear";var colors=mesh.colors;var vertices=mesh.vertices;
    for(int i=0;i<colors.Length;i++){var pos=f.transform.TransformPoint(vertices[i]);float macro=Mathf.PerlinNoise(pos.x*.13f+13,pos.z*.13f+2);colors[i].a*=shoulder?.35f+.65f*macro:.55f+.45f*macro;}
    mesh.colors=colors;string meshPath=RoadStyleFolder+"/Mesh_"+AssetDatabase.AssetPathToGUID(originalPath)+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else{Undo.RecordObject(saved,"Natural road wear mesh");EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}AssetDatabase.SaveAssetIfDirty(saved);
    Undo.RecordObject(f,"Road overlay mesh binding");Undo.RecordObject(renderer,"Road overlay material binding");f.sharedMesh=saved;renderer.sharedMaterial=mat;meshes++;triangles+=saved.triangles.Length/3;
   }
   EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);File.WriteAllText(RoadStyleDocs+"/Authoring.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{roads=roads.GetComponentsInChildren<MeshFilter>().Length,overlayMeshes=meshes,triangles,paintedPixels=painted,materials=mats.Count,newRenderers=0,newTriangles=0},Newtonsoft.Json.Formatting.Indented));
  }
 }
}
