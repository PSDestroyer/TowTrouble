using System;
using UnityEngine;

namespace TowTrouble
{
    // Only the destination overlay is controlled here. Physical yard markings remain visible.
    [DisallowMultipleComponent]
    public sealed class RecoveryDeliveryHighlight : MonoBehaviour
    {
        [SerializeField] private RecoveryWorld world;
        [SerializeField] private string dropoffId = "A";
        [SerializeField] private Renderer[] highlights = Array.Empty<Renderer>();
        private bool visible;

        public void Configure(RecoveryWorld owner, string destinationId, Renderer[] renderers)
        {
            SetVisible(false, true);
            world = owner;
            dropoffId = destinationId;
            highlights = renderers ?? Array.Empty<Renderer>();
            SetVisible(false, true);
        }

        public static bool IsActiveDestination(RecoveryPhase phase, RecoveryContract contract, RecoveryDropoff destination)
        {
            return phase == RecoveryPhase.Deliver && contract != null && contract.IsOpenWorld &&
                destination != null && (contract.Delivery - destination.Position).sqrMagnitude < .25f;
        }

        private void OnEnable() { SetVisible(false, true); }
        private void OnDisable() { SetVisible(false, true); }

        private void LateUpdate()
        {
            bool active = false;
            if (world != null && world.FreeRoam && world.Phase == RecoveryPhase.Deliver &&
                world.Map != null && world.Map.Sites.Length > 0)
            {
                foreach (var destination in world.Map.Deliveries)
                {
                    if (destination == null || destination.Id != dropoffId) continue;
                    active = IsActiveDestination(world.Phase, world.Contract, destination);
                    break;
                }
            }
            SetVisible(active);
        }

        private void SetVisible(bool value, bool force = false)
        {
            if (!force && visible == value) return;
            visible = value;
            foreach (var renderer in highlights)
                if (renderer != null) renderer.enabled = value;
        }
    }
}
