using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private readonly int[] expeditionSelection = { -1, -1 };
        private readonly float[] expeditionScrollY = new float[2];
        private static readonly string[] winterJobLabels = { "Cargo delivery", "Vehicle recovery", "Road clearance", "Snow plowing" };
        private static readonly string[] winterJobTerrain = { "Packed road / deep forest snow", "Snowy ditch and meadow", "Icy lakeside road", "Deep drifts and packed snow" };
        private static readonly string[] winterPhotoNames =
        {
            "Pinewood supplies", "Workshop Meadow rescue", "Lakeside timber", "Pinewood plowing",
            "Pinewood weather station", "Frozen lakeshore", "High cabin"
        };
        private Texture2D[] winterBoardPhotos;

        private void BuildExpeditions()
        {
            Header(L("MISIUNI DE IARNĂ", "WINTER EXPEDITIONS"), L("Alege o misiune. Pregătește mașina. Pornește la drum.", "Pick a job. Prepare your ride. Head out."));
            Wallet(pageRoot, P.credits);
            BuildBoardTabs();

            var expedition = session.Expedition;
            var entries = new System.Collections.Generic.List<int>();
            for (int id = 0; id < WinterRoadContracts.Offers.Length + expedition.Surveys.Count; id++)
                if (WinterEntryCompleted(id) == boardCompleted) entries.Add(id);
            boardEntries = entries;
            int tab = boardCompleted ? 1 : 0;
            if (!entries.Contains(boardIndex)) boardIndex = entries.Contains(expeditionSelection[tab])
                ? expeditionSelection[tab] : entries.Count > 0 ? entries[0] : -1;
            expeditionSelection[tab] = boardIndex;

            if (winterBoardPhotos == null)
            {
                winterBoardPhotos = new Texture2D[winterPhotoNames.Length];
                for (int id = 0; id < winterBoardPhotos.Length; id++)
                    winterBoardPhotos[id] = Resources.Load<Texture2D>("WinterBoard/" + winterPhotoNames[id]);
            }

            var position = session.World.Drivers[Mathf.Max(0, session.LocalSlot)].transform.position;
            if (boardCompleted || entries.Count > 3)
                BuildBoardScroll(entries, (parent, id, scroll) => WinterCard(parent, id, 0, 0, scroll));
            else
                for (int row = 0; row < entries.Count; row++) WinterCard(pageRoot, entries[row], 76, 232 + row * 158, null);

            if (entries.Count == 0)
                Text(pageRoot, boardCompleted ? "No completed expeditions yet." : "All expeditions are completed. Replay them in Completed.",
                    76, 300, 580, 140, 23, Muted, true);
            var detail = Panel(pageRoot, 690, 232, 834, 458);
            detail.name = "Recovery details";
            TMPro.TMP_Text description = null;
            if (boardIndex >= 0)
            {
                var entry = WinterEntry(boardIndex);
                Text(detail, entry.Name, 30, 17, 774, 50, 33, Paper, true);
                ContractPhoto(detail, WinterPhoto(boardIndex), 30, 80, 774, 178);
                description = Text(detail, entry.Brief + (entry.Terrain == null ? "" : "\n" + L("Teren: ", "Terrain: ") + entry.Terrain),
                    30, 274, 482, 93, 18, Paper);
                BuildBoardReward(detail, boardCompleted ? "REPLAY REWARD" : "REWARD", entry.Reward);
                if (!boardCompleted && boardIndex == (int)WinterJob.Supply)
                    Text(detail, "Bonus for delivering the crate intact", 30, 411, 774, 36, 16, Muted);
            }
            else Text(detail, boardCompleted ? "Finished expeditions will appear here for replay." : "Choose an expedition from Completed to replay.",
                30, 30, 774, 100, 25, Paper, true);

            int active = expedition.ActiveJob is WinterJob job ? (int)job :
                expedition.Selected >= 0 ? WinterRoadContracts.Offers.Length + expedition.Selected : -1;
            string status = session.World.HasContract ? "Finish or abandon the active recovery to start an expedition." :
                expedition.Attached ? "Detach the winch before changing expeditions." :
                active >= 0 ? "IN PROGRESS: " + WinterEntry(active).Name : "No active expedition. Choose where we go next.";
            string action = IsActiveSurvey(boardIndex) ? "IN PROGRESS" : active >= 0 && boardIndex == active ? "RESTART JOB" :
                boardCompleted ? "REPLAY EXPEDITION" : boardIndex >= WinterRoadContracts.Offers.Length ? "START SURVEY" : "ACCEPT JOB";
            BuildBoardActions(entries.Count + (boardCompleted ? " completed expeditions" : " available expeditions"), status, active >= 0, action);
            FitNewPanelText();
            if (description != null) description.fontSizeMin = 14f;
            RefreshBoardHints();

            void WinterCard(Transform parent, int id, float x, float y, UnityEngine.UI.ScrollRect scroll)
            {
                var entry = WinterEntry(id);
                BuildBoardCard(parent, id, x, y, scroll, entry.Name, entry.Label,
                    Vector3.Distance(position, entry.Point), entry.Reward, WinterPhoto(id));
            }
        }

        private Texture2D WinterPhoto(int id) => id < winterBoardPhotos.Length ? winterBoardPhotos[id] : null;

        private bool WinterEntryCompleted(int id) => id < WinterRoadContracts.Offers.Length
            ? (P.winterJobs & (1 << id)) != 0 : (P.winterSurveys & (1 << (id - WinterRoadContracts.Offers.Length))) != 0;

        private bool IsActiveSurvey(int id) => id >= WinterRoadContracts.Offers.Length &&
            session.Expedition.Selected == id - WinterRoadContracts.Offers.Length;

        private (string Name, string Brief, string Label, string Terrain, Vector3 Point, int Reward) WinterEntry(int id)
        {
            bool completed = WinterEntryCompleted(id);
            if (id < WinterRoadContracts.Offers.Length)
            {
                var offer = WinterRoadContracts.Offers[id];
                return (offer.Name, offer.Brief, winterJobLabels[id], winterJobTerrain[id],
                    session.Expedition.JobStart((WinterJob)id), completed ? 0 : offer.Reward);
            }
            var survey = session.Expedition.Surveys[id - WinterRoadContracts.Offers.Length];
            return (survey.Name, survey.Brief, "Field survey", null, survey.Point, completed ? 0 : survey.Reward);
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
                bool localWork = plowing || expedition.OnShortcut || expedition.ShowingPickupExit ||
                    expedition.ShowingPullPoint || expedition.Reconnecting || expedition.ExtractingPickup ||
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
            string brake = session.UsingGamepad ? RecoveryInputPrompts.CancelLabel(family) : P.ReadKey(P.brakeKey, Key.Space).ToString();
            if (expedition.Attached) return;
            if (expedition.CanFitPlow)
                ShowExploreHint(hook, "Fit the plow blade while stopped");
            else if (expedition.PlowFitted)
                ShowExploreHint(hook, expedition.BladeLowered ? "Raise blade" : "Lower blade",
                    expedition.BladeLoad > .55f ? "Heavy snow · keep a steady pace" : null);
            else if (expedition.NearTowTarget)
                ShowExploreHint(hook, expedition.TowHintUsesRear ? "Attach rear cable" : "Attach front cable",
                    expedition.ActiveJob == WinterJob.Tree ? expedition.NearbyTowName : null);
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

        private void BuildWinterResults()
        {
            if (session.Expedition.Result is not { } result) return;
            string objective = result.Job switch
            {
                WinterJob.Supply => L("Ladă livrată", "Crate delivered"),
                WinterJob.Recovery => L("Mașină livrată", "Vehicle delivered"),
                WinterJob.Tree => L("Banda este liberă", "Lane cleared"),
                WinterJob.Plow => L("Drum deszăpezit", "Road plowed"),
                _ => L("Date înregistrate", "Survey logged")
            };
            string breakdown = result.Reward == 0 ? L("Deja recompensată", "Already rewarded") :
                L("Bază  ", "Base  ") + (result.Reward - result.Bonus).ToString("N0") + " CR";
            if (result.Bonus > 0)
                breakdown += L("   +   Bonus ladă intactă  ", "   +   Intact crate bonus  ") + result.Bonus.ToString("N0") + " CR";
            else if (result.Job == WinterJob.Supply)
                breakdown += result.CargoIntact ? L("  ·  Ladă intactă", "  ·  Crate intact") : L("  ·  Ladă deteriorată", "  ·  Crate damaged");
            string nextStep = result.Job switch
            {
                WinterJob.Supply => L("Livrată la Pinewood", "Delivered to Pinewood"),
                WinterJob.Recovery => L("Livrată la atelier", "Delivered to the workshop"),
                WinterJob.Tree => L("Traseu redeschis la Lakeside", "Lakeside Road reopened"),
                WinterJob.Plow => L("Traseu redeschis la Pinewood", "Pinewood Road reopened"),
                _ => L("Raport predat la bază", "Report filed at expedition base")
            };
            BuildResultsSummary(new ResultSummary
            {
                Title = result.Name,
                Heading = L("EXPEDIȚIE ÎNCHEIATĂ", "EXPEDITION COMPLETE"),
                MedalTitle = L("Finalizată", "Completed"),
                MedalDetail = objective,
                Elapsed = result.Elapsed,
                Resets = result.Resets,
                Snaps = result.CableBreaks,
                Reward = result.Reward,
                Balance = P.credits,
                Breakdown = breakdown,
                NextStep = nextStep,
                FreeRoam = true,
                Host = true,
                ContinueExploring = ContinueWinterResults
            });
        }

        private void ContinueWinterResults()
        {
            session.ConsumeMenuInput();
            session.Expedition.DismissResult();
            session.CloseOverlay();
            Show("Gameplay");
        }
    }
}
