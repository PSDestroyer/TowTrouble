using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryStarterProgressionTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void FirstChoiceGrantsExactlyTheSelectedStarterOnce(int model)
        {
            var p=RecoveryProfile.Parse("");Assert.That(p.starterChosen,Is.False);Assert.That(p.ownedVehicles,Is.Zero);
            Assert.That(RecoveryEconomy.ChooseStarter(p,model),Is.True);Assert.That(p.credits,Is.Zero);
            Assert.That(p.vehicle,Is.EqualTo(model));Assert.That(p.starterVehicle,Is.EqualTo(model));Assert.That(p.ownedVehicles,Is.EqualTo(1<<model));
            Assert.That(RecoveryEconomy.ChooseStarter(p,(model+1)%4),Is.False);
            p.RecordFinish(0,true,true);p.RecordFinish(1,true,true);
            var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(copy.starterChosen,Is.True);Assert.That(copy.starterVehicle,Is.EqualTo(model));Assert.That(copy.ownedVehicles,Is.EqualTo(1<<model));
        }
        [Test] public void OtherStarterVariantsArePaidIncludingScoutWhenRangerWasChosen()
        {
            var p=RecoveryProfile.Parse("");RecoveryEconomy.ChooseStarter(p,3);p.credits=5000;
            Assert.That(Enumerable.Range(0,4).Select(RecoveryEconomy.VehiclePrice),Is.EqualTo(new[]{1200,1200,1200,1200}));
            Assert.That(RecoveryEconomy.BuyVehicle(p,0),Is.True);Assert.That(p.credits,Is.EqualTo(3800));Assert.That(RecoveryEconomy.BuyVehicle(p,0),Is.False);p.vehicle=0;
            var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(copy.starterVehicle,Is.EqualTo(3));Assert.That(copy.OwnsVehicle(0),Is.True);Assert.That(copy.OwnsVehicle(3),Is.True);Assert.That(copy.OwnsVehicle(1),Is.False);
        }
        [Test] public void UnchosenProfilesAndInsufficientFundsCannotBuyVehicles()
        {
            var p=new RecoveryProfile{credits=5000};Assert.That(RecoveryEconomy.BuyVehicle(p,1),Is.False);
            RecoveryEconomy.ChooseStarter(p,0);p.credits=1199;
            foreach(int model in new[]{-1,0,1,2,3,4})Assert.That(RecoveryEconomy.BuyVehicle(p,model),Is.False);
            Assert.That(p.credits,Is.EqualTo(1199));Assert.That(p.ownedVehicles,Is.EqualTo(1));
        }
        [Test] public void LegacyMigrationKeepsActiveRideEquipmentAndMoneyWithoutGrantingOtherModels()
        {
            var old=new RecoveryProfile{version=2,vehicle=3,completedMask=7,credits=851};old.ownedParts.Add("cable_18");old.Build(3).Items=new[]{"cable_18"};
            var p=RecoveryProfile.Parse(JsonUtility.ToJson(old));Assert.That(p.version,Is.EqualTo(4));Assert.That(p.vehicle,Is.EqualTo(3));Assert.That(p.starterVehicle,Is.EqualTo(3));Assert.That(p.ownedVehicles,Is.EqualTo(9));Assert.That(p.credits,Is.EqualTo(851));
            Assert.That(p.Build(3).Items,Does.Contain("cable_18"));Assert.That(p.ownedParts,Does.Contain("cable_18"));
            Assert.That(RecoveryProfile.Parse(JsonUtility.ToJson(p)).ownedVehicles,Is.EqualTo(9));
        }
        [Test] public void CurrentProfilesCannotSelectAnUnownedVehicleThroughSavedIndex()
        {
            var p=RecoveryProfile.Parse("{\"version\":4,\"starterChosen\":true,\"starterVehicle\":2,\"vehicle\":3,\"ownedVehicles\":4,\"credits\":2000}");
            Assert.That(p.vehicle,Is.EqualTo(2));Assert.That(p.credits,Is.EqualTo(2000));Assert.That(p.ownedVehicles,Is.EqualTo(4));
        }
        [Test] public void AnUnusedLegacyScoutProfileStillGetsItsFirstChoice()
        {
            var p=RecoveryProfile.Parse("{\"version\":3,\"ownedVehicles\":1,\"onboarded\":true}");
            Assert.That(p.starterChosen,Is.False);Assert.That(p.ownedVehicles,Is.Zero);
            RecoveryEconomy.ChooseStarter(p,3);var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(copy.ownedVehicles,Is.EqualTo(8));Assert.That(copy.OwnsVehicle(0),Is.False);
        }
        [Test] public void LegacyMigrationPreservesVehiclesWithInvestedEquipmentOrSavedSetups()
        {
            var old=new RecoveryProfile{version=2};old.ownedParts.Add("engine_2");old.Build(1).Items=new[]{"engine_2"};old.presets.Add(new GaragePreset{Name="setup_3_0",Build=old.Build(3).Copy()});
            var p=RecoveryProfile.Parse(JsonUtility.ToJson(old));Assert.That(p.ownedVehicles,Is.EqualTo(11));Assert.That(p.OwnsVehicle(2),Is.False);Assert.That(p.starterChosen,Is.True);
        }
        [Test] public void EveryPaidBodyHasMoreBasePullThanEveryStarter()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
            var paid=catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly&&p.Price>0).ToArray();var first=paid.OrderBy(p=>p.Price).First();
            for(int model=0;model<4;model++)
            {
                var stock=new RecoveryBuild{Model=model};stock.Normalize(catalog);var before=GaragePerformance.Calculate(stock,catalog);
                Assert.That(paid.All(p=>p.BodyDrive>before.Drive),Is.True);
                var after=GaragePerformance.Calculate(RecoveryGarageWorkflow.Preview(stock,first,catalog),catalog);
                Assert.That(after.Drive,Is.GreaterThan(before.Drive));Assert.That(after.Mass,Is.GreaterThan(before.Mass));Assert.That(after.Speed,Is.GreaterThan(before.Speed));
                foreach(string id in new[]{"engine_3","low_range","tires_mud"})stock.Set(catalog.Find(id),catalog);
                before=GaragePerformance.Calculate(stock,catalog);after=GaragePerformance.Calculate(RecoveryGarageWorkflow.Preview(stock,first,catalog),catalog);
                Assert.That(after.Drive,Is.GreaterThan(before.Drive),"Compare with the same installed upgrades");
            }
            Assert.That(RecoveryVehicle.Masses,Is.EqualTo(new[]{1350f,1750f,1150f,1550f}),"Target archetypes stay authored");
        }
        [Test] public void FirstPayoutBuysLightCableButCannotSkipStraightToStandard()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
            var p=RecoveryProfile.Parse("");RecoveryEconomy.Claim(p,"starter-test",0,true,true);
            Assert.That(p.credits,Is.EqualTo(652));Assert.That(RecoveryEconomy.Buy(p,catalog.Find("cable_18")),Is.False);
            Assert.That(RecoveryEconomy.Buy(p,catalog.Find("cable_14")),Is.True);Assert.That(p.credits,Is.EqualTo(452));
            RecoveryEconomy.Claim(p,"standard-test",1,true,true);
            Assert.That(RecoveryEconomy.Buy(p,catalog.Find("cable_18")),Is.True);
            Assert.That(p.credits,Is.GreaterThanOrEqualTo(0));
        }
        [Test] public void FourCleanEarlyJobsCanFundTheCableAndEnginePathWithoutSellingParts()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
            var p=new RecoveryProfile{credits=new[]{450,800,1300,450}.Sum(reward=>RecoveryEconomy.Payout(reward,true,true,true))};
            foreach(string id in new[]{"cable_14","cable_18","engine_2"})Assert.That(RecoveryEconomy.Buy(p,catalog.Find(id)),Is.True);
            Assert.That(p.credits,Is.GreaterThanOrEqualTo(0));Assert.That(p.ownedParts.Count,Is.EqualTo(3));
        }
        [Test] public void MudCollisionMeshProvidesTheRenderedUvForWheelStamps()
        {
            const string folder="Assets/TowTrouble/OpenWorld/Visuals/BroadGround/";
            var support=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"R03 soft ground support.asset");
            var visible=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"R03 broad ground.asset");
            Assert.That(support.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0),Is.True);
            Assert.That(support.uv,Is.EqualTo(visible.uv));
            var go=new GameObject("Temporary mud UV collision probe");
            try
            {
                var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=support;var bounds=support.bounds;
                Assert.That(collider.Raycast(new Ray(new Vector3(bounds.center.x,bounds.max.y+5,bounds.center.z),Vector3.down),out var hit,50),Is.True);
                Assert.That(hit.textureCoord.x,Is.EqualTo(.5f).Within(.025f));Assert.That(hit.textureCoord.y,Is.EqualTo(.5f).Within(.025f));
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
