using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private bool waypointGarage;

        private void RefreshGarageGuidance(RecoveryWorld world, int slot, Camera camera)
        {
            if (camera == null || !world.FreeRoam || session.MenuOpen || session.GarageCatalog == null)
            {
                ClearRecoveryGuidance();
                return;
            }

            Vector3 delta = world.GaragePosition - world.Drivers[slot].transform.position;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            // Only visibility has a dead band. The key always follows the same
            // exact eligibility check as opening the workshop.
            if (distance > (waypointGarage ? 85f : 75f) || Mathf.Abs(delta.y) > 12f)
            {
                ClearRecoveryGuidance();
                return;
            }

            string key = session.UsingGamepad ? RecoveryInputPrompts.WestLabel(Gamepad.current!=null?RecoveryInputPrompts.Family(Gamepad.current):RecoveryControllerFamily.Xbox) : P.ReadKey(P.garageKey, Key.G).ToString();
            string ready = key + " · Customize";
            string caption = session.CanOpenGarage ? ready : world.InGarageArea(slot)
                ? world.Attached(slot) ? "Detach cable first" : "Stop to customize"
                : distance < 16f && Mathf.Abs(delta.y) < 3f ? "Park here" : Mathf.RoundToInt(distance) + " m";
            // Reserve one stable width while driving, stopping and switching input.
            string sample = waypointCaption.GetPreferredValues(ready).x > waypointCaption.GetPreferredValues("Stop to customize").x
                ? ready : "Stop to customize";
            waypointGarage = true;
            waypointWorld = world;
            waypointCamera = camera;
            waypointSlot = slot;
            waypointDelivery = waypointAtHitch = false;
            SetWaypointContent("GARAGE", caption, false, false, false, RecoveryIcon.Garage, sample);
        }
    }
}
