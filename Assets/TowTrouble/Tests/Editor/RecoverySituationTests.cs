using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoverySituationTests
    {
        [Test] public void EngineSmokeOnlyRunsForAnActiveFailedEngine()
        {
            foreach(RecoverySituationKind kind in System.Enum.GetValues(typeof(RecoverySituationKind)))
            {
                Assert.That(RecoveryDistressVisuals.SmokeRate(kind,false,1),Is.Zero);
                Assert.That(RecoveryDistressVisuals.SmokeRate(kind,true,1),Is.EqualTo(kind==RecoverySituationKind.EngineFailure?8:0));
            }
        }
        [Test] public void EngineSmokeRespectsDisabledAndClampedEffectsGain()
        {
            Assert.That(RecoveryDistressVisuals.SmokeRate(RecoverySituationKind.EngineFailure,true,0),Is.Zero);
            Assert.That(RecoveryDistressVisuals.SmokeRate(RecoverySituationKind.EngineFailure,true,-1),Is.Zero);
            Assert.That(RecoveryDistressVisuals.SmokeRate(RecoverySituationKind.EngineFailure,true,.5f),Is.EqualTo(4));
            Assert.That(RecoveryDistressVisuals.SmokeRate(RecoverySituationKind.EngineFailure,true,4),Is.EqualTo(8));
        }
        [Test] public void SituationMetadataPreservesContractProgressRewardsAndDelivery()
        {
            var go=new GameObject("Disposable situation map");
            try
            {
                var map=go.AddComponent<RecoveryMap>();var sites=new RecoverySite[24];
                for(int i=0;i<24;i++)sites[i]=new RecoverySite{Id="R"+(i+1).ToString("00"),Name="Recovery",English="Recovery",Tier=i%5,Position=new Vector3(i,20,0),Depth=.2f,Delivery=new Vector3(0,20,50),DeliveryName="Atelier",DeliveryEnglish="Workshop",Situation="Motor defect.",EnglishSituation="Engine failure.",Kind=RecoverySituationKind.EngineFailure};
                map.Configure(sites,System.Array.Empty<RecoveryDropoff>(),System.Array.Empty<Vector3>());
                for(int i=0;i<24;i++)
                {
                    var contract=map.Contracts[i];Assert.That(contract.CompletionId,Is.EqualTo(3+i));
                    Assert.That(contract.Reward,Is.EqualTo(RecoveryEconomy.Rewards[i%5]));Assert.That(contract.Delivery,Is.EqualTo(sites[i].Delivery));
                    Assert.That(contract.SituationKind,Is.EqualTo(RecoverySituationKind.EngineFailure));
                    Assert.That(contract.Description,Does.Contain("Motor defect."));Assert.That(contract.EnglishDescription,Does.Contain("Engine failure."));
                }
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void ReconfiguringASiteInvalidatesItsContractPresentation()
        {
            var go=new GameObject("Disposable map cache");
            try
            {
                var map=go.AddComponent<RecoveryMap>();var site=new RecoverySite{Kind=RecoverySituationKind.Mud};
                map.Configure(new[]{site},System.Array.Empty<RecoveryDropoff>(),System.Array.Empty<Vector3>());var before=map.Contracts[0];
                site.Kind=RecoverySituationKind.EngineFailure;site.Situation="Motor defect";
                map.Configure(new[]{site},map.Deliveries,System.Array.Empty<Vector3>());
                Assert.That(map.Contracts[0],Is.Not.SameAs(before));Assert.That(map.Contracts[0].SituationKind,Is.EqualTo(RecoverySituationKind.EngineFailure));
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
