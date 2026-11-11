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
  [MenuItem("Tow Trouble/Art Integration/R01 Stylized Material Study")]
  public static void StyleR01()
  {
   VisualContext();var scene=EditorSceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new InvalidOperationException("Open recovery world");
   const string backup="/private/tmp/TowR01StyleBackup";Directory.CreateDirectory(backup);if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);
   var shader=AssetDatabase.LoadAssetAtPath<Shader>(ShowcaseFolder+"/StylizedSoil.shader");if(shader==null)throw new InvalidOperationException("R01 shader missing");
   var site=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>().Map.Sites.Single(s=>s.Id=="R01");
   var rut=environment.Find("R01 visible wheel rut");if(rut==null)throw new InvalidOperationException("R01 rut missing");
   foreach(var mat in rut.GetComponentsInChildren<MeshRenderer>().Select(r=>r.sharedMaterial).Distinct())
   {
    if(!AssetDatabase.GetAssetPath(mat).StartsWith(ShowcaseFolder+"/"))throw new InvalidOperationException("Expected isolated R01 materials");
    string path=backup+"/"+mat.name+".mat";if(!File.Exists(path))File.Copy(AssetDatabase.GetAssetPath(mat),path);
    Undo.RecordObject(mat,"R01 stylized soil");bool bed=mat.name.Contains("Exposed"),clods=mat.name.Contains("clods");
    if(clods){mat.SetTexture("_BaseMap",null);mat.SetColor("_BaseColor",new Color(.39f,.32f,.23f));mat.SetFloat("_Smoothness",.04f);}
    else{mat.shader=shader;mat.SetFloat("_TextureDetail",.22f);mat.SetFloat("_ShadowStrength",.72f);mat.SetFloat("_Compaction",.94f);mat.SetFloat("_RutDepth",bed?.045f:.018f);mat.SetFloat("_EdgeBreakup",.3f);mat.SetColor("_BaseColor",bed?new Color(.47f,.39f,.29f,.95f):new Color(.60f,.51f,.37f,.55f));}
    EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
   }
   // Desaturate only R01's foliage instances. Shared nature materials are never edited.
   var copies=new Dictionary<Material,Material>();
   foreach(string name in new[]{"Workshop forest - finished clearing","R01 showcase - clustered understory"})
   {
    var root=environment.Find(name);if(root==null)continue;
    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
    {
     if(!renderer.transform.root.gameObject.activeSelf)continue;
     var materials=renderer.sharedMaterials;bool changed=false;
     for(int i=0;i<materials.Length;i++)
     {
      var original=materials[i];if(original==null)continue;
      string source=AssetDatabase.GetAssetPath(original);if(source.StartsWith(ShowcaseFolder+"/Foliage ")){source=AssetDatabase.GUIDToAssetPath(Path.GetFileNameWithoutExtension(source).Substring(8));original=AssetDatabase.LoadAssetAtPath<Material>(source);if(original==null)continue;}
      if(!source.StartsWith("Assets/SoStylized/")||!(source.ToLowerInvariant().Contains("leaf")||source.ToLowerInvariant().Contains("leaves")||source.ToLowerInvariant().Contains("grass")||source.ToLowerInvariant().Contains("foliage")))continue;
      if(!copies.TryGetValue(original,out var copy))
      {
       string path=ShowcaseFolder+"/Foliage "+AssetDatabase.AssetPathToGUID(source)+".mat";
       copy=AssetDatabase.LoadAssetAtPath<Material>(path);if(copy==null){copy=new Material(original);AssetDatabase.CreateAsset(copy,path);}
       foreach(string property in new[]{"_BaseColor","_Color","_Main_Color","_Second_Color","_Bottom_Color","_Tip_Color","_SSS_Color"})
       if(copy.HasProperty(property)){var c=original.GetColor(property);float grey=c.r*.299f+c.g*.587f+c.b*.114f;var balanced=Color.Lerp(c,new Color(grey,grey,grey,c.a),.12f);copy.SetColor(property,new Color(balanced.r*.98f,balanced.g*.96f,balanced.b*.98f,c.a));}
       EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);copies.Add(original,copy);
      }
      materials[i]=copy;changed=true;
     }
     if(changed){Undo.RecordObject(renderer,"R01 foliage colour balance");renderer.sharedMaterials=materials;}
    }
   }
   // A small feathered forest-soil layer unifies the rut and surrounding grass.
   var old=environment.Find("R01 soft forest floor");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
   var go=new GameObject("R01 soft forest floor",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(environment,false);Undo.RegisterCreatedObjectUndo(go,"R01 soft ground transition");
   var verts=new List<Vector3>();var uv=new List<Vector2>();var channels=new List<Vector2>();var ends=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
   var forward=Flat(site.ExitDirection).normalized;var side=Vector3.Cross(Vector3.up,forward);const int n=24;
   for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
   {
    float u=x/(float)n*2-1,v=z/(float)n*2-1;var p=Ground(site.Position+side*u*5+forward*(v*7-1))+Vector3.up*.012f;
    verts.Add(go.transform.InverseTransformPoint(p));uv.Add(new Vector2(p.x,p.z)*.15f);channels.Add(new Vector2(.5f,2));ends.Add(Vector2.one);
    float radius=Mathf.Sqrt(u*u+v*v),noise=Mathf.PerlinNoise(p.x*.8f,p.z*.8f);
    float alpha=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,radius)))*(.22f+.12f*noise);
    colors.Add(new Color(1,1,1,alpha));if(x<n&&z<n){int a=z*(n+1)+x;triangles.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
   }
   var mesh=new Mesh{name="R01 feathered woodland soil"};mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.SetUVs(1,channels);mesh.SetUVs(2,ends);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   string meshPath=ShowcaseFolder+"/Soft forest floor.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}AssetDatabase.SaveAssetIfDirty(saved);go.GetComponent<MeshFilter>().sharedMesh=saved;
   string matPath=ShowcaseFolder+"/Soft forest floor.mat";var floor=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(floor==null){floor=new Material(shader);AssetDatabase.CreateAsset(floor,matPath);}
   floor.SetColor("_BaseColor",new Color(.49f,.43f,.30f,1));floor.SetFloat("_TextureDetail",0);floor.SetFloat("_RutDepth",0);floor.SetFloat("_Compaction",1);floor.SetFloat("_ShadowStrength",.72f);floor.renderQueue=3003;EditorUtility.SetDirty(floor);AssetDatabase.SaveAssetIfDirty(floor);
   var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=floor;mr.shadowCastingMode=ShadowCastingMode.Off;
   var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.04f,new Renderer[]{mr})});lod.RecalculateBounds();
   EditorSceneManager.MarkSceneDirty(scene);
  }
 }
}
