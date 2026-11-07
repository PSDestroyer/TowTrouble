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
    }
}
