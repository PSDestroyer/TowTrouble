using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryProfileTests
    {
        [Test] public void ProgressionStartsWithOnlyTutorialUnlocked()
        {var p=new RecoveryProfile();Assert.That(p.Unlocked(0),Is.True);Assert.That(p.Unlocked(1),Is.False);Assert.That(p.Unlocked(2),Is.False);}
        [Test] public void CompletingTutorialUnlocksDitchWithoutRequiringDuo()
        {var p=new RecoveryProfile{completedMask=1};Assert.That(p.Unlocked(1),Is.True);Assert.That(p.Unlocked(2),Is.False);}
        [Test] public void ProfileSurvivesSerializationWithBindingsAndAccessibility()
        {var p=new RecoveryProfile{completedMask=3,medalMask=1,vehicle=3,hookKey="H",reducedMotion=true,textScale=1.2f};var copy=JsonUtility.FromJson<RecoveryProfile>(JsonUtility.ToJson(p));Assert.That(copy.Completed(1),Is.True);Assert.That(copy.Clean(0),Is.True);Assert.That(copy.Clean(1),Is.False);Assert.That(copy.vehicle,Is.EqualTo(3));Assert.That(copy.hookKey,Is.EqualTo("H"));Assert.That(copy.reducedMotion,Is.True);}
        [Test] public void InvalidSavedKeyCannotIndexOutsideTheKeyboard()
        {var p=new RecoveryProfile();Assert.That(p.ReadKey("-1",UnityEngine.InputSystem.Key.E),Is.EqualTo(UnityEngine.InputSystem.Key.E));Assert.That(p.ReadKey("None",UnityEngine.InputSystem.Key.E),Is.EqualTo(UnityEngine.InputSystem.Key.E));}
        [Test] public void FirstThreeContractsHaveDifferentConditionsAndDeliveryLocations()
        {Assert.That(RecoveryContract.All.Length,Is.EqualTo(3));Assert.That(RecoveryContract.Get(0).MudDrag,Is.Zero);Assert.That(RecoveryContract.Get(1).Target.x,Is.LessThan(-3));Assert.That(RecoveryContract.Get(2).MudDrag,Is.GreaterThan(RecoveryContract.Get(1).MudDrag));Assert.That(RecoveryContract.Get(0).Delivery,Is.Not.EqualTo(RecoveryContract.Get(2).Delivery));}
    }
}
