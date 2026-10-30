using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public class RecoveryRulesTests
    {
        [Test] public void SlackCableNeverPushesTheVehiclesApart()
        {
            Assert.That(RecoveryRules.CableForce(5f, 6f, 20f), Is.Zero);
        }
        [Test] public void StretchedCableProducesTensionAndDampsSeparation()
        {
            float stationary = RecoveryRules.CableForce(7f, 6f, 0f);
            Assert.That(stationary, Is.GreaterThan(0f));
            Assert.That(RecoveryRules.CableForce(7f, 6f, 2f), Is.GreaterThan(stationary));
        }
        [Test] public void ClosingCableCannotProduceNegativeTension()
        {
            Assert.That(RecoveryRules.CableForce(6.01f, 6f, -10f), Is.Zero);
        }
        [Test] public void DeliveryRequiresTheCasualtyInsideTheBayAndSlow()
        {
            Assert.That(RecoveryRules.Delivered(RecoveryRules.Delivery, 0f), Is.True);
            Assert.That(RecoveryRules.Delivered(RecoveryRules.Delivery, 3f), Is.False);
            Assert.That(RecoveryRules.Delivered(RecoveryRules.Delivery + Vector3.right * 8f, 0f), Is.False);
        }
        [Test] public void DeliveryIgnoresSuspensionHeight()
        {
            Assert.That(RecoveryRules.Delivered(RecoveryRules.Delivery + Vector3.up * 1.5f, .5f), Is.True);
        }
    }
}
