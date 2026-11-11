using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryScoutMuscleTests
    {
        [Test] public void CruiseHasNoPeriodicGearPitchResetsAndReverseUsesSameDemand()
        {
            float last=RecoveryScoutEngineMix.Drive(0,.5f);
            for(float speed=0;speed<40;speed+=.05f)
            {
                float value=RecoveryScoutEngineMix.Drive(speed,.5f);
                Assert.That(value,Is.GreaterThanOrEqualTo(last));
                Assert.That(value-last,Is.LessThan(.01f));last=value;
                Assert.That(RecoveryScoutEngineMix.Drive(speed,-.5f),Is.EqualTo(value));
            }
        }
        [Test] public void IdleCruiseAndTowingStayWithinMixBudget()
        {
            Assert.That(RecoveryScoutEngineMix.Weights(0,0),Is.EqualTo(new Vector3(.27f,0,0)));
            var cruise=RecoveryScoutEngineMix.Weights(.65f,0);
            Assert.That(cruise.z,Is.Zero);Assert.That(cruise.y,Is.GreaterThan(cruise.x));
            Assert.That(RecoveryScoutEngineMix.Weights(.65f,1).y,Is.GreaterThan(cruise.y));
            for(float drive=0;drive<=1;drive+=.01f)
            {
                var w=RecoveryScoutEngineMix.Weights(drive,1);
                Assert.That(w.x+w.y+w.z,Is.LessThan(.44f));Assert.That(w.z,Is.LessThan(.071f));
            }
            Assert.That(RecoveryScoutEngineMix.Smooth(0,1,1f/30,.4f),Is.InRange(0,.09f));
            float a=0,b=0;for(int i=0;i<30;i++)a=RecoveryScoutEngineMix.Smooth(a,1,1f/30,.4f);
            for(int i=0;i<60;i++)b=RecoveryScoutEngineMix.Smooth(b,1,1f/60,.4f);
            Assert.That(a,Is.EqualTo(b).Within(.00001f));
        }
        [Test] public void DefaultMixReducesScoutWithoutChangingPlayerPreferences()
        {
            var bank=UnityEditor.AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>("Assets/TowTrouble/Art/Audio/Resources/RecoveryAudioBank.asset");
            float previous=.85f*RecoveryAudioPolicy.EngineBoost(6);
            Assert.That(20*Mathf.Log10(bank.ScoutEngineGain/previous),Is.InRange(-8.1f,-7.8f));
            Assert.That(bank.Find(RecoverySound.Accept).Gain,Is.EqualTo(.24f));
            Assert.That(bank.Find(RecoverySound.Complete).Gain,Is.EqualTo(.32f));
            Assert.That(bank.Find(RecoverySound.Snap).Gain,Is.EqualTo(.58f));
            var profile=new RecoveryProfile();Assert.That(profile.master,Is.EqualTo(.8f));
            Assert.That(profile.effects,Is.EqualTo(.8f));Assert.That(profile.music,Is.EqualTo(.45f));
        }
        [Test] public void FleetProfilesHaveDistinctCompleteBoundedClipsAndSafeFallback()
        {
            var bank=UnityEditor.AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>("Assets/TowTrouble/Art/Audio/Resources/RecoveryAudioBank.asset");
            var clips=new System.Collections.Generic.HashSet<AudioClip>();long bytes=0;
            for(int model=1;model<=3;model++)
            {
                var profile=bank.EngineFor(model);Assert.That(profile,Is.Not.Null);
                Assert.That(profile.Gain,Is.InRange(.5f,.8f));
                for(int layer=0;layer<3;layer++)
                {
                    var clip=profile.Clip(layer);Assert.That(clips.Add(clip),Is.True);
                    Assert.That(clip.channels,Is.EqualTo(1));clip.LoadAudioData();
                    var samples=new float[clip.samples];Assert.That(clip.GetData(samples,0),Is.True);
                    foreach(float sample in samples)Assert.That(Mathf.Abs(sample),Is.LessThan(.9f));
                    bytes+=clip.samples*4L;
                }
            }
            Assert.That(bytes,Is.LessThan(1100000));Assert.That(bank.EngineFor(0),Is.Null);Assert.That(bank.EngineFor(999),Is.Null);
            var temporary=ScriptableObject.CreateInstance<RecoveryAudioBank>();
            try{temporary.VehicleEngines=new[]{new RecoveryVehicleEngineSounds{Model=2}};Assert.That(temporary.EngineFor(2),Is.Null);}
            finally{Object.DestroyImmediate(temporary);}
        }
        [Test] public void ScoutAssetsAreWiredSmallAndRetainHeadroom()
        {
            var bank=UnityEditor.AssetDatabase.LoadAssetAtPath<RecoveryAudioBank>("Assets/TowTrouble/Art/Audio/Resources/RecoveryAudioBank.asset");
            Assert.That(bank.HasScoutMuscle,Is.True);
            long bytes=0;
            foreach(var clip in new[]{bank.ScoutMuscleIdle,bank.ScoutMuscleLow,bank.ScoutMuscleHigh})
            {
                Assert.That(clip.channels,Is.EqualTo(1));clip.LoadAudioData();
                var samples=new float[clip.samples];Assert.That(clip.GetData(samples,0),Is.True);
                foreach(float sample in samples)Assert.That(Mathf.Abs(sample),Is.LessThan(.9f));
                bytes+=clip.samples*4L;
            }
            Assert.That(bytes,Is.LessThan(400000));
            Assert.That(bank.EngineIdle,Is.Not.EqualTo(bank.ScoutMuscleIdle));
        }
    }
}
