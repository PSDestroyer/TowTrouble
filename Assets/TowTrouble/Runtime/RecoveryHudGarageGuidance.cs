using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private bool waypointGarage;
        private Vector3 waypointGaragePosition;

        private void RefreshGarageGuidance(RecoveryWorld world, int slot, Camera camera)
        {
            if (camera == null || !world.FreeRoam || session.MenuOpen || session.GarageCatalog == null)
            {
                ClearRecoveryGuidance();
                return;
            }

            var position = world.Drivers[slot].transform.position;
            bool service = session.Expedition?.ServiceReady == true &&
                Vector3.Distance(position, session.Expedition.ServicePosition) < Vector3.Distance(position, world.GaragePosition);
            waypointGaragePosition = service ? session.Expedition.ServicePosition : world.GaragePosition;
            Vector3 delta = waypointGaragePosition - position;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            // Only visibility has a dead band. The key always follows the same
            // exact eligibility check as opening the workshop.
            if (distance > (waypointGarage ? 85f : 75f) || Mathf.Abs(delta.y) > 12f)
            {
                ClearRecoveryGuidance();
                return;
            }

            string key = session.UsingGamepad ? RecoveryInputPrompts.WestLabel(RecoveryInputDevices.Pad!=null?RecoveryInputPrompts.Family(RecoveryInputDevices.Pad):RecoveryControllerFamily.Xbox) : P.ReadKey(P.garageKey, Key.G).ToString();
            string ready = key + " · Customize";
            string caption = session.CanOpenGarage ? ready : world.InGarageArea(slot)
                ? world.Attached(slot) ? "Detach cable first" : "Stop to customize"
                : distance < 16f && Mathf.Abs(delta.y) < 3f ? "Park here" : Mathf.RoundToInt(distance) + " m";
            // Reserve one stable width while driving, stopping and switching input.
            string sample = waypointCaption.GetLocalizedPreferredValues(ready).x > waypointCaption.GetLocalizedPreferredValues("Stop to customize").x
                ? ready : "Stop to customize";
            waypointGarage = true;
            waypointWorld = world;
            waypointCamera = camera;
            waypointSlot = slot;
            waypointDelivery = waypointAtHitch = false;
            SetWaypointContent(service ? "PINEWOOD SERVICE" : "GARAGE", caption, false, false, false, RecoveryIcon.Garage, sample);
        }
    }
}
