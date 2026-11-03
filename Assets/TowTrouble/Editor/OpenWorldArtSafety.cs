using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        [MenuItem("Tow Trouble/Art Integration/Apply Complete Art Pass")]
        public static void ApplyCompleteArtPass()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!=ScenePath||scene.isDirty)
                throw new InvalidOperationException("Open and save OpenWorldRecovery in Edit Mode first.");
            IntegrateLandforms();IntegrateBiomeArt();FinishArtComposition();BlendWorkshopApproaches();
            IntegrateWorkshopAndLighting();FinalizeArtSafety();RecomposeMountainGorge();RecomposeMountainApproaches();BakeGroundSurfaceMap();ConfigureWheelTracks();CaptureMapPhotos();
        }
        private static Vector3 SafeSignPoint(Vector3 origin, Vector3 approach, float radius, float halfWidth)
        {
            Vector3 best=origin;float score=float.MinValue;
            foreach(float extra in new[]{0f,3f,6f,9f})foreach(float angle in new[]{90f,-90f,135f,-135f,180f,45f,-45f})
            {
                var p=Ground(origin+Quaternion.Euler(0,angle,0)*approach*(radius+extra));float clearance=ScenicDistance(p,out _,out var road)-road.Width-halfWidth;
                float rank=Mathf.Min(clearance,4)*10-Mathf.Abs(p.y-origin.y)*2-extra;
                if(clearance>1.5f&&rank>score){score=rank;best=p;}
            }
            if(score==float.MinValue)throw new InvalidOperationException("No safe sign position near "+origin);
            return best;
        }
        [MenuItem("Tow Trouble/Art Integration/6 - Fit Signs and Finish Mud Materials")]
        public static void FinalizeArtSafety()
        {
            VisualContext();ScenicPaths();
            var timber=AssetDatabase.LoadAssetAtPath<Material>(VisualRoot+"/Workshop aged timber.mat");
            foreach(var bay in plan.Deliveries)
            {
                var group=environment.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("Dropoff "+bay.Id+" -"));var origin=nodes[bay.Node].Point;
                var p=SafeSignPoint(origin,Vector3.back,19,3);var facing=Flat(origin-p).normalized;var right=Vector3.Cross(Vector3.up,facing);
                var board=group.Find("Destination sign");board.position=p+Vector3.up*2.5f;board.rotation=Quaternion.LookRotation(-facing);board.localScale=new Vector3(5.7f,1.35f,.2f);
                var text=DestinationCaption(group);text.transform.position=board.position+facing*.13f;text.transform.rotation=board.rotation;FitDestinationCaption(text, board);
                foreach(var old in group.Cast<Transform>().Where(t=>t.name=="Destination timber post").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach(int side in new[]{-1,1}){var foot=Ground(p+right*side*2.2f);float height=board.position.y+.6f-foot.y;Cube("Destination timber post",foot+Vector3.up*height*.5f,new Vector3(.14f,height,.14f),timber,group);}
            }
            var workshop=environment.Find("Central garage - Tow County");
            if(workshop!=null)
            {
                var post=workshop.Find("Service sign post");
                if(post!=null)
                {
                    var destination=SafeSignPoint(nodes["g"].Point,Vector3.back,26,1.7f);var delta=destination-(post.position-Vector3.up*1.7f);
                    foreach(var part in workshop.Cast<Transform>().Where(t=>t.name=="Service sign post"||t.name=="Service information board"||t.name=="SERVICE\nTOW COUNTY"))part.position+=delta;
                }
            }
            var mudRenderers=environment.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.EndsWith(" - Living Mud")).ToArray();
            if(mudRenderers.Length>0)
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/TowTrouble/Art/Mud/Tutorial Living Mud.mat");
                var mat=new Material(source){name="County wet silt - Living Mud"};mat.SetColor("_Tint",new Color(.96f,.80f,.61f));mat.SetFloat("_Smoothness",.44f);mat.SetFloat("_Height_Amount",.015f);
                mat=VisualAsset(mat,"County wet silt.mat");foreach(var renderer in mudRenderers)renderer.sharedMaterial=mat;
            }
            terrain.drawTreesAndFoliage=true;terrain.terrainData.SetBaseMapDirty();terrain.Flush();SaveVisualScene();
        }
    }
}
