using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoverySoftGroundTests
    {
        [Test] public void FirmGroundDoesNotGainSoftDrag(){Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Rock,1),Is.Zero);}
        [Test] public void SnowResistsTravelAndDeepFieldsAreHeavier(){Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Snow,0),Is.EqualTo(110));Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Snow,1),Is.EqualTo(340));}
        [Test] public void SandResistsTravelAndDeepFieldsAreHeavier(){Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Sand,0),Is.EqualTo(75));Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Sand,1),Is.EqualTo(260));}
        [Test] public void SurfaceDepthIsClamped(){Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Snow,-10),Is.EqualTo(110));Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Sand,10),Is.EqualTo(260));}
        [Test] public void AppropriateTiresHelpWithoutRemovingResistance(){float normal=RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Snow,1);Assert.That(RecoveryTraction.SoftGroundDrag(RecoveryGroundKind.Snow,1,1.65f),Is.InRange(200,normal-1));}
        [Test] public void ResistanceOpposesGroundMotionWithoutDownwardSuction(){var f=RecoveryTraction.MudResistance(new Vector3(3,-4,5),Vector3.up,340,1350);Assert.That(f.y,Is.Zero);Assert.That(Vector3.Dot(f,new Vector3(3,0,5)),Is.LessThan(0));}
        [Test] public void StoppedVehicleHasNoArtificialHoldingForce(){Assert.That(RecoveryTraction.MudResistance(Vector3.zero,Vector3.up,340,1350),Is.EqualTo(Vector3.zero));}
        [Test] public void CoatingCleansGraduallyAndNeverGoesNegative(){float amount=RecoveryTraction.CleanCoverage(1,RecoveryGroundKind.Snow,5,1);Assert.That(amount,Is.InRange(.9f,.99f));Assert.That(RecoveryTraction.CleanCoverage(.01f,RecoveryGroundKind.Snow,5,10),Is.Zero);}
        [Test] public void PausedCoatingDoesNotDecay(){Assert.That(RecoveryTraction.CleanCoverage(.7f,RecoveryGroundKind.Sand,10,0),Is.EqualTo(.7f));}
    }
}
