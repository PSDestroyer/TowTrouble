using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class WinterWeatherTests
    {
        [Test]
        public void ForecastAndPhysicalConditionsStayContinuousAcrossEveryChange()
        {
            for (int boundary = 1; boundary <= 8; boundary++)
            {
                var before = WinterWeatherRules.At(boundary * WinterWeatherRules.Period - .001f);
                var after = WinterWeatherRules.At(boundary * WinterWeatherRules.Period);
                Assert.That(before.Next, Is.EqualTo(after.Current));
                Assert.That(before.Snow, Is.EqualTo(after.Snow).Within(.001f));
                Assert.That(before.Gusts, Is.EqualTo(after.Gusts).Within(.001f));
                Assert.That(before.Ice, Is.EqualTo(after.Ice).Within(.001f));
            }
        }
        [Test]
        public void FreezeOnlyReducesGripOnTheVisibleRoadPatchesAndKeepsUsableTraction()
        {
            var frost = WinterWeatherRules.At(WinterWeatherRules.Period * 3);
            var flurries = WinterWeatherRules.At(0);
            var patch = new Vector3(117, 66.1f, 161.6f);
            Assert.That(WinterWeatherRules.Grip(patch, true, frost), Is.InRange(.67f, .7f));
            Assert.That(WinterWeatherRules.Grip(patch, true, frost), Is.LessThan(WinterWeatherRules.Grip(patch, true, flurries)));
            Assert.That(WinterWeatherRules.Grip(patch, false, frost), Is.EqualTo(1));
            Assert.That(WinterWeatherRules.Grip(patch + Vector3.right * 25, true, frost), Is.EqualTo(1));
            Assert.That(WinterWeatherRules.Grip(patch + Vector3.up * 20, true, frost), Is.EqualTo(1));
        }
        [Test]
        public void WeatherForecastNeverConsumesGameplayRandomOrMutatesCareerReceipts()
        {
            var profile = RecoveryProfile.Parse("{\"version\":4,\"winterJobs\":15,\"winterSurveys\":5,\"credits\":1500}");
            var saved = JsonUtility.ToJson(profile); var random = Random.state;
            for (int i = 0; i < 100; i++)
            {
                var weather = WinterWeatherRules.At(i * 123);
                Assert.That(weather.Snow, Is.InRange(.14f, .96f));
                Assert.That(weather.Forecast, Is.Not.Empty);
            }
            Assert.That(Random.state, Is.EqualTo(random));
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(saved));
        }
    }
}
