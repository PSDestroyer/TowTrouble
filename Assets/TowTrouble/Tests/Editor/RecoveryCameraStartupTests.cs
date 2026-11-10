using System;
using System.Reflection;
using FishNet.Transporting;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble.Tests
{
    public sealed class RecoveryCameraStartupTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance|BindingFlags.NonPublic;
        private static void WithCamera(Action<RecoveryCamera> check)
        {
            var scene=EditorSceneManager.NewPreviewScene(); var go=new GameObject("Inactive startup camera",typeof(Camera));go.SetActive(false);SceneManager.MoveGameObjectToScene(go,scene);
            try { check(go.AddComponent<RecoveryCamera>()); }
            finally { UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static void Pose(RecoveryCamera camera,Vector3 position,Vector3 look,bool preview,bool gameplay)
            => typeof(RecoveryCamera).GetMethod("ApplyPresentationPose",Hidden).Invoke(camera,new object[]{position,look,preview,gameplay,false,1f/60f,true});

        [Test] public void FirstGameplayFrameUsesTheFinalPoseAndRotationWithoutAFlight()
        {
            WithCamera(camera=>
            {
                camera.transform.SetPositionAndRotation(new Vector3(-10000,-500,-10000),Quaternion.Euler(90,80,0));
                var position=new Vector3(10000,10008,10012); var look=new Vector3(10000,10001,10000);
                Pose(camera,position,look,false,true);
                Assert.That(Vector3.Distance(camera.transform.position,position),Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.LookRotation(look-position)),Is.LessThan(.01f));
            });
        }
        [Test] public void FollowingStillSmoothsOrdinaryPlayerMovementAfterTheEntryCut()
        {
            WithCamera(camera=>
            {
                var position=new Vector3(10000,10008,10012); var look=new Vector3(10000,10001,10000);
                Pose(camera,position,look,false,true); var next=position+Vector3.right*2;
                Pose(camera,next,look,false,true);
                Assert.That(camera.transform.position.x,Is.GreaterThan(position.x));
                Assert.That(camera.transform.position.x,Is.LessThan(next.x));
            });
        }
        [Test] public void ReturningFromWorkshopUsesASafeGameplayCutAgain()
        {
            WithCamera(camera=>
            {
                var position=new Vector3(10000,10008,10012); var look=new Vector3(10000,10001,10000);
                Pose(camera,position,look,false,true); Pose(camera,new Vector3(9990,10002,9997),look,true,false);
                Pose(camera,position,look,false,true);
                Assert.That(Vector3.Distance(camera.transform.position,position),Is.LessThan(.0001f));
            });
        }
        [Test] public void MapLobbySpawnsAtItsGarageBeforeFreeRoamHasBeenConfigured()
        {
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Inactive map lobby spawn");root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);
            RecoveryMap map=null;
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab");
                RecoveryVehicle Car()
                {var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.transform.SetParent(root.transform,false);return go.GetComponent<RecoveryVehicle>();}
                var drivers=new[]{Car(),Car()};var target=Car();var world=root.AddComponent<RecoveryWorld>();world.Configure(drivers,target,Array.Empty<LineRenderer>());
                map=root.AddComponent<RecoveryMap>();world.ConfigureMap(map);var garage=new Vector3(10000,10000,10000);world.ConfigureGarage(garage);
                Assert.That(world.FreeRoam,Is.False);
                typeof(RecoveryWorld).GetMethod("ResetRecovery",Hidden).Invoke(world,null);
                Assert.That(Vector3.Distance(drivers[0].transform.position,garage+new Vector3(-2.5f,.05f,-2)),Is.LessThan(.01f));
                Assert.That(Vector3.Distance(drivers[1].transform.position,garage+new Vector3(2.5f,.05f,-2)),Is.LessThan(.01f));
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(drivers[0].transform.eulerAngles.y,180)),Is.LessThan(.01f));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);if(map!=null)UnityEngine.Object.DestroyImmediate(map);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void ClientTransportStartingDoesNotExposeTheUnpreparedWorld()
        {
            var scene=EditorSceneManager.NewPreviewScene();var go=new GameObject("Inactive startup readiness");go.SetActive(false);SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                var session=go.AddComponent<DemoSession>();typeof(DemoSession).GetProperty("Connecting").SetValue(session,true);
                typeof(DemoSession).GetMethod("ClientState",Hidden).Invoke(session,new object[]{new ClientConnectionStateArgs(LocalConnectionState.Started,0)});
                Assert.That(session.Connecting,Is.True);Assert.That(session.ReadyForPresentation,Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
