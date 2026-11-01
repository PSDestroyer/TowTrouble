using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TowTrouble.Tests
{
    public sealed class RecoveryGarageCameraTests
    {
        [Test] public void EffectBoundsCannotResizeBodyOrAnyGarageSlot()
        {
            using(var f=new Fixture())
            {
                var build=new RecoveryBuild{Model=3};
                build.Set(f.Catalog.Parts.Single(p=>p.Slot==GarageSlot.Assembly&&p.EnglishName.Contains("Speedster")&&p.EnglishName.Contains("Electron")),f.Catalog);
                f.Custom.Apply(build,0,true);
                var slots=(GarageSlot[])Enum.GetValues(typeof(GarageSlot));
                var bodies=new Bounds[slots.Length];var details=new Bounds[slots.Length];
                for(int i=0;i<slots.Length;i++)f.Custom.GarageBounds(slots[i],build.Item(slots[i],f.Catalog),out bodies[i],out details[i]);
                f.Effects.Rebind();
                Assert.That(f.InflateEffectBounds(),Is.GreaterThan(0),"Use the actual exhaust effect on the reported body");
                // Both types are also Renderer subclasses, but neither is fitted geometry.
                var wheel=f.Car.WheelVisual(0);
                var trail=new GameObject("Frozen trail bounds",typeof(TrailRenderer));trail.transform.SetParent(wheel,false);
                trail.GetComponent<TrailRenderer>().localBounds=new Bounds(new Vector3(50,0,0),Vector3.one*60);
                var line=new GameObject("Frozen line bounds",typeof(LineRenderer));line.transform.SetParent(wheel,false);
                line.GetComponent<LineRenderer>().localBounds=new Bounds(new Vector3(-50,0,0),Vector3.one*60);
                for(int i=0;i<slots.Length;i++)
                {
                    f.Custom.GarageBounds(slots[i],build.Item(slots[i],f.Catalog),out var body,out var detail);
                    SameBounds(bodies[i],body,slots[i]+" body");SameBounds(details[i],detail,slots[i]+" detail");
                    var clean=RecoveryGarageView.Frame(slots[i],bodies[i],details[i],57,16f/9);
                    var withEffects=RecoveryGarageView.Frame(slots[i],body,detail,57,16f/9);
                    Assert.That(withEffects.Distance,Is.EqualTo(clean.Distance).Within(.0001f),slots[i].ToString());
                }
            }
        }

        [Test] public void BodyAndBumperReplacementsKeepTheMovingCameraNearTheCar()
        {
            using(var f=new Fixture())
            {
                int index=0;
                foreach(var bodyPart in f.Catalog.Parts.Where(p=>p.Slot==GarageSlot.Assembly))
                {
                    var build=new RecoveryBuild{Model=3};build.Set(bodyPart,f.Catalog);
                    f.Car.transform.rotation=Quaternion.Euler(0,(index++%4)*90,0);
                    f.Custom.Apply(build,0,true);
                    // Includes geometry replacement when the same fitted build is applied again.
                    f.Custom.Apply(build,0,true);f.Effects.Rebind();f.InflateEffectBounds();
                    f.CheckCamera(build,GarageSlot.Assembly);
                    foreach(var slot in new[]{GarageSlot.FrontBumper,GarageSlot.RearBumper})
                    {
                        var part=f.Catalog.Parts.FirstOrDefault(p=>p.Slot==slot&&p.Prefabs!=null&&p.Prefabs.Length>0&&f.Catalog.Compatible(p,build));
                        if(part!=null)build.Set(part,f.Catalog);
                        f.Custom.Apply(build,0,true);f.Effects.Rebind();f.InflateEffectBounds();
                        f.CheckCamera(build,slot);
                    }
                }
            }
        }
        private static void SameBounds(Bounds expected,Bounds actual,string label)
        {
            Assert.That(Vector3.Distance(actual.center,expected.center),Is.LessThan(.0001f),label+" center");
            Assert.That(Vector3.Distance(actual.size,expected.size),Is.LessThan(.0001f),label+" size");
        }
        private sealed class Fixture : IDisposable
        {
            public readonly RecoveryGarageCatalog Catalog;
            public readonly RecoveryVehicle Car;
            public readonly RecoveryCustomization Custom;
            public readonly RecoveryVehicleEffects Effects;
            private readonly Scene scene;
            private readonly GameObject root;
            private readonly DemoSession session;
            private readonly RecoveryCamera controller;
            private readonly Camera camera;
            private readonly MethodInfo pose=typeof(RecoveryCamera).GetMethod("GarageCameraPose",BindingFlags.Instance|BindingFlags.NonPublic);
            public Fixture()
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Static Edit Mode fixture only");
                scene=EditorSceneManager.NewPreviewScene();root=new GameObject("Garage framing regression fixture");SceneManager.MoveGameObjectToScene(root,scene);root.SetActive(false);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.transform.SetParent(root.transform,false);
                Car=go.GetComponent<RecoveryVehicle>();Car.SetSpawn(new Vector3(10000,10000,10000),Quaternion.Euler(0,180,0));Car.GetComponent<Rigidbody>().detectCollisions=false;
                Car.SetModel(3);
                Catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
                Custom=go.GetComponent<RecoveryCustomization>();if(Custom==null)Custom=go.AddComponent<RecoveryCustomization>();Custom.Configure(Car,Catalog);
                Effects=go.GetComponent<RecoveryVehicleEffects>();if(Effects==null)Effects=go.AddComponent<RecoveryVehicleEffects>();
                Effects.Configure(Car,AssetDatabase.LoadAssetAtPath<RecoveryVehicleEffectsProfile>("Assets/TowTrouble/Art/VehicleEffects/Hot Rod vehicle effects.asset"));
                var state=new GameObject("Memory-only session");state.transform.SetParent(root.transform,false);session=state.AddComponent<DemoSession>();
                var view=new GameObject("Static camera",typeof(Camera),typeof(RecoveryCamera));view.transform.SetParent(root.transform,false);camera=view.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=57;camera.aspect=16f/9;camera.scene=scene;
                controller=view.GetComponent<RecoveryCamera>();controller.enabled=false;controller.Bind(session);view.transform.position=Car.transform.TransformPoint(new Vector3(-9,4,12));
                root.SetActive(true);
            }
            public int InflateEffectBounds()
            {
                int count=0;
                foreach(var r in Car.GetComponentsInChildren<ParticleSystemRenderer>(true))if(r.name=="Exhaust vapor"&&r.gameObject.activeInHierarchy)
                {
                    // Frozen bounds only: no emission, Play, Simulate or PlayerLoop.
                    r.localBounds=new Bounds(Vector3.zero,Vector3.one*24);count++;
                    Assert.That(r.GetComponent<ParticleSystem>().isPlaying,Is.False);
                }
                return count;
            }
            public void CheckCamera(RecoveryBuild build,GarageSlot slot)
            {
                session.FocusGarage(slot,build.Item(slot,Catalog));
                Custom.GarageBounds(slot,session.GarageFocusPart,out var body,out var detail);
                var frame=RecoveryGarageView.Frame(slot,body,detail,57,16f/9);
                Assert.That(frame.Distance,Is.LessThan(body.size.magnitude*5),"Excessive framing distance: "+slot);
                object[] args={Car,0f,false,1f/60,Vector3.zero,Vector3.zero};
                var expanded=body;expanded.Expand(.65f);
                for(int i=0;i<180;i++)
                {
                    pose.Invoke(controller,args);var p=(Vector3)args[4];var look=(Vector3)args[5];
                    camera.transform.SetPositionAndRotation(p,Quaternion.Slerp(camera.transform.rotation,Quaternion.LookRotation(look-p),1-Mathf.Exp(-7f/60)));
                    Assert.That(expanded.Contains(Car.transform.InverseTransformPoint(p)),Is.False,"Transition passes through body");
                }
                var focus=Car.transform.TransformPoint(frame.Focus);
                Assert.That(Vector3.Distance(camera.transform.position,focus),Is.EqualTo(frame.Distance).Within(.03f),"Camera did not converge: "+slot);
                var vp=camera.WorldToViewportPoint(focus);
                Assert.That(vp.x,Is.InRange(RecoveryGarageLayout.PreviewCenter-.04f,RecoveryGarageLayout.PreviewCenter+.04f));Assert.That(vp.y,Is.InRange(.4f,.48f));
                var min=Vector2.one;var max=Vector2.zero;
                for(int i=0;i<8;i++)
                {
                    var corner=body.center+Vector3.Scale(body.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var p=camera.WorldToViewportPoint(Car.transform.TransformPoint(corner));min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                }
                Assert.That((max.x-min.x)*1600,Is.GreaterThan(150),"Car becomes too small on screen: "+slot);
            }
            public void Dispose(){if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
