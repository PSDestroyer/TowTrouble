using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TowTrouble.Tests
{
    public sealed class RecoveryMusicTests
    {
        [Test] public void PlaylistKeepsFamiliarOpeningThenPlaysEachTrackBeforeReusingTheBag()
        {
            var clips = Clips(6);
            try
            {
                var playlist = new RecoveryMusicPlaylist(clips, 127);
                AudioClip last = null;
                for (int cycle = 0; cycle < 8; cycle++)
                {
                    var heard = new HashSet<AudioClip>();
                    for (int i = 0; i < clips.Length; i++)
                    {
                        var next = playlist.Next(last);
                        if (cycle == 0 && i == 0) Assert.That(next, Is.SameAs(clips[0]));
                        Assert.That(next, Is.Not.SameAs(last)); Assert.That(heard.Add(next), Is.True); last = next;
                    }
                }
            }
            finally { Destroy(clips); }
        }
        [Test] public void PlaylistSkipsNullsDuplicatesAndTheThemeJustHeardInTheMenu()
        {
            var clips = Clips(3);
            try
            {
                var playlist = new RecoveryMusicPlaylist(new[] { clips[0], null, clips[0], clips[1], clips[2] }, 11);
                Assert.That(playlist.Next(clips[0]), Is.Not.SameAs(clips[0]));
                var single = new RecoveryMusicPlaylist(new[] { null, clips[0], clips[0] }, 1);
                Assert.That(single.Next(clips[0]), Is.SameAs(clips[0]));
                Assert.That(new RecoveryMusicPlaylist(null, 1).Next(null), Is.Null);
            }
            finally { Destroy(clips); }
        }
        [Test] public void MusicSelectionCannotConsumeGameplayRandomNumbers()
        {
            var clips = Clips(4);
            var state = UnityEngine.Random.state;
            try
            {
                var playlist = new RecoveryMusicPlaylist(clips, 37); AudioClip last = null;
                for (int i = 0; i < 32; i++) last = playlist.Next(last);
                Assert.That(UnityEngine.Random.state, Is.EqualTo(state));
            }
            finally { Destroy(clips); UnityEngine.Random.state = state; }
        }
        [Test] public void CrossfadeKeepsPowerAndDoesNotBoostAnInterruptedOutgoingDeck()
        {
            for (int i = 0; i <= 100; i++)
            {
                float t = i / 100f;
                float incoming = RecoveryAudioPolicy.MusicEnvelope(0, true, t);
                float outgoing = RecoveryAudioPolicy.MusicEnvelope(1, false, t);
                Assert.That(incoming, Is.InRange(0, 1)); Assert.That(outgoing, Is.InRange(0, 1));
                Assert.That(incoming * incoming + outgoing * outgoing, Is.EqualTo(1).Within(.0001f));
                Assert.That(RecoveryAudioPolicy.MusicEnvelope(.35f, false, t), Is.LessThanOrEqualTo(.35001f));
            }
        }
        [Test] public void OpenContractPanelCannotSuppressCriticalRecoveryMusicDucking()
        {
            Assert.That(RecoveryAudioPolicy.MusicDuck(true, .99f, false), Is.EqualTo(RecoveryAudioPolicy.MusicDuck(false, .99f, false)));
            Assert.That(RecoveryAudioPolicy.MusicDuck(true, 0, true), Is.LessThan(.65f));
        }
        [Test] public void SwitchColourCorrectionDoesNotAccumulateAndRestoresManualControlsAndOverrides()
        {
            bool previous = RecoveryPlatform.PreviewSwitchRendering;
            var grade = ScriptableObject.CreateInstance<ColorAdjustments>();
            grade.saturation.value = 23; grade.saturation.overrideState = true;
            grade.contrast.value = 15; grade.contrast.overrideState = false;
            grade.postExposure.value = .2f; grade.postExposure.overrideState = true;
            var correction = new RecoverySwitchColorGrade();
            try
            {
                RecoveryPlatform.PreviewSwitchRendering = true;
                for (int i = 0; i < 40; i++) correction.Apply(grade, 0, 0);
                Assert.That(grade.saturation.value, Is.EqualTo(31)); Assert.That(grade.contrast.value, Is.EqualTo(19));
                Assert.That(grade.postExposure.value, Is.EqualTo(.14f).Within(.0001f));
                correction.Apply(grade, 1, 0);
                Assert.That(grade.saturation.value, Is.EqualTo(27)); Assert.That(grade.contrast.value, Is.EqualTo(16));
                correction.Dispose(); correction.Dispose();
                Assert.That(grade.saturation.value, Is.EqualTo(23)); Assert.That(grade.contrast.value, Is.EqualTo(15));
                Assert.That(grade.postExposure.value, Is.EqualTo(.2f)); Assert.That(grade.contrast.overrideState, Is.False);
                Assert.That(grade.saturation.overrideState && grade.postExposure.overrideState, Is.True);
            }
            finally { correction.Dispose(); UnityEngine.Object.DestroyImmediate(grade); RecoveryPlatform.PreviewSwitchRendering = previous; }
        }
        [Test] public void PcColourControlsRemainExactlyAsAuthored()
        {
#if !UNITY_SWITCH
            bool previous = RecoveryPlatform.PreviewSwitchRendering;
            var grade = ScriptableObject.CreateInstance<ColorAdjustments>();
            grade.saturation.value = 23; grade.contrast.value = 15; grade.postExposure.value = .2f;
            string before = JsonUtility.ToJson(grade);
            try
            {
                RecoveryPlatform.PreviewSwitchRendering = false;
                using (var correction = new RecoverySwitchColorGrade()) correction.Apply(grade, 0, 0);
                Assert.That(JsonUtility.ToJson(grade), Is.EqualTo(before));
            }
            finally { UnityEngine.Object.DestroyImmediate(grade); RecoveryPlatform.PreviewSwitchRendering = previous; }
#endif
        }
        private static AudioClip[] Clips(int count)
        { var clips = new AudioClip[count]; for (int i = 0; i < count; i++) clips[i] = AudioClip.Create("Music test " + i, 100, 1, 1000, false); return clips; }
        private static void Destroy(AudioClip[] clips) { foreach (var clip in clips) UnityEngine.Object.DestroyImmediate(clip); }
    }
}
