using NUnit.Framework;
using UnityEngine;

namespace TowTrouble.Tests
{
    public sealed class RecoveryDeliveryHighlightTests
    {
        private static RecoveryContract Contract(Vector3 delivery, bool openWorld = true)
        {
            return new RecoveryContract(1, 0, "Workshop", "", "", "", Vector3.zero, delivery, 8, 0)
                { IsOpenWorld = openWorld };
        }

        [Test]
        public void WorkshopHighlightOnlyAppearsDuringDeliveryToThatBay()
        {
            var bay = new RecoveryDropoff { Id = "A", Position = new Vector3(15, 2, 40) };
            foreach (RecoveryPhase phase in System.Enum.GetValues(typeof(RecoveryPhase)))
            {
                Assert.That(RecoveryDeliveryHighlight.IsActiveDestination(phase, Contract(bay.Position), bay),
                    Is.EqualTo(phase == RecoveryPhase.Deliver), phase.ToString());
            }
            Assert.That(RecoveryDeliveryHighlight.IsActiveDestination(RecoveryPhase.Deliver,
                Contract(bay.Position + Vector3.right * 20), bay), Is.False, "Another delivery yard");
            Assert.That(RecoveryDeliveryHighlight.IsActiveDestination(RecoveryPhase.Deliver,
                Contract(bay.Position + Vector3.up * 10), bay), Is.False, "Another mountain level");
            Assert.That(RecoveryDeliveryHighlight.IsActiveDestination(RecoveryPhase.Deliver,
                Contract(bay.Position, false), bay), Is.False, "Tutorial destination");
            Assert.That(RecoveryDeliveryHighlight.IsActiveDestination(RecoveryPhase.Deliver, null, bay), Is.False);
            Assert.That(RecoveryDeliveryHighlight.IsActiveDestination(RecoveryPhase.Deliver,
                Contract(bay.Position), null), Is.False);
        }

        [Test]
        public void ConfiguringAndDisablingOnlyHideTheHighlightRenderers()
        {
            var root = new GameObject("Disposable delivery markings");
            try
            {
                var permanent = new GameObject("Physical corner", typeof(MeshRenderer), typeof(BoxCollider));
                permanent.transform.SetParent(root.transform);
                var overlay = new GameObject("Delivery highlight", typeof(MeshRenderer));
                overlay.transform.SetParent(root.transform);
                var renderer = overlay.GetComponent<Renderer>();
                var highlight = root.AddComponent<RecoveryDeliveryHighlight>();
                highlight.Configure(null, "A", new[] { renderer });
                Assert.That(renderer.enabled, Is.False, "The saved scene starts with no active delivery");
                renderer.enabled = true;
                highlight.enabled = false;
                // Ordinary MonoBehaviours do not receive Play Mode lifecycle callbacks in this Edit Mode fixture.
                typeof(RecoveryDeliveryHighlight).GetMethod("OnDisable",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(highlight, null);
                Assert.That(renderer.enabled, Is.False);
                Assert.That(permanent.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(permanent.GetComponent<Collider>().enabled, Is.True);
                Assert.That(overlay.activeSelf, Is.True, "The component only controls rendering");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
