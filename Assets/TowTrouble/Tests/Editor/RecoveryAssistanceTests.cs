using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryAssistanceTests
    {
        [Test] public void PullingAdviceWaitsAndUsesObservedCause()
        {
            Assert.IsNull(RecoveryAssistance.PullAdvice(1, 0, .8f, false, 1.9f, WinchState.Stalled, 0, true));
            Assert.AreEqual("Find firmer ground", RecoveryAssistance.PullAdvice(1, 0, .8f, false, 3, WinchState.Holding, 1, true));
            Assert.AreEqual("Winch at limit · Reposition", RecoveryAssistance.PullAdvice(0, 0, 0, true, 3, WinchState.Stalled, 0, true));
            Assert.AreEqual("Try pulling toward the road", RecoveryAssistance.PullAdvice(0, 0, 0, false, 3, WinchState.Pulling, .4f, true));
            Assert.IsNull(RecoveryAssistance.PullAdvice(1, 2, .8f, false, 3, WinchState.Holding, 1, true));
            Assert.IsNull(RecoveryAssistance.PullAdvice(0, 0, 0, false, 3, WinchState.Holding, 0, false));
        }
        [Test] public void SafePointDoesNotTreatParkingOrSlowProgressAsBeingTrapped()
        {
            Assert.IsFalse(RecoveryAssistance.NeedsSafePoint(1, 0, 0, false, 0));
            Assert.IsFalse(RecoveryAssistance.NeedsSafePoint(1, 0, 1, true, 0));
            Assert.IsFalse(RecoveryAssistance.NeedsSafePoint(1, .8f, 1, false, 0));
            Assert.IsFalse(RecoveryAssistance.NeedsSafePoint(1, 0, 1, false, 1));
            Assert.IsTrue(RecoveryAssistance.NeedsSafePoint(0, 0, 0, false, 0));
            Assert.IsTrue(RecoveryAssistance.NeedsSafePoint(1, 0, -1, false, 0));
        }
        [Test] public void PreparationComparesRealSurfaceAndCableEquipment()
        {
            var contract = new RecoveryContract(1, 0, "", "", "", "", Vector3.zero, Vector3.one, 2, 0) {
                Load = new RecoveryLoadSettings { Material = RecoveryLoadMaterial.Mud },
                ExtractionProfile = new RecoveryExtractionSettings { BreakawayForce = 12000 }
            };
            var stock = new GaragePerformance { Drive = 3500, MudGrip = 1, SnowGrip = 1, SandGrip = 1, Cable = 11000 };
            Assert.AreEqual("Mud tires recommended\nStronger cable recommended", RecoveryAssistance.Preparation(contract, stock));
            stock.MudGrip = 1.5f; stock.Cable = 18000;
            Assert.AreEqual("Position and terrain affect the pull", RecoveryAssistance.Preparation(contract, stock));
            contract.Load.Material = RecoveryLoadMaterial.Snow;
            Assert.AreEqual("Snow tires recommended", RecoveryAssistance.Preparation(contract, stock));
            contract.Load.Material = RecoveryLoadMaterial.Sand;
            Assert.AreEqual("Sand tires recommended", RecoveryAssistance.Preparation(contract, stock));
        }
        [Test] public void NewJobNoticeOnlyWhenAvailableSelectionActuallyGainsAnEntry()
        {
            var contracts = new RecoveryContract[5];
            for (int i = 0; i < contracts.Length; i++) contracts[i] = new RecoveryContract(i + 1, 0, "", "", "", "", Vector3.zero, Vector3.one, 2, 0) { ProgressId = i + 3 };
            Assert.IsTrue(RecoveryAssistance.NewRecoveryAvailable(contracts, 0, 3));
            Assert.IsFalse(RecoveryAssistance.NewRecoveryAvailable(contracts, 1 << 3, 3));
            Assert.IsFalse(RecoveryAssistance.NewRecoveryAvailable(contracts, (1 << 3) | (1 << 4) | (1 << 5), 6));
            Assert.IsFalse(RecoveryAssistance.NewRecoveryAvailable(contracts, 0, -1));
        }
    }
}
