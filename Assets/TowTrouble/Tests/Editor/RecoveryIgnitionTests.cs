using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryIgnitionTests
    {
        [Test] public void EdgesDoNotReplayOnPauseMuteOrModelRemoval()
        {
            Assert.That(RecoveryIgnitionPolicy.Edge(false,true,false,true),Is.EqualTo(1));
            Assert.That(RecoveryIgnitionPolicy.Edge(true,true,false,true),Is.Zero);
            Assert.That(RecoveryIgnitionPolicy.Edge(true,false,false,true),Is.EqualTo(-1));
            Assert.That(RecoveryIgnitionPolicy.Edge(false,false,false,true),Is.Zero);
            Assert.That(RecoveryIgnitionPolicy.Edge(true,true,true,true),Is.EqualTo(1));
            Assert.That(RecoveryIgnitionPolicy.Edge(true,false,true,true),Is.Zero);
            foreach(bool before in new[]{false,true})foreach(bool after in new[]{false,true})
                Assert.That(RecoveryIgnitionPolicy.Edge(before,after,false,false),Is.Zero);
            Assert.That(RecoveryIgnitionPolicy.Edge(true,true,false,true),Is.Zero,"Resume/unmute does not restart an already running motor");
        }
        [Test] public void AllModelsHaveDistinctShortIgnitionClipsWithHeadroom()
        {
            var bank=UnityEditor.AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>("Assets/TowTrouble/Art/Audio/Resources/RecoveryAudioBank.asset");
            var unique=new System.Collections.Generic.HashSet<AudioClip>();long bytes=0;
            for(int model=0;model<4;model++)
            {
                var profile=bank.IgnitionFor(model);Assert.That(profile,Is.Not.Null);
                Assert.That(profile.StartGain,Is.InRange(.1f,.2f));Assert.That(profile.StopGain,Is.InRange(.1f,.2f));
                foreach(var clip in new[]{profile.Start,profile.Stop})
                {
                    Assert.That(clip,Is.Not.Null);Assert.That(unique.Add(clip),Is.True);
                    Assert.That(clip.length,Is.InRange(.6f,2f));Assert.That(clip.channels,Is.EqualTo(1));
                    clip.LoadAudioData();var values=new float[clip.samples];Assert.That(clip.GetData(values,0),Is.True);
                    foreach(float value in values)Assert.That(Mathf.Abs(value),Is.LessThan(.8f));bytes+=values.Length*4;
                }
            }
            Assert.That(bytes,Is.LessThan(1000000));Assert.That(bank.IgnitionFor(999),Is.Null);
        }
    }
}
