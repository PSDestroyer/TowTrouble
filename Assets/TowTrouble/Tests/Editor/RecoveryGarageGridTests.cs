using NUnit.Framework;
using UnityEngine;
namespace TowTrouble.Tests
{
    public sealed class RecoveryGarageGridTests
    {
        [Test] public void FocusScrollsInBothDirectionsAndClampsAtTheLastRow()
        {
            Assert.That(RecoveryGarageCardFocus.VisibleOffset(800,1002,468,0,1200),Is.EqualTo(534));
            Assert.That(RecoveryGarageCardFocus.VisibleOffset(0,202,468,534,1200),Is.Zero);
            Assert.That(RecoveryGarageCardFocus.VisibleOffset(214,416,468,0,1200),Is.Zero);
            Assert.That(RecoveryGarageCardFocus.VisibleOffset(1100,1302,468,534,1200),Is.EqualTo(732));
            Assert.That(RecoveryGarageCardFocus.VisibleOffset(0,202,468,0,202),Is.Zero);
        }
        [Test] public void CatalogRefreshKeepsBakedImagesWithoutTouchingPerformance()
        {
            var catalog=ScriptableObject.CreateInstance<RecoveryGarageCatalog>();var texture=new Texture2D(2,2);
            try
            {
                var uv=new Rect(.25f,.5f,.125f,.125f);
                catalog.Configure(new[]{new GaragePart{Id="part_a",Thumbnail=texture,ThumbnailUV=uv}},new GameObject[0]);
                var replacement=new GaragePart{Id="part_a",DriveMultiplier=1.2f};catalog.Configure(new[]{replacement},new GameObject[0]);
                Assert.That(replacement.Thumbnail,Is.SameAs(texture));Assert.That(replacement.ThumbnailUV,Is.EqualTo(uv));Assert.That(replacement.DriveMultiplier,Is.EqualTo(1.2f));
            }
            finally{Object.DestroyImmediate(catalog);Object.DestroyImmediate(texture);}
        }
        [Test] public void CosmeticsDoNotClaimHorsepowerAndTechnicalCardsDescribeTheirRealEffect()
        {
            Assert.That(RecoveryGarageCardInfo.Performance(new GaragePart{Slot=GarageSlot.Bonnet},true),Is.Empty);
            Assert.That(RecoveryGarageCardInfo.Performance(new GaragePart{Slot=GarageSlot.Engine,DriveMultiplier=1.2f},true),Is.EqualTo("+20% pull"));
            Assert.That(RecoveryGarageCardInfo.Performance(new GaragePart{Slot=GarageSlot.Cable,CableStrength=26000},true),Is.EqualTo("26 kN"));
            Assert.That(RecoveryGarageCardInfo.Performance(new GaragePart{Id="tires_winter_pro",Slot=GarageSlot.Wheels},false),Does.Contain("135%"));
        }
    }
}
