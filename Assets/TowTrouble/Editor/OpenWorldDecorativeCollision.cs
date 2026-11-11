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
  private static string DecorativeCollisionReason(Collider collider)
  {
   if(collider.isTrigger||collider is TerrainCollider||collider.attachedRigidbody!=null||collider.GetComponentInParent<RecoveryTowObstacle>()!=null)return null;
   var instance=PrefabUtility.GetNearestPrefabInstanceRoot(collider.gameObject);
   var root=instance!=null?instance:collider.gameObject;
   string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collider.gameObject);
   string name=(string.IsNullOrEmpty(path)?root.name:Path.GetFileNameWithoutExtension(path)).ToLowerInvariant();
   if(new[]{"p_gras","p_fern","p_foxtail","p_marshtail","p_flower","p_reed","p_bush","p_shrub"}.Any(name.StartsWith))return "soft vegetation";
   if(!name.StartsWith("p_rock")&&!name.StartsWith("p_boulder")&&!name.StartsWith("pebble")&&!name.StartsWith("small stone"))return null;
   // Mesh bounds remain valid even on disabled LODs and inactive dressing.
   bool any=false;var bounds=new Bounds();
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
   {
    if(filter.sharedMesh==null)continue;var local=filter.sharedMesh.bounds;
    for(int corner=0;corner<8;corner++)
    {
     var point=filter.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
     if(!any){bounds=new Bounds(point,Vector3.zero);any=true;}else bounds.Encapsulate(point);
    }
   }
   if(!any||Mathf.Max(bounds.size.x,bounds.size.z)>1.8f)return null;
   float exposed=bounds.max.y-(terrain.SampleHeight(bounds.center)+terrain.transform.position.y);
   return bounds.size.y<=.8f||(bounds.size.y<=1.3f&&exposed<=.5f)?"small decorative rock":null;
  }
  private static int PruneSoftDecorativeColliders()
  {
   if(environment==null||terrain==null)return 0;
   var unwanted=environment.GetComponentsInChildren<Collider>(true).Where(c=>DecorativeCollisionReason(c)!=null).ToArray();
   foreach(var collider in unwanted)Undo.DestroyObjectImmediate(collider);
   return unwanted.Length;
  }
  [MenuItem("Tow Trouble/Art Integration/Remove Grass and Small Rock Collisions")]
  public static void CleanDecorativeCollisions()
  {
   VisualContext();var scene=EditorSceneManager.GetActiveScene();var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();string map=EditorJsonUtility.ToJson(world.Map);
   Directory.CreateDirectory("/private/tmp/TowDecorCollisionBackup");if(!File.Exists("/private/tmp/TowDecorCollisionBackup/Before.unity"))EditorSceneManager.SaveScene(scene,"/private/tmp/TowDecorCollisionBackup/Before.unity",true);
   var before=environment.GetComponentsInChildren<Collider>(true);var targets=before.Where(c=>DecorativeCollisionReason(c)!=null).ToArray();
   var retained=before.Except(targets).ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
   var visuals=environment.GetComponentsInChildren<Renderer>(true).ToDictionary(r=>r,r=>EditorJsonUtility.ToJson(r));
   var rows=targets.Select(c=>new{path=CollisionPath(c.transform),reason=DecorativeCollisionReason(c),wasActive=c.enabled&&c.gameObject.activeInHierarchy}).ToArray();
   Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Remove decorative collision snags");int removed=PruneSoftDecorativeColliders();Physics.SyncTransforms();
   if(retained.Any(p=>p.Key==null||EditorJsonUtility.ToJson(p.Key)!=p.Value)||visuals.Any(p=>p.Key==null||EditorJsonUtility.ToJson(p.Key)!=p.Value)||map!=EditorJsonUtility.ToJson(world.Map))throw new InvalidOperationException("Protected scene data changed");
   int remaining=environment.GetComponentsInChildren<Collider>(true).Count(c=>DecorativeCollisionReason(c)!=null);if(remaining!=0)throw new InvalidOperationException("Decorative colliders remain");
   Directory.CreateDirectory("Docs/Validation/DecorativeCollision");File.WriteAllText("Docs/Validation/DecorativeCollision/Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{removed,activeRemoved=rows.Count(r=>r.wasActive),grassRemoved=rows.Count(r=>r.reason=="soft vegetation"),remaining,retainedCollidersUnchanged=true,visualsUnchanged=true,mapUnchanged=true,items=rows},Newtonsoft.Json.Formatting.Indented));
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(group);
  }
  private static string CollisionPath(Transform t)=>t.parent==null?t.name:CollisionPath(t.parent)+"/"+t.name;
 }
}
