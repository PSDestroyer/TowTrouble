using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static Vector3 RecoveryPoint(Site site)=>nodes[site.Node].Point+new Vector3(site.OffsetX,0,site.OffsetZ);
        private static void EnsureRecoveryOffsets()
        {
            bool changed=false;
            foreach(var site in plan.Sites)
            {
                if(Mathf.Abs(site.OffsetX)+Mathf.Abs(site.OffsetZ)>1)continue;
                var origin=nodes[site.Node].Point;var edge=plan.Edges.First(e=>e.A==site.Node||e.B==site.Node);
                var approach=Flat(nodes[edge.A==site.Node?edge.B:edge.A].Point-origin).normalized;
                Vector3 best=origin;float score=float.MinValue;
                foreach(float radius in new[]{19f,23f,27f})foreach(float angle in new[]{90f,-90f,120f,-120f,150f,-150f,180f})
                {
                    var p=Ground(origin+Quaternion.Euler(0,angle,0)*approach*radius);
                    float clear=ScenicDistance(p,out _,out var road)-road.Width;
                    if(clear<14||RiverDistance(p)<17||ShoreDistance(p)<-3||Mathf.Abs(p.x)>455||Mathf.Abs(p.z)>455)continue;
                    float rank=-Mathf.Abs(p.y-origin.y)*2-radius*.12f+Mathf.Min(clear,18)*.3f;
                    if(rank>score){score=rank;best=p;}
                }
                if(score==float.MinValue)throw new InvalidOperationException("No off-road recovery pocket for "+site.Id);
                site.OffsetX=best.x-origin.x;site.OffsetZ=best.z-origin.z;changed=true;
            }
            if(changed){File.WriteAllText("Assets/TowTrouble/Editor/OpenWorldPlan.json",JsonUtility.ToJson(plan,true)+"\n");AssetDatabase.ImportAsset("Assets/TowTrouble/Editor/OpenWorldPlan.json");}
        }
        private static bool ClearRecoveryAccess(Vector3 p,float radius)
        {
            foreach(var site in plan.Sites)
            {
                var a=nodes[site.Node].Point;var b=RecoveryPoint(site);
                if(Distance(new Vector2(p.x,p.z),new Vector2(a.x,a.z),new Vector2(b.x,b.z),out _)<6+radius||Flat(p-b).magnitude<9+radius)return false;
            }
            return true;
        }
        private static void GradeRecoveryPullouts()
        {
            var data=terrain.terrainData;int n=data.heightmapResolution;var heights=data.GetHeights(0,0,n,n);float scale=(n-1)/1000f;
            foreach(var site in plan.Sites)
            {
                var a=nodes[site.Node].Point;var b=RecoveryPoint(site);
                // The stranded vehicle sits below its approach, away from the through lane.
                b.y=a.y-(site.Biome==1?.75f:site.Tier>=3?1.3f:.4f);
                int x0=Mathf.Clamp((int)((Mathf.Min(a.x,b.x)+500-22)*scale),0,n-1),x1=Mathf.Clamp((int)((Mathf.Max(a.x,b.x)+500+22)*scale),0,n-1);
                int z0=Mathf.Clamp((int)((Mathf.Min(a.z,b.z)+500-22)*scale),0,n-1),z1=Mathf.Clamp((int)((Mathf.Max(a.z,b.z)+500+22)*scale),0,n-1);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)
                {
                    var p=new Vector2(x/scale-500,z/scale-500);float d=Distance(p,new Vector2(a.x,a.z),new Vector2(b.x,b.z),out float t);
                    float radius=Mathf.Lerp(3.8f,7,t);float blend=1-Smooth01(radius,radius+15,d);
                    if(roadDistance[z,x]<roadWidth[z,x]+.7f)continue;
                    float target=Mathf.Lerp(a.y,b.y,Smooth01(0,1,t));
                    heights[z,x]=Mathf.Lerp(heights[z,x],(target+50)/350,blend);
                }
            }
            data.SetHeights(0,0,heights);EditorUtility.SetDirty(data);
        }
        private static Vector3 RecoverySignPoint(Site site)
        {
            var origin=RecoveryPoint(site);var approach=Flat(nodes[site.Node].Point-origin).normalized;
            Vector3 best=origin;float bestScore=float.MinValue;
            foreach(float radius in new[]{10f,13f,16f})foreach(float angle in new[]{90f,-90f,120f,-120f,160f,-160f})
            {
                var p=Ground(origin+Quaternion.Euler(0,angle,0)*approach*radius);
                float roadClear=ScenicDistance(p,out _,out var road)-road.Width;
                float accessClear=Distance(new Vector2(p.x,p.z),new Vector2(origin.x,origin.z),new Vector2(nodes[site.Node].Point.x,nodes[site.Node].Point.z),out _);
                if(roadClear<5||accessClear<8||p.y<10.6f||RiverDistance(p)<8)continue;
                float score=-Mathf.Abs(p.y-Ground(origin).y)-radius*.15f;
                if(score>bestScore){best=p;bestScore=score;}
            }
            if(bestScore==float.MinValue)throw new InvalidOperationException("No off-road sign position for "+site.Id);
            return best;
        }
        private static void UpdateRecoveryPullouts()
        {
            var map=UnityEngine.Object.FindFirstObjectByType<RecoveryMap>();
            foreach(var site in plan.Sites)
            {
                var saved=map.Sites.Single(s=>s.Id==site.Id);var p=Ground(RecoveryPoint(site));
                saved.Position=p+Vector3.up*.12f;saved.Heading=Quaternion.LookRotation(Flat(nodes[site.Node].Point-p)).eulerAngles.y;
                var marker=environment.GetComponentsInChildren<Transform>().First(t=>t.name==site.Id+" - "+site.Name);
                marker.position=p;
            }
            map.Configure(map.Sites,map.Deliveries,new SerializedObject(map).FindProperty("roadCheckpoints").arraySize==0?Array.Empty<Vector3>():scenicRoads.Where(r=>!r.Edge.Trail).SelectMany(r=>r.Points.Where((p,i)=>i%8==0||i==r.Points.Length-1).Select(p=>p+Vector3.up*.14f)).ToArray());
            EditorUtility.SetDirty(map);
        }
    }
}
