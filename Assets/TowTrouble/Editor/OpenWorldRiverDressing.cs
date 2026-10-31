using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void BuildMountainFormations()
        {
            var parent=ReplaceArtGroup("Mountain outcrops and scree");int count=0;
            for(float z=100;z<450;z+=24)for(float x=-350;x<330;x+=24)
            {
                var anchor=Ground(new Vector3(x+Pick(-8,8),0,z+Pick(-8,8)));
                if(anchor.y<58||ArtNoise(x,z,.018f)<.40f)continue;
                for(int j=0;j<4;j++)
                {
                    var p=Ground(anchor+new Vector3(Pick(-11,11),0,Pick(-11,11)));float height=j==0?Pick(5,12):j==1?Pick(2.5f,6):Pick(.7f,2.4f);
                    if(!ClearRecoveryAccess(p,height*.5f)||RiverDistance(p)<12+height*.6f)continue;
                    float clearance=ScenicDistance(p,out _,out var road)-road.Width;if(clearance<height+3)continue;
                    var go=ArtPrefab("P_BoulderClassic"+(j%3+1),p,height,parent,true);
                    go.transform.rotation*=Quaternion.Euler(Pick(-18,18),0,Pick(-14,14));
                    var b=ScenicBounds(go);go.transform.position+=new Vector3(p.x-b.center.x,Ground(p).y-b.min.y-height*.32f,p.z-b.center.z);
                    b=ScenicBounds(go);float radius=Mathf.Max(b.extents.x,b.extents.z);
                    if(ScenicDistance(b.center,out _,out road)<road.Width+radius+2){UnityEngine.Object.DestroyImmediate(go);continue;}
                    count++;
                }
            }
            Debug.Log("[TowTrouble] Embedded mountain outcrops and scree: "+count);
        }
        private static void BuildRiverBanks()
        {
            var parent=ReplaceArtGroup("River banks - rocks and riparian cover");var centers=RiverPlan();SolveRiver(centers,out var arcs);
            for(int i=4;i<centers.Count-4;i+=7)
            {
                if(centers[i].z>203)continue; // Upper gorge has authored cliffs and pools.
                var flow=Flat(centers[i+1]-centers[i-1]).normalized;var right=Vector3.Cross(Vector3.up,flow);
                foreach(int side in new[]{-1,1})
                {
                    var p=Ground(centers[i]+right*side*Pick(6.5f,10));float roadClear=ScenicDistance(p,out _,out var road)-road.Width;
                    if(roadClear<4||!ClearRecoveryAccess(p,2)||OnRiverBridge(p,3))continue;
                    bool rock=i%3!=0||p.y>45;var go=ArtPrefab(rock?"P_BoulderClassic"+(i%3+1):"P_Marshtail03",p,rock?Pick(.9f,3.1f):Pick(.6f,1.1f),parent,rock);
                    if(rock)go.transform.position-=Vector3.up*.3f;
                }
            }
            // Clear the full river corridor, including canopies, after all dressing passes.
            foreach(string name in new[]{"Authored biome groves and rock formations","Roadside undergrowth and gravel","Embedded rocky road banks","Recovery situations - 24 authored surrounds"})
            {
                var group=environment.Find(name);if(group==null)continue;
                foreach(var child in group.Cast<Transform>().ToArray())
                {
                    var b=ScenicBounds(child.gameObject);float radius=Mathf.Min(6,Mathf.Max(b.extents.x,b.extents.z));
                    if(RiverDistance(b.center)<6+radius)UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
