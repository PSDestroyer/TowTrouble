using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryAudioTests
    {
        [Test] public void AudioPreferencesMigrateWithUsefulDefaultsAndPreserveMute()
        {
            var old=JsonUtility.FromJson<RecoveryProfile>("{\"version\":4,\"master\":0,\"effects\":0.3,\"ui\":0.2}");
            Assert.That(old.music,Is.EqualTo(.45f));Assert.That(old.ambience,Is.EqualTo(.7f));Assert.That(old.master,Is.Zero);
            old.music=0;old.ambience=.25f;var copy=new RecoveryProfile();RecoveryMenuSettings.CopyPreferences(old,copy);
            Assert.That(copy.music,Is.Zero);Assert.That(copy.ambience,Is.EqualTo(.25f));
            var reloaded=JsonUtility.FromJson<RecoveryProfile>(JsonUtility.ToJson(copy));Assert.That(reloaded.music,Is.Zero);Assert.That(reloaded.ambience,Is.EqualTo(.25f));
        }
        [TestCase(WinchState.Disconnected,false)][TestCase(WinchState.Holding,false)][TestCase(WinchState.Minimum,false)][TestCase(WinchState.Maximum,false)]
        [TestCase(WinchState.TakingUp,true)][TestCase(WinchState.Pulling,true)][TestCase(WinchState.PayingOut,true)][TestCase(WinchState.Stalled,true)]
        public void OnlyMotorStatesProduceContinuousWinchAudio(WinchState state,bool active)=>Assert.That(RecoveryAudioPolicy.MotorActive(state),Is.EqualTo(active));
        [Test] public void EnginePresentationStaysBoundedAcrossBodiesAndLoads()
        {
            foreach(float mass in new[]{950f,1100,1840,2600})foreach(float speed in new[]{0f,2,7,13,22,40})foreach(float throttle in new[]{-1f,0,1})
            {float pitch=RecoveryAudioPolicy.EnginePitch(speed,throttle,1,mass);Assert.That(pitch,Is.InRange(.65f,1.65f));}
            Assert.That(RecoveryAudioPolicy.EnginePitch(0,0,0,2200),Is.LessThan(RecoveryAudioPolicy.EnginePitch(0,0,0,950)));
        }
        [Test] public void CosmeticCounterHandlesWrapWithoutReplayingOldEvents()
        {Assert.That(RecoveryAudioPolicy.Changed(0,uint.MaxValue),Is.True);Assert.That(RecoveryAudioPolicy.Changed(2,3),Is.False);Assert.That(RecoveryAudioPolicy.Changed(3,3),Is.False);}
        [Test] public void CriticalRecoveryFeedbackDucksMusicGently()
        {Assert.That(RecoveryAudioPolicy.MusicDuck(false,0,false),Is.EqualTo(1));Assert.That(RecoveryAudioPolicy.MusicDuck(false,1,false),Is.LessThan(.4f));Assert.That(RecoveryAudioPolicy.MusicDuck(false,0,true),Is.LessThan(.6f));Assert.That(RecoveryAudioPolicy.MusicDuck(true,0,false),Is.InRange(.5f,.8f));}
        [Test] public void InvalidAudioGainsCannotReachNativeAudioSources()
        {foreach(float f in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f})Assert.That(RecoveryAudioPolicy.Gain(f),Is.Zero);Assert.That(RecoveryAudioPolicy.Gain(4),Is.EqualTo(1));}
    }
}
