using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryVehicleEffectsTests
    {
        [Test] public void BrakeSignalsDistinguishBrakingFromSteadyReverse()
        {
            Assert.That(RecoveryVehicleEffects.ShouldBrake(-1,false,4),Is.True);
            Assert.That(RecoveryVehicleEffects.ShouldBrake(1,false,-4),Is.True);
            Assert.That(RecoveryVehicleEffects.ShouldBrake(-1,false,-4),Is.False);
            Assert.That(RecoveryVehicleEffects.ShouldBrake(1,false,4),Is.False);
            Assert.That(RecoveryVehicleEffects.ShouldBrake(0,false,0),Is.False);
            Assert.That(RecoveryVehicleEffects.ShouldBrake(0,true,0),Is.True);
            Assert.That(RecoveryVehicleEffects.ShouldBrake(-1,false,.1f),Is.False);
        }
        [Test] public void ExhaustStopsWithEngineAndSharesABoundedEmissionBudget()
        {
            Assert.That(RecoveryVehicleEffects.SmokeRate(false,1,1,2),Is.Zero);
            Assert.That(RecoveryVehicleEffects.SmokeRate(true,1,1,0),Is.Zero);
            Assert.That(RecoveryVehicleEffects.SmokeRate(true,0,0,1),Is.EqualTo(4));
            Assert.That(RecoveryVehicleEffects.SmokeRate(true,1,1,8)*8,Is.EqualTo(18));
            Assert.That(RecoveryVehicleEffects.SmokeRate(true,-1,0,2),Is.EqualTo(9));
        }
        [Test] public void BrakingAtlasOnlyChangesTheNativeRearLampCells()
        {
            var profile=AssetDatabase.LoadAssetAtPath<RecoveryVehicleEffectsProfile>("Assets/TowTrouble/Art/VehicleEffects/Hot Rod vehicle effects.asset");
            foreach(var atlas in profile.Atlases)
            {
                var running=((Texture2D)atlas.Running).GetPixels();var braking=((Texture2D)atlas.Braking).GetPixels();int changed=0,steadyLamps=0;
                for(int i=0;i<running.Length;i++)
                {
                    if((running[i]-braking[i]).maxColorComponent>.001f||(braking[i]-running[i]).maxColorComponent>.001f)
                    {changed++;Assert.That(braking[i].r,Is.GreaterThan(running[i].r*10));Assert.That(braking[i].r,Is.GreaterThan(braking[i].g*1.45f));}
                    else if(running[i].maxColorComponent>.05f&&running[i].g>.05f)steadyLamps++;
                }
                Assert.That(changed,Is.GreaterThan(100));Assert.That(changed,Is.LessThan(running.Length/30));Assert.That(steadyLamps,Is.GreaterThan(100));
            }
        }
        [Test] public void RemoteSignalsSurviveSnapshotsAndResetWithoutPhysics()
        {
            var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
            try
            {
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab"),scene);root.SetActive(false);
                var car=root.GetComponent<RecoveryVehicle>();car.SetSpawn(Vector3.zero,Quaternion.identity);
                car.Receive(new VehicleFrame{Position=Vector3.zero,Rotation=Quaternion.identity,BrakeLights=true,EngineRunning=true});
                Assert.That(car.BrakeLights,Is.True);Assert.That(car.EngineRunning,Is.True);
                var copy=new VehicleFrame(car);Assert.That(copy.BrakeLights,Is.True);Assert.That(copy.EngineRunning,Is.True);
                car.ResetVehicle(true);Assert.That(car.BrakeLights,Is.False);Assert.That(car.EngineRunning,Is.False);Assert.That(car.Body.isKinematic,Is.True);
            }
            finally{if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void EffectsRebindToVisiblePipesAndDoNotAccumulateOnModelChanges()
        {
            var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
            try
            {
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab"),scene);root.SetActive(false);
                var car=root.GetComponent<RecoveryVehicle>();car.SetSpawn(Vector3.zero,Quaternion.identity);var effects=root.GetComponent<RecoveryVehicleEffects>();
                car.SetModel(2);effects.Rebind();effects.ApplyState(true,true,1,1);
                var systems=root.GetComponentsInChildren<ParticleSystem>(true);int exhaustCount=0;
                foreach(var ps in systems)if(ps.name=="Exhaust vapor"){exhaustCount++;Assert.That(ps.main.playOnAwake,Is.False);Assert.That(ps.isPlaying,Is.False);Assert.That(ps.main.maxParticles,Is.EqualTo(20));}
                Assert.That(exhaustCount,Is.GreaterThan(0));
                effects.Rebind();int second=0;foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))if(ps.name=="Exhaust vapor")second++;Assert.That(second,Is.EqualTo(exhaustCount));
                for(int model=0;model<car.ModelCount;model++)
                {
                    car.SetModel(model);effects.Rebind();int count=0;
                    foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))if(ps.name=="Exhaust vapor")
                    {
                        count++;Assert.That(ps.isPlaying,Is.False);Assert.That(ps.main.maxParticles,Is.EqualTo(20));
                    }
                    Assert.That(count,Is.InRange(1,8),"Every stock model needs an exhaust outlet");
                }
                effects.ApplyState(false,false,0,0);
                Assert.That(car.EngineRunning,Is.False);
                foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))if(ps.name=="Exhaust vapor")Assert.That(ps.emission.rateOverTime.constant,Is.Zero);
            }
            finally{if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
