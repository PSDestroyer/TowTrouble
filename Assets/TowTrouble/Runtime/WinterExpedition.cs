using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowTrouble
{
    [DisallowMultipleComponent]
    public sealed class WinterExpedition : MonoBehaviour
    {
        [Serializable]
        public struct Survey
        {
            public string Name;
            public string Brief;
            public Vector3 Point;
            public int Reward;
        }

        [SerializeField] private Survey[] surveys;
        [SerializeField] private Transform[] anchors;
        [SerializeField] private LineRenderer cable;

        private DemoSession session;
        private RecoveryVehicle driver;
        private Transform attachedAnchor;
        private Rigidbody attachedBody;
        private bool rearCable;
        private WinterRoadContracts jobs;
        private float holdSeconds, cableLength, reelSpeed, stress, nextAnchorCheck;
        private bool reel, payOut;
        public int Selected { get; private set; } = -1;
        public bool Returning { get; private set; }
        public string Report { get; private set; }
        public bool Attached => attachedAnchor != null;
        public Transform NearbyAnchor { get; private set; }
        public float CableLoad { get; private set; }
        internal Vector3 CableReaction { get; private set; }
        public IReadOnlyList<Survey> Surveys => surveys;
        public bool Active => Selected >= 0 || ActiveJob.HasValue;
        internal WinterJob? ActiveJob => jobs?.Active;
        internal bool DeliveryLeg => Returning || jobs?.DeliveryLeg == true;
        internal bool HasCargo => jobs?.CargoLoaded == true;
        internal bool NearTowTarget { get; private set; }
        internal bool TowHintUsesRear { get; private set; }
        internal bool TowingLoad => attachedBody != null;
        internal bool UsingRearCable => Attached && rearCable;
        internal bool Reconnecting => jobs?.Reconnecting(Attached) == true;
        internal bool ShowingEntry => jobs?.ApproachingEntry == true;
        internal bool ShowingPullPoint => !ShowingEntry && driver != null && jobs?.MarkingPullPoint(driver.transform.position) == true &&
            (!NearTowTarget || !jobs.AtPullPoint(driver.transform.position) ||
            ActiveJob == WinterJob.Recovery && !TowHintUsesRear);
        internal bool MissedPlowSection => driver != null && jobs?.MissedPlowSection(driver.FrontHitch) == true;
        internal bool ExtractingPickup => TowingLoad && jobs?.ExtractingPickup == true;
        internal Vector3 GuidancePoint => ShowingEntry ? jobs.EntryPoint : ShowingPullPoint || ExtractingPickup ? jobs.PullPoint :
            (Reconnecting || NearTowTarget) && jobs?.TowEye != null ? jobs.TowEye.position : Destination;
        internal string GuidanceName => ShowingEntry ? (ActiveJob == WinterJob.Tree ? "Lay-by approach" : "Meadow entrance") :
            ShowingPullPoint || ExtractingPickup ? "Marked pull point" :
            Reconnecting ? "Reconnect the cable" : NearTowTarget ? "Tow eye" : DestinationName;
        internal bool CanFitPlow => driver != null && jobs?.AtPlowStart(driver.transform.position) == true;
        internal bool PlowFitted => jobs?.PlowFitted == true;
        public Vector3 Destination => ActiveJob.HasValue ? jobs.Destination :
            Returning ? session.World.GaragePosition : surveys[Selected].Point;
        public string DestinationName => ActiveJob.HasValue ? jobs.DestinationName :
            Returning ? "Expedition base" : surveys[Selected].Name;
        internal float DriftAt(Vector3 point) => jobs?.DriftAt(point) ?? 0;

        public void Configure(Survey[] sites, Transform[] winchAnchors, LineRenderer line)
        {
            surveys = sites;
            anchors = winchAnchors;
            cable = line;
        }

        internal void Bind(DemoSession owner)
        {
            session = owner;
            jobs = new WinterRoadContracts(owner.World, transform);
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

        public void Depart(int index)
        {
            if (!session.ReadyForPresentation || !session.BoardOpen || session.World.Phase != RecoveryPhase.Explore ||
                index < 0 || index >= surveys.Length || Attached) return;
            jobs.Clear();
            Selected = index;
            Returning = false;
            holdSeconds = 0;
            Report = null;
            session.CloseOverlay();
        }

        internal void Accept(WinterJob job)
        {
            if (!session.ReadyForPresentation || !session.BoardOpen || session.World.Phase != RecoveryPhase.Explore || Attached) return;
            Selected = -1;
            Returning = false;
            holdSeconds = 0;
            Report = null;
            jobs.Begin(job);
            session.CloseOverlay();
        }

        internal string Instruction(bool near)
        {
            if (ActiveJob.HasValue && driver != null)
                return jobs.Instruction(driver, near, Attached, Attached ? rearCable : TowHintUsesRear, NearTowTarget);
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
            if (session.MenuOpen) return;
            if (Time.time >= nextAnchorCheck)
            {
                nextAnchorCheck = Time.time + .18f;
                bool useRear = false;
                NearTowTarget = !Attached && TryTowHitch(out useRear);
                TowHintUsesRear = useRear;
                NearbyAnchor = Attached || NearTowTarget ? null : FindAnchor();
            }
            if (ActiveJob is WinterJob job)
            {
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
            Report = first ? $"Survey logged · +{survey.Reward:N0} CR" : "Survey logged";
            Returning = false;
            Selected = -1;
        }

        private void CompleteJob(WinterJob job)
        {
            Detach();
            var offer = WinterRoadContracts.Offers[(int)job];
            var profile = session.Profile;
            int mask = 1 << (int)job;
            bool first = (profile.winterJobs & mask) == 0;
            if (first)
            {
                profile.winterJobs |= mask;
                profile.credits = Mathf.Min(1000000000, profile.credits + offer.Reward);
                profile.Save();
            }
            Report = first ? $"{offer.Name} complete · +{offer.Reward:N0} CR" : $"{offer.Name} complete";
            jobs.Finish(job);
            NearTowTarget = false;
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
            if (TryTowHitch(out rearCable))
            {
                attachedAnchor = jobs.TowEye;
                attachedBody = jobs.TowBody;
                jobs.AttachedTarget();
            }
            else
            {
                rearCable = false;
                attachedAnchor = FindAnchor();
            }
            if (!Attached) return;
            cableLength = Mathf.Max(RecoveryWinch.MinimumLength, Vector3.Distance(Hitch, attachedAnchor.position));
            cable.enabled = true;
        }

        private Vector3 Hitch => rearCable ? driver.RearHitch : driver.FrontHitch;

        private bool TryTowHitch(out bool rear)
        {
            rear = false;
            if (jobs?.TowEye == null) return false;
            var eye = jobs.TowEye.position;
            var front = (eye - driver.FrontHitch).sqrMagnitude;
            var back = (eye - driver.RearHitch).sqrMagnitude;
            rear = back < front;
            return Mathf.Min(front, back) < 16f * 16f &&
                !Physics.Linecast(rear ? driver.RearHitch : driver.FrontHitch, eye, 1 << 8, QueryTriggerInteraction.Ignore);
        }

        private Transform FindAnchor()
        {
            Transform closest = null;
            float limit = 18f * 18f;
            Vector3 hitch = driver.FrontHitch;
            foreach (var anchor in anchors)
            {
                Vector3 delta = anchor.position - hitch;
                if (delta.sqrMagnitude > limit || Vector3.Dot(driver.transform.forward, delta) < -2f) continue;
                if (Physics.Linecast(hitch, anchor.position, 1 << 8, QueryTriggerInteraction.Ignore)) continue;
                limit = delta.sqrMagnitude;
                closest = anchor;
            }
            return closest;
        }

        private void FixedUpdate()
        {
            if (driver == null || driver.Body.isKinematic || session.MenuOpen) return;
            jobs.FixedTick(driver);
            if (!Attached) return;
            Vector3 hitch = Hitch;
            Vector3 delta = attachedAnchor.position - hitch;
            float distance = delta.magnitude;
            if (distance > 22f || distance < .1f) { Detach(); return; }
            Vector3 direction = delta / distance;
            Vector3 targetVelocity = attachedBody != null ? attachedBody.GetPointVelocity(attachedAnchor.position) : Vector3.zero;
            float separating = Vector3.Dot(targetVelocity - driver.Body.GetPointVelocity(hitch), direction);
            var frame = RecoveryWinch.Step(cableLength, distance, separating, reel, payOut,
                driver.WinchForce, driver.WinchSpeed, reelSpeed, Time.fixedDeltaTime);
            cableLength = frame.Length;
            reelSpeed = frame.Speed;
            float demand = RecoveryRules.CableDemand(distance, cableLength, separating);
            stress = RecoveryRules.CableStressAfter(stress, demand, driver.CableStrength, Time.fixedDeltaTime);
            if (stress >= 1) { Detach(); return; }
            CableLoad = Mathf.Clamp01(demand / driver.CableStrength);
            float force = Mathf.Min(demand, driver.CableStrength);
            CableReaction = direction * force;
            driver.Body.AddForceAtPosition(CableReaction, hitch);
            if (attachedBody != null) attachedBody.AddForceAtPosition(-CableReaction, attachedAnchor.position);
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
            CableReaction = Vector3.zero;
            reel = payOut = false;
            if (cable != null) cable.enabled = false;
        }

        private void OnDestroy() => jobs?.Dispose();
    }
}
