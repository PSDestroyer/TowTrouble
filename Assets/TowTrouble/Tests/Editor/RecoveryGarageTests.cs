using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryGarageTests
    {
        private RecoveryGarageCatalog catalog;
        [SetUp] public void Setup(){catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");Assert.That(catalog,Is.Not.Null);}
        [Test] public void NewProfileHasStockBuildAndNoDebt(){var p=RecoveryProfile.Parse("");p.Build(0).Normalize(catalog,p.Owns);Assert.That(p.credits,Is.Zero);Assert.That(GaragePerformance.Calculate(p.Build(0),catalog).Cable,Is.EqualTo(11000));Assert.That(p.Build(0).Item(GarageSlot.Cable,catalog),Is.EqualTo("stock_Cable"));Assert.That(RecoveryRules.BreakForce,Is.EqualTo(11000));}
        [Test] public void FirstCableUpgradeIsEarnedAfterOneStarterRecovery()
        {
            var p=RecoveryProfile.Parse("");var upgrade=catalog.Find("cable_14");
            Assert.That(upgrade,Is.Not.Null);Assert.That(RecoveryEconomy.Buy(p,upgrade),Is.False);
            RecoveryEconomy.Claim(p,"first-cable-test",0,false,false);
            Assert.That(RecoveryEconomy.Buy(p,upgrade),Is.True);
            p.Build(0).Set(upgrade,catalog);
            var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));copy.Build(0).Normalize(catalog,copy.Owns);
            Assert.That(GaragePerformance.Calculate(copy.Build(0),catalog).Cable,Is.EqualTo(14000));
            Assert.That(copy.Owns("cable_14"),Is.True);Assert.That(copy.credits,Is.EqualTo(p.credits));
        }
        [Test] public void CableTiersIncreaseInStrengthAndCostWithoutGivingFreeUpgrades()
        {
            var tiers=catalog.Parts.Where(p=>p.Slot==GarageSlot.Cable).OrderBy(p=>p.CableStrength).ToArray();
            Assert.That(tiers.Select(p=>p.CableStrength).ToArray(),Is.EqualTo(new[]{11000f,14000f,18000f,26000f,36000f}));
            Assert.That(tiers.Select(p=>p.Price).ToArray(),Is.EqualTo(new[]{0,200,1200,2000,4200}));
            var p=RecoveryProfile.Parse("");p.Build(0).Set(catalog.Find("cable_14"),catalog);p.Build(0).Normalize(catalog,p.Owns);
            Assert.That(GaragePerformance.Calculate(p.Build(0),catalog).Cable,Is.EqualTo(11000));
        }
        [Test] public void PurchasedStrongerCablesRemainEquippedAfterSaveRoundTrip()
        {
            foreach(var id in new[]{"cable_14","cable_18","cable_26","cable_36"})
            {
                var p=RecoveryProfile.Parse("");p.ownedParts.Add(id);p.Build(0).Set(catalog.Find(id),catalog);
                var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));copy.Build(0).Normalize(catalog,copy.Owns);
                Assert.That(copy.Build(0).Item(GarageSlot.Cable,catalog),Is.EqualTo(id));
                Assert.That(GaragePerformance.Calculate(copy.Build(0),catalog).Cable,Is.EqualTo(catalog.Find(id).CableStrength));
            }
        }
        [Test] public void PurchaseIsAffordableAndCannotBeChargedTwice(){var p=RecoveryProfile.Parse("");p.credits=catalog.Find("tires_mud").Price;var part=catalog.Find("tires_mud");Assert.That(RecoveryEconomy.Buy(p,part),Is.True);Assert.That(p.credits,Is.Zero);Assert.That(RecoveryEconomy.Buy(p,part),Is.False);Assert.That(p.ownedParts.Count,Is.EqualTo(1));}
        [Test] public void FailedPurchaseDoesNotChangeInventoryOrBalance(){var p=RecoveryProfile.Parse("");p.credits=599;Assert.That(RecoveryEconomy.Buy(p,catalog.Find("tires_mud")),Is.False);Assert.That(p.credits,Is.EqualTo(599));Assert.That(p.ownedParts,Is.Empty);}
        [Test] public void MountedAndPresetPartsCannotBeSold(){var p=RecoveryProfile.Parse("");p.ownedParts.Add("tires_mud");p.Build(0).Set(catalog.Find("tires_mud"),catalog);Assert.That(RecoveryEconomy.Sell(p,catalog.Find("tires_mud"),catalog),Is.False);p.presets.Add(new GaragePreset{Name="Forest",Build=p.Build(0).Copy()});p.Build(0).Set(catalog.Stock(GarageSlot.Wheels),catalog);Assert.That(RecoveryEconomy.Sell(p,catalog.Find("tires_mud"),catalog),Is.False);}
        [Test] public void SalePaysSeventyPercentOnlyOnce(){var p=RecoveryProfile.Parse("");p.ownedParts.Add("tires_mud");Assert.That(RecoveryEconomy.Sell(p,catalog.Find("tires_mud"),catalog),Is.True);Assert.That(p.credits,Is.EqualTo(420));Assert.That(RecoveryEconomy.Sell(p,catalog.Find("tires_mud"),catalog),Is.False);}
        [Test] public void CompletionPaysOnceEvenAfterSerialization(){var p=RecoveryProfile.Parse("");Assert.That(RecoveryEconomy.Claim(p,"run-a",0,true,true),Is.EqualTo(652));var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(RecoveryEconomy.Claim(copy,"run-a",0,true,true),Is.Zero);Assert.That(RecoveryEconomy.Claim(copy,"run-b",0,true,true),Is.EqualTo(270));Assert.That(copy.credits,Is.EqualTo(922));}
        [Test] public void LegacyProfileMigratesOnceAndPreservesProgressAndPaint(){var p=RecoveryProfile.Parse("{\"version\":1,\"completedMask\":3,\"noResetMask\":1,\"noSnapMask\":1,\"paint\":2,\"vehicle\":0,\"hookKey\":\"H\"}");Assert.That(p.version,Is.EqualTo(4));Assert.That(p.credits,Is.EqualTo(1402));Assert.That(p.Completed(1),Is.True);Assert.That(p.hookKey,Is.EqualTo("H"));Assert.That(p.Build(0).Items,Does.Contain("paint_2"));Assert.That(RecoveryProfile.Parse(JsonUtility.ToJson(p)).credits,Is.EqualTo(1402));}
        [Test] public void PresetsAndInventorySurviveSaveRoundTrip(){var p=RecoveryProfile.Parse("");p.credits=500;p.ownedParts.Add("low_range");p.Build(0).Set(catalog.Find("low_range"),catalog);p.presets.Add(new GaragePreset{Name="Mountain",Build=p.Build(0).Copy()});var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(copy.Owns("low_range"),Is.True);Assert.That(copy.presets[0].Build.Items,Does.Contain("low_range"));Assert.That(copy.credits,Is.EqualTo(500));}
        [Test] public void PreviewIsIndependentAndUnownedEquipmentIsRemovedBeforeCommit(){var p=RecoveryProfile.Parse("");var preview=p.Build(0).Copy();preview.Set(catalog.Find("engine_2"),catalog);Assert.That(p.Build(0).Items,Does.Not.Contain("engine_2"));preview.Normalize(catalog,p.Owns);Assert.That(preview.Items,Does.Not.Contain("engine_2"));}
        [Test] public void InvalidAndIncompatibleItemsAreNormalized(){var build=new RecoveryBuild{Items=new[]{"invalid-id","tires_mud","tires_mud"}};build.Normalize(catalog);Assert.That(build.Items.Distinct().Count(),Is.EqualTo(build.Items.Length));Assert.That(build.Items,Does.Not.Contain("invalid-id"));var wrong=catalog.Parts.First(p=>p.Slot==GarageSlot.Bonnet&&!catalog.Compatible(p,build));build.Set(wrong,catalog);build.Normalize(catalog);Assert.That(build.Items,Does.Not.Contain(wrong.Id));}
        [Test] public void EngineUpgradeTradesInOldEngineWithoutLeavingDanglingPresets(){var p=RecoveryProfile.Parse("");p.credits=1800;p.ownedParts.Add("engine_2");p.Build(0).Set(catalog.Find("engine_2"),catalog);p.presets.Add(new GaragePreset{Name="Forest",Build=p.Build(0).Copy()});Assert.That(RecoveryEconomy.Buy(p,catalog.Find("engine_3")),Is.True);Assert.That(p.credits,Is.Zero);Assert.That(p.Owns("engine_2"),Is.False);Assert.That(p.presets[0].Build.Items,Does.Contain("engine_3"));}
        [Test] public void LowRangeAndEngineRespectTheApprovedTradeoff(){var build=new RecoveryBuild();build.Set(catalog.Find("engine_2"),catalog);build.Set(catalog.Find("low_range"),catalog);var stats=GaragePerformance.Calculate(build,catalog);Assert.That(stats.Drive*4,Is.EqualTo(12600).Within(.1));Assert.That(stats.Speed*3.6f,Is.EqualTo(48f).Within(.001));}
        [Test] public void CatalogIdsAreUniqueAndAssembliesHaveFourValidWheelAnchors(){Assert.That(catalog.Parts.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(catalog.Parts.Length));for(int i=0;i<4;i++){var assembly=catalog.Assembly(new RecoveryBuild{Model=i}).GetComponent<RecoveryAssembly>();Assert.That(assembly.Wheels.Length,Is.EqualTo(4));Assert.That(assembly.Anchors.Length,Is.EqualTo(4));Assert.That(assembly.Wheels.All(w=>w!=null),Is.True);Assert.That(assembly.Radius,Is.InRange(.2f,1.2f));}}
        [Test] public void UnsupportedSaveVersionIsRejected(){Assert.Throws<ArgumentException>(()=>RecoveryProfile.Parse("{\"version\":999}"));}
    }
}
