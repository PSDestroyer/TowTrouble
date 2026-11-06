using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryWinchTests
    {
        private static WinchFrame Wind(float length,float distance,float speed=0,float force=8000,float rate=1,float previous=0,float step=.02f)
        {return RecoveryWinch.Step(length,distance,speed,true,false,force,rate,previous,step);}

        [Test] public void TakesUpSlackWithoutPullingTheTarget()
        {
            var f=Wind(12,10);
            Assert.That(f.State,Is.EqualTo(WinchState.TakingUp));
            Assert.That(f.Speed,Is.GreaterThan(0).And.LessThan(1));
            Assert.That(RecoveryRules.CableDemand(10,f.Length,0),Is.Zero);
        }
        [Test] public void SlowsUnderLoadComparedWithAnUnloadedDrum()
        {
            var free=Wind(12,10,previous:1);
            var loaded=Wind(10-6000f/RecoveryRules.CableStiffness,10,previous:1);
            Assert.That(loaded.Speed,Is.GreaterThan(0).And.LessThan(free.Speed*.5f));
            Assert.That(loaded.State,Is.EqualTo(WinchState.Pulling));
        }
        [Test] public void ImmovableTargetStallsStockInsteadOfBreakingStarterCable()
        {
            foreach(float step in new[]{.01f,.02f,.04f})
            {
                var f=new WinchFrame{Length=9.7f};float stress=0;
                for(int i=0;i<Mathf.CeilToInt(10/step);i++)
                {
                    f=Wind(f.Length,10,previous:f.Speed,step:step);
                    float demand=RecoveryRules.CableDemand(10,f.Length,0);
                    Assert.That(demand,Is.LessThanOrEqualTo(8000.1f));
                    stress=RecoveryRules.CableStressAfter(stress,demand,11000,step);
                }
                Assert.That(f.State,Is.EqualTo(WinchState.Stalled));Assert.That(f.Speed,Is.Zero);
                Assert.That(f.Load*8000,Is.GreaterThan(5600));Assert.That(stress,Is.Zero);
            }
        }
        [Test] public void AReleasedTargetRestartsTheMotorGradually()
        {
            var stalled=Wind(10-8000f/RecoveryRules.CableStiffness,10);
            Assert.That(stalled.State,Is.EqualTo(WinchState.Stalled));
            var released=Wind(stalled.Length,9,previous:stalled.Speed);
            Assert.That(released.Speed,Is.InRange(.07f,.09f));
            Assert.That(released.State,Is.EqualTo(WinchState.TakingUp));
        }
        [Test] public void ReleasingTheButtonHoldsLengthAndDoesNotEraseTension()
        {
            var f=RecoveryWinch.Step(9.5f,10,0,false,false,8000,1,1,.02f);
            Assert.That(f.Length,Is.EqualTo(9.5f));Assert.That(f.Speed,Is.Zero);
            Assert.That(f.State,Is.EqualTo(WinchState.Holding));Assert.That(f.Load,Is.GreaterThan(0));
        }
        [Test] public void PayingOutRelievesLoadAndRespectsMaximumLength()
        {
            var f=RecoveryWinch.Step(9.5f,10,0,false,true,8000,1,0,.1f);
            Assert.That(f.Length,Is.GreaterThan(9.5f));Assert.That(f.Speed,Is.LessThan(0));
            Assert.That(f.Load*8000,Is.LessThan(RecoveryRules.CableDemand(10,9.5f,0)));
            Assert.That(f.State,Is.EqualTo(WinchState.PayingOut));
            f=RecoveryWinch.Step(20,10,0,false,true,8000,1,0,.02f);
            Assert.That(f.Length,Is.EqualTo(20));Assert.That(f.State,Is.EqualTo(WinchState.Maximum));
        }
        [Test] public void MinimumLengthStopsTheMotorAndBothCommandsCancel()
        {
            var f=Wind(3.5f,3);
            Assert.That(f.Length,Is.EqualTo(3.5f));Assert.That(f.State,Is.EqualTo(WinchState.Minimum));
            f=RecoveryWinch.Step(10,10,0,true,true,8000,1,1,.02f);
            Assert.That(f.Length,Is.EqualTo(10));Assert.That(f.State,Is.EqualTo(WinchState.Holding));
        }
        [Test] public void ExternalShockStillBreaksBeforeAnyMotorForceCap()
        {
            var f=Wind(9.5f,10,20,previous:1);
            Assert.That(f.Length,Is.EqualTo(9.5f));
            float demand=RecoveryRules.CableDemand(10,f.Length,20);
            Assert.That(demand,Is.GreaterThan(8000));
            Assert.That(RecoveryRules.CableStressAfter(0,demand,11000,.02f),Is.EqualTo(1));
        }
        [Test] public void ExistingOverloadIsNeverReleasedAutomatically()
        {
            var f=Wind(8,10);
            Assert.That(f.Length,Is.EqualTo(8));Assert.That(f.Load,Is.GreaterThan(1));
            Assert.That(RecoveryRules.CableStressAfter(0,f.Load*8000,11000,.02f),Is.EqualTo(1));
        }
        [Test] public void ClosingSpeedCannotHideExcessiveMotorStretch()
        {
            var f=new WinchFrame{Length=9.7f};
            for(int i=0;i<500;i++)f=Wind(f.Length,10,-10,previous:f.Speed);
            Assert.That(RecoveryRules.CableDemand(10,f.Length,0),Is.LessThanOrEqualTo(8000.1f));
        }
        [Test] public void StrongerWinchRequiresAMatchingCable()
        {
            var f=new WinchFrame{Length=9.7f};float weak=0,strong=0;
            for(int i=0;i<500;i++)
            {
                f=Wind(f.Length,10,force:14000,rate:1.3f,previous:f.Speed);
                float demand=RecoveryRules.CableDemand(10,f.Length,0);
                weak=RecoveryRules.CableStressAfter(weak,demand,11000,.02f);
                strong=RecoveryRules.CableStressAfter(strong,demand,14000,.02f);
                if(weak>=1)break;
            }
            Assert.That(weak,Is.EqualTo(1));Assert.That(strong,Is.Zero);
        }
        [Test] public void TransitionFromSlackCannotWindPastTheForceBudget()
        {
            foreach(float step in new[]{.01f,.02f,.04f})
            {
                var f=Wind(10.001f,10,4,previous:1,step:step);
                Assert.That(RecoveryRules.CableDemand(10,f.Length,4),Is.LessThanOrEqualTo(8000.1f));
            }
        }
        [Test] public void ZeroTimeDoesNotWindOrPayOut()
        {
            var f=RecoveryWinch.Step(10,10,0,true,false,8000,1,1,0);
            Assert.That(f.Length,Is.EqualTo(10));Assert.That(f.Speed,Is.Zero);
        }
        [Test] public void BrakeSupportUsesTheExistingSharedTireGripLimit()
        {
            var perWheel=RecoveryTraction.CableBrake(Vector3.back*8000,Vector3.forward,Vector3.right,4);
            Assert.That(perWheel.x*4,Is.EqualTo(8000));
            var dry=RecoveryTraction.LimitForces(perWheel,4000,4000);
            var mud=RecoveryTraction.LimitForces(perWheel,1000,1000);
            Assert.That(dry.x*4,Is.EqualTo(8000));Assert.That(mud.x*4,Is.EqualTo(4000));
        }
        [Test] public void WinchCatalogKeepsTwoStableTiersAndExistingOwnership()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
            var stock=catalog.Find("stock_Winch");var improved=catalog.Find("winch_2");
            Assert.That(stock.WinchForce,Is.EqualTo(8000));Assert.That(stock.WinchSpeed,Is.EqualTo(1));
            Assert.That(improved.WinchForce,Is.EqualTo(14000));Assert.That(improved.WinchSpeed,Is.EqualTo(1.3f));
            Assert.That(improved.Price,Is.EqualTo(1600));
            var profile=RecoveryProfile.Parse("");profile.Build(0).Normalize(catalog,profile.Owns);
            Assert.That(GaragePerformance.Calculate(profile.Build(0),catalog).WinchForce,Is.EqualTo(8000));
            profile.ownedParts.Add("winch_2");profile.Build(0).Set(improved,catalog);
            var copy=RecoveryProfile.Parse(JsonUtility.ToJson(profile));copy.Build(0).Normalize(catalog,copy.Owns);
            Assert.That(copy.Owns("winch_2"),Is.True);
            Assert.That(GaragePerformance.Calculate(copy.Build(0),catalog).WinchForce,Is.EqualTo(14000));
        }
        [Test] public void UnownedWinchDoesNotGrantFreePerformance()
        {
            var c=AssetDatabase.LoadAssetAtPath<RecoveryGarageCatalog>("Assets/TowTrouble/Garage/GarageCatalog.asset");
            var p=RecoveryProfile.Parse("");p.Build(0).Set(c.Find("winch_2"),c);p.Build(0).Normalize(c,p.Owns);
            Assert.That(GaragePerformance.Calculate(p.Build(0),c).WinchForce,Is.EqualTo(8000));
        }
        [Test] public void HudDistinguishesStalledMotorFromCableFailureInBothLanguages()
        {
            Assert.That(RecoveryHud.WinchLabel(WinchState.Stalled,false),Is.EqualTo("Troliu la limită"));
            Assert.That(RecoveryHud.WinchLabel(WinchState.Stalled,true),Is.EqualTo("Winch at limit"));
            Assert.That(RecoveryHud.WinchLabel(WinchState.Pulling,false),Is.Not.EqualTo(RecoveryHud.WinchLabel(WinchState.TakingUp,false)));
        }
    }
}
