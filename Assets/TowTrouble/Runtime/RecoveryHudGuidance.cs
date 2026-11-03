using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private readonly List<Vector3> guidanceRoad = new List<Vector3>();
        private string guidanceRun;
        private float acceptedUntil;
        private bool atRecoverySite;
        private bool routeSampled;
        private bool routingToDelivery;
        private bool routingToObstacle;
        private Vector3 sampledDriver;
        private Vector3 sampledObjective;
        private float nextRouteTime;
        private float sampledRoadLength;
        private RecoveryWorld waypointWorld;
        private Camera waypointCamera;
        private bool waypointAtHitch;
        private bool waypointDelivery;
        private bool hasExtractionCaption;

        private void OnEnable()
        {
            Canvas.preWillRenderCanvases += PositionTrackedWaypoint;
        }

        private void OnDisable()
        {
            Canvas.preWillRenderCanvases -= PositionTrackedWaypoint;
            HideRoadTrail();
        }

        private void TrackWaypoint(RecoveryWorld world, Camera camera, bool atHitch, bool delivery, string caption)
        {
            waypointWorld = world;
            waypointCamera = camera;
            waypointAtHitch = atHitch;
            waypointDelivery = delivery;
            SetWaypointContent(caption, delivery);
        }

        // Project after the follow camera has moved. Captions and routing stay on the slower HUD clock.
        private void PositionTrackedWaypoint()
        {
            PositionCrewTags(waypointWorld, waypointCamera);
            if (waypointWorld == null || gameplay == null || !gameplay.activeInHierarchy) return;
            if (waypointCamera == null || !waypointWorld.HasContract)
            {
                ClearRecoveryGuidance();
                return;
            }

            Vector3 anchor;
            if (hasExtractionCaption)
            {
                anchor = ExtractionLabelAnchor(waypointWorld);
                if (TryPlaceExtractionCaption(waypointCamera, anchor))
                {
                    HideWaypoint();
                    return;
                }
            }
            else if (waypointDelivery) anchor = waypointWorld.Contract.Delivery + Vector3.up * 3f;
            else if (waypointAtHitch) anchor = waypointWorld.TowPoint + Vector3.up;
            else anchor = waypointWorld.TowTargetPosition + Vector3.up * 3f;

            if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
            ProjectWaypoint(waypointCamera, anchor, waypointDelivery);
        }

        private void ClearRecoveryGuidance()
        {
            if (waypointWorld != null && marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
            waypointWorld = null;
            waypointCamera = null;
            hasExtractionCaption = false;
            localCaptionVisible = false;
            guidanceRun = null;
            routeSampled = false;
            atRecoverySite = false;
            guidanceRoad.Clear();
            HideRoadTrail();
            HideWaypoint();
        }

        private bool UpdateApproach(RecoveryWorld world, int slot)
        {
            if (!world.HasContract) return false;
            if (guidanceRun != world.RunId)
            {
                guidanceRun = world.RunId;
                acceptedUntil = Time.unscaledTime + 3f;
                routeSampled = false;
                atRecoverySite = false;
            }

            Vector3 separation = world.TowTargetPosition - world.Drivers[slot].transform.position;
            // A nearby switchback can still sit on a different level of the mountain.
            float radius = atRecoverySite ? 33f : 28f;
            float height = atRecoverySite ? 10f : 8f;
            bool inRange = separation.sqrMagnitude <= radius * radius && Mathf.Abs(separation.y) <= height;
            if (world.Phase == RecoveryPhase.Deliver || world.Attached(slot) || !inRange || world.Map == null)
            {
                atRecoverySite = world.Phase != RecoveryPhase.Deliver && (world.Attached(slot) || inRange);
                return atRecoverySite;
            }

            Vector3 driver = world.Drivers[slot].transform.position;
            SampleRoad(world, driver, world.TowTargetPosition, false);
            bool onDifferentBend = sampledRoadLength > 60f && separation.sqrMagnitude > 9f * 9f &&
                (driver - guidanceRoad[0]).sqrMagnitude < 7f * 7f;
            atRecoverySite = !onDifferentBend;
            return atRecoverySite;
        }

        private void RefreshRecoveryGuidance(RecoveryWorld world, int slot, Camera camera, bool workingAtSite)
        {
            if (!world.HasContract)
            {
                ClearRecoveryGuidance();
                marker.gameObject.SetActive(false);
                return;
            }

            bool delivery = world.Phase == RecoveryPhase.Deliver;
            var contract = world.Contract;
            var driver = world.Drivers[slot].transform;
            Vector3 target = delivery ? contract.Delivery : world.TowTargetPosition;
            string title = delivery && contract.IsOpenWorld
                ? L("Livrează la " + contract.DeliveryName, "Deliver to " + contract.DeliveryEnglish)
                : contract.TitleFor(P);
            string instruction = delivery ? L("Livrează mașina", "Deliver the car") : L("Recuperează mașina", "Recover the car");
            if (!delivery && Time.unscaledTime < acceptedUntil)
                instruction = L("Intervenție acceptată", "Recovery accepted");

            hasExtractionCaption = workingAtSite && RefreshExtractionCaption(world);
            if (!hasExtractionCaption) localCaptionVisible = false;
            if (workingAtSite)
            {
                HideRoadTrail();
                if (world.ClearingObstacle)
                    instruction = L("1/2 · Trage copacul în lateral", "1/2 · Pull the log aside");
            }
            else
            {
                string directions = FollowRoad(world, camera, driver, target, delivery);
                if (!string.IsNullOrEmpty(directions) && (delivery || Time.unscaledTime >= acceptedUntil))
                    instruction = directions;
            }

            objective.text = title + "\n" + instruction;
            Vector3 delta = target - driver.position;
            string caption = Mathf.RoundToInt(delta.magnitude) + " m";
            if (workingAtSite && world.ClearingObstacle)
                caption = L("COPAC", "FALLEN TREE") + " · " + caption;
            if (Mathf.Abs(delta.y) >= 8f)
            {
                string level = delta.y > 0 ? L("mai sus", "higher") : L("mai jos", "lower");
                caption += "\n" + Mathf.RoundToInt(Mathf.Abs(delta.y)) + " m " + level;
            }
            TrackWaypoint(world, camera, workingAtSite, delivery, caption);
        }

        private void SampleRoad(RecoveryWorld world, Vector3 position, Vector3 target, bool delivery)
        {
            bool changed = !routeSampled || routingToDelivery != delivery || routingToObstacle != world.ClearingObstacle;
            bool moved = (position - sampledDriver).sqrMagnitude >= 36f || (target - sampledObjective).sqrMagnitude >= 16f;
            if (changed || (moved && Time.unscaledTime >= nextRouteTime))
            {
                routeSampled = true;
                routingToDelivery = delivery;
                routingToObstacle = world.ClearingObstacle;
                sampledDriver = position;
                sampledObjective = target;
                nextRouteTime = Time.unscaledTime + .5f;
                world.Map.TryGetRoadRoute(position, target, guidanceRoad);
                sampledRoadLength = 0f;
                for (int i = 1; i < guidanceRoad.Count; i++)
                    sampledRoadLength += Vector3.Distance(guidanceRoad[i - 1], guidanceRoad[i]);
            }
        }

        private string FollowRoad(RecoveryWorld world, Camera camera, Transform driver, Vector3 target, bool delivery)
        {
            if (world.Map == null)
            {
                HideRoadTrail();
                return null;
            }

            Vector3 position = driver.position;
            SampleRoad(world, position, target, delivery);

            if (guidanceRoad.Count == 0)
            {
                HideRoadTrail();
                return null;
            }

            Vector3 access = guidanceRoad[guidanceRoad.Count - 1];
            bool atAccess = (position - access).sqrMagnitude < 10f * 10f;
            // Keep guiding toward the goal once the player has left this road on its final approach.
            bool beyondAccess = (guidanceRoad[0] - access).sqrMagnitude < 10f * 10f &&
                Vector3.Distance(position, target) + 2f < Vector3.Distance(access, target);
            if (sampledRoadLength < 45f && (atAccess || beyondAccess))
            {
                HideRoadTrail();
                return delivery ? L("Intră în zona de predare", "Enter the drop-off area") :
                    L("De aici, mergi spre mașina marcată", "Head for the marked vehicle from here");
            }

            ShowRoadTrail(camera, position, guidanceRoad);
            bool rejoin = (position - guidanceRoad[0]).sqrMagnitude > 12f * 12f;
            Vector3 next = guidanceRoad[0];
            if (!rejoin)
            {
                foreach (var point in guidanceRoad)
                {
                    next = point;
                    if ((point - position).sqrMagnitude >= 16f * 16f) break;
                }
            }
            Vector3 direction = Vector3.ProjectOnPlane(next - position, Vector3.up);
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(driver.forward, Vector3.up), direction, Vector3.up);
            string lead = rejoin ? L("Revino la drum", "Rejoin the road") : L("Urmează drumul", "Follow the road");
            if (Mathf.Abs(angle) > 130f) return lead + L(" · întoarce", " · turn around");
            if (angle > 25f) return lead + L(" · spre dreapta", " · bear right");
            if (angle < -25f) return lead + L(" · spre stânga", " · bear left");
            return lead + L(" · înainte", " · straight ahead");
        }
    }
}
