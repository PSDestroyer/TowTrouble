using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryWeatherTests
    {
        [Test] public void DayWrapsAndDuskDawnHaveGradualHeadlights()
        {
            Assert.That(RecoveryWeatherRules.Day(0),Is.EqualTo(.4f).Within(.0001f));
            Assert.That(RecoveryWeatherRules.Day(RecoveryWeatherRules.DaySeconds),Is.EqualTo(.4f).Within(.0001f));
            Assert.That(RecoveryWeatherRules.Night(.5f),Is.Zero);
            Assert.That(RecoveryWeatherRules.Night(0),Is.EqualTo(1));
            Assert.That(RecoveryWeatherRules.Night(19f/24),Is.InRange(.1f,.9f));
            Assert.That(RecoveryWeatherRules.Night(6.4f/24),Is.InRange(.1f,.9f));
        }
        [Test] public void ForecastIsDeterministicContinuousAndDoesNotConsumeGameplayRandom()
        {
            var before=Random.state;int first=RecoveryWeatherRules.Forecast(12,12345);
            Assert.That(RecoveryWeatherRules.Forecast(12,12345),Is.EqualTo(first));Assert.That(Random.state,Is.EqualTo(before));
            RecoveryWeatherRules.Weather(299.999f,12345,out _,out int target,out float a);
            RecoveryWeatherRules.Weather(300,12345,out int next,out _,out float b);
            Assert.That(target,Is.EqualTo(next));Assert.That(a,Is.GreaterThan(.999f));Assert.That(b,Is.Zero);
            for(int i=0;i<40;i++)Assert.That(RecoveryWeatherRules.Forecast(i,12345),Is.InRange(0,3));
        }
        [Test] public void SnowRegionKeepsSnowfallAndReplacesRain()
        {
            RecoveryWeatherRules.Precipitation(1,0,out float warmRain,out float warmSnow);
            RecoveryWeatherRules.Precipitation(1,1,out float coldRain,out float coldSnow);
            Assert.That(warmRain,Is.GreaterThan(.5f));Assert.That(warmSnow,Is.Zero);
            Assert.That(coldRain,Is.Zero);Assert.That(coldSnow,Is.GreaterThan(.5f));
            RecoveryWeatherRules.Precipitation(0,1,out _,out float flurries);Assert.That(flurries,Is.GreaterThan(0));
        }
        [Test] public void EveryStarterHasTwoAutomaticHeadlightsWithoutAccumulatingAfterBodyChanges()
        {
            var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
            try
            {
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab"),scene);root.SetActive(false);
                var car=root.GetComponent<RecoveryVehicle>();car.SetSpawn(Vector3.zero,Quaternion.identity);var effects=root.GetComponent<RecoveryVehicleEffects>();
                var present=typeof(RecoveryVehicleEffects).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                for(int i=0;i<4;i++)
                {
                    car.SetModel(i);car.Receive(new VehicleFrame{Rotation=Quaternion.identity,EngineRunning=true});effects.Rebind();effects.SetNightLighting(1);present.Invoke(effects,null);
                    var lights=root.GetComponentsInChildren<Light>(true);Assert.That(lights.Length,Is.EqualTo(2));
                    foreach(var light in lights){Assert.That(light.enabled,Is.True);Assert.That(light.intensity,Is.GreaterThan(20));Assert.That(light.type,Is.EqualTo(LightType.Spot));Assert.That(light.shadows,Is.EqualTo(LightShadows.None));}
                    effects.SetNightLighting(0);present.Invoke(effects,null);foreach(var light in lights)Assert.That(light.enabled,Is.False);
                    effects.SetNightLighting(1);car.Receive(new VehicleFrame{Rotation=Quaternion.identity,EngineRunning=false});present.Invoke(effects,null);foreach(var light in lights)Assert.That(light.enabled,Is.False);
                }
            }
            finally{if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
