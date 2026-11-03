using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public class RecoveryRoadNetworkTests
    {
        [Test]
        public void SwitchbackKeepsDriversOnConnectedRoadsInEitherDirection()
        {
            var network = new RecoveryRoadNetwork();
            var low = Vector3.zero;
            var high = new Vector3(0, 15, 6);
            var turn = new Vector3(80, 8, 0);
            network.AddRoad("low", "bend", new[] { low, turn });
            network.AddRoad("bend", "high", new[] { turn, high });
            var route = new List<Vector3>();

            Assert.That(network.TryRoute(low, high, route), Is.True);
            Assert.That(route, Is.EqualTo(new[] { low, turn, high }));
            Assert.That(network.TryRoute(high, low, route), Is.True);
            Assert.That(route, Is.EqualTo(new[] { high, turn, low }));
        }

        [Test]
        public void OneRoadProjectsToAccessPointsWithoutOvershooting()
        {
            var network = new RecoveryRoadNetwork();
            network.AddRoad("west", "east", new[] { Vector3.zero, Vector3.right * 100 });
            var route = new List<Vector3>();
            Assert.That(network.TryRoute(new Vector3(70, 1, -4), new Vector3(40, 2, 6), route), Is.True);
            Assert.That(route, Is.EqualTo(new[] { Vector3.right * 70, Vector3.right * 40 }));
        }

        [Test]
        public void CrossingsWithoutAJunctionDoNotCreateShortcuts()
        {
            var network = new RecoveryRoadNetwork();
            network.AddRoad("west", "east", new[] { Vector3.left * 50, Vector3.right * 50 });
            network.AddRoad("north", "south", new[] { Vector3.forward * 50, Vector3.back * 50 });
            var route = new List<Vector3> {Vector3.one};
            Assert.That(network.TryRoute(Vector3.left * 45, Vector3.forward * 45, route), Is.False);
            Assert.That(route, Is.Empty);
        }
    }
}
