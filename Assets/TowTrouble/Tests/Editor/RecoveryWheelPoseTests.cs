using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryWheelPoseTests
    {
        [Test]
        public void SlopedContactsCannotPushHubSideways()
        {
            var anchor = new Vector3(1, 2, 3);
            const float radius = .47f, travel = .32f, distance = .87f;
            var hitPoint = anchor + Vector3.up * (RecoveryWheelPose.ProbeLift - distance);
            var normalA = Quaternion.Euler(0, 0, 20) * Vector3.up;
            var normalB = Quaternion.Euler(0, 0, -20) * Vector3.up;
            // The former hit + normal * radius placement moves laterally by 32 cm here.
            float oldSideShift = Mathf.Abs(((hitPoint + normalA * radius) - (hitPoint + normalB * radius)).x);
            Assert.That(oldSideShift, Is.GreaterThan(.3f));
            var center = RecoveryWheelPose.Position(anchor, Vector3.up,
                RecoveryWheelPose.SuspensionOffset(distance, radius, travel));
            Assert.That(center.x, Is.EqualTo(anchor.x).Within(.00001f));
            Assert.That(center.z, Is.EqualTo(anchor.z).Within(.00001f));
            Assert.That(center.y, Is.EqualTo(1.9f).Within(.00001f));
        }

        [TestCase(30)] [TestCase(60)] [TestCase(144)]
        public void WheelsFollowInterpolatedChassisBetweenPhysicsTicks(int frameRate)
        {
            const float step = .02f;
            var anchor = new Vector3(-.9f, .6f, 1.4f);
            float oldLocalError = 0;
            for(int frame = 1; frame <= frameRate * 2; frame++)
            {
                double time = frame / (double)frameRate;
                double current = System.Math.Floor(time / step) * step;
                float blend = RecoveryWheelPose.InterpolationAlpha(time, current, step);
                float before = (float)current - step, now = (float)current;
                var positionA = new Vector3(before * 3, 0, before * 10);
                var positionB = new Vector3(now * 3, 0, now * 10);
                var rotationA = Quaternion.Euler(7, before * 25, 9);
                var rotationB = Quaternion.Euler(7, now * 25, 9);
                var position = Vector3.Lerp(positionA, positionB, blend);
                var rotation = Quaternion.Slerp(rotationA, rotationB, blend);
                float offsetA = -.17f + Mathf.Sin(before * 4) * .08f;
                float offsetB = -.17f + Mathf.Sin(now * 4) * .08f;
                var renderedAnchor = position + rotation * anchor;
                var center = RecoveryWheelPose.Position(renderedAnchor, rotation * Vector3.up,
                    Mathf.Lerp(offsetA, offsetB, blend));
                var local = Quaternion.Inverse(rotation) * (center - position);
                Assert.That(local.x, Is.EqualTo(anchor.x).Within(.00002f));
                Assert.That(local.z, Is.EqualTo(anchor.z).Within(.00002f));
                Assert.That(local.y - anchor.y, Is.InRange(-.251f, -.089f));
                var formerWorldCenter = positionB + rotationB * (anchor + Vector3.up * offsetB);
                var formerLocal = Quaternion.Inverse(rotation) * (formerWorldCenter - position);
                oldLocalError = Mathf.Max(oldLocalError, new Vector2(formerLocal.x-anchor.x, formerLocal.z-anchor.z).magnitude);
            }
            Assert.That(oldLocalError, Is.GreaterThan(.05f), "Fixture must expose the old fixed-world-position jitter.");
        }

        [Test]
        public void RollingDoesNotChangeTheSteeredAxle()
        {
            var chassis = Quaternion.Euler(14, 117, -9);
            foreach(float steer in new[]{-35f, 0f, 35f})
            {
                var axle = chassis * Quaternion.AngleAxis(steer, Vector3.up) * Vector3.right;
                foreach(float spin in new[]{-720f, -91f, 0f, 87f, 180f, 359.99f, 360f, 721f})
                    Assert.That(Vector3.Distance(RecoveryWheelPose.Rotation(chassis, steer, spin) * Vector3.right, axle), Is.LessThan(.00001f));
            }
        }

        [Test]
        public void SuspensionAndInterpolationStayWithinTheirLimits()
        {
            Assert.That(RecoveryWheelPose.SuspensionOffset(.1f, .47f, .32f), Is.Zero);
            Assert.That(RecoveryWheelPose.SuspensionOffset(10f, .47f, .32f), Is.EqualTo(-.32f));
            Assert.That(RecoveryWheelPose.InterpolationAlpha(100000.01, 100000, .02f), Is.EqualTo(.5f).Within(.00001f));
            Assert.That(RecoveryWheelPose.InterpolationAlpha(9, 10, .02f), Is.Zero);
            Assert.That(RecoveryWheelPose.InterpolationAlpha(11, 10, .02f), Is.EqualTo(1));
            Assert.That(RecoveryWheelPose.InterpolationAlpha(10, 10, 0), Is.EqualTo(1));
        }

        [Test]
        public void ModelChangesResetsAndRemoteTeleportsDiscardOldWheelSamples()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            try
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TowTrouble/Prefabs/Scout.prefab"), scene);
                root.SetActive(false);
                var vehicle = root.GetComponent<RecoveryVehicle>();
                vehicle.SetSpawn(Vector3.zero, Quaternion.identity);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var previous = (float[])typeof(RecoveryVehicle).GetField("previousSuspensionOffsets", flags).GetValue(vehicle);
                var current = (float[])typeof(RecoveryVehicle).GetField("suspensionOffsets", flags).GetValue(vehicle);
                System.Action poison = () => {for(int i=0;i<4;i++){previous[i]=-.31f;current[i]=0;}};
                System.Action check = () => {for(int i=0;i<4;i++){Assert.That(previous[i],Is.EqualTo(-.12f).Within(.00001f));Assert.That(current[i],Is.EqualTo(previous[i]));}};
                poison(); vehicle.ResetVehicle(true); check();
                poison(); vehicle.SetModel(2); check();
                poison(); vehicle.Receive(new Vector3(100, 10, 100), Quaternion.Euler(0, 90, 0), 0); check();
                Assert.That(vehicle.Body.isKinematic, Is.True);
                Assert.That(root.activeSelf, Is.False);
            }
            finally
            {
                if(root != null)Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
