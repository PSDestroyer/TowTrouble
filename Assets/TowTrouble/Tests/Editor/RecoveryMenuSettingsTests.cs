using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
namespace TowTrouble.Tests
{
    public class RecoveryMenuSettingsTests
    {
        [Test] public void ProgressResetKeepsPreferencesButRemovesEveryCareerReceipt()
        {
            var source=new RecoveryProfile{credits=9000,completedMask=127,ownedVehicles=15,starterChosen=true,starterVehicle=3,vehicle=3,textScale=1.3f,controllerFamily=3,displayWidth=1920,displayHeight=1080,displayMode=1,hookKey="H",name="Driver"};
            source.EnsureGarage();source.ownedParts.Add("engine_3");source.paidRuns.Add("paid");source.builds[3].Items=new[]{"engine_3"};
            var reset=RecoveryMenuSettings.WithoutProgress(source);
            Assert.That(reset.credits,Is.Zero);Assert.That(reset.completedMask,Is.Zero);Assert.That(reset.starterChosen,Is.False);Assert.That(reset.ownedVehicles,Is.Zero);
            Assert.That(reset.ownedParts,Is.Empty);Assert.That(reset.paidRuns,Is.Empty);Assert.That(reset.presets,Is.Empty);
            Assert.That(reset.textScale,Is.EqualTo(1.3f));Assert.That(reset.controllerFamily,Is.EqualTo(3));Assert.That(reset.hookKey,Is.EqualTo("H"));Assert.That(reset.displayWidth,Is.EqualTo(1920));
            Assert.That(source.credits,Is.EqualTo(9000));Assert.That(source.ownedParts,Has.Count.EqualTo(1));
        }
        [TestCase(Key.Escape)][TestCase(Key.Enter)][TestCase(Key.NumpadEnter)][TestCase(Key.C)][TestCase(Key.UpArrow)][TestCase(Key.None)]
        public void ReservedControlsAreRejected(Key key)=>Assert.That(RecoveryMenuSettings.BindingError(new RecoveryProfile(),"hookKey",key),Is.Not.Null);
        [Test] public void DuplicateBindingExplainsTheExistingAction()=>Assert.That(RecoveryMenuSettings.BindingError(new RecoveryProfile(),"hookKey",Key.W),Does.Contain("accelerate"));
        [Test] public void CurrentAndUnusedBindingsAreAccepted(){Assert.That(RecoveryMenuSettings.BindingError(new RecoveryProfile(),"hookKey",Key.F),Is.Null);Assert.That(RecoveryMenuSettings.BindingError(new RecoveryProfile(),"hookKey",Key.H),Is.Null);}
        [Test] public void RefreshRatesDoNotDuplicateOrTrapResolutionSelection()
        {var values=new[]{new Resolution{width=1920,height=1080},new Resolution{width=1280,height=720},new Resolution{width=1920,height=1080}};var result=RecoveryMenuSettings.Resolutions(values,1280,720);Assert.That(result,Has.Length.EqualTo(2));Assert.That(result[0],Is.EqualTo(new Vector2Int(1280,720)));Assert.That(result[1],Is.EqualTo(new Vector2Int(1920,1080)));}
        [Test] public void FirstRunStillGrantsExactlyOneStarter()
        {for(int model=0;model<4;model++){var p=new RecoveryProfile();Assert.That(RecoveryEconomy.ChooseStarter(p,model),Is.True);Assert.That(p.ownedVehicles,Is.EqualTo(1<<model));Assert.That(RecoveryEconomy.ChooseStarter(p,(model+1)%4),Is.False);}}
        [Test] public void TrainingProfileCannotPersist()
        {
            const string key="TowTrouble.Validation.MenuSettings";var previous=RecoveryProfile.ValidationStorageKey;
            try{RecoveryProfile.ValidationStorageKey=key;RecoveryProfile.Storage.SetString(key,"sentinel");var training=new RecoveryProfile{SuppressSaving=true,credits=999};training.Save();Assert.That(RecoveryProfile.Storage.GetString(key),Is.EqualTo("sentinel"));}
            finally{RecoveryProfile.Storage.Delete(key);RecoveryProfile.Storage.Delete(key+".previous");RecoveryProfile.ValidationStorageKey=previous;}
        }
    }
}
