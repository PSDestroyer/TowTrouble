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
  private const string BiomeStyleFolder="Assets/TowTrouble/OpenWorld/Visuals/Biome Style";
  [MenuItem("Tow Trouble/Art Integration/Apply Approved Biome Material Style")]
  public static void StyleRecoveryBiomes()
  {
   VisualContext();var scene=EditorSceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new InvalidOperationException("Recovery scene required");
   var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;string before=EditorJsonUtility.ToJson(map);
   const string backup="/private/tmp/TowBiomeStyleBackup";Directory.CreateDirectory(backup);if(!File.Exists(backup+"/Before.unity"))EditorSceneManager.SaveScene(scene,backup+"/Before.unity",true);
   var ground=AssetDatabase.LoadAssetAtPath<Shader>(BiomeStyleFolder+"/StylizedGroundBlend.shader");var tracks=AssetDatabase.LoadAssetAtPath<Shader>(ShowcaseFolder+"/StylizedSoil.shader");
   if(ground==null||tracks==null)throw new InvalidOperationException("Style shaders unavailable");
   Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Biome recovery material study");
   var cache=new Dictionary<string,Material>();var counts=map.Sites.ToDictionary(s=>s.Id,s=>0);
   foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>(true))
   {
    if(!renderer.gameObject.activeInHierarchy)continue;
    RecoverySite site=null;
    for(var t=renderer.transform;t!=null&&t!=environment;t=t.parent)
    {site=map.Sites.FirstOrDefault(s=>t.name.StartsWith(s.Id+" -")||t.name.StartsWith(s.Id+" "));if(site!=null)break;}
    if(site==null||site.Id=="R01")continue;
    var materials=renderer.sharedMaterials;bool changed=false;
    for(int i=0;i<materials.Length;i++)
    {
     var original=materials[i];if(original==null)continue;string path=AssetDatabase.GetAssetPath(original);
     if(path.StartsWith(BiomeStyleFolder+"/")){path=AssetDatabase.GUIDToAssetPath(Path.GetFileNameWithoutExtension(path).Substring(3));original=AssetDatabase.LoadAssetAtPath<Material>(path);if(original==null)throw new InvalidOperationException("Missing original style material");}
     bool blend=original.HasProperty("_CoverEnabled"),rut=original.shader.name=="TowTrouble/Wheel Impressions",mud=original.HasProperty("_Dynamic_Normal_Amount");
     string lower=path.ToLowerInvariant();bool foliage=path.StartsWith("Assets/SoStylized/")&&(lower.Contains("foliage")||lower.Contains("leaves")||lower.Contains("grass")||lower.Contains("leaf"));
     if(!blend&&!rut&&!mud&&!foliage)continue;
     string key="B"+site.Biome+"_"+AssetDatabase.AssetPathToGUID(path);
     if(!cache.TryGetValue(key,out var styled))
     {
      string target=BiomeStyleFolder+"/"+key+".mat";styled=AssetDatabase.LoadAssetAtPath<Material>(target);
      if(styled==null){styled=new Material(original);AssetDatabase.CreateAsset(styled,target);}else{Undo.RecordObject(styled,"Biome material style");EditorUtility.CopySerialized(original,styled);styled.name=key;}
      int b=site.Biome;
      if(blend){styled.shader=ground;styled.SetFloat("_TextureDetail",b==3?.42f:.28f);styled.SetFloat("_TextureMean",b==3?.88f:b>=4?.72f:.62f);styled.SetFloat("_ShadowStrength",.78f);}
      if(rut){styled.shader=tracks;styled.SetFloat("_TextureDetail",b==3?.4f:.22f);styled.SetFloat("_ShadowStrength",.72f);styled.SetFloat("_RutDepth",Mathf.Min(original.GetFloat("_RutDepth"),.045f));styled.SetFloat("_EdgeBreakup",.3f);var c=original.GetColor("_BaseColor");float l=c.r*.299f+c.g*.587f+c.b*.114f;styled.SetColor("_BaseColor",Color.Lerp(c,new Color(l,l,l,c.a),b>=4?.08f:.12f));}
      if(mud){styled.SetFloat("_Normal_Strength",.2f);styled.SetFloat("_Dynamic_Normal_Amount",.85f);styled.SetFloat("_Smoothness",b==1?.26f:.17f);if(styled.HasProperty("_Tint"))styled.SetColor("_Tint",b==1?new Color(.82f,.78f,.66f):new Color(.87f,.79f,.65f));}
      if(foliage)foreach(string prop in new[]{"_BaseColor","_Color","_Main_Color","_Second_Color","_Bottom_Color","_Tip_Color","_SSS_Color"})
       if(styled.HasProperty(prop)){var c=original.GetColor(prop);float l=c.r*.299f+c.g*.587f+c.b*.114f;var toned=Color.Lerp(c,new Color(l,l,l,c.a),b==3?.06f:b>=4?.08f:.12f);styled.SetColor(prop,new Color(toned.r*.98f,toned.g*.96f,toned.b*.98f,c.a));}
      EditorUtility.SetDirty(styled);AssetDatabase.SaveAssetIfDirty(styled);cache.Add(key,styled);
     }
     materials[i]=styled;changed=true;
    }
    if(changed){Undo.RecordObject(renderer,"Bind local biome materials");renderer.sharedMaterials=materials;counts[site.Id]++;}
   }
   if(before!=EditorJsonUtility.ToJson(map))throw new InvalidOperationException("Map mutated unexpectedly");
   if(counts.Where(x=>x.Key!="R01").Any(x=>x.Value==0))throw new InvalidOperationException("A recovery has no styled renderers");
   EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
   Directory.CreateDirectory("Docs/Validation/BiomeStyle");File.WriteAllText("Docs/Validation/BiomeStyle/Authoring.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{materials=cache.Count,sites=counts,extraRenderers=0,extraTriangles=0,extraLights=0},Newtonsoft.Json.Formatting.Indented));
  }
 }
}
