using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryBodyBalanceTests
    {
        private RecoveryGarageCatalog Catalog => AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
        [Test] public void BodyPricesHaveProgressionAndEqualProfilesHaveEqualPrices()
        {
            var bodies=Catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly&&p.Price>0).ToArray();
            Assert.That(bodies.Min(p=>p.Price),Is.LessThanOrEqualTo(1800));
            Assert.That(bodies.Max(p=>p.Price),Is.GreaterThanOrEqualTo(4000));
            Assert.That(bodies.Select(p=>p.Price).Distinct().Count(),Is.GreaterThan(10));
            foreach(var group in bodies.GroupBy(p=>(p.BodyMass,p.BodyDrive,p.BodySpeed)))
                Assert.That(group.Select(p=>p.Price).Distinct().Count(),Is.EqualTo(1));
            Assert.That(bodies.First(p=>p.Name=="Historic Lorry").Price,Is.GreaterThan(bodies.First(p=>p.Name=="Historic Bucket").Price));
        }
        [Test] public void EveryBodyAndWheelCombinationRespectsTheEightyKmhCeiling()
        {
            var catalog=Catalog;
            foreach(var body in catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly))
                foreach(var wheel in catalog.Parts.Where(p=>p.Slot==GarageSlot.Wheels))
                {
                    var b=new RecoveryBuild();b.Set(body,catalog);b.Set(wheel,catalog);
                    float kmh=GaragePerformance.Calculate(b,catalog).Speed*3.6f;
                    Assert.That(kmh,Is.InRange(40,80.001f),body.Name+" / "+wheel.Name);
                }
            Assert.That(catalog.Parts.Max(p=>p.BodySpeed)*3.6f,Is.EqualTo(80).Within(.001f));
        }
        [Test] public void FreeBodyArchetypesRetainPullAndHaveDistinctTravelSpeeds()
        {
            var expected=new[]{68f,60f,72f,66f};
            for(int model=0;model<4;model++)
            {
                var b=new RecoveryBuild{Model=model};b.Normalize(Catalog);
                var stats=GaragePerformance.Calculate(b,Catalog);
                Assert.That(stats.Speed*3.6f,Is.EqualTo(expected[model]).Within(.001f));
                Assert.That(stats.Drive,Is.EqualTo(RecoveryVehicle.PullForces[model]));
                Assert.That(stats.Mass,Is.EqualTo(RecoveryVehicle.Masses[model]));
            }
        }
        [Test] public void CruisingCompensatesOrdinaryDragWithoutAddingRecoveryForce()
        {
            const float force=3020,mass=1140,damping=.08f,limit=80f/3.6f;
            Assert.That(RecoveryTraction.SpeedLimitedDrive(force,0,limit,mass,damping,4),Is.EqualTo(force));
            Assert.That(RecoveryTraction.SpeedLimitedDrive(force,limit,limit,mass,damping,4),Is.EqualTo(limit*(35+mass*damping/4)).Within(.01f));
            Assert.That(RecoveryTraction.SpeedLimitedDrive(force,limit+8,limit,mass,damping,4),Is.Zero);
            for(float speed=-30;speed<=30;speed+=.1f)
                Assert.That(RecoveryTraction.SpeedLimitedDrive(force,speed,limit,mass,damping,4),Is.InRange(0,force));
        }
        [Test] public void CatalogOrderingRemainsAscendingAndNumericLevelsReadNaturally()
        {
            var profile=new RecoveryProfile{language="en"};var parts=Catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly).ToList();
            parts.Sort((a,b)=>RecoveryGarageOrdering.Compare(a,b,profile));
            Assert.That(parts[0].Id,Is.EqualTo("stock_Assembly"));
            for(int i=1;i<parts.Count;i++)Assert.That(parts[i].Price,Is.GreaterThanOrEqualTo(parts[i-1].Price));
            Assert.That(RecoveryGarageOrdering.NaturalCompare("Plate Lvl2 Front","Plate Lvl10 Front"),Is.LessThan(0));
            Assert.That(RecoveryGarageOrdering.NaturalCompare("Level 0002","Level 2"),Is.Zero);
            Assert.That(RecoveryGarageOrdering.NaturalCompare("engine 3","Engine 2"),Is.GreaterThan(0));
        }
        [Test] public void RepricedBodiesPurchaseAndSellOnceWithoutChangingExistingOwnership()
        {
            var catalog=Catalog;var body=catalog.Parts.First(p=>p.Name=="Historic Lorry"&&p.Slot==GarageSlot.Assembly);
            var profile=new RecoveryProfile{credits=body.Price-1};
            Assert.That(RecoveryEconomy.Buy(profile,body),Is.False);
            profile.credits++;Assert.That(RecoveryEconomy.Buy(profile,body),Is.True);Assert.That(profile.credits,Is.Zero);
            var copy=RecoveryProfile.Parse(JsonUtility.ToJson(profile));Assert.That(copy.Owns(body.Id),Is.True);
            Assert.That(RecoveryEconomy.Buy(copy,body),Is.False);
            Assert.That(RecoveryEconomy.Sell(copy,body,catalog),Is.True);Assert.That(copy.credits,Is.EqualTo(body.Price*70/100));
            Assert.That(RecoveryEconomy.Sell(copy,body,catalog),Is.False);
        }
    }
}
