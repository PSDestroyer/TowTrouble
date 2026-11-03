using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private readonly List<Vector3> guidanceRoad = new List<Vector3>();
        private string guidanceRun;
        private float acceptedUntil;
        private float releaseCueUntil;
        private float wheelCueUntil;
        private float obstacleCueUntil;
        private bool showedDelivery;
        private bool showedSite;
        private bool pullingObstacle;
        private bool awayFromRoad;
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
        private int waypointSlot;
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
            ClearRecoveryGuidance();
        }

        private void TrackWaypoint(RecoveryWorld world, Camera camera, int slot, bool atHitch, bool delivery, string place, string distance, bool showHeight)
        {
            waypointWorld = world;
            waypointCamera = camera;
            waypointSlot = slot;
            waypointAtHitch = atHitch;
            waypointDelivery = delivery;
            SetWaypointContent(place, distance, delivery, showHeight);
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

            bool prompting = RefreshHitchPrompt(waypointWorld, waypointCamera, waypointSlot);
            bool workingCable = !waypointDelivery && waypointWorld.Attached(waypointSlot);
            if (prompting || workingCable)
            {
                if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
                localCaptionVisible = false;
                if (prompting) HideGuidanceNote();
                HideWaypoint();
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
            if (waypointWorld != null && marker != null && marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
            waypointWorld = null;
            waypointCamera = null;
            hasExtractionCaption = false;
            localCaptionVisible = false;
            guidanceRun = null;
            routeSampled = false;
            atRecoverySite = false;
            showedDelivery = false;
            showedSite = false;
            pullingObstacle = false;
            awayFromRoad = false;
            releaseCueUntil = wheelCueUntil = obstacleCueUntil = 0f;
            guidanceRoad.Clear();
            HideHitchPrompt();
            HideGuidanceNote();
            HideRoadTrail();
            HideWaypoint();
        }

        private bool UpdateApproach(RecoveryWorld world, int slot)
        {
            if (!world.HasContract) return false;
            if (guidanceRun != world.RunId)
            {
                guidanceRun = world.RunId;
                acceptedUntil = Time.unscaledTime + 2.4f;
                releaseCueUntil = wheelCueUntil = obstacleCueUntil = 0f;
                showedDelivery = world.Phase == RecoveryPhase.Deliver;
                showedSite = pullingObstacle = awayFromRoad = false;
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
            string place = delivery
                ? (contract.IsOpenWorld ? L(contract.DeliveryName, contract.DeliveryEnglish) : L("Zona de predare", "Drop-off area"))
                : contract.TitleFor(P);
            float now = Time.unscaledTime;
            bool attached = world.Attached(slot);
            if (delivery && !showedDelivery) releaseCueUntil = now + 1.8f;
            if (workingAtSite && !showedSite) wheelCueUntil = now + 2.4f;
            bool onObstacle = workingAtSite && world.ClearingObstacle && attached;
            if (onObstacle && !pullingObstacle) obstacleCueUntil = now + 2.4f;
            showedDelivery = delivery;
            showedSite = workingAtSite;
            pullingObstacle = onObstacle;

            // A specific caught wheel can be pointed out once on arrival. At the hitch
            // the interaction itself takes over, and a connected cable owns the feedback.
            hasExtractionCaption = workingAtSite && !attached && now < wheelCueUntil &&
                world.UsesArcadeExtraction && !contract.ExtractionProfile.TerrainAware && RefreshExtractionCaption(world);
            if (!hasExtractionCaption) localCaptionVisible = false;

            string note = null;
            if (workingAtSite)
            {
                awayFromRoad = false;
                HideRoadTrail();
                if (onObstacle && now < obstacleCueUntil && world.Tension(slot) < .82f)
                    note = L("Trage copacul în lateral", "Pull the log aside");
            }
            else
            {
                note = FollowRoad(world, camera, driver, target, delivery);
                if (delivery && now < releaseCueUntil) note = L("Mașină eliberată", "Vehicle freed");
                else if (!delivery && now < acceptedUntil) note = L("Intervenție acceptată", "Recovery accepted");
            }
            SetGuidanceNote(note);

            Vector3 delta = target - driver.position;
            float distance = delta.magnitude;
            // Keep the height cue on nearby cliff faces, with a dead band around its threshold.
            bool showHeight = !workingAtSite && Mathf.Abs(delta.y) >= (waypointHasHeight ? 6f : 9f);
            string metric = Mathf.RoundToInt(distance) + " m";
            if (showHeight)
            {
                string level = delta.y > 0f ? L("mai sus", "higher") : L("mai jos", "lower");
                metric += "  <size=75%>·  " + Mathf.RoundToInt(Mathf.Abs(delta.y)) + " m " + level + "</size>";
            }
            if (workingAtSite && world.ClearingObstacle) place = L("Copac căzut", "Fallen tree");
            TrackWaypoint(world, camera, slot, workingAtSite, delivery, place, metric, showHeight);
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
                awayFromRoad = false;
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
                awayFromRoad = false;
                return null;
            }

            ShowRoadTrail(camera, position, guidanceRoad);
            Vector3 toRoad = guidanceRoad[0] - position;
            float limit = awayFromRoad ? 10f : 18f;
            awayFromRoad = toRoad.sqrMagnitude > limit * limit;
            if (!awayFromRoad) return null;

            var flat = Vector3.ProjectOnPlane(toRoad, Vector3.up);
            float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(driver.forward, Vector3.up), flat, Vector3.up);
            if (Mathf.Abs(angle) > 130f) return L("Întoarce spre drum", "Turn back toward the road");
            if (angle > 25f) return L("Revino la drum · dreapta", "Rejoin the road · right");
            if (angle < -25f) return L("Revino la drum · stânga", "Rejoin the road · left");
            return L("Revino la drum", "Rejoin the road");
        }
    }
}
