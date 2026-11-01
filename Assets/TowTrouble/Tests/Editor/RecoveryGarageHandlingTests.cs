using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryGarageHandlingTests
    {
        private RecoveryGarageCatalog catalog;
        [SetUp] public void Setup(){catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");}
        [Test] public void EveryWheelHasAHonestCardAndPositiveSurfaceResponse()
        {
            foreach(var p in catalog.Parts.Where(p=>p.Slot==GarageSlot.Wheels))
            {
                Assert.That(p.TireKind,Is.Not.EqualTo(GarageTireKind.Unspecified),p.Id);
                foreach(float value in new[]{p.DryGrip,p.MudGrip,p.SandGrip,p.SnowGrip,p.MudResistance})Assert.That(value,Is.GreaterThan(0).And.LessThan(3),p.Id);
                Assert.That(RecoveryGarageCardInfo.Performance(p,false),Is.Not.Empty,p.Id);Assert.That(RecoveryGarageCardInfo.Performance(p,true),Is.Not.Empty,p.Id);
            }
        }
        [Test] public void RoadAndSnowTiresHaveDifferentRealTractionAndKeepTheirTradeoffs()
        {
            var road=catalog.Parts.First(p=>p.TireKind==GarageTireKind.Road);var snow=catalog.Parts.First(p=>p.TireKind==GarageTireKind.Winter&&p.Id.StartsWith("part_"));
            var build=new RecoveryBuild();build.Set(road,catalog);var a=GaragePerformance.Calculate(build,catalog);build.Set(snow,catalog);var b=GaragePerformance.Calculate(build,catalog);
            Assert.That(a.DryGrip,Is.GreaterThan(b.DryGrip));Assert.That(a.SnowGrip,Is.LessThan(b.SnowGrip));Assert.That(a.DryGrip,Is.EqualTo(road.DryGrip));Assert.That(b.SnowGrip,Is.EqualTo(snow.SnowGrip));
            Assert.That(a.SandGrip,Is.LessThan(1));
        }
        [Test] public void SpecialistTireProgressionKeepsItsApprovedLimits()
        {
            foreach(var item in new[]{("tires_mud",1.3f),("tires_sand",1.45f),("tires_winter",1.65f),("tires_winter_pro",2.35f)})
            {
                var b=new RecoveryBuild();b.Set(catalog.Find(item.Item1),catalog);var s=GaragePerformance.Calculate(b,catalog);
                Assert.That(item.Item1=="tires_mud"?s.MudGrip:item.Item1=="tires_sand"?s.SandGrip:s.SnowGrip,Is.EqualTo(item.Item2).Within(.001f));
            }
        }
        [Test] public void AChosenBodyUsesItsOwnMassAndPowerOnEveryBaseVehicle()
        {
            var body=catalog.Parts.First(p=>p.Slot==GarageSlot.Assembly&&p.Name.Contains("Lorry"));
            for(int model=0;model<4;model++)
            {
                var b=new RecoveryBuild{Model=model};b.Set(body,catalog);var s=GaragePerformance.Calculate(b,catalog);
                Assert.That(s.Mass,Is.EqualTo(body.BodyMass));Assert.That(s.Drive,Is.EqualTo(body.BodyDrive));Assert.That(s.Speed,Is.EqualTo(body.BodySpeed));
            }
        }
        [Test] public void BodyEngineAndTransmissionComposeInEitherItemOrder()
        {
            var body=catalog.Parts.First(p=>p.Slot==GarageSlot.Assembly&&p.BodyMass>0);var ids=new[]{body.Id,"engine_2","low_range"};
            var a=GaragePerformance.Calculate(new RecoveryBuild{Items=ids},catalog);var b=GaragePerformance.Calculate(new RecoveryBuild{Items=ids.Reverse().ToArray()},catalog);
            Assert.That(a.Drive,Is.EqualTo(body.BodyDrive*1.2f*1.25f).Within(.01f));Assert.That(a.Speed,Is.EqualTo(body.BodySpeed*.8f).Within(.001f));Assert.That(a.Drive,Is.EqualTo(b.Drive).Within(.01f));Assert.That(a.Mass,Is.EqualTo(b.Mass));
        }
        [Test] public void EveryBodyHasMassAndPullInformationAndFamiliesOfferDifferentTradeoffs()
        {
            var bodies=catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly).ToArray();
            foreach(var p in bodies){Assert.That(RecoveryGarageCardInfo.Performance(p,true,3),Does.Contain("kg"));Assert.That(RecoveryGarageCardInfo.Performance(p,false,0),Does.Contain("kN"));if(p.Price>0){Assert.That(p.BodyMass,Is.InRange(900,2400));Assert.That(p.BodyDrive,Is.GreaterThan(0));}}
            Assert.That(bodies.Where(p=>p.Price>0).Select(p=>p.BodyMass+"/"+p.BodyDrive+"/"+p.BodySpeed).Distinct().Count(),Is.GreaterThan(15));
            var light=bodies.First(p=>p.Name=="Historic Bucket");var heavy=bodies.First(p=>p.Name=="Historic Lorry");Assert.That(light.BodyMass,Is.LessThan(heavy.BodyMass));Assert.That(light.BodyDrive,Is.LessThan(heavy.BodyDrive));Assert.That(light.BodySpeed,Is.GreaterThan(heavy.BodySpeed));
        }
        [Test] public void StockBodiesAndTiresKeepTheirOriginalVehicleBaselines()
        {
            for(int i=0;i<4;i++){var build=new RecoveryBuild{Model=i};build.Normalize(catalog);var stats=GaragePerformance.Calculate(build,catalog);Assert.That(stats.Mass,Is.EqualTo(RecoveryVehicle.Masses[i]));Assert.That(stats.Drive,Is.EqualTo(RecoveryVehicle.PullForces[i]));Assert.That(stats.DryGrip,Is.EqualTo(1));Assert.That(stats.SandGrip,Is.EqualTo(1));Assert.That(stats.SnowGrip,Is.EqualTo(1));}
        }
    }
}
