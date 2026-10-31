using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryGroundMapTests
    {
        [Test] public void WorldCoordinatesSelectThePaintedSurfaceAndClampAtBoundary()
        {
            var map=ScriptableObject.CreateInstance<RecoveryGroundMap>();
            try
            {
                map.Configure(new Vector2(-500,-500),new Vector2(1000,1000),2,new byte[]{0,1,2,0});
                Assert.That(map.Sample(new Vector3(500,80,-500)),Is.EqualTo(RecoveryGroundKind.Sand));
                Assert.That(map.Sample(new Vector3(-500,0,500)),Is.EqualTo(RecoveryGroundKind.Snow));
                Assert.That(map.Sample(new Vector3(-900,200,900)),Is.EqualTo(RecoveryGroundKind.Snow));
            }
            finally { Object.DestroyImmediate(map); }
        }
        [Test] public void WheelCoverageStaysInsideRoadMaskAndWorldBounds()
        {
            var map=ScriptableObject.CreateInstance<RecoveryGroundMap>();
            try
            {
                map.Configure(Vector2.zero,new Vector2(10,10),2,new byte[]{0,0,0,0});
                map.ConfigureWheelImpressions(new byte[]{0,1,0,0});
                Assert.That(map.CanLeaveWheelImpression(new Vector3(10,0,0)),Is.True);
                Assert.That(map.CanLeaveWheelImpression(Vector3.zero),Is.False);
                Assert.That(map.CanLeaveWheelImpression(new Vector3(11,0,0)),Is.False);
                Assert.Throws<System.ArgumentException>(()=>map.ConfigureWheelImpressions(new byte[]{1}));
            }
            finally { Object.DestroyImmediate(map); }
        }
        [Test] public void MissingMapSafelyUsesDryGroundAndInvalidDimensionsAreRejected()
        {
            var map=ScriptableObject.CreateInstance<RecoveryGroundMap>();
            try
            {
                Assert.That(map.Sample(Vector3.zero),Is.EqualTo(RecoveryGroundKind.Rock));
                Assert.Throws<System.ArgumentException>(()=>map.Configure(Vector2.zero,Vector2.one,2,new byte[]{0}));
            }
            finally { Object.DestroyImmediate(map); }
        }
    }
}
