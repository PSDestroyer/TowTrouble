using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryWheelDebrisTests
    {
        [Test] public void StationaryAirborneAndWoodDoNotThrowDebris()
        {
            foreach(RecoveryWheelDebrisKind kind in System.Enum.GetValues(typeof(RecoveryWheelDebrisKind)))
            {
                Assert.That(RecoveryWheelDebrisRules.Rate(kind,0,0,0,true),Is.Zero);
                Assert.That(RecoveryWheelDebrisRules.Rate(kind,20,1,50,false),Is.Zero);
            }
            Assert.That(RecoveryWheelDebrisRules.Rate(RecoveryWheelDebrisKind.Wood,20,1,50,true),Is.Zero);
            Assert.That(RecoveryWheelDebrisRules.Rate(RecoveryWheelDebrisKind.Stone,20,0,50,true),Is.Zero);
        }
        [Test] public void ActualWheelspinInLooseGroundThrowsMoreWithABoundedRate()
        {
            foreach(var kind in new[]{RecoveryWheelDebrisKind.Mud,RecoveryWheelDebrisKind.Sand,RecoveryWheelDebrisKind.Snow,RecoveryWheelDebrisKind.Dirt})
            {
                Assert.That(RecoveryWheelDebrisRules.Rate(kind,0,1,25,true),Is.GreaterThan(0));
                Assert.That(RecoveryWheelDebrisRules.Rate(kind,5,1,25,true),Is.GreaterThan(RecoveryWheelDebrisRules.Rate(kind,5,0,10,true)));
                Assert.That(RecoveryWheelDebrisRules.Rate(kind,100,1,1000,true),Is.InRange(1,32));
            }
            Assert.That(RecoveryWheelDebrisRules.Rate(RecoveryWheelDebrisKind.Mud,0,1,0,true),Is.Zero,"Load without movement or spin must not spray");
        }
        [Test] public void SurfaceColorsAndTrackMappingsRemainDistinct()
        {
            Assert.That(RecoveryWheelDebrisRules.FromTrack(RecoveryTrackKind.Snow),Is.EqualTo(RecoveryWheelDebrisKind.Snow));
            Assert.That(RecoveryWheelDebrisRules.FromTrack(RecoveryTrackKind.Sand),Is.EqualTo(RecoveryWheelDebrisKind.Sand));
            Assert.That(RecoveryWheelDebrisRules.FromTrack(RecoveryTrackKind.Dirt),Is.EqualTo(RecoveryWheelDebrisKind.Dirt));
            var snow=RecoveryWheelDebrisRules.Color(RecoveryWheelDebrisKind.Snow);
            var mud=RecoveryWheelDebrisRules.Color(RecoveryWheelDebrisKind.Mud);
            Assert.That(snow.b-mud.b,Is.GreaterThan(.6f));
            Assert.That(RecoveryWheelDebrisRules.Color(RecoveryWheelDebrisKind.Grass).g,Is.GreaterThan(mud.g));
        }
        [Test] public void EjectionFollowsRollingDirectionEvenWhileBrakingAndReversing()
        {
            Assert.That(RecoveryWheelDebrisRules.ThrowDirection(Vector3.forward,Vector3.forward*5,10,-1).z,Is.LessThan(0));
            Assert.That(RecoveryWheelDebrisRules.ThrowDirection(Vector3.forward,Vector3.back*5,-10,1).z,Is.GreaterThan(0));
            Assert.That(RecoveryWheelDebrisRules.ThrowDirection(Vector3.forward,Vector3.zero,-10,-1).z,Is.GreaterThan(0));
            var uphill=new Vector3(0,.5f,1).normalized;
            Assert.That(Vector3.Dot(RecoveryWheelDebrisRules.ThrowDirection(uphill,uphill*3,10,1),uphill),Is.LessThan(-.99f));
        }
        [Test] public void PowderRequiresRealGroundedMovementOrWheelspin()
        {
            foreach(RecoveryWheelDebrisKind kind in System.Enum.GetValues(typeof(RecoveryWheelDebrisKind)))
            {
                Assert.That(RecoveryWheelDebrisRules.PowderRate(kind,0,1,0,true),Is.Zero,"Load alone must not raise dust");
                Assert.That(RecoveryWheelDebrisRules.PowderRate(kind,12,1,35,false),Is.Zero,"Airborne tires must not raise dust");
            }
            Assert.That(RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Wood,12,1,35,true),Is.Zero);
            Assert.That(RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Stone,12,0,35,true),Is.Zero);
            Assert.That(RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Dirt,0,1,25,true),Is.GreaterThan(0));
        }
        [Test] public void OrdinaryDryDrivingSustainsVisibleDustWithinTheParticleBudget()
        {
            foreach(var kind in new[]{RecoveryWheelDebrisKind.Dirt,RecoveryWheelDebrisKind.Sand,RecoveryWheelDebrisKind.Snow})
            {
                float rate=RecoveryWheelDebrisRules.PowderRate(kind,3,0,8,true);
                Assert.That(rate*RecoveryWheelDebrisRules.PowderLifetime(kind),Is.GreaterThan(8),"Normal rolling needs a continuous trail, without requiring slip");
                Assert.That(RecoveryWheelDebrisRules.PowderSize(kind,3),Is.GreaterThan(.45f),"Dust must read from the follow camera");
                float maximum=RecoveryWheelDebrisRules.PowderRate(kind,100,1,1000,true);
                Assert.That(maximum*RecoveryWheelDebrisRules.PowderLifetime(kind)*1.1f,Is.LessThanOrEqualTo(28));
            }
            Assert.That(RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Grass,3,0,8,true),Is.GreaterThan(0));
            Assert.That(RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Mud,3,0,8,true),Is.LessThan(RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Dirt,3,0,8,true)));
        }
        [Test] public void GroundedWheelspinKeepsACloudWithoutVehicleTranslation()
        {
            float rolling=RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Sand,2.5f,0,5,true);
            float spinning=RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Sand,0,.5f,9,true);
            Assert.That(spinning,Is.GreaterThanOrEqualTo(rolling));
            Assert.That(RecoveryWheelDebrisRules.PowderRate(RecoveryWheelDebrisKind.Sand,0,.5f,0,true),Is.Zero);
        }
        [Test] public void SwitchLimitsBothLivePowderAndDistantOverdraw()
        {
            foreach(var kind in new[]{RecoveryWheelDebrisKind.Dirt,RecoveryWheelDebrisKind.Sand,RecoveryWheelDebrisKind.Snow})
            {
                float rate=RecoveryWheelDebrisRules.PowderRate(kind,100,1,1000,true,true);
                Assert.That(rate*RecoveryWheelDebrisRules.PowderLifetime(kind,true)*1.1f,Is.LessThanOrEqualTo(16));
                Assert.That(rate,Is.LessThan(RecoveryWheelDebrisRules.PowderRate(kind,100,1,1000,true)));
            }
            Assert.That(RecoveryWheelDebrisRules.DistanceGain(20,true),Is.EqualTo(1));
            Assert.That(RecoveryWheelDebrisRules.DistanceGain(40,true),Is.InRange(.01f,.99f));
            Assert.That(RecoveryWheelDebrisRules.DistanceGain(50,true),Is.Zero);
            Assert.That(RecoveryWheelDebrisRules.DistanceGain(50,false),Is.EqualTo(1));
        }
        [Test] public void SwitchKeepsRearTrailsOrTheTwoMostActivelySpinningTires()
        {
            var spin=new Vector4(.33f,.5f,.16f,0);
            for(int wheel=0;wheel<4;wheel++)
            {
                Assert.That(RecoveryWheelDebrisRules.PowderWheel(wheel,Vector4.zero,true),Is.EqualTo(wheel>=2));
                Assert.That(RecoveryWheelDebrisRules.PowderWheel(wheel,spin,true),Is.EqualTo(wheel<2));
                Assert.That(RecoveryWheelDebrisRules.PowderWheel(wheel,spin,false),Is.True);
            }
        }
    }
}
