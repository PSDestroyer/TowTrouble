using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Editor
{
    public static partial class OpenWorldAuthoring
    {
        private static bool RoadsideRecovery(RecoverySite site)=>site.Id=="R10"||site.Id=="R12"||site.Id=="R23";
        private static void DressRoadsideRecoveries(RecoveryWorld world)
        {
            var baseline=JsonUtility.FromJson<SituationBaseline>(System.IO.File.ReadAllText(SituationEvidence+"/TerrainPatchesBaseline.json"));
            var roads=environment.Find("Road network - 24 recovery spurs").GetComponentsInChildren<MeshFilter>().SelectMany(f=>
            {var v=f.sharedMesh.vertices;return Enumerable.Range(0,v.Length/6).Where(i=>i%3==0).Select(i=>f.transform.TransformPoint((v[i*6+2]+v[i*6+3])*.5f));}).ToArray();var data=terrain.terrainData;
            foreach(int index in new[]{9,11,22})
            {
                var site=world.Map.Sites[index];var reference=baseline.Patches[index].Center;
                Vector3 best=default,bestRoad=default;float score=float.MaxValue;
                foreach(var road in roads.Where(p=>Flat(p-reference).sqrMagnitude<180*180))
                for(int angle=0;angle<360;angle+=15)for(int distance=7;distance<=18;distance+=1)
                {
                    var p=Ground(road+Quaternion.Euler(0,angle,0)*Vector3.forward*distance);var land=Ground(road);
                    float rise=land.y-p.y;if(rise<1.6f||rise>8)continue;
                    var normal=data.GetInterpolatedNormal((p.x-terrain.transform.position.x)/data.size.x,(p.z-terrain.transform.position.z)/data.size.z);
                    float slope=Vector3.Angle(normal,Vector3.up);if(slope<14||slope>32)continue;
                    if(roads.Min(r=>Flat(r-p).sqrMagnitude)<25)continue;
                    if(world.Map.Sites.Where(s=>s!=site).Any(s=>Flat(s.Position-p).sqrMagnitude<18*18))continue;
                    var exit=Flat(land-p).normalized;bool smooth=true;
                    for(int step=1;step<distance;step++)
                    {
                        var a=Ground(p+exit*(step-1));var b=Ground(p+exit*step);
                        if(b.y-a.y>1.25f||b.y-a.y<-.8f){smooth=false;break;}
                    }
                    if(!smooth)continue;
                    // Prefer a nearby continuous bank with a clear rise to the road.
                    float cost=Flat(p-reference).magnitude*.55f+Mathf.Abs(slope-(18+site.Tier*3))*2.5f+Mathf.Abs(distance-(6+site.Tier*2))*2+Mathf.Abs(rise-(1.4f+site.Tier*.8f))*4;
                    if(cost<score){score=cost;best=p;bestRoad=land;}
                }
                if(score==float.MaxValue)throw new InvalidOperationException("No safe roadside bank for "+site.Id);
                Undo.RecordObject(world.Map,"Replace decorative rock recovery with roadside bank");
                site.Position=best;site.ExitDirection=Flat(bestRoad-best).normalized;site.Heading=Quaternion.LookRotation(site.ExitDirection).eulerAngles.y+25;
                site.Kind=RecoverySituationKind.Embankment;
                site.Name=index==9?"Alunecare sub drumul de munte":index==11?"Ieșire de pe serpentină":"Taluzul canionului";
                site.English=index==9?"Below the mountain road":index==11?"Slipped off the switchback":"Canyon roadside bank";
                site.Situation="Mașina a alunecat sub marginea drumului și stă înclinată pe taluz. Poziționează-te sus pe drum și trage înapoi pe pantă.";
                site.EnglishSituation="The car slipped below the road and sits tilted on the bank. Set up on the road above and pull it back uphill.";
                FitSituationCar(site,world.Casualty);
                var group=environment.Find("Distinct recovery situations").Cast<Transform>().Single(t=>t.name.StartsWith(site.Id+" -"));
                foreach(Transform child in group.Cast<Transform>().ToArray())Undo.DestroyObjectImmediate(child.gameObject);
                group.name=site.Id+" - Roadside bank";group.SetPositionAndRotation(site.Position,Quaternion.LookRotation(site.ExitDirection));
                var setting=environment.Find("Recovery situations - 24 authored surrounds")?.Find(site.Id+" recovery setting");
                if(setting!=null)foreach(Transform child in setting)child.gameObject.SetActive(false);
                DressRoadsideAccidentTrail(group,site,bestRoad);
            }
        }
    }
}
