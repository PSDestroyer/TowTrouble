using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryDeliveryGuidanceTests
    {
        private static RecoveryContract Contract() => new RecoveryContract(1, 0, "", "", "", "",
            new Vector3(100, 20, 100), new Vector3(20, 10, 30), 1, 0);

        [Test]
        public void DeliveryInstructionsOnlyAppearInDeliveryPhase()
        {
            var contract = Contract();
            foreach (var phase in new[] { RecoveryPhase.Lobby, RecoveryPhase.HookUp, RecoveryPhase.Recover,
                RecoveryPhase.Complete, RecoveryPhase.Explore })
                Assert.That(RecoveryHud.DeliveryWaypointInstruction(phase, contract, contract.Delivery,
                    contract.Delivery, 0f), Is.Null, phase.ToString());
            Assert.That(RecoveryHud.DeliveryWaypointInstruction(RecoveryPhase.Deliver, null,
                Vector3.zero, Vector3.zero, 0f), Is.Null);
        }

        [Test]
        public void NearbyDriverMustBringTheRecoveredCarIntoTheBay()
        {
            var contract = Contract();
            Assert.That(RecoveryHud.DeliveryWaypointInstruction(RecoveryPhase.Deliver, contract,
                contract.Delivery, contract.Delivery + Vector3.forward * 10f, 0f),
                Is.EqualTo("Bring recovered car here"));
            Assert.That(RecoveryHud.DeliveryWaypointInstruction(RecoveryPhase.Deliver, contract,
                contract.Delivery + Vector3.right * 25f, contract.Target, 0f), Is.Null);
            // A nearby mountain road above the bay is not its final approach.
            Assert.That(RecoveryHud.DeliveryWaypointInstruction(RecoveryPhase.Deliver, contract,
                contract.Delivery + Vector3.up * 6f, contract.Target, 0f), Is.Null);
        }

        [Test]
        public void ReadinessUsesRecoveredCarSpeedAndResetsWhenItMovesAgain()
        {
            var contract = Contract();
            Vector3 driver = contract.Delivery + Vector3.right * 12f;
            foreach (float speed in new[] { 10f, 2f, 1.999f, 0f, 2.01f })
            {
                string expected = speed < 2f ? "Delivering…" : "Stop to deliver";
                Assert.That(RecoveryHud.DeliveryWaypointInstruction(RecoveryPhase.Deliver, contract,
                    driver, contract.Delivery, speed), Is.EqualTo(expected));
            }
        }

        [Test]
        public void BayBoundaryAndWrongAltitudeNeverClaimDelivery()
        {
            var contract = Contract();
            foreach (var offset in new[] { Vector3.right * 7f, Vector3.left * 7f,
                Vector3.forward * 7f, Vector3.up * 4f, Vector3.down * 4f })
                Assert.That(RecoveryHud.DeliveryWaypointInstruction(RecoveryPhase.Deliver, contract,
                    contract.Delivery, contract.Delivery + offset, 0f), Is.EqualTo("Bring recovered car here"));
            Assert.That(RecoveryHud.DeliveryWaypointInstruction(RecoveryPhase.Deliver, contract,
                contract.Delivery, contract.Delivery + new Vector3(6.99f, 3.99f, 0), 0f), Is.EqualTo("Delivering…"));
        }
    }
}
