using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class CableStressTests
    {
        private static float BreakTime(float force, float strength, float step)
        {
            float stress = 0;
            for (int tick = 1; tick <= Mathf.CeilToInt(2 / step); tick++)
            {
                stress = RecoveryRules.CableStressAfter(stress, force, strength, step);
                if (stress >= 1) return tick * step;
            }
            return float.PositiveInfinity;
        }

        [Test] public void RatedAndGentleLoadsDoNotBreakAnyCableTier()
        {
            foreach (float strength in new[] {11000f,14000f,18000f,26000f,36000f})
            {
                Assert.That(BreakTime(strength, strength, .02f), Is.EqualTo(float.PositiveInfinity));
                Assert.That(BreakTime(5600, strength, .02f), Is.EqualTo(float.PositiveInfinity));
            }
        }
        [Test] public void AShortMajorShockBreaksTheStarterInTheSameStep()
        {
            Assert.That(RecoveryRules.CableStressAfter(0, 30000, 11000, .02f), Is.EqualTo(1));
            Assert.That(RecoveryRules.CableStressAfter(0, 18150, 11000, .02f), Is.EqualTo(1));
        }
        [Test] public void LoadRampCannotHideTheRuptureDemand()
        {
            float smoothed = RecoveryExtraction.LoadedCableForce(0, 30000, 11000, .02f);
            Assert.That(smoothed, Is.LessThan(11000));
            Assert.That(RecoveryRules.CableStressAfter(0, 30000, 11000, .02f), Is.EqualTo(1));
        }
        [Test] public void PhysicsForceCeilingDoesNotCapTheBreakingDemand()
        {
            float demand = RecoveryRules.CableDemand(16, 6, 20);
            Assert.That(demand, Is.GreaterThan(RecoveryRules.MaxCableForce));
            Assert.That(RecoveryRules.CableForce(16, 6, 20), Is.EqualTo(RecoveryRules.MaxCableForce));
            Assert.That(RecoveryRules.CableStressAfter(0, demand, 36000, .02f), Is.EqualTo(1));
        }
        [Test] public void GreaterExcessFailsFasterThanAMinorOverload()
        {
            float minor = BreakTime(11550, 11000, .01f);
            float moderate = BreakTime(13750, 11000, .01f);
            float large = BreakTime(16500, 11000, .01f);
            Assert.That(minor, Is.InRange(.12f, .15f));
            Assert.That(moderate, Is.InRange(.05f, .08f));
            Assert.That(large, Is.InRange(.03f, .05f));
            Assert.That(large, Is.LessThan(moderate)); Assert.That(moderate, Is.LessThan(minor));
        }
        [Test] public void TheSameShockRewardsStrongerEquipment()
        {
            Assert.That(BreakTime(21000, 11000, .02f), Is.EqualTo(.02f));
            Assert.That(BreakTime(21000, 14000, .02f), Is.GreaterThan(.02f));
            Assert.That(BreakTime(21000, 18000, .02f), Is.GreaterThan(BreakTime(21000, 14000, .02f)));
            Assert.That(BreakTime(21000, 26000, .02f), Is.EqualTo(float.PositiveInfinity));
            Assert.That(BreakTime(21000, 36000, .02f), Is.EqualTo(float.PositiveInfinity));
        }
        [Test] public void RepeatedShortPulsesAccumulateInsteadOfResettingTolerance()
        {
            float stress = 0;
            for (int i = 0; i < 4 && stress < 1; i++)
            {
                stress = RecoveryRules.CableStressAfter(stress, 13750, 11000, .02f);
                if (stress < 1) stress = RecoveryRules.CableStressAfter(stress, 0, 11000, .04f);
            }
            Assert.That(stress, Is.EqualTo(1));
        }
        [Test] public void UnloadingRelaxesStressGraduallyIncludingWhileDetached()
        {
            float stress = RecoveryRules.CableStressAfter(0, 13750, 11000, .02f);
            float brieflyDetached = RecoveryRules.CableStressAfter(stress, 0, 11000, .35f);
            Assert.That(brieflyDetached, Is.GreaterThan(0).And.LessThan(stress));
            Assert.That(RecoveryRules.CableStressAfter(brieflyDetached, 0, 11000, 2), Is.Zero);
            Assert.That(RecoveryRules.CableStressAfter(stress, 0, 11000, 0), Is.EqualTo(stress));
        }
        [Test] public void SlackCableAtSpeedDoesNotAccumulateStress()
        {
            float demand = RecoveryRules.CableDemand(5, 6, 30);
            Assert.That(demand, Is.Zero);
            Assert.That(RecoveryRules.CableStressAfter(0, demand, 11000, .02f), Is.Zero);
        }
        [Test] public void ContinuousFailureTimingIsStableAcrossPhysicsSteps()
        {
            float small = BreakTime(12100, 11000, .01f);
            foreach (float step in new[] {.02f,.033333f,.04f})
                Assert.That(Mathf.Abs(BreakTime(12100, 11000, step) - small), Is.LessThanOrEqualTo(.041f));
        }
    }
}
