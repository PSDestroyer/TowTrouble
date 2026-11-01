using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryLoadTests
    {
        [Test] public void TwiceTheMassRequiresTwiceTheGroundForce()
        {
            var a=new RecoveryLoadSettings{Mass=1000,Material=RecoveryLoadMaterial.Mud,Burial=.3f,BuriedWheels=.8f,LipHeight=.1f,LipWheels=.5f,BellyContact=.4f};
            float first=RecoveryLoad.Forces(a,.47f).Total;a.Mass=2000;
            Assert.That(RecoveryLoad.Forces(a,.47f).Total,Is.EqualTo(first*2).Within(.01f));
        }
        [Test] public void BurialLipChassisDamageAndRetainedWaterEachAddLoad()
        {
            var a=new RecoveryLoadSettings{Mass=1350,Material=RecoveryLoadMaterial.Silt};float previous=RecoveryLoad.Forces(a,.47f).Total;
            a.Burial=.3f;a.BuriedWheels=.75f;float next=RecoveryLoad.Forces(a,.47f).Total;Assert.That(next,Is.GreaterThan(previous));previous=next;
            a.LipHeight=.2f;a.LipWheels=.5f;next=RecoveryLoad.Forces(a,.47f).Total;Assert.That(next,Is.GreaterThan(previous));previous=next;
            a.BellyContact=.5f;next=RecoveryLoad.Forces(a,.47f).Total;Assert.That(next,Is.GreaterThan(previous));previous=next;
            a.LockedWheels=.5f;next=RecoveryLoad.Forces(a,.47f).Total;Assert.That(next,Is.GreaterThan(previous));previous=next;
            a.RetainedWater=180;Assert.That(RecoveryLoad.Forces(a,.47f).Total,Is.GreaterThan(previous));
        }
        [Test] public void LargerWheelsReduceTheSameRutAndLipLoad()
        {
            var a=new RecoveryLoadSettings{Mass=1350,Burial=.3f,BuriedWheels=1,LipHeight=.2f,LipWheels=.75f};
            Assert.That(RecoveryLoad.Forces(a,.55f).Total,Is.LessThan(RecoveryLoad.Forces(a,.35f).Total));
        }
        [Test] public void ActualSlopeHelpsDownhillAndCostsForceUphill()
        {
            var normal=Quaternion.Euler(-20,0,0)*Vector3.up;
            Assert.That(RecoveryLoad.AlongSlope(1350,normal,Vector3.forward),Is.GreaterThan(4000));
            Assert.That(RecoveryLoad.AlongSlope(1350,normal,Vector3.back),Is.LessThan(-4000));
            Assert.That(RecoveryLoad.AlongSlope(1350,Vector3.up,Vector3.forward),Is.Zero);
        }
        [Test] public void EnginePowerCannotExceedGroundGripAndSlopeConsumesDrawbar()
        {
            var car=new GaragePerformance{Mass=1350,Drive=6000};
            Assert.That(RecoveryLoad.Drawbar(car,.52f,0,0),Is.LessThan(7000));
            Assert.That(RecoveryLoad.Drawbar(car,1.25f,15,0),Is.LessThan(RecoveryLoad.Drawbar(car,1.25f,0,0)));
            Assert.That(RecoveryLoad.Drawbar(car,.85f,0,340),Is.GreaterThan(RecoveryLoad.Drawbar(car,.52f,0,340)));
        }
        [Test] public void TerrainAwareLoadHoldsGravityWithoutInventingAnUpwardLaunch()
        {
            var cfg=new RecoveryExtractionSettings{Enabled=true,TerrainAware=true,BreakawayForce=18000,MovingForce=16000,MovingDrag=0};
            var normal=Quaternion.Euler(-12,0,0)*Vector3.up;
            var model=new RecoveryExtraction(cfg,Vector3.zero,Vector3.zero,Vector3.forward);
            var pull=Vector3.forward*6000;
            var force=model.Step(Vector3.zero,Vector3.zero,Vector3.zero,pull,1750,true,true,.02f,normal);
            Assert.That(Vector3.Dot(force,normal),Is.EqualTo(0).Within(.01f));
            Assert.That((Vector3.ProjectOnPlane(pull+Physics.gravity*1750,normal)+force).magnitude,Is.LessThan(.01f));
            Assert.That(model.Step(Vector3.zero,Vector3.zero,Vector3.zero,pull,1750,false,true,.02f,normal),Is.EqualTo(Vector3.zero));
        }
        [Test] public void TerrainAwarePocketDoesNotLoseItsLoadAfterATinyJerk()
        {
            var cfg=new RecoveryExtractionSettings{Enabled=true,TerrainAware=true,Travel=5,BreakawayForce=14000,MovingForce=12320,MovingDrag=0};
            var model=new RecoveryExtraction(cfg,Vector3.zero,Vector3.zero,Vector3.forward);
            var point=Vector3.forward*.05f;var pull=Vector3.forward*11000;
            Assert.That(model.Step(point,point,Vector3.zero,pull,1550,true,true,.02f,Vector3.up).magnitude,Is.EqualTo(11000).Within(.01f));
            Assert.That(model.IsFree,Is.False);
        }
        [Test] public void SoftGroundTracksFollowTheVisibleSkinAboveTheirSupportBed()
        {
            var go=new GameObject("Inactive soft-ground projection fixture");go.SetActive(false);
            var bed=new Mesh();var skin=new Mesh();
            try
            {
                bed.vertices=new[]{new Vector3(-2,0,-2),new Vector3(2,0,-2),new Vector3(-2,0,2),new Vector3(2,0,2)};
                skin.vertices=new[]{new Vector3(-2,.2f,-2),new Vector3(2,.3f,-2),new Vector3(-2,.3f,2),new Vector3(2,.4f,2)};
                var track=go.AddComponent<RecoveryTrackSurface>();track.ConfigureVisualCover(skin,bed);
                Assert.That(track.VisualPoint(Vector3.zero).y,Is.EqualTo(.3f).Within(.001f));
                go.transform.SetPositionAndRotation(new Vector3(20,10,30),Quaternion.Euler(0,45,0));
                Assert.That(track.VisualPoint(go.transform.position).y,Is.EqualTo(10.3f).Within(.001f));
                track.ConfigureVisualCover(bed,bed);Assert.That(track.VisualPoint(go.transform.position).y,Is.EqualTo(10));
            }
            finally {Object.DestroyImmediate(go);Object.DestroyImmediate(bed);Object.DestroyImmediate(skin);}
        }
        [Test] public void MapCarriesMassAndSoilSettingsWhileTutorialMassStaysLegacy()
        {
            var go=new GameObject("Inactive load contract fixture");go.SetActive(false);
            try
            {
                var load=new RecoveryLoadSettings{Mass=1750,Material=RecoveryLoadMaterial.Snow,Burial=.43f};
                var map=go.AddComponent<RecoveryMap>();map.Configure(new[]{new RecoverySite{Load=load}},new RecoveryDropoff[0],new Vector3[0]);
                Assert.That(map.Contracts[0].Load,Is.SameAs(load));Assert.That(map.Contracts[0].Load.Mass,Is.EqualTo(1750));
                foreach(var c in RecoveryContract.All)Assert.That(c.Load,Is.Null);
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void StockCableShocksCannotBuildHardMissionLoadThroughLoadSmoothing()
        {
            for(float demand=11000;demand<30000;demand+=100)
            {
                float stress=0,loaded=11000;
                for(int frame=0;frame<100;frame++)
                {
                    stress=RecoveryRules.CableStressAfter(stress,demand,11000,.02f);
                    if(stress>=1)break;
                    loaded=RecoveryExtraction.LoadedCableForce(loaded,demand,11000,.02f);
                    Assert.That(loaded,Is.LessThan(13200));
                }
            }
        }
    }
}
