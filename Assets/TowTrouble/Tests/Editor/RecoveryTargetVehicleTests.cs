using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryTargetVehicleTests
    {
        [Test] public void ExistingSitesDefaultToTheOriginalVehicle(){var site=new RecoverySite();Assert.That(site.TargetModel,Is.Zero);Assert.That(site.TargetPaint,Is.Zero);}
        [Test] public void TutorialContractsKeepTheirOriginalVehicle(){foreach(var c in RecoveryContract.All){Assert.That(c.TargetModel,Is.Zero);Assert.That(c.TargetPaint,Is.Zero);}}
        [Test] public void MapContractsCarryTargetModelAndPaintWithoutChangingProgression()
        {
            var go=new GameObject("Inactive contract map fixture");go.SetActive(false);
            try{var map=go.AddComponent<RecoveryMap>();map.Configure(new[]{new RecoverySite{Id="R02",TargetModel=2,TargetPaint=3,Tier=1,Position=Vector3.forward*10,Delivery=Vector3.forward*40}},new RecoveryDropoff[0],new Vector3[0]);var c=map.Contracts[0];Assert.That(c.TargetModel,Is.EqualTo(2));Assert.That(c.TargetPaint,Is.EqualTo(3));Assert.That(c.Tier,Is.EqualTo(1));Assert.That(c.CompletionId,Is.EqualTo(3));}
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void ReconfiguredMapRefreshesTheTargetAppearance()
        {
            var go=new GameObject("Inactive appearance fixture");go.SetActive(false);
            try{var map=go.AddComponent<RecoveryMap>();var sites=new[]{new RecoverySite{TargetModel=1,TargetPaint=2}};map.Configure(sites,new RecoveryDropoff[0],new Vector3[0]);Assert.That(map.Contracts[0].TargetModel,Is.EqualTo(1));sites[0].TargetModel=3;sites[0].TargetPaint=1;map.Configure(sites,new RecoveryDropoff[0],new Vector3[0]);Assert.That(map.Contracts[0].TargetModel,Is.EqualTo(3));Assert.That(map.Contracts[0].TargetPaint,Is.EqualTo(1));}
            finally{Object.DestroyImmediate(go);}
        }
    }
}
