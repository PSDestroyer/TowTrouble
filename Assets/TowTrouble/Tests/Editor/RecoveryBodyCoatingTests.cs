using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryBodyCoatingTests
    {
        [Test]
        public void FirstContactIsGradualAndOldLayerSurvivesAChangeOfSurface()
        {
            var first = RecoveryBodyCoatingRules.Step(Vector3.zero, BodyCoatingSurface.Snow, 7, 0, .1f);
            Assert.That(first.x, Is.GreaterThan(0).And.LessThan(.02f));
            var snowy = RecoveryBodyCoatingRules.Step(Vector3.zero, BodyCoatingSurface.Snow, 7, 0, 25);
            var mud = RecoveryBodyCoatingRules.Step(snowy, BodyCoatingSurface.Mud, 7, 0, .1f);
            Assert.That(mud.x, Is.GreaterThan(snowy.x * .97f));
            Assert.That(mud.y, Is.GreaterThan(0).And.LessThan(.02f));
            var mixed = RecoveryBodyCoatingRules.Step(snowy, BodyCoatingSurface.Mud, 7, 0, 10);
            Assert.That(mixed.x, Is.GreaterThan(.1f)); Assert.That(mixed.y, Is.InRange(.4f, .8f));
        }

        [Test]
        public void RoadCleansProgressivelyAndMudLastsLongerThanSnowAndSand()
        {
            var dirty = new Vector3(.33f, .34f, .33f);
            var shortTrip = RecoveryBodyCoatingRules.Step(dirty, BodyCoatingSurface.Road, 7, 0, 1);
            var longTrip = RecoveryBodyCoatingRules.Step(dirty, BodyCoatingSurface.Road, 7, 0, 60);
            Assert.That(shortTrip.x + shortTrip.y + shortTrip.z, Is.GreaterThan(.9f));
            Assert.That(longTrip.y / dirty.y, Is.GreaterThan(longTrip.x / dirty.x));
            Assert.That(longTrip.x / dirty.x, Is.GreaterThan(longTrip.z / dirty.z));
            Assert.That(longTrip.sqrMagnitude, Is.LessThan(shortTrip.sqrMagnitude));
        }

        [Test]
        public void ParkingAirAndPauseDoNotWashOrCoatTheBodyButWheelspinThrowsMud()
        {
            var dirty = new Vector3(.2f, .4f, .1f);
            Assert.That(RecoveryBodyCoatingRules.Step(dirty, BodyCoatingSurface.Road, 0, 1, 60), Is.EqualTo(dirty));
            Assert.That(RecoveryBodyCoatingRules.Step(dirty, BodyCoatingSurface.Mud, 0, 0, 60), Is.EqualTo(dirty));
            Assert.That(RecoveryBodyCoatingRules.Step(dirty, BodyCoatingSurface.Air, 20, 1, 60), Is.EqualTo(dirty));
            Assert.That(RecoveryBodyCoatingRules.Step(dirty, BodyCoatingSurface.Snow, 7, 1, 0), Is.EqualTo(dirty));
            Assert.That(RecoveryBodyCoatingRules.Step(dirty, BodyCoatingSurface.Mud, 0, 1, 20).y, Is.GreaterThan(.8f));
        }

        [Test]
        public void AccumulationAndCleaningAreIndependentOfSampleRate()
        {
            foreach (var surface in new[] { BodyCoatingSurface.Snow, BodyCoatingSurface.Mud, BodyCoatingSurface.Sand, BodyCoatingSurface.Road })
            {
                var value = new Vector3(.2f, .3f, .1f);
                var whole = RecoveryBodyCoatingRules.Step(value, surface, 4, .3f, 12);
                for (int sample = 0; sample < 120; sample++) value = RecoveryBodyCoatingRules.Step(value, surface, 4, .3f, .1f);
                Assert.That(Vector3.Distance(value, whole), Is.LessThan(.0001f));
            }
        }

        [Test]
        public void RepeatedBiomeChangesDoNotExceedTheAvailableCoatingWeight()
        {
            var value = Vector3.zero;
            for (int step = 0; step < 1000; step++)
            {
                value = RecoveryBodyCoatingRules.Step(value, (BodyCoatingSurface)(2 + step % 5), 9, .8f, .4f);
                Assert.That(value.x + value.y + value.z, Is.LessThanOrEqualTo(1.00001f));
                Assert.That(value.x, Is.InRange(0, 1)); Assert.That(value.y, Is.InRange(0, 1)); Assert.That(value.z, Is.InRange(0, 1));
            }
        }

        [Test]
        public void ClassicUsesActualContactMaterialAndWinterOverrideStaysScoped()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Loose sand", typeof(BoxCollider));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var ground = go.AddComponent<RecoveryGroundSurface>(); ground.Configure(RecoveryGroundKind.Sand);
                Physics.SyncTransforms(); var collider = go.GetComponent<BoxCollider>();
                Assert.That(collider.Raycast(new Ray(Vector3.up * 2, Vector3.down), out var hit, 4), Is.True);
                Assert.That(RecoveryBodyCoatingRules.Classify(hit, false), Is.EqualTo(BodyCoatingSurface.Sand));
                Assert.That(RecoveryBodyCoatingRules.Classify(hit, true), Is.EqualTo(BodyCoatingSurface.Snow));
                go.name = "Road-test";
                Assert.That(RecoveryBodyCoatingRules.Classify(hit, false), Is.EqualTo(BodyCoatingSurface.Road));
                var track = go.AddComponent<RecoveryTrackSurface>(); track.Configure(RecoveryTrackKind.Wood);
                Assert.That(RecoveryBodyCoatingRules.Classify(hit, true), Is.EqualTo(BodyCoatingSurface.Road));
                track.Configure(RecoveryTrackKind.Snow);
                Assert.That(RecoveryBodyCoatingRules.Classify(hit, false), Is.EqualTo(BodyCoatingSurface.Snow));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
