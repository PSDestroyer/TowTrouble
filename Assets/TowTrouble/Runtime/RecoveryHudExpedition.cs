using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private bool journalSurveys;

        private void BuildExpeditions()
        {
            var expedition = session.Expedition;
            Panel(pageRoot, 53, 54, 1090, 741);
            Text(pageRoot, "EXPEDITION JOURNAL", 90, 78, 960, 64, 44, Paper, true);
            Button(pageRoot, "WINTER JOBS", 90, 152, 240, () => OpenJournalTab(false), !journalSurveys, 45);
            Button(pageRoot, "FIELD SURVEYS", 342, 152, 240, () => OpenJournalTab(true), journalSurveys, 45);
            bool classicRecovery = session.World.HasContract;
            if (classicRecovery)
                Text(pageRoot, "Finish or abandon the active recovery to start an expedition.", 600, 146, 490, 57, 18, Muted);

            int count = journalSurveys ? expedition.Surveys.Count : WinterRoadContracts.Offers.Length;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                var job = (WinterJob)i;
                var offer = journalSurveys ? default : WinterRoadContracts.Offers[i];
                var survey = journalSurveys ? expedition.Surveys[i] : default;
                bool logged = ((journalSurveys ? P.winterSurveys : P.winterJobs) & (1 << i)) != 0;
                bool selected = journalSurveys ? expedition.Selected == i : expedition.ActiveJob == job;
                var card = Panel(pageRoot, 88, 216 + i * 116, 1018, 104);
                Text(card, journalSurveys ? survey.Name : offer.Name, 20, 10, 666, 33, 26, Paper, true);
                Text(card, journalSurveys ? survey.Brief : offer.Brief, 20, 50, 666, 48, 19, Muted);
                string label = selected ? (journalSurveys ? "IN PROGRESS" : "RESTART JOB") :
                    logged ? "REVISIT" : journalSurveys ? "START SURVEY" : "ACCEPT JOB";
                Button(card, label, 724, 42, 270, () =>
                {
                    session.ConsumeMenuInput();
                    if (journalSurveys) expedition.Depart(index);
                    else expedition.Accept(job);
                    Show("Gameplay");
                }, !logged, 46).interactable = !classicRecovery && !expedition.Attached && !(journalSurveys && selected);
                Text(card, selected ? "ACTIVE" : logged ? "COMPLETE" : (journalSurveys ? survey.Reward : offer.Reward).ToString("N0") + " CR",
                    726, 10, 268, 26, 16, Amber, true);
            }

            Button(pageRoot, "BACK TO DRIVING", 90, 690, 320, MenuBack);
            if (expedition.Attached) Text(pageRoot, "Detach the winch before changing expeditions.", 434, 700, 650, 30, 17, Muted);
            BuildMenuFooter();
        }

        private void OpenJournalTab(bool surveys)
        {
            if (journalSurveys == surveys) return;
            session.ConsumeMenuInput();
            journalSurveys = surveys;
            Show("Expeditions");
        }

        private void BuildExpeditionHelp()
        {
            Header("DRIVING IN WINTER", "Find local work and surveys in the expedition journal.");
            var card = Panel(pageRoot, 80, 225, 1030, 485);
            Text(card, "Choose a job or survey", 25, 20, 960, 37, 25, Paper, true);
            Text(card, "Open the expedition journal with " + ContractControl() + ". Stop in the marked loading zones for cargo. Use the hook control for towing and fitting the plow.", 25, 65, 966, 58, 20, Muted);
            Text(card, "Read the snow", 25, 144, 960, 37, 25, Paper, true);
            Text(card, "Deep ruts pull the steering along the track. Turn firmly to climb out. Spinning in powder digs you in. Brake before stopping on a slope.", 25, 192, 966, 61, 20, Muted);
            Text(card, "Winch yourself uphill", 25, 272, 960, 37, 25, Paper, true);
            Text(card, "Face a yellow anchor post. Use the hook control to attach, then hold reel in and steer gently. Detach before stopping to survey.", 25, 321, 966, 65, 20, Muted);
            Button(pageRoot, "BACK", 80, 750, 280, MenuBack);
            BuildMenuFooter();
        }

        private void RefreshExpedition(RecoveryWorld world, int slot)
        {
            var expedition = session.Expedition;
            var driver = world.Drivers[slot];
            var camera = Camera.main;
            marker.gameObject.SetActive(false);
            foreach (var tag in hudPlates) tag.gameObject.SetActive(false);
            exploreHintWanted = false;
            if (expedition.Active && camera != null)
            {
                var destination = expedition.GuidancePoint;
                var separation = destination - driver.transform.position;
                bool near = expedition.NearDestination(driver.transform.position);
                var job = expedition.ActiveJob;
                SetMissionStatus(job.HasValue ? WinterRoadContracts.Offers[(int)job.Value].Name.ToUpperInvariant() :
                    expedition.Returning ? "RETURN TO BASE" : "FIELD SURVEY", expedition.Instruction(near));
                bool plowing = expedition.PlowFitted;
                bool localWork = plowing || expedition.ShowingPullPoint || expedition.Reconnecting || expedition.ExtractingPickup ||
                    job == WinterJob.Tree && expedition.DeliveryLeg ||
                    near && (job == WinterJob.Supply || job == WinterJob.Recovery);
                bool delivery = expedition.DeliveryLeg && !expedition.Reconnecting;
                string guidance = localWork ? null : FollowRoad(world, camera, driver.transform, destination, delivery);
                if (localWork) HideRoadTrail();
                SetGuidanceNote(guidance);
                string action = near && job == WinterJob.Supply ? "Park inside the ring" :
                    near && job == WinterJob.Plow && !plowing ? "Stop to fit blade" :
                    expedition.MissedPlowSection ? "Snow remaining" :
                    near && !job.HasValue ? "Stop here" : null;
                string caption = action ?? (Mathf.RoundToInt(localWork ? separation.magnitude : Mathf.Max(separation.magnitude, sampledRoadLength)) + " m");
                SetWaypointContent(expedition.GuidanceName, caption, delivery, false, action != null,
                    expedition.Returning ? RecoveryIcon.Garage : job == WinterJob.Tree ? RecoveryIcon.Tree :
                    job == WinterJob.Recovery ? RecoveryIcon.WaypointCar : job == WinterJob.Plow ? RecoveryIcon.Bumper : RecoveryIcon.Flag,
                    action);
                ProjectWaypoint(camera, destination + Vector3.up * (expedition.NearTowTarget || expedition.Reconnecting ? .65f : 2.2f), delivery);
            }
            else
            {
                ClearRecoveryGuidance();
                SetMissionStatus("WINTER EXPEDITIONS", ContractControl() + " · Expedition journal");
                SetGuidanceNote(expedition.Report);
            }

            var family = RecoveryInputPrompts.Family(RecoveryInputDevices.Pad);
            string hook = session.UsingGamepad ? RecoveryInputPrompts.ConfirmLabel(family) : P.ReadKey(P.hookKey, Key.F).ToString();
            string reel = session.UsingGamepad ? RecoveryInputPrompts.PreviousTabLabel(family) : P.ReadKey(P.reelKey, Key.Q).ToString();
            string brake = session.UsingGamepad ? RecoveryInputPrompts.CancelLabel(family) : P.ReadKey(P.brakeKey, Key.Space).ToString();
            if (expedition.TowingLoad)
            {
                string load = Mathf.RoundToInt(expedition.CableLoad * 100) + "% load";
                ShowExploreHint(reel, "Reel in · " + load, expedition.CableLoad > .78f ? "Ease off the throttle" :
                    (expedition.UsingRearCable ? "Rear cable" : "Front cable") + " · " + hook + " to detach");
            }
            else if (expedition.Attached)
                ShowExploreHint(reel, "Reel in · " + Mathf.RoundToInt(expedition.CableLoad * 100) + "% load", hook + " · Detach");
            else if (expedition.CanFitPlow)
                ShowExploreHint(hook, "Fit the plow blade while stopped");
            else if (expedition.PlowFitted)
                ShowExploreHint("", expedition.DestinationName);
            else if (expedition.NearTowTarget)
                ShowExploreHint(hook, expedition.TowHintUsesRear ? "Attach rear cable" : "Attach front cable");
            else if (expedition.ActiveJob == WinterJob.Supply && expedition.NearDestination(driver.transform.position))
                ShowExploreHint(brake, expedition.HasCargo ? "Hold brake to unload" : "Hold brake to load");
            else if (expedition.HasCargo)
                ShowExploreHint("", "Cargo secured · 1 crate");
            else if (expedition.NearbyAnchor != null)
                ShowExploreHint(hook, "Attach to anchor");
            else if (driver.SnowSink >= .11f && driver.Speed > .15f)
                ShowExploreHint("", "Deep snow · " + Mathf.RoundToInt(driver.SnowSink * 100) + " cm");
            else if (!RefreshUrgentHint(world, slot))
                RefreshExploreHint(world, slot);
        }
    }
}
