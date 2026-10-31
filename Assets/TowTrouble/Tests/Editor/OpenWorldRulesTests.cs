using NUnit.Framework;
using UnityEngine;
using UnityEditor;
namespace TowTrouble.Tests
{
    public sealed class OpenWorldRulesTests
    {
        private RecoveryContract Job()=>new RecoveryContract(1,0,"R01","","","",new Vector3(100,20,-100),new Vector3(-80,80,50),8,0){IsOpenWorld=true,ProgressId=3,Tier=0};
        [Test] public void ExtractionWorksAcrossTheWorldAxes(){var c=Job();Assert.That(c.IsExtracted(c.Target+Vector3.left*9),Is.True);Assert.That(c.IsExtracted(c.Target+Vector3.back*9),Is.True);Assert.That(c.IsExtracted(c.Target+Vector3.right*2),Is.False);}
        [Test] public void DeliveryRejectsWrongAltitudeAndAllowsElevatedBays(){var c=Job();Assert.That(c.ContainsDelivery(c.Delivery+Vector3.up),Is.True);Assert.That(c.ContainsDelivery(c.Delivery+Vector3.down*10),Is.False);Assert.That(c.ContainsDelivery(c.Delivery+Vector3.right*8),Is.False);}
        [Test] public void WorldPayoutPreservesTutorialProgressAndDeduplicates(){var p=RecoveryProfile.Parse("");p.RecordFinish(0,true,true);int last=p.lastContract;var c=Job();Assert.That(RecoveryEconomy.Claim(p,"world-test",c,true,true),Is.EqualTo(562));Assert.That(p.Completed(3),Is.True);Assert.That(p.Completed(1),Is.False);Assert.That(p.lastContract,Is.EqualTo(last));Assert.That(RecoveryEconomy.Claim(p,"world-test",c,true,true),Is.Zero);var copy=RecoveryProfile.Parse(JsonUtility.ToJson(p));Assert.That(copy.Completed(3),Is.True);Assert.That(copy.Completed(0),Is.True);}
        [Test] public void TierControlsRewardIndependentlyOfMapIndex(){var c=Job();c.Tier=4;c.ProgressId=26;Assert.That(c.Reward,Is.EqualTo(2700));Assert.That(c.CompletionId,Is.EqualTo(26));}
        [Test] public void SpecializedTiresKeepTheirTradeoffs(){var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");var build=new RecoveryBuild();build.Set(catalog.Find("tires_sand"),catalog);var sand=GaragePerformance.Calculate(build,catalog);Assert.That(sand.SandGrip,Is.EqualTo(1.45f));Assert.That(sand.MudGrip,Is.LessThan(1));build.Set(catalog.Find("tires_winter"),catalog);var winter=GaragePerformance.Calculate(build,catalog);Assert.That(winter.SnowGrip,Is.EqualTo(1.65f));Assert.That(winter.SandGrip,Is.EqualTo(1));}
    }
}
