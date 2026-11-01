using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static void RepairNaturalLakeRecoveries(RecoveryWorld world)
        {
            var data=terrain.terrainData;
            var baseline=JsonUtility.FromJson<SituationBaseline>(File.ReadAllText(SituationEvidence+"/TerrainPatchesBaseline.json"));
            if(baseline.Resolution!=data.heightmapResolution||baseline.TerrainGuid!=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data)))throw new InvalidOperationException("Lake terrain baseline mismatch");
            Undo.RegisterCompleteObjectUndo(data,"Restore original connected lakeshore");
            foreach(int i in new[]{4,7})
            {
                var patch=baseline.Patches[i];var heights=new float[patch.Height,patch.Width];
                for(int z=0;z<patch.Height;z++)for(int x=0;x<patch.Width;x++)heights[z,x]=patch.Heights[z*patch.Width+x];
                data.SetHeights(patch.X,patch.Z,heights);
            }
            var lake=environment.GetComponentsInChildren<MeshFilter>().First(f=>f.name.StartsWith("Lake -"));
            var polygon=lake.sharedMesh.vertices.Select(lake.transform.TransformPoint).ToArray();var centre=polygon.Aggregate(Vector3.zero,(a,b)=>a+b)/polygon.Length;
            float waterY=polygon[0].y;
            foreach(int i in new[]{4,7})
            {
                var site=world.Map.Sites[i];var reference=baseline.Patches[i].Center;
                Vector3 best=default;float score=float.MaxValue;
                // Select real shallow water connected to the main lake, rather than excavating
                // a round pool at the old job marker. The shore itself is not carved.
                for(int z=-40;z<=40;z++)for(int x=-40;x<=40;x++)
                {
                    var p=Ground(reference+new Vector3(x,0,z));if(p.y<waterY-.5f||p.y>waterY-.16f||!InsideNaturalLake(p,polygon))continue;
                    var inward=Flat(centre-p).normalized;bool connected=true;
                    for(int step=1;step<=10;step++)if(Ground(p+inward*(step*2)).y>waterY-.10f){connected=false;break;}
                    if(!connected)continue;
                    float cost=Flat(p-reference).sqrMagnitude+Mathf.Abs(p.y-(waterY-.28f))*30;
                    if(cost<score){score=cost;best=p;}
                }
                if(score==float.MaxValue)throw new InvalidOperationException("No natural shallow-water recovery found for "+site.Id);
                var outward=Flat(best-centre).normalized;Vector3 exit=outward;float landScore=float.MaxValue;
                for(int angle=-70;angle<=70;angle+=5)
                {
                    var direction=Quaternion.Euler(0,angle,0)*outward;
                    for(int distance=5;distance<=24;distance++)if(Ground(best+direction*distance).y>waterY+.65f)
                    {float cost=distance+Mathf.Abs(angle)*.05f;if(cost<landScore){landScore=cost;exit=direction;}break;}
                }
                var old=site.Position;site.Position=best;site.ExitDirection=exit;site.Heading=Quaternion.LookRotation(exit).eulerAngles.y;
                var group=environment.Find("Distinct recovery situations").Cast<Transform>().Single(t=>t.name.StartsWith(site.Id+" -"));
                Undo.RecordObject(group,"Move recovery to real shallow water");group.position+=best-old;
                foreach(Transform child in group)
                {
                    if(child.name=="Approach from existing road")continue;
                    var renderers=child.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                    var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                    child.position+=Vector3.up*(Ground(bounds.center).y-bounds.min.y-.06f);
                }
                FitSituationCar(site,world.Casualty);
            }
            EditorUtility.SetDirty(data);Physics.SyncTransforms();
        }
        private static bool InsideNaturalLake(Vector3 p,Vector3[] polygon)
        {
            bool inside=false;for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
                if((polygon[i].z>p.z)!=(polygon[j].z>p.z)&&p.x<(polygon[j].x-polygon[i].x)*(p.z-polygon[i].z)/(polygon[j].z-polygon[i].z)+polygon[i].x)inside=!inside;
            return inside;
        }
    }
}
