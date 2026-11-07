using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoverySteeringTests
    {
        private static MeshFilter Wheel(GameObject root)=>root.GetComponentsInChildren<MeshFilter>(true)
            .Single(m=>m.sharedMesh!=null&&m.sharedMesh.name.Contains("SteerWheel")&&RecoveryVehicleEffects.VisiblePart(m.transform,root.transform));

        [Test] public void EveryBodyAndStarterIncludesASeparateSteeringWheel()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
            var bodies=catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly).SelectMany(p=>p.Prefabs)
                .Concat(Enumerable.Range(0,4).Select(i=>catalog.Assembly(new RecoveryBuild{Model=i}))).Distinct();
            foreach(var body in bodies)Assert.That(Wheel(body),Is.Not.Null,body.name);
        }

        [Test] public void SteeringTurnsBothWaysAndRestoresAuthoredPoseAcrossBodyChanges()
        {
            var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
            try
            {
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab"),scene);
                root.SetActive(false);var car=root.GetComponent<RecoveryVehicle>();car.SetSpawn(Vector3.zero,Quaternion.identity);
                var update=typeof(RecoveryVehicle).GetMethod("UpdateSteeringPresentation",BindingFlags.Instance|BindingFlags.NonPublic);
                for(int i=0;i<car.ModelCount;i++)
                {
                    car.SetModel(i);var wheel=Wheel(root).transform;var rest=wheel.localRotation;var position=wheel.localPosition;
                    car.Receive(new VehicleFrame{Position=Vector3.zero,Rotation=Quaternion.identity,Controls=Vector2.right,EngineRunning=true});
                    Assert.That(car.ControlInput.x,Is.EqualTo(1));
                    update.Invoke(car,new object[]{30f,1f});
                    Assert.That(Quaternion.Angle(wheel.localRotation,rest),Is.GreaterThan(100));
                    Assert.That(Vector3.SignedAngle(rest*Vector3.forward,wheel.localRotation*Vector3.forward,rest*Vector3.up),Is.GreaterThan(100),"Right steering must follow the road wheels");
                    Assert.That(Vector3.Dot(wheel.localRotation*Vector3.up,rest*Vector3.up),Is.GreaterThan(.9999f),"Column axis must stay fixed");
                    var right=wheel.localRotation;update.Invoke(car,new object[]{-30f,1f});
                    Assert.That(Quaternion.Angle(right,wheel.localRotation),Is.GreaterThan(70));
                    Assert.That(Vector3.SignedAngle(rest*Vector3.forward,wheel.localRotation*Vector3.forward,rest*Vector3.up),Is.LessThan(-100),"Left steering must follow the road wheels");
                    Assert.That(wheel.localPosition,Is.EqualTo(position),"Wheel must turn at its hub, without orbiting");
                    update.Invoke(car,new object[]{0f,.02f});Assert.That(Quaternion.Angle(wheel.localRotation,rest),Is.GreaterThan(1),"Return should be smooth");
                    update.Invoke(car,new object[]{0f,1f});Assert.That(Quaternion.Angle(wheel.localRotation,rest),Is.LessThan(.05f));
                    update.Invoke(car,new object[]{30f,1f});car.ResetVehicle(true);
                    Assert.That(Quaternion.Angle(wheel.localRotation,rest),Is.LessThan(.01f));
                    car.SetModel((i+1)%car.ModelCount);Assert.That(Quaternion.Angle(wheel.localRotation,rest),Is.LessThan(.01f));
                }
            }
            finally{if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
