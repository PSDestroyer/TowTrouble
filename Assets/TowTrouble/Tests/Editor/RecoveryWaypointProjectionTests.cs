using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryWaypointProjectionTests
    {
        private static readonly Vector2 Canvas = new Vector2(1600, 900);
        private static readonly Rect Bounds = Rect.MinMaxRect(135, 140, 1465, 735);

        [Test]
        public void OnScreenTargetStaysOnItsProjectedLocation()
        {
            bool edge = RecoveryWaypointProjection.Place(new Vector3(.6f, .45f, 60), Vector3.forward,
                Canvas, Bounds, out var position, out _);
            Assert.That(edge, Is.False);
            Assert.That(position.x, Is.EqualTo(960).Within(.001f));
            Assert.That(position.y, Is.EqualTo(405).Within(.001f));
        }

        [Test]
        public void TargetDirectlyBehindProducesADownArrow()
        {
            bool edge = RecoveryWaypointProjection.Place(new Vector3(.5f, .5f, -70), Vector3.back * 70,
                Canvas, Bounds, out var position, out var angle);
            Assert.That(edge, Is.True);
            Assert.That(position.x, Is.EqualTo(800).Within(.001f));
            Assert.That(position.y, Is.EqualTo(Bounds.yMin).Within(.001f));
            Assert.That(angle, Is.EqualTo(-90).Within(.001f));
        }

        [Test]
        public void RearRightTargetKeepsItsTrueSideDespiteMirroredProjection()
        {
            bool edge = RecoveryWaypointProjection.Place(new Vector3(-.2f, .5f, -50), new Vector3(100, 0, -50),
                Canvas, Bounds, out var position, out var angle);
            Assert.That(edge, Is.True);
            Assert.That(position.x, Is.GreaterThan(800).And.LessThanOrEqualTo(Bounds.xMax + .001f));
            Assert.That(position.y, Is.EqualTo(Bounds.yMin).Within(.001f));
            Assert.That(angle, Is.InRange(-90f, 0f));
        }

        [Test]
        public void TargetAboveTopHudKeepsItsBadgeBelowTheObjective()
        {
            bool edge = RecoveryWaypointProjection.Place(new Vector3(.5f, .98f, 50), Vector3.forward * 50,
                Canvas, Bounds, out var position, out var angle);
            Assert.That(edge, Is.True);
            Assert.That(position.y, Is.EqualTo(Bounds.yMax).Within(.001f));
            Assert.That(angle, Is.EqualTo(90).Within(.001f));
        }
    }
}
