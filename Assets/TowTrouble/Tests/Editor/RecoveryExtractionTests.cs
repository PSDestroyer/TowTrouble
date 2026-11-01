using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryExtractionTests
    {
        [Test] public void ReleaseParticlesRespectDisabledEffectsAndTheParticleBudget()
        {
            Assert.That(RecoveryExtractionVisuals.ParticleCount(0, 18), Is.Zero);
            Assert.That(RecoveryExtractionVisuals.ParticleCount(-1, 18), Is.Zero);
            Assert.That(RecoveryExtractionVisuals.ParticleCount(5, 18), Is.EqualTo(18));
            Assert.That(RecoveryExtractionVisuals.ParticleCount(.5f, 14), Is.EqualTo(7));
        }
        private static RecoveryExtractionSettings Config() => new RecoveryExtractionSettings
        { Enabled = true, Travel = 2.2f, BreakawayForce = 5600, MovingForce = 3600, MovingDrag = 0, ClearHold = .2f, ReleaseDuration = .4f };
        private static RecoveryExtraction Model(RecoveryExtractionSettings config = null) =>
            new RecoveryExtraction(config ?? Config(), Vector3.zero, Vector3.forward, Vector3.forward);

        [Test] public void HoldingThrottleWithoutDisplacementNeverReleasesTheCar()
        {
            var model = Model();
            for (int i = 0; i < 1000; i++) model.Step(Vector3.zero, Vector3.forward, Vector3.zero, Vector3.forward * 4000, 950, true, true, .02f);
            Assert.That(model.Progress, Is.Zero);
            Assert.That(model.Stage, Is.EqualTo(ExtractionStage.TakingLoad));
            Assert.That(model.StalledSeconds, Is.GreaterThan(1.5f));
        }
        [Test] public void SidewaysReverseAndVerticalTravelDoNotClearTheExit()
        {
            foreach (var displacement in new[] { Vector3.right * 20, Vector3.back * 20, Vector3.up * 20 })
            {
                var model = Model();
                for (int i = 0; i < 100; i++) model.Step(displacement, Vector3.forward + displacement, Vector3.zero, displacement.normalized * 8000, 950, true, true, .02f);
                Assert.That(model.Progress, Is.Zero);
                Assert.That(model.IsFree, Is.False);
            }
        }
        [Test] public void RotatingTheWheelAcrossTheExitWithoutMovingTheBodyDoesNotRelease()
        {
            var model = Model();
            model.Step(Vector3.zero, Vector3.forward * 5, Vector3.zero, Vector3.forward * 8000, 950, true, true, 2);
            Assert.That(model.Progress, Is.Zero);
            Assert.That(model.IsFree, Is.False);
        }
        [Test] public void BodyCrossingBeforeTheBlockedWheelDoesNotRelease()
        {
            var model = Model();
            for (int i = 0; i < 100; i++) model.Step(Vector3.forward * 3, Vector3.forward * 2, Vector3.zero, Vector3.forward * 8000, 950, true, true, .02f);
            Assert.That(model.Progress, Is.LessThan(1));
            Assert.That(model.IsFree, Is.False);
        }
        [Test] public void AirborneExitDoesNotReleaseOrApplyGroundResistance()
        {
            var model = Model();
            for (int i = 0; i < 100; i++)
                Assert.That(model.Step(Vector3.forward * 3, Vector3.forward * 4, Vector3.forward, Vector3.forward * 8000, 950, false, true, .02f), Is.EqualTo(Vector3.zero));
            Assert.That(model.IsFree, Is.False);
        }
        [Test] public void ForwardExitRequiresContactHoldThenSmoothlyRemovesResistance()
        {
            var model = Model();
            var pull = Vector3.forward * 8000;
            model.Step(Vector3.forward * 3, Vector3.forward * 4, Vector3.forward, pull, 950, true, true, .1f);
            Assert.That(model.Stage, Is.Not.EqualTo(ExtractionStage.Releasing));
            float first = model.Step(Vector3.forward * 3, Vector3.forward * 4, Vector3.forward, pull, 950, true, true, .1f).magnitude;
            Assert.That(model.Stage, Is.EqualTo(ExtractionStage.Releasing));
            float second = model.Step(Vector3.forward * 3, Vector3.forward * 4, Vector3.forward, pull, 950, true, true, .1f).magnitude;
            Assert.That(second, Is.LessThan(first));
            for (int i = 0; i < 10; i++) model.Step(Vector3.forward * 3, Vector3.forward * 4, Vector3.forward, pull, 950, true, true, .1f);
            Assert.That(model.IsFree, Is.True);
        }
        [Test] public void LostSupportRestartsTheContactHold()
        {
            var model = Model();
            var position = Vector3.forward * 3;
            model.Step(position, position + Vector3.forward, Vector3.zero, Vector3.forward * 8000, 950, true, true, .15f);
            model.Step(position, position + Vector3.forward, Vector3.zero, Vector3.forward * 8000, 950, false, true, .1f);
            model.Step(position, position + Vector3.forward, Vector3.zero, Vector3.forward * 8000, 950, true, true, .1f);
            Assert.That(model.Stage, Is.Not.EqualTo(ExtractionStage.Releasing));
        }
        [Test] public void BeforeConnectionCrossingThePlaneCannotCompleteRecovery()
        {
            var model = Model();
            for (int i = 0; i < 100; i++) model.Step(Vector3.forward * 3, Vector3.forward * 4, Vector3.zero, Vector3.zero, 950, true, false, .02f);
            Assert.That(model.Stage, Is.EqualTo(ExtractionStage.Blocked));
        }
        [Test] public void ResistanceAtRestNeverPullsTheCarBackOrExceedsTheAppliedPull()
        {
            var model = Model();
            var pull = Vector3.forward * 1000;
            var resistance = model.Step(Vector3.zero, Vector3.forward, Vector3.zero, pull, 950, true, true, .02f);
            Assert.That((pull + resistance).magnitude, Is.LessThan(.001f));
            Assert.That(model.Step(Vector3.zero, Vector3.forward, Vector3.zero, Vector3.zero, 950, true, true, .02f), Is.EqualTo(Vector3.zero));
        }
        [Test] public void PoorAngleNeedsMoreForceThanAnAlignedPull()
        {
            var good = Model(); var bad = Model();
            var aligned = good.Step(Vector3.zero, Vector3.forward, Vector3.zero, Vector3.forward * 6000, 950, true, true, .02f);
            var sideways = bad.Step(Vector3.zero, Vector3.forward, Vector3.zero, Vector3.right * 6000, 950, true, true, .02f);
            Assert.That(aligned.magnitude, Is.LessThan(sideways.magnitude));
            Assert.That(good.Alignment, Is.EqualTo(1));
            Assert.That(bad.Stage, Is.EqualTo(ExtractionStage.TakingLoad));
        }
        [Test] public void AStrongPullWithoutMovementDoesNotClaimTheCarIsMoving()
        {
            var model = Model();
            model.Step(Vector3.zero, Vector3.forward, Vector3.zero, Vector3.forward * 8000, 950, true, true, .02f);
            Assert.That(model.Stage, Is.EqualTo(ExtractionStage.TakingLoad));
            model.Step(Vector3.forward * .1f, Vector3.forward * 1.1f, Vector3.forward * .2f, Vector3.forward * 8000, 950, true, true, .02f);
            Assert.That(model.Stage, Is.EqualTo(ExtractionStage.Moving));
        }
        [Test] public void AReleasedCarStaysFreeWhenTurningOrReversingAndANewRunResets()
        {
            var model = Model();
            for (int i = 0; i < 100; i++) model.Step(Vector3.forward * 3, Vector3.forward * 4, Vector3.zero, Vector3.forward * 8000, 950, true, true, .02f);
            Assert.That(model.IsFree, Is.True);
            var force = model.Step(Vector3.back * 5, Vector3.back * 4, Vector3.back, Vector3.back * 8000, 950, true, true, .02f);
            Assert.That(model.IsFree, Is.True);
            Assert.That(force, Is.EqualTo(Vector3.zero));
            Assert.That(Model().Stage, Is.EqualTo(ExtractionStage.Blocked));
        }
        [Test] public void PartialProgressIsPositionBasedAndSurvivesDetaching()
        {
            var model = Model(); var position = Vector3.forward * 1.1f;
            model.Step(position, position + Vector3.forward, Vector3.zero, Vector3.forward * 8000, 950, true, true, .02f);
            model.Step(position, position + Vector3.forward, Vector3.zero, Vector3.zero, 950, true, true, 5);
            Assert.That(model.Progress, Is.EqualTo(.5f).Within(.001f));
            Assert.That(model.IsFree, Is.False);
        }
        [Test] public void CableTakesLoadGraduallyButSlackDropsForceImmediately()
        {
            float force = RecoveryExtraction.LoadedCableForce(0, 20000, 31000, .02f);
            Assert.That(force, Is.GreaterThan(0).And.LessThan(2000));
            Assert.That(RecoveryExtraction.LoadedCableForce(force, 0, 31000, .02f), Is.Zero);
            Assert.That(RecoveryExtraction.LoadedCableForce(0, 20000, 31000, 0), Is.Zero);
        }
        [Test] public void ReleaseAndAngleFeedbackAreLocalizedAndExpire()
        {
            Assert.That(RecoveryHud.ExtractionMessage(ExtractionStage.Free, 1, 0, .5f, false), Does.Contain("ELIBERATĂ"));
            Assert.That(RecoveryHud.ExtractionMessage(ExtractionStage.Free, 1, 0, 3, true), Is.Empty);
            Assert.That(RecoveryHud.ExtractionMessage(ExtractionStage.TakingLoad, 0, 2, 0, true), Does.Contain("ANGLE"));
            Assert.That(RecoveryHud.ExtractionMessage(ExtractionStage.TakingLoad, 1, 2, 0, false), Does.Contain("FĂRĂ PROGRES"));
        }
    }
}
