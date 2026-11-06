using NUnit.Framework;

namespace TowTrouble.Tests
{
    public sealed class RecoveryReconnectGuidanceTests
    {
        [Test] public void ReconnectOnlyWhenDeliveryHasNoCableAndVehicleIsOutsideDropOff()
        {
            Assert.IsTrue(RecoveryHud.DeliveryNeedsReconnect(RecoveryPhase.Deliver, false, false));
            Assert.IsFalse(RecoveryHud.DeliveryNeedsReconnect(RecoveryPhase.Deliver, true, false));
            Assert.IsFalse(RecoveryHud.DeliveryNeedsReconnect(RecoveryPhase.Deliver, false, true));
            Assert.IsFalse(RecoveryHud.DeliveryNeedsReconnect(RecoveryPhase.Recover, false, false));
            Assert.IsFalse(RecoveryHud.DeliveryNeedsReconnect(RecoveryPhase.Explore, false, false));
        }
        [Test] public void LostVehicleReturnsThenReconnectsThenResumesDelivery()
        {
            Assert.AreEqual("Return to the recovered vehicle", RecoveryHud.MissionInstruction(RecoveryPhase.Deliver, false, false, false, false, 0, true, "Tab"));
            Assert.AreEqual("Reconnect your cable to the vehicle", RecoveryHud.MissionInstruction(RecoveryPhase.Deliver, true, false, false, false, 0, true, "Tab"));
            // Towing is true when either the local player or their co-op partner is attached.
            Assert.AreEqual("Tow the vehicle to the drop-off", RecoveryHud.MissionInstruction(RecoveryPhase.Deliver, false, true, false, false, 0, true, "Tab"));
        }
        [Test] public void DroppingCableAtDestinationKeepsDeliveryInstructions()
        {
            Assert.AreEqual("Stop the recovered vehicle", RecoveryHud.MissionInstruction(RecoveryPhase.Deliver, true, false, false, true, 2, true, "Tab"));
            Assert.AreEqual("Delivering the vehicle…", RecoveryHud.MissionInstruction(RecoveryPhase.Deliver, true, false, false, true, 0, true, "Tab"));
        }
    }
}
