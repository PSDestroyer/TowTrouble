using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryWheelTracksTests
    {
        [Test] public void WheelImpressionsBlendSurfacesButBreakAtTeleports()
        {
            Assert.That(RecoveryWheelTracks.CanConnect(Vector3.zero,Vector3.forward*.5f,0,0),Is.True);
            Assert.That(RecoveryWheelTracks.CanConnect(Vector3.zero,Vector3.forward*40,0,0),Is.False);
            Assert.That(RecoveryWheelTracks.CanConnect(Vector3.zero,Vector3.forward*.5f,0,2),Is.True);
            Assert.That(RecoveryWheelTracks.CanConnect(Vector3.zero,new Vector3(0,1.3f,.5f),0,0),Is.False);
        }
        [Test] public void StationaryWheelsDoNotAllocateRepeatedGeometry()
        {
            Assert.That(RecoveryWheelTracks.CanConnect(Vector3.one,Vector3.one,1,1),Is.False);
            Assert.That(RecoveryWheelTracks.CanConnect(Vector3.zero,Vector3.forward*.1f,2,2),Is.False);
        }
    }
}
