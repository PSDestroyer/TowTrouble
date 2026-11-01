using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Editor
{
    public static class RecoveryProgressionAudit
    {
        private static bool ClearSetup(Vector3 point,Vector3 direction,Collider floor)
        {
            var rotation=Quaternion.LookRotation(direction);
            foreach(var blocker in Physics.OverlapBox(point+Vector3.up*.85f,new Vector3(.9f,.55f,1.8f),rotation,1<<8,QueryTriggerInteraction.Ignore))
            {
                if(blocker==floor||blocker is TerrainCollider||blocker.TryGetComponent<RecoveryGroundSurface>(out _)||blocker.TryGetComponent<RecoveryMudTerrain>(out _)||blocker.TryGetComponent<RecoveryTrackSurface>(out _)||blocker.name.StartsWith("Road-")||blocker.GetComponentInParent<RecoveryVehicle>()!=null||blocker.GetComponentInParent<RecoveryTowObstacle>()!=null)continue;
                return false;
            }
            var side=Vector3.Cross(Vector3.up,direction);float lowest=float.PositiveInfinity,highest=float.NegativeInfinity;
            for(int i=0;i<4;i++)
            {
                var p=point+side*(i%2==0?-.8f:.8f)+direction*(i<2?1.3f:-1.3f);
                if(!Physics.Raycast(p+Vector3.up*3,Vector3.down,out var hit,6,1<<8,QueryTriggerInteraction.Ignore))return false;
                lowest=Mathf.Min(lowest,hit.point.y);highest=Mathf.Max(highest,hit.point.y);
            }
            return highest-lowest<1.7f;
        }
        [MenuItem("Tow Trouble/Recovery/Audit Mission Load Progression")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode only");
            var world=UnityEngine.Object.FindFirstObjectByType<RecoveryWorld>();var map=world.Map;
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
            var obstacles=UnityEngine.Object.FindObjectsByType<RecoveryTowObstacle>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var rows=new List<object>();var csv=new System.Text.StringBuilder("Site,Tier,Vehicle,Mass kg,Material,Burial m,Lip m,Locked fraction,Static ground kN,Gravity kN,Aligned demand kN,Best stock steady kN,Best equipped steady kN,Equipped pull margin kN,Setup X,Setup Z,Separate obstacle kg,Separate obstacle static kN\n");
            foreach(var site in map.Sites)
            {
                var model=world.Casualty.ModelAt(site.TargetModel);var load=site.Load;
                var forces=RecoveryLoad.Forces(load,model.Radius);
                var rotation=Quaternion.Euler(site.Pitch,site.Heading,site.Roll);var up=rotation*Vector3.up;var normal=Vector3.zero;int contacts=0;
                for(int i=0;i<4;i++)if(Physics.Raycast(site.Position-Vector3.up*site.Depth+rotation*model.Anchors[i]+up*RecoveryWheelPose.ProbeLift,-up,out var hit,model.Radius+.7f,1<<8,QueryTriggerInteraction.Ignore)){normal+=hit.normal;contacts++;}
                normal=contacts>0?normal.normalized:Vector3.up;
                float mass=load.Mass+load.RetainedWater;
                float staticForce=site.Extraction.BreakawayForce*(site.Extraction.TerrainAware?Mathf.Max(.25f,normal.y):1);
                float gravity=RecoveryLoad.AlongSlope(mass,normal,site.ExitDirection);
                float demand=Mathf.Max(0,staticForce+gravity);
                float stockBest=0,equippedBest=0;Vector3 equippedSetup=default;string equippedModel="";float bestGrade=0,bestGrip=0;int samples=0;
                var configs=new List<object>();
                for(int m=0;m<4;m++)for(int stage=0;stage<3;stage++)
                {
                    var build=new RecoveryBuild{Model=m};
                    if(stage>0)
                    {
                        foreach(var id in new[]{stage==1?"engine_2":"engine_3","low_range",stage==1?"cable_18":"cable_26",stage==1?"winch_2":"winch_3","suspension_offroad",load.Material==RecoveryLoadMaterial.Snow?(stage==1?"tires_winter":"tires_winter_pro"):load.Material==RecoveryLoadMaterial.Sand?"tires_sand":"tires_mud"})
                        {var part=catalog.Find(id);if(part!=null)build.Set(part,catalog);}
                    }
                    var performance=GaragePerformance.Calculate(build,catalog);float best=0;Vector3 bestPoint=default;float gradeAtBest=0,gripAtBest=0;
                    // Candidate rear-hitch setups inside a 19m cable envelope, including a firm shoulder.
                    for(int a=-2;a<=2;a++)for(int length=7;length<=19;length+=2)
                    {
                        var dir=Quaternion.Euler(0,a*12,0)*site.ExitDirection.normalized;
                        var p=site.Position+dir*length;
                        if(!Physics.Raycast(new Vector3(p.x,site.Position.y+20,p.z),Vector3.down,out var hit,60,1<<8,QueryTriggerInteraction.Ignore))continue;
                        if(hit.collider.GetComponentInParent<RecoveryVehicle>()!=null||hit.normal.y<.65f)continue;
                        p=hit.point;var slopeTangent=Vector3.ProjectOnPlane(dir,hit.normal).normalized;
                        float grade=Mathf.Asin(Mathf.Clamp(slopeTangent.y,-1,1))*Mathf.Rad2Deg;
                        float grip=1.25f,drag=0;
                        if(hit.collider.TryGetComponent<RecoveryMudTerrain>(out var mud))
                        {grip=RecoveryTraction.Grip(mud.LongitudinalGrip*performance.MudGrip,.45f,mud.AggressiveThrottleLoss);drag=mud.ResistancePerWheel*performance.MudResistance;}
                        else if(hit.collider.TryGetComponent<RecoveryGroundSurface>(out var surface))
                        {var kind=surface.KindAt(p);grip=kind==RecoveryGroundKind.Snow?.52f*performance.SnowGrip:kind==RecoveryGroundKind.Sand?.68f*performance.SandGrip:1.25f;drag=RecoveryTraction.SoftGroundDrag(kind,surface.Softness,kind==RecoveryGroundKind.Snow?performance.SnowGrip:performance.SandGrip);}
                        float drive=RecoveryLoad.Drawbar(performance,grip,grade,drag);
                        // A braked winch can hold only the friction budget remaining after slope gravity.
                        float weight=performance.Mass*9.81f,angle=grade*Mathf.Deg2Rad;
                        float hold=Mathf.Max(0,weight*(Mathf.Cos(angle)*grip-Mathf.Sin(angle)));
                        float winch=Mathf.Min(performance.WinchForce,hold);
                        float aligned=Vector3.Dot(dir,site.ExitDirection.normalized);
                        float anglePenalty=Mathf.Lerp(1,1.9f,Mathf.InverseLerp(.9f,0,aligned));
                        float useful=Mathf.Min(performance.Cable*.95f,Mathf.Max(drive,winch))/anglePenalty;
                        if(useful>best&&ClearSetup(p,dir,hit.collider)){best=useful;bestPoint=p;gradeAtBest=grade;gripAtBest=grip;}
                        samples++;
                    }
                    if(stage==0)stockBest=Mathf.Max(stockBest,best);
                    else if(best>equippedBest){equippedBest=best;equippedSetup=bestPoint;equippedModel=RecoveryVehicle.ModelNames[m]+(stage==1?" L2":" L3");bestGrade=gradeAtBest;bestGrip=gripAtBest;}
                    configs.Add(new{model=RecoveryVehicle.ModelNames[m],stage,steadyKN=Math.Round(best/1000,2),clearanceKN=Math.Round((best-demand)/1000,2)});
                }
                var obstacle=Array.Find(obstacles,o=>o.SiteIndex==Array.IndexOf(map.Sites,site));
                float obstacleMass=0,obstacleForce=0;
                if(obstacle!=null)
                {
                    obstacleMass=obstacle.Body.mass;var n=Vector3.up;
                    if(Physics.Raycast(obstacle.Home+Vector3.up*3,Vector3.down,out var h,8,1<<8,QueryTriggerInteraction.Ignore)&&h.collider.GetComponent<RecoveryTowObstacle>()==null)n=h.normal;
                    obstacleForce=Mathf.Max(0,obstacleMass*9.81f*.6f*n.y+RecoveryLoad.AlongSlope(obstacleMass,n,obstacle.Side));
                }
                if(site.Tier>=3&&demand<13200)throw new InvalidOperationException(site.Id+" needs a stronger physical scenario to exceed stock shock capacity");
                if(equippedBest<demand*1.04f)throw new InvalidOperationException(site.Id+" has no equipped solo force margin in the sampled setups");
                var record=new{site.Id,site.Tier,model=RecoveryVehicle.ModelNames[site.TargetModel],massKg=mass,material=load.Material.ToString(),load.Burial,load.BuriedWheels,load.LipHeight,load.LipWheels,load.BellyContact,load.LockedWheels,contacts,forces,obstacleMassKg=obstacleMass,obstacleStaticKN=Math.Round(obstacleForce/1000,2),groundKN=Math.Round(staticForce/1000,2),gravityKN=Math.Round(gravity/1000,2),demandKN=Math.Round(demand/1000,2),stockKN=Math.Round(stockBest/1000,2),equippedKN=Math.Round(equippedBest/1000,2),marginKN=Math.Round((equippedBest-demand)/1000,2),equippedModel,setup=new[]{equippedSetup.x,equippedSetup.y,equippedSetup.z},bestGrade,bestGrip,samples,configs};rows.Add(record);
                csv.AppendLine(FormattableString.Invariant($"{site.Id},{site.Tier},{RecoveryVehicle.ModelNames[site.TargetModel]},{mass:0},{load.Material},{load.Burial:0.00},{load.LipHeight:0.00},{load.LockedWheels:0.00},{staticForce/1000:0.00},{gravity/1000:0.00},{demand/1000:0.00},{stockBest/1000:0.00},{equippedBest/1000:0.00},{(equippedBest-demand)/1000:0.00},{equippedSetup.x:0.0},{equippedSetup.z:0.0},{obstacleMass:0},{obstacleForce/1000:0.00}"));
            }
            Directory.CreateDirectory("Docs/Validation/RecoveryProgression");File.WriteAllText("Docs/Validation/RecoveryProgression/Loads.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));File.WriteAllText("Docs/Validation/RecoveryProgression/Loads.csv",csv.ToString());
        }
    }
}
