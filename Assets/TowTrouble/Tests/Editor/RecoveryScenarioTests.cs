using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
 public sealed class RecoveryScenarioTests
 {
  [Test] public void FallenTreeMustMoveAsideBeforeCarRecovery()
  {
   Assert.That(RecoveryTowObstacle.IsClear(Vector3.forward*8,Vector3.right),Is.False);
   Assert.That(RecoveryTowObstacle.IsClear(Vector3.right*2,Vector3.right),Is.False);
   Assert.That(RecoveryTowObstacle.IsClear(new Vector3(5,0,3),Vector3.right),Is.True);
   Assert.That(RecoveryTowObstacle.IsClear(new Vector3(-5,0,3),Vector3.right),Is.True);
  }
  [Test] public void OpenWorldExtractionAcceptsAlternativeGroundRoutes()
  {
   var c=new RecoveryContract(1,0,"","","","",Vector3.zero,Vector3.zero,8,0){IsOpenWorld=true,ExitDirection=Vector3.forward};
   Assert.That(c.IsExtracted(Vector3.right*9),Is.True);Assert.That(c.IsExtracted(Vector3.back*9),Is.True);
   Assert.That(c.IsExtracted(Vector3.forward*7),Is.False);Assert.That(c.IsExtracted(Vector3.up*20),Is.False);Assert.That(c.IsExtracted(new Vector3(3,0,9)),Is.True);
   var tutorial=RecoveryContract.All[0];Assert.That(tutorial.IsExtracted(new Vector3(0,0,26)),Is.True);
  }
  [Test] public void FallenTreeResetRestoresItsAuthoredPoseWithoutSimulation()
  {
   var go=new GameObject("Disposable obstacle",typeof(Rigidbody),typeof(RecoveryTowObstacle));
   try{var obstacle=go.GetComponent<RecoveryTowObstacle>();obstacle.Body=go.GetComponent<Rigidbody>();obstacle.Home=new Vector3(3,7,5);obstacle.HomeRotation=Quaternion.Euler(0,60,0);go.transform.position=Vector3.one*20;obstacle.ResetObstacle();Assert.That(obstacle.Body.isKinematic,Is.True);Assert.That(Vector3.Distance(go.transform.position,obstacle.Home),Is.LessThan(.001f));}
   finally{Object.DestroyImmediate(go);}
  }
 }
}
