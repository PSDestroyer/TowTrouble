using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble.Tests
{
    public sealed class RecoveryHapticsTests
    {
        private static RecoveryHapticFrame Frame()=>new RecoveryHapticFrame{Active=true,Slot=0,Run="run",Recovering=true};
        private static Vector2 Settle(RecoveryHaptics model,RecoveryHapticFrame frame,float intensity=1)
        {Vector2 value=default;for(int i=0;i<90;i++)value=model.Step(frame,1f/60,intensity);return value;}
        [Test] public void OrdinaryDrivingAndSlackCableStaySilent()
        {
            var model=new RecoveryHaptics();var frame=Frame();Assert.That(Settle(model,frame),Is.EqualTo(Vector2.zero));frame.Attached=true;frame.Tension=.2f;Assert.That(Settle(model,frame),Is.EqualTo(Vector2.zero));
        }
        [Test] public void TensionBuildsGraduallyAndPaysOutSmoothly()
        {
            var model=new RecoveryHaptics();var frame=Frame();frame.Attached=true;model.Step(frame,.016f,1);frame.Tension=.9f;
            float initial=model.Step(frame,.016f,1).x;float loaded=Settle(model,frame).x;Assert.That(initial,Is.LessThan(.01f));Assert.That(loaded,Is.GreaterThan(.1f));
            frame.Tension=0;Assert.That(model.Step(frame,.016f,1).x,Is.LessThan(loaded));Assert.That(Settle(model,frame),Is.EqualTo(Vector2.zero));
        }
        [Test] public void MotorHoldingPullingAndStallingAreDistinct()
        {
            var held=Frame();held.Attached=true;var motor=held;motor.Winch=new WinchFrame{State=WinchState.Pulling,Load=.7f};
            Assert.That(Settle(new RecoveryHaptics(),held).sqrMagnitude,Is.Zero);Assert.That(Settle(new RecoveryHaptics(),motor).sqrMagnitude,Is.GreaterThan(0));
            motor.Winch.State=WinchState.Stalled;var model=new RecoveryHaptics();float min=1,max=0;for(int i=0;i<120;i++){float h=model.Step(motor,1f/60,1).y;min=Mathf.Min(min,h);max=Mathf.Max(max,h);}Assert.That(max-min,Is.GreaterThan(.1f));
        }
        [Test] public void AttachDetachAndSnapHaveDifferentShortSignals()
        {
            var model=new RecoveryHaptics();var frame=Frame();model.Step(frame,.016f,1);frame.Events.Attached++;frame.Attached=true;
            var attach=model.Step(frame,.016f,1);Assert.That(model.Pulse,Is.EqualTo(RecoveryHapticPulse.Connect));Assert.That(attach.y,Is.GreaterThan(attach.x));
            Settle(model,frame);frame.Events.Detached++;frame.Attached=false;var detach=model.Step(frame,.016f,1);Assert.That(detach.magnitude,Is.LessThan(attach.magnitude));
            Settle(model,frame);frame.Events.Broken++;var snap=model.Step(frame,.016f,1);Assert.That(model.Pulse,Is.EqualTo(RecoveryHapticPulse.Snap));Assert.That(snap.magnitude,Is.GreaterThan(attach.magnitude));Assert.That(Settle(model,frame),Is.EqualTo(Vector2.zero));
        }
        [Test] public void OldEventsAreNotPlayedOnLateJoinOrMissionChange()
        {
            var frame=Frame();frame.Events.Broken=12;frame.Events.Impact=9;frame.Events.Attached=7;
            var model=new RecoveryHaptics();Assert.That(model.Step(frame,.016f,1),Is.EqualTo(Vector2.zero));
            frame.Run="new";frame.Events.Broken=0;Assert.That(model.Step(frame,.016f,1),Is.EqualTo(Vector2.zero));
        }
        [Test] public void PauseDisabledVibrationAndDeviceChangeDiscardPendingPulses()
        {
            var model=new RecoveryHaptics();var frame=Frame();model.Step(frame,.016f,1);frame.Events.Broken++;Assert.That(model.Step(frame,.016f,1).x,Is.GreaterThan(0));
            frame.Active=false;Assert.That(model.Step(frame,.016f,1),Is.EqualTo(Vector2.zero));frame.Events.Broken++;model.Step(frame,.016f,1);frame.Active=true;Assert.That(model.Step(frame,.016f,1),Is.EqualTo(Vector2.zero));
            frame.Events.Broken++;model.Step(frame,.016f,1);Assert.That(model.Step(frame,.016f,0),Is.EqualTo(Vector2.zero));model.Reset();Assert.That(model.Step(frame,.016f,1),Is.EqualTo(Vector2.zero));
        }
        [Test] public void LocalSlotDoesNotFeelItsTeammatesCableBreak()
        {
            var first=Frame();var second=Frame();second.Slot=1;var a=new RecoveryHaptics();var b=new RecoveryHaptics();a.Step(first,.016f,1);b.Step(second,.016f,1);
            first.Events.Broken++;Assert.That(a.Step(first,.016f,1).x,Is.GreaterThan(0));Assert.That(b.Step(second,.016f,1),Is.EqualTo(Vector2.zero));
            first.Slot=1;Assert.That(a.Step(first,.016f,1),Is.EqualTo(Vector2.zero));
        }
        [Test] public void ResettingVehicleDoesNotPlayTeleportImpacts()
        {
            var model=new RecoveryHaptics();var frame=Frame();model.Step(frame,.016f,1);frame.Resets++;frame.Events.Impact++;frame.Events.ImpactStrength=1;
            Assert.That(model.Step(frame,.016f,1),Is.EqualTo(Vector2.zero));
        }
        [Test] public void CableBreakSurvivesAMissedDisconnectedSnapshotAndBeatsCollision()
        {
            var model=new RecoveryHaptics();var frame=Frame();frame.Attached=true;model.Step(frame,.016f,1);
            frame.Events.Broken++;frame.Events.Attached++;frame.Events.Impact++;frame.Events.ImpactStrength=1;
            model.Step(frame,.016f,1);Assert.That(model.Pulse,Is.EqualTo(RecoveryHapticPulse.Snap));frame.Events.Impact++;model.Step(frame,.016f,1);Assert.That(model.Pulse,Is.EqualTo(RecoveryHapticPulse.Snap));
        }
        [Test] public void ExtractionSuccessIsFeltOnlyByAnAttachedDriverAndOnlyOnce()
        {
            var model=new RecoveryHaptics();var frame=Frame();frame.Attached=true;model.Step(frame,.016f,1);frame.Recovering=false;frame.Delivered=true;model.Step(frame,.016f,1);Assert.That(model.Pulse,Is.EqualTo(RecoveryHapticPulse.Release));Settle(model,frame);Assert.That(model.Pulse,Is.EqualTo(RecoveryHapticPulse.None));
            model=new RecoveryHaptics();frame=Frame();model.Step(frame,.016f,1);frame.Recovering=false;frame.Delivered=true;Assert.That(model.Step(frame,.016f,1),Is.EqualTo(Vector2.zero));
        }
        [Test] public void IntensityIsLinearAndEverySignalStaysWithinTheChillCeiling()
        {
            var frame=Frame();frame.Attached=true;frame.Tension=1;frame.Winch.State=WinchState.Stalled;
            var full=new RecoveryHaptics();var half=new RecoveryHaptics();for(int i=0;i<180;i++)
            {var a=full.Step(frame,1f/60,1);var b=half.Step(frame,1f/60,.5f);Assert.That(b.x,Is.EqualTo(a.x*.5f).Within(.00001f));Assert.That(a.x,Is.InRange(0,RecoveryHaptics.MaximumLow));Assert.That(a.y,Is.InRange(0,RecoveryHaptics.MaximumHigh));}
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1)]
        public void InvalidIntensityCannotReachAHardwareMotor(float intensity)=>Assert.That(new RecoveryHaptics().Step(Frame(),.016f,intensity),Is.EqualTo(Vector2.zero));
        [TestCase(2f,10f,0f)][TestCase(10f,.1f,0f)][TestCase(10f,3f,1f)]
        public void SmallRoadContactsAreFiltered(float speed,float change,float expected)=>Assert.That(RecoveryHaptics.ImpactStrength(speed,change),Is.EqualTo(expected));
        [Test] public void SnapshotOrderingRejectsDuplicatesAndOlderPacketsAcrossWrap()
        {Assert.That(RecoveryHaptics.Newer(8,7),Is.True);Assert.That(RecoveryHaptics.Newer(7,7),Is.False);Assert.That(RecoveryHaptics.Newer(6,7),Is.False);Assert.That(RecoveryHaptics.Newer(0,uint.MaxValue),Is.True);Assert.That(RecoveryHaptics.Newer(uint.MaxValue,0),Is.False);}
        [Test] public void VibrationIntensityMigratesAndSurvivesPreferenceCopies()
        {
            Assert.That(RecoveryProfile.Parse("{\"version\":4}").vibrationIntensity,Is.EqualTo(.7f));var profile=new RecoveryProfile{vibrationIntensity=.33f,vibration=false};var copy=RecoveryMenuSettings.WithoutProgress(profile);
            Assert.That(copy.vibrationIntensity,Is.EqualTo(.33f));Assert.That(copy.vibration,Is.False);Assert.That(RecoveryProfile.Parse(JsonUtility.ToJson(copy)).vibrationIntensity,Is.EqualTo(.33f));
        }
        public class RumbleTestPad:Gamepad
        {
            public int Commands,Stops;public Vector2 Motors;
            public override void SetMotorSpeeds(float low,float high){Commands++;Motors=new Vector2(low,high);}
            public override void ResetHaptics(){Stops++;Motors=Vector2.zero;}
        }
        [Test] public void DisablingInputOnAConnectedPadAlsoStopsItsMotors()
        {
            RumbleTestPad pad=null;var output=new RecoveryHapticOutput();InputSystem.RegisterLayout<RumbleTestPad>("TowHapticDisabled");
            try
            {
                pad=(RumbleTestPad)InputSystem.AddDevice("TowHapticDisabled");output.Write(pad,new Vector2(.2f,.1f),0);InputSystem.DisableDevice(pad);output.Write(pad,new Vector2(.2f,.1f),.05);
                Assert.That(pad.Motors,Is.EqualTo(Vector2.zero));Assert.That(pad.Stops,Is.EqualTo(1));
            }
            finally{output.Clear();if(pad!=null)InputSystem.RemoveDevice(pad);InputSystem.RemoveLayout("TowHapticDisabled");}
        }
        [Test] public void ResettingOutputResendsAnUnchangedLoadOnTheSameReconnectedDevice()
        {
            RumbleTestPad pad=null;var output=new RecoveryHapticOutput();InputSystem.RegisterLayout<RumbleTestPad>("TowHapticReconnect");
            try
            {
                pad=(RumbleTestPad)InputSystem.AddDevice("TowHapticReconnect");output.Write(pad,new Vector2(.2f,.1f),0);output.Clear();output.Write(pad,new Vector2(.2f,.1f),.001);
                Assert.That(pad.Commands,Is.EqualTo(2));Assert.That(pad.Stops,Is.EqualTo(1));Assert.That(pad.Motors.x,Is.EqualTo(.2f));
            }
            finally{output.Clear();if(pad!=null)InputSystem.RemoveDevice(pad);InputSystem.RemoveLayout("TowHapticReconnect");}
        }
        [Test] public void HardwareAdapterStopsOldDeviceAndBypassesThrottleForSilence()
        {
            RumbleTestPad a=null,b=null;InputSystem.RegisterLayout<RumbleTestPad>("TowHapticTest");var output=new RecoveryHapticOutput();
            try
            {
                a=(RumbleTestPad)InputSystem.AddDevice("TowHapticTest");b=(RumbleTestPad)InputSystem.AddDevice("TowHapticTest");
                output.Write(a,new Vector2(.1f,.05f),0);output.Write(a,new Vector2(.2f,.1f),.001);Assert.That(a.Commands,Is.EqualTo(1));
                output.Write(b,new Vector2(.15f,.1f),.002);Assert.That(a.Motors,Is.EqualTo(Vector2.zero));Assert.That(a.Stops,Is.EqualTo(1));
                output.Write(b,Vector2.zero,.003);Assert.That(b.Motors,Is.EqualTo(Vector2.zero));Assert.That(b.Stops,Is.EqualTo(1));output.Clear();Assert.That(b.Stops,Is.EqualTo(1));
            }
            finally{output.Clear();if(a!=null)InputSystem.RemoveDevice(a);if(b!=null)InputSystem.RemoveDevice(b);InputSystem.RemoveLayout("TowHapticTest");}
        }
    }
}
