using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowTrouble.Tests
{
    // These exercise initialization on disposable inactive copies in Edit Mode.
    // No scene gameplay, physics simulation, networking or persistent storage are started.
    // Inactive preview bodies have no simulated pose; the authored Transform is checked.
    public sealed class RecoveryVehicleInitializationTests
    {
        private Scene preview;
        private GameObject root;
        private RecoveryVehicle vehicle;
        [SetUp] public void Setup()
        {
            preview=EditorSceneManager.NewPreviewScene();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/StrandedClassic.prefab");
            Assert.That(prefab,Is.Not.Null);
            root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview);
            root.SetActive(false);vehicle=root.GetComponent<RecoveryVehicle>();
        }
        [TearDown] public void Cleanup()
        {if(root!=null)Object.DestroyImmediate(root);if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);}

        [Test] public void InactiveTargetCanBeSpawnedBeforeAwake()
        {
            var position=new Vector3(100,22,-100);var rotation=Quaternion.Euler(0,125,0);
            Assert.DoesNotThrow(()=>vehicle.SetSpawn(position,rotation));
            Assert.That(vehicle.Body,Is.Not.Null);
            Assert.That(Vector3.Distance(vehicle.transform.position,position),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(vehicle.transform.rotation,rotation),Is.LessThan(.01f));
            Assert.That(vehicle.Body.isKinematic,Is.True);
            Assert.That(root.activeSelf,Is.False);
            for(int i=0;i<4;i++)Assert.That(vehicle.WheelVisual(i),Is.Not.Null);
        }
        [Test] public void DelayedAwakeDoesNotRebuildGeometryOrReplaceSpawn()
        {
            var position=new Vector3(-200,80,150);var rotation=Quaternion.Euler(0,240,0);
            vehicle.SetSpawn(position,rotation);int geometry=vehicle.GeometryVersion;
            typeof(RecoveryVehicle).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(vehicle,null);
            Assert.That(vehicle.GeometryVersion,Is.EqualTo(geometry));
            vehicle.ResetVehicle(true);
            Assert.That(Vector3.Distance(vehicle.transform.position,position),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(vehicle.transform.rotation,rotation),Is.LessThan(.01f));
        }
        [Test] public void InitializingSimulationBeforeSpawnKeepsLaterResetPosition()
        {
            vehicle.SetSimulation(false);
            var position=new Vector3(30,150,400);vehicle.SetSpawn(position,Quaternion.identity);
            vehicle.ResetVehicle(false);
            Assert.That(Vector3.Distance(vehicle.transform.position,position+Vector3.up*.35f),Is.LessThan(.001f));
            Assert.That(vehicle.Body.isKinematic,Is.True);
        }
        [Test] public void ResetBeforeAwakeHasAValidBodyAndSafePosition()
        {
            var position=root.transform.position;
            Assert.DoesNotThrow(()=>vehicle.ResetVehicle(false));
            Assert.That(Vector3.Distance(vehicle.transform.position,position+Vector3.up*.35f),Is.LessThan(.001f));
            Assert.That(vehicle.GeometryVersion,Is.EqualTo(1));
        }
        [Test] public void RepeatedSpawnsDoNotRepeatInitialization()
        {
            vehicle.SetSpawn(Vector3.zero,Quaternion.identity);int geometry=vehicle.GeometryVersion;
            var position=new Vector3(42,25,-70);vehicle.SetSpawn(position,Quaternion.identity);vehicle.SetSimulation(false);
            Assert.That(vehicle.GeometryVersion,Is.EqualTo(geometry));
            Assert.That(Vector3.Distance(vehicle.transform.position,position),Is.LessThan(.001f));
        }
    }
}
