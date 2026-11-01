using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Recovery/Deepen Loose Ground Wheel Tracks")]
        public static void DeepenLooseGroundWheelTracks()
        {
            VisualContext();int covers=0,materials=0;
            foreach(var track in environment.GetComponentsInChildren<RecoveryTrackSurface>(true))
            {
                var filter=track.GetComponent<MeshFilter>();var collider=track.GetComponent<MeshCollider>();
                if(filter==null||collider==null||filter.sharedMesh==collider.sharedMesh)continue;
                Undo.RecordObject(track,"Match wheel tracks to visual loose-ground skin");track.ConfigureVisualCover(filter.sharedMesh,collider.sharedMesh);EditorUtility.SetDirty(track);covers++;
            }
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{VisualRoot}))
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(mat.shader.name=="TowTrouble/Wheel Impressions")
                {
                    Undo.RecordObject(mat,"Recessed wheel track shading");bool snow=mat.name.EndsWith(" 2")||mat.name=="Roadside skid 4";bool sand=mat.name.EndsWith(" 1")||mat.name=="Roadside skid 5";
                    mat.renderQueue=3005;bool hard=mat.GetFloat("_HardSurface")>.5f;mat.SetFloat("_RutDepth",hard?0:snow?.115f:sand?.085f:.06f);
                    if(!hard){var c=mat.GetColor("_BaseColor");c.a=mat.name.StartsWith("Dynamic")?.88f:c.a;mat.SetColor("_BaseColor",c);}
                    EditorUtility.SetDirty(mat);materials++;
                }
                else if(mat.HasProperty("_Dynamic_Normal_Amount")&&mat.HasProperty("_Stamp_Darken"))
                {Undo.RecordObject(mat,"Readable Living Mud wheel channels");mat.name=Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mat));mat.SetFloat("_Dynamic_Normal_Amount",1.35f);mat.SetFloat("_Stamp_Darken",.48f);EditorUtility.SetDirty(mat);}
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Directory.CreateDirectory("Docs/Validation/RecoveryProgression");File.WriteAllText("Docs/Validation/RecoveryProgression/TrackBudget.json",$"{{\"visualCoverBindings\":{covers},\"trackMaterials\":{materials},\"maxChunks\":36,\"segmentsPerChunk\":128,\"newColliders\":0}}");
        }
    }
}
