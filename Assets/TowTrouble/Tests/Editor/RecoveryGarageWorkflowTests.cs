using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryGarageWorkflowTests
    {
        private RecoveryGarageCatalog catalog;
        [SetUp] public void Setup(){catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");}
        [Test] public void PreviewNeverSpendsOrAccumulatesAnUnboughtEarlierPart()
        {
            var profile=new RecoveryProfile{credits=10000};var fitted=profile.Build(0);fitted.Normalize(catalog,profile.Owns);
            var first=RecoveryGarageWorkflow.Preview(fitted,catalog.Find("engine_2"),catalog);
            var second=RecoveryGarageWorkflow.Preview(fitted,catalog.Find("cable_18"),catalog);
            Assert.That(first.Item(GarageSlot.Engine,catalog),Is.EqualTo("engine_2"));
            Assert.That(second.Item(GarageSlot.Engine,catalog),Is.EqualTo("stock_Engine"));
            Assert.That(profile.credits,Is.EqualTo(10000));Assert.That(profile.ownedParts,Is.Empty);
            Assert.That(fitted.Item(GarageSlot.Engine,catalog),Is.EqualTo("stock_Engine"));
        }
        [Test] public void PurchaseOnlyFitsTheChosenPartAndKeepsFittedEquipment()
        {
            var profile=new RecoveryProfile{credits=10000};profile.ownedParts.Add("tires_mud");profile.Build(0).Set(catalog.Find("tires_mud"),catalog);
            Assert.That(RecoveryGarageWorkflow.TryPrepareFit(profile,catalog.Find("cable_18"),catalog,out var next),Is.True);
            Assert.That(profile.credits,Is.EqualTo(10000-catalog.Find("cable_18").Price));
            Assert.That(next.Item(GarageSlot.Wheels,catalog),Is.EqualTo("tires_mud"));Assert.That(next.Item(GarageSlot.Cable,catalog),Is.EqualTo("cable_18"));
            Assert.That(profile.Build(0).Item(GarageSlot.Cable,catalog),Is.EqualTo("stock_Cable"),"Preparation does not persist the build before DemoSession commit");
        }
        [Test] public void InsufficientFundsAndAlreadyEquippedPartsCannotChargeTheProfile()
        {
            var profile=new RecoveryProfile{credits=1};
            Assert.That(RecoveryGarageWorkflow.TryPrepareFit(profile,catalog.Find("engine_2"),catalog,out _),Is.False);
            Assert.That(RecoveryGarageWorkflow.TryPrepareFit(profile,catalog.Stock(GarageSlot.Wheels),catalog,out _),Is.False);
            Assert.That(profile.credits,Is.EqualTo(1));Assert.That(profile.ownedParts,Is.Empty);
        }
        [Test] public void FittingAnOwnedPartIsFreeAndStatesDistinguishEquippedFromOwned()
        {
            var profile=new RecoveryProfile{credits=77};var part=catalog.Find("cable_18");profile.ownedParts.Add(part.Id);
            Assert.That(RecoveryGarageWorkflow.State(profile,part,catalog),Is.EqualTo(GarageItemState.Owned));
            Assert.That(RecoveryGarageWorkflow.TryPrepareFit(profile,part,catalog,out var build),Is.True);profile.builds[0]=build;
            Assert.That(profile.credits,Is.EqualTo(77));Assert.That(RecoveryGarageWorkflow.State(profile,part,catalog),Is.EqualTo(GarageItemState.Equipped));
        }
        [Test] public void BodyReplacementListsPaidIncompatiblePartsAndDoesNotRemoveOwnership()
        {
            var profile=new RecoveryProfile();var fitted=profile.Build(0);fitted.Normalize(catalog);
            var trim=catalog.Parts.First(p=>p.Slot==GarageSlot.Bonnet&&p.Price>0&&catalog.Compatible(p,fitted));fitted.Set(trim,catalog);profile.ownedParts.Add(trim.Id);
            var body=catalog.Parts.First(p=>p.Slot==GarageSlot.Assembly&&p.Price>0&&!catalog.Compatible(trim,RecoveryGarageWorkflow.Preview(fitted,p,catalog)));
            var preview=RecoveryGarageWorkflow.Preview(fitted,body,catalog);var replaced=RecoveryGarageWorkflow.ReplacedParts(fitted,preview,body,catalog);
            Assert.That(replaced.Select(p=>p.Id),Does.Contain(trim.Id));Assert.That(profile.Owns(trim.Id),Is.True);Assert.That(fitted.Item(GarageSlot.Bonnet,catalog),Is.EqualTo(trim.Id));
        }
        [Test] public void BodyLivesInTechAndCosmeticPartsDoNotClaimHandlingChanges()
        {
            Assert.That(RecoveryGarageWorkflow.Section(GarageSlot.Assembly),Is.EqualTo(1));
            Assert.That(Array.IndexOf(RecoveryGarageWorkflow.Slots(0),GarageSlot.Assembly),Is.EqualTo(-1));
            Assert.That(RecoveryGarageWorkflow.AppearanceOnly(catalog.Parts.First(p=>p.Slot==GarageSlot.Bonnet&&p.Price>0)),Is.True);
            Assert.That(RecoveryGarageWorkflow.AppearanceOnly(catalog.Find("low_range")),Is.False);
        }
        [Test] public void NamedSetupsRemainCompatibleWithLegacyKeysAndProtectReferencedParts()
        {
            var profile=new RecoveryProfile();var part=catalog.Find("tires_mud");profile.ownedParts.Add(part.Id);var build=profile.Build(0).Copy();build.Set(part,catalog);
            var old=new GaragePreset{Name="setup_0_0",Build=build};profile.presets.Add(old);
            Assert.That(RecoveryGarageWorkflow.SetupLabel(old,0),Is.EqualTo("Forest"));
            old.Label="Snow";var copy=RecoveryProfile.Parse(JsonUtility.ToJson(profile));
            Assert.That(copy.presets[0].Name,Is.EqualTo("setup_0_0"));Assert.That(copy.presets[0].Label,Is.EqualTo("Snow"));
            Assert.That(RecoveryGarageWorkflow.InUseReason(copy,part.Id),Is.EqualTo("Used in Snow setup"));
        }
        [Test] public void ThreeColumnNavigationStopsAtRowsAndPreservesColumns()
        {
            Assert.That(RecoveryGarageWorkflow.GridIndex(1,8,0,-1),Is.EqualTo(1));
            Assert.That(RecoveryGarageWorkflow.GridIndex(1,8,0,1),Is.EqualTo(4));
            Assert.That(RecoveryGarageWorkflow.GridIndex(4,8,0,1),Is.EqualTo(7));
            Assert.That(RecoveryGarageWorkflow.GridIndex(7,8,0,1),Is.EqualTo(7));
            Assert.That(RecoveryGarageWorkflow.GridIndex(2,8,1,0),Is.EqualTo(2));
            Assert.That(RecoveryGarageWorkflow.GridIndex(3,8,-1,0),Is.EqualTo(3));
            Assert.That(RecoveryGarageWorkflow.GridIndex(-1,0,1,0),Is.EqualTo(-1));
        }
        [Test] public void TireFiltersAndRecommendationsUseActualSurfaceStats()
        {
            var build=new RecoveryBuild();build.Normalize(catalog);var perf=GaragePerformance.Calculate(build,catalog);
            var contract=new RecoveryContract(1,0,"","","","",Vector3.zero,Vector3.zero,0,0){Tier=4,Load=new RecoveryLoadSettings{Material=RecoveryLoadMaterial.Snow}};
            var part=RecoveryGarageWorkflow.Recommend(contract,perf,build,catalog);
            Assert.That(part,Is.Not.Null);Assert.That(part.SnowGrip,Is.GreaterThanOrEqualTo(2.2f));Assert.That(RecoveryGarageWorkflow.TireMatches(part,4),Is.True);
            Assert.That(RecoveryGarageWorkflow.TireMatches(part,3),Is.False);
        }
        [Test] public void DuplicateBodyNamesHaveUnderstandableVisualVariantLabels()
        {
            var profile=new RecoveryProfile{language="en"};var a=catalog.Parts.First(p=>p.Name=="Historic Bucket");var b=catalog.Parts.First(p=>p.Name=="Rod Bucket");
            Assert.That(RecoveryGarageWorkflow.DisplayName(a,profile,catalog),Is.EqualTo("Bucket Classic"));
            Assert.That(RecoveryGarageWorkflow.DisplayName(b,profile,catalog),Is.EqualTo("Bucket Hot Rod"));
        }
        [Test] public void ForceRecommendationDoesNotRepeatAnEquippedUpgrade()
        {
            var profile=new RecoveryProfile();var build=profile.Build(0);build.Normalize(catalog);
            foreach(string id in new[]{"engine_2","low_range","cable_26"}){profile.ownedParts.Add(id);build.Set(catalog.Find(id),catalog);}
            var contract=new RecoveryContract(1,0,"","","","",Vector3.zero,Vector3.zero,0,0){BreakoutResistance=20000};
            var part=RecoveryGarageWorkflow.Recommend(contract,GaragePerformance.Calculate(build,catalog),build,catalog,profile);
            Assert.That(part,Is.Not.Null);Assert.That(part.Id,Is.Not.EqualTo("engine_2").And.Not.EqualTo("low_range"));Assert.That(GaragePerformance.Calculate(RecoveryGarageWorkflow.Preview(build,part,catalog),catalog).Drive,Is.GreaterThan(GaragePerformance.Calculate(build,catalog).Drive));
        }
        [Test] public void SurfaceRecommendationPrefersASuitableOwnedTire()
        {
            var profile=new RecoveryProfile();var build=profile.Build(0);build.Normalize(catalog);
            var suitable=catalog.Parts.Where(p=>p.Slot==GarageSlot.Wheels&&p.MudGrip>=1.15f&&catalog.Compatible(p,build)).OrderByDescending(p=>p.Price).First();profile.ownedParts.Add(suitable.Id);
            var contract=new RecoveryContract(1,0,"","","","",Vector3.zero,Vector3.zero,0,0){Load=new RecoveryLoadSettings{Material=RecoveryLoadMaterial.Mud}};
            Assert.That(RecoveryGarageWorkflow.Recommend(contract,GaragePerformance.Calculate(build,catalog),build,catalog,profile)?.Id,Is.EqualTo(suitable.Id));
        }
    }
}
