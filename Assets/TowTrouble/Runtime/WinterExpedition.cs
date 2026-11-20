using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    [DisallowMultipleComponent]
    public sealed partial class WinterExpedition : MonoBehaviour
    {
        [Serializable]
        public struct Survey
        {
            public string Name;
            public string Brief;
            public Vector3 Point;
            public int Reward;
            internal int Tier, ExpectedSeconds;
        }

        [SerializeField] private Survey[] surveys;
        [SerializeField] private Transform[] anchors;
        [SerializeField] private LineRenderer cable;

        private DemoSession session;
        private RecoveryVehicle driver;
        private Transform attachedAnchor, nearbyTowEye;
        private Rigidbody attachedBody;
        private bool rearCable;
        private WinterRoadContracts jobs;
        private float holdSeconds, cableLength, reelSpeed, stress, nextAnchorCheck, startedAt;
        private int resetsAtStart, cableBreaks;
        private bool reel, payOut;
        public int Selected { get; private set; } = -1;
        public bool Returning { get; private set; }
        public string Report { get; private set; }
        internal (string Name, WinterJob? Job, string Location, int Reward, int Bonus, float Elapsed, int Resets, int CableBreaks, bool CargoIntact)? Result { get; private set; }
        internal WinchFrame CableFrame { get; private set; }
        public bool Attached => attachedAnchor != null;
        public Transform NearbyAnchor { get; private set; }
        public float CableLoad { get; private set; }
        internal Vector3 CableReaction { get; private set; }
        public IReadOnlyList<Survey> Surveys => surveys;
        public bool Active => Selected >= 0 || ActiveJob.HasValue;
        internal WinterJob? ActiveJob => jobs?.Active;
        internal int ActiveJobIndex => jobs?.ActiveIndex ?? -1;
        internal string ActiveName => ActiveJobIndex >= 0 ? WinterRoadContracts.Offers[ActiveJobIndex].Name :
            Selected >= 0 ? surveys[Selected].Name : null;
        internal int CargoUnits => jobs?.CargoUnits ?? 0;
        internal bool DeliveryLeg => Returning || jobs?.DeliveryLeg == true;
        internal bool HasCargo => jobs?.CargoLoaded == true;
        internal bool NearTowTarget { get; private set; }
        internal bool TowHintUsesRear { get; private set; }
        internal bool TowingLoad => attachedBody != null;
        internal RecoveryVehicle TowedPickup => Attached && TowingLoad ? jobs?.Pickup : null;
        internal bool UsingRearCable => Attached && rearCable;
        internal bool Reconnecting => jobs?.Reconnecting(Attached) == true;
        internal bool ShowingEntry => jobs?.ApproachingEntry == true;
        internal bool ShowingPullPoint => !ShowingEntry && driver != null && jobs?.MarkingPullPoint(driver.transform.position) == true &&
            (!NearTowTarget || !jobs.AtPullPoint(driver.transform.position) ||
            ActiveJob == WinterJob.Recovery && !TowHintUsesRear);
        internal bool MissedPlowSection => driver != null && jobs?.MissedPlowSection(driver.FrontHitch) == true;
        internal bool ExtractingPickup => TowingLoad && jobs?.ExtractingPickup == true;
        internal bool ShowingPickupExit => TowingLoad && jobs?.GuidingPickupExit == true;
        internal Vector3 GuidancePoint => FollowingRegionalRoute ? region.RouteTarget(driver.transform.position, ActiveJobIndex == 15, ForestRoute) : ShowingPickupExit ? jobs.PickupExit : ShowingEntry ? jobs.EntryPoint : ShowingPullPoint || ExtractingPickup ? jobs.PullPoint :
            (Reconnecting || NearTowTarget) && (nearbyTowEye != null ? nearbyTowEye : jobs?.TowEye) is { } eye ? eye.position : Destination;
        internal string GuidanceName => FollowingRegionalRoute ? ForestRoute ? "Forest cut · follow the blue stakes" : "Packed road · follow the route" : ShowingPickupExit ? "Recovery exit · take the bend wide" :
            ShowingEntry ? (ActiveJob == WinterJob.Tree ? "Lay-by approach" : "Recovery approach") :
            ShowingPullPoint || ExtractingPickup ? "Marked pull point" :
            Reconnecting ? "Reconnect the cable" : NearTowTarget ? NearbyTowName : DestinationName;
        internal string NearbyTowName => jobs.TowEyeName(nearbyTowEye);
        internal bool CanFitPlow => driver != null && jobs?.AtPlowStart(driver.transform.position) == true;
        internal bool PlowFitted => jobs?.PlowFitted == true;
        internal bool BladeLowered => jobs?.BladeLowered == true;
        internal float BladeLoad => jobs?.BladeLoad ?? 0;
        public Vector3 Destination => ActiveJob.HasValue ? jobs.Destination :
            Returning ? SurveyBase.Position : surveys[Selected].Point;
        public string DestinationName => ActiveJob.HasValue ? jobs.DestinationName :
            Returning ? SurveyBase.English : surveys[Selected].Name;
        private RecoveryDropoff SurveyBase => Array.Find(session.World.Map.Deliveries, bay => bay.Id == (Selected == 0 ? "E" : "B"));
        internal float DriftAt(Vector3 point) => Mathf.Max(jobs?.SnowAt(point) ?? 0, region?.SnowAt(point, Weather) ?? 0);
        internal Vector3 JobStart(int index) => jobs.JobStart(index);

        public void Configure(Survey[] sites, Transform[] winchAnchors, LineRenderer line)
        {
            surveys = sites;
            anchors = winchAnchors;
            cable = line;
        }

        internal void Bind(DemoSession owner)
        {
            session = owner;
            // Winter jobs own their obstacles. Classic recovery props can block the new winch approaches.
            foreach (var obstacle in FindObjectsByType<RecoveryTowObstacle>(FindObjectsSortMode.None))
                if (obstacle.gameObject.scene == gameObject.scene) obstacle.gameObject.SetActive(false);
            jobs = new WinterRoadContracts(owner.World, transform, owner.Profile.winterJobs);
            BalanceSurveys();
            region = new WinterRegion(owner.World, transform, owner.Profile);
            var ruts = gameObject.AddComponent<WinterSnowRuts>();
            ruts.Bind(owner.World.Drivers[0].transform);
            foreach (var car in owner.World.Drivers)
            {
                car.WinterDriving = true;
                car.SnowRuts = ruts;
                car.WinterExpedition = this;
                car.gameObject.AddComponent<WinterBodySnow>();
            }
            cable.enabled = false;
        }

        private void BalanceSurveys()
        {
            var route = new List<Vector3>();
            for (int i = 0; i < surveys.Length; i++)
            {
                var survey = surveys[i];
                var outbound = Measure(session.World.GaragePosition, survey.Point);
                var depot = Array.Find(session.World.Map.Deliveries, bay => bay.Id == (i == 0 ? "E" : "B"));
                var returning = Measure(survey.Point, depot.Position);
                float distance = outbound.Length + returning.Length;
                float climb = outbound.Climb + returning.Climb;
                survey.Tier = climb >= 55f ? 2 : distance >= 650f ? 1 : 0;
                survey.ExpectedSeconds = Mathf.RoundToInt((20f + distance / 6f + climb * .5f) / 15f) * 15;
                float reward = 100 + survey.Tier * 75 + survey.ExpectedSeconds / 60f * (220 + survey.Tier * 40);
                survey.Reward = Mathf.Max(400, Mathf.RoundToInt(reward / 50f) * 50);
                surveys[i] = survey;
            }

            (float Length, float Climb) Measure(Vector3 from, Vector3 to)
            {
                if (!session.World.Map.TryGetRoadRoute(from, to, route))
                    return (Vector3.Distance(from, to), Mathf.Max(0, to.y - from.y));
                float length = Vector3.Distance(from, route[0]) + Vector3.Distance(to, route[route.Count - 1]);
                float climb = 0;
                for (int point = 1; point < route.Count; point++)
                {
                    length += Vector3.Distance(route[point - 1], route[point]);
                    climb += Mathf.Max(0, route[point].y - route[point - 1].y);
                }
                return (length, climb);
            }
        }

        public void Depart(int index)
        {
            if (!session.ReadyForPresentation || !session.BoardOpen || session.World.Phase != RecoveryPhase.Explore ||
                index < 0 || index >= surveys.Length || Attached || Active || !CanStart(WinterRoadContracts.Offers.Length + index)) return;
            jobs.Clear();
            region.SetActiveJob(-1);
            Selected = index;
            Returning = false;
            holdSeconds = 0;
            Report = null;
            StartResultTracking();
            session.CloseOverlay();
        }

        internal void Accept(int index)
        {
            if (!session.ReadyForPresentation || !session.BoardOpen || session.World.Phase != RecoveryPhase.Explore ||
                Attached || Active || index < 0 || index >= WinterRoadContracts.Offers.Length || !CanStart(index)) return;
            Selected = -1;
            Returning = false;
            holdSeconds = 0;
            Report = null;
            StartResultTracking();
            region.SetActiveJob(index);
            jobs.Begin(index);
            session.CloseOverlay();
        }

        internal void Abandon()
        {
            Detach();
            jobs.Clear();
            region?.SetActiveJob(-1);
            Selected = -1;
            Returning = false;
            holdSeconds = 0;
            Result = null;
            Report = null;
            nearbyTowEye = null;
            NearTowTarget = false;
            nextAnchorCheck = 0;
        }

        internal string Instruction(bool near)
        {
            if (ActiveJob.HasValue && driver != null)
            {
                if (FollowingRegionalRoute && !near && !jobs.CargoWarning) return ForestRoute
                    ? "Forest cut · stay between blue stakes · winter tires and gentle throttle"
                    : "Packed road · follow the cleared access and brake before turns";
                return jobs.Instruction(driver, near, Attached, Attached ? rearCable : TowHintUsesRear, NearTowTarget);
            }
            return near ? (Attached ? "Detach the winch" : "Stop to log your readings") : DestinationName;
        }

        public bool NearDestination(Vector3 position)
        {
            Vector3 delta = Destination - position;
            return Mathf.Abs(delta.y) < 4f && new Vector2(delta.x, delta.z).sqrMagnitude <= 100f;
        }

        private void Update()
        {
            if (session == null || !session.ReadyForPresentation || session.World.Phase != RecoveryPhase.Explore)
            {
                Detach();
                NearbyAnchor = null;
                NearTowTarget = false;
                if (ActiveJob.HasValue) jobs.Clear();
                return;
            }

            driver = session.World.Drivers[session.LocalSlot];
            region.Tick(driver.transform.position, Weather);
            if (session.MenuOpen) return;
            if (Time.time >= nextAnchorCheck)
            {
                nextAnchorCheck = Time.time + .18f;
                bool useRear = false;
                nearbyTowEye = Attached ? null : jobs.FindTowEye(driver, out useRear);
                NearTowTarget = nearbyTowEye != null;
                TowHintUsesRear = useRear;
                NearbyAnchor = Attached || NearTowTarget ? null : FindAnchor();
            }
            if (ActiveJob is WinterJob job)
            {
                jobs.UpdateRecoveryStage(driver, Attached, CableLoad);
                if (jobs.Tick(driver, Attached)) CompleteJob(job);
                return;
            }
            if (!Active) return;
            bool stopped = NearDestination(driver.transform.position) && driver.Speed < .75f && !Attached;
            holdSeconds = stopped ? holdSeconds + Time.deltaTime : 0;
            if (holdSeconds < 1.5f) return;
            holdSeconds = 0;

            if (!Returning)
            {
                Returning = true;
                return;
            }

            int mask = 1 << Selected;
            var profile = session.Profile;
            bool first = (profile.winterSurveys & mask) == 0;
            var survey = surveys[Selected];
            if (first)
            {
                profile.winterSurveys |= mask;
                profile.credits = Mathf.Min(1000000000, profile.credits + survey.Reward);
                profile.Save();
            }
            region.RefreshProgress();
            Report = first ? $"Survey logged · +{survey.Reward:N0} CR" : "Survey logged";
            Result = (survey.Name, null, SurveyBase.English, first ? survey.Reward : 0, 0, Time.time - startedAt, RunResets, cableBreaks, false);
            Returning = false;
            Selected = -1;
        }

        private void CompleteJob(WinterJob job)
        {
            Detach();
            int index = jobs.ActiveIndex;
            var offer = WinterRoadContracts.Offers[index];
            var profile = session.Profile;
            int mask = 1 << index;
            bool first = (profile.winterJobs & mask) == 0;
            int bonus = job == WinterJob.Supply ? jobs.CargoBonus : 0;
            int reward = first ? offer.Reward + bonus : 0;
            if (first)
            {
                profile.winterJobs |= mask;
                profile.credits = Mathf.Min(1000000000, profile.credits + reward);
                profile.Save();
            }
            Report = first ? $"{offer.Name} complete · +{reward:N0} CR" : $"{offer.Name} complete";
            Result = (offer.Name, job, jobs.CompletionLocation, reward, first ? bonus : 0, Time.time - startedAt, RunResets, cableBreaks, bonus > 0);
            jobs.Finish(job);
            region.SetActiveJob(-1);
            region.RefreshProgress();
            NearTowTarget = false;
        }

        internal void DismissResult() => Result = null;

        private int RunResets => Mathf.Max(0, session.World.Resets(session.LocalSlot) - resetsAtStart);

        private void StartResultTracking()
        {
            startedAt = Time.time;
            resetsAtStart = session.World.Resets(session.LocalSlot);
            cableBreaks = 0;
        }

        internal void SetWinchInput(bool wind, bool release)
        {
            reel = wind;
            payOut = release;
        }

        internal void ToggleWinch()
        {
            if (Attached) { Detach(); return; }
            if (session == null || session.MenuOpen || session.World.Phase != RecoveryPhase.Explore) return;
            driver = session.World.Drivers[session.LocalSlot];
            if (CanFitPlow)
            {
                jobs.FitPlow(driver);
                return;
            }
            if (PlowFitted)
            {
                jobs.ToggleBlade();
                return;
            }
            var eye = jobs.FindTowEye(driver, out rearCable);
            if (eye != null)
            {
                attachedAnchor = eye;
                attachedBody = jobs.TowBody;
                jobs.RecordRecoveryEye(eye);
                jobs.AttachedTarget();
            }
            else
            {
                rearCable = false;
                attachedAnchor = FindAnchor();
            }
            if (!Attached) return;
            cableLength = Mathf.Max(RecoveryWinch.MinimumLength, Vector3.Distance(Hitch, attachedAnchor.position));
            CableFrame = new WinchFrame { Length = cableLength, State = WinchState.Holding };
            cable.enabled = true;
        }

        private Vector3 Hitch => rearCable ? driver.RearHitch : driver.FrontHitch;

        private Transform FindAnchor()
        {
            Transform closest = null;
            float limit = 18f * 18f;
            Vector3 hitch = driver.FrontHitch;
            foreach (var anchor in anchors) Consider(anchor);
            if (region != null) foreach (var anchor in region.RouteAnchors) Consider(anchor);
            return closest;

            void Consider(Transform anchor)
            {
                if (anchor == null) return;
                Vector3 delta = anchor.position - hitch;
                if (delta.sqrMagnitude > limit || Vector3.Dot(driver.transform.forward, delta) < -2f) return;
                if (Physics.Linecast(hitch, anchor.position, 1 << 8, QueryTriggerInteraction.Ignore)) return;
                limit = delta.sqrMagnitude;
                closest = anchor;
            }
        }

        private void FixedUpdate()
        {
            if (driver == null || driver.Body.isKinematic || session.MenuOpen) return;
            jobs.FixedRecoveryStage(Attached);
            jobs.FixedTick(driver);
            jobs.TickCargo(driver);
            if (jobs.CargoImpact) session.Audio?.Play(RecoverySound.Impact, driver.RearHitch, session.LocalSlot, .65f);
            if (!Attached) return;
            Vector3 hitch = Hitch;
            Vector3 delta = attachedAnchor.position - hitch;
            float distance = delta.magnitude;
            if (distance > 22f) { cableBreaks++; Detach(); return; }
            if (distance < .1f) { Detach(); return; }
            Vector3 direction = delta / distance;
            Vector3 targetVelocity = attachedBody != null ? attachedBody.GetPointVelocity(attachedAnchor.position) : Vector3.zero;
            float separating = Vector3.Dot(targetVelocity - driver.Body.GetPointVelocity(hitch), direction);
            CableFrame = RecoveryWinch.Step(cableLength, distance, separating, reel, payOut,
                driver.WinchForce, driver.WinchSpeed, reelSpeed, Time.fixedDeltaTime);
            cableLength = CableFrame.Length;
            reelSpeed = CableFrame.Speed;
            float demand = RecoveryRules.CableDemand(distance, cableLength, separating);
            stress = RecoveryRules.CableStressAfter(stress, demand, driver.CableStrength, Time.fixedDeltaTime);
            if (stress >= 1) { cableBreaks++; Detach(); return; }
            CableLoad = Mathf.Clamp01(demand / driver.CableStrength);
            float force = Mathf.Min(demand, driver.CableStrength);
            CableReaction = direction * force;
            driver.Body.AddForceAtPosition(CableReaction, hitch);
            if (attachedBody != null)
            {
                attachedBody.AddForceAtPosition(-CableReaction, attachedAnchor.position);
                jobs.ApplyTowResistance(attachedAnchor, -CableReaction, CableLoad);
            }
        }

        private void LateUpdate()
        {
            if (!Attached || driver == null) return;
            Vector3 start = Hitch, end = attachedAnchor.position;
            cable.SetPosition(0, start);
            cable.SetPosition(1, Vector3.Lerp(start, end, .5f) - Vector3.up * Mathf.Lerp(.23f, .02f, CableLoad));
            cable.SetPosition(2, end);
        }

        internal void Detach()
        {
            if (!Attached && CableLoad == 0) return;
            attachedAnchor = null;
            attachedBody = null;
            NearbyAnchor = null;
            stress = reelSpeed = CableLoad = 0;
            CableFrame = default;
            CableReaction = Vector3.zero;
            reel = payOut = false;
            if (cable != null) cable.enabled = false;
        }

        private void OnDestroy() { jobs?.Dispose(); region?.Dispose(); }
    }
}
