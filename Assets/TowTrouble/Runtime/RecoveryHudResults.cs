using System;
using TMPro;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        // A presentation snapshot also lets Editor previews render without claiming rewards.
        private sealed class ResultSummary
        {
            public string Title, Destination, Crew;
            public float Elapsed;
            public int Resets, Snaps, Reward, Balance, BaseReward, ContractIndex;
            public bool FreeRoam, Cooperative, Host, NewRecovery;
        }

        private void BuildResults()
        {
            session.RecordCompletion();
            var world = session.World;
            BuildResultsSummary(new ResultSummary {
                Title = world.Contract.TitleFor(P),
                Destination = world.Contract.IsOpenWorld ? world.Contract.DeliveryEnglish : "the drop-off",
                Elapsed = world.Elapsed, Resets = world.Resets(0) + world.Resets(1), Snaps = world.Snaps,
                Reward = session.LastReward, Balance = P.credits, BaseReward = world.Contract.Reward,
                ContractIndex = world.ContractIndex, FreeRoam = world.FreeRoam, Cooperative = world.Cooperative,
                Host = session.IsHost, NewRecovery = world.FreeRoam && newRecoveryAvailable,
                Crew = world.Cooperative ? world.DriverName(0) + " / " + world.DriverName(1) + "  ·  " +
                    Mathf.RoundToInt(world.PullSeconds(0)) + "s / " + Mathf.RoundToInt(world.PullSeconds(1)) + "s pulling" : null
            });
        }

        private void BuildResultsSummary(ResultSummary result)
        {
            float actionY = result.Cooperative ? 548f : 504f;
            float height = actionY + (result.FreeRoam ? 102f : 180f);
            var card = Panel(pageRoot, 380, (900 - height) * .5f, 840, height);
            card.name = "Recovery result";
            Icon(card, RecoveryIcon.FinishFlag, 32, 26, 30, Amber);
            Text(card, L("RECUPERARE ÎNCHEIATĂ", "RECOVERY COMPLETE"), 76, 22, 732, 36, 23, Amber, true);
            Text(card, result.Title, 32, 66, 776, 45, 30, Paper, true);

            bool noResets = result.Resets == 0, noSnaps = result.Snaps == 0;
            ResultMedal(card, 32, L("Recuperată", "Recovered"), L("Mașină livrată", "Vehicle delivered"), true);
            ResultMedal(card, 296, L("Fără resetări", "No resets"), noResets ? L("+10% bonus", "+10% bonus") : L("Bonus ratat", "Bonus missed"), noResets);
            ResultMedal(card, 560, L("Cablu intact", "No cable breaks"), noSnaps ? L("+10% bonus", "+10% bonus") : L("Bonus ratat", "Bonus missed"), noSnaps);

            var payout = Panel(card, 32, 232, 494, 164);
            payout.name = "Recovery payout";
            payout.GetComponentInChildren<RecoveryPlateGraphic>().Style = RecoveryPlateStyle.Paper;
            Text(payout, L("RECOMPENSA TA", "YOUR PAYOUT"), 20, 14, 454, 27, 14, Ink, true);
            Icon(payout, RecoveryIcon.Coins, 20, 51, 40, Ink);
            Text(payout, "+" + result.Reward.ToString("N0") + " CR", 74, 40, 400, 70, 44, Ink, true);
            Text(payout, L("Sold  ", "Balance  ") + result.Balance.ToString("N0") + " CR", 20, 117, 454, 29, 17, Ink);

            var stats = ResultInset(card, "Recovery statistics", 542, 232, 266, 164);
            ResultStat(stats, 10, L("Timp", "Time"), TimeSpan.FromSeconds(Mathf.Max(0, result.Elapsed)).ToString(@"mm\:ss"));
            ResultStat(stats, 61, L("Resetări", "Resets"), result.Resets.ToString());
            ResultStat(stats, 112, L("Cabluri rupte", "Cable breaks"), result.Snaps.ToString());
            Panel(stats, 16, 57, 234, 1, new Color(.79f, .79f, .72f, .15f));
            Panel(stats, 16, 108, 234, 1, new Color(.79f, .79f, .72f, .15f));

            bool knownPayout = result.Reward == RecoveryEconomy.Payout(result.BaseReward, noResets, noSnaps, false)
                || result.Reward == RecoveryEconomy.Payout(result.BaseReward, noResets, noSnaps, true);
            string breakdown = knownPayout ? L("Bază  ", "Base  ") + result.BaseReward.ToString("N0") + " CR     +     " +
                L("Bonusuri  ", "Bonuses  ") + (result.Reward - result.BaseReward).ToString("N0") + " CR" : L("Recuperare încheiată", "Recovery finished");
            // Detect the awarded first-recovery bonus from the real payout, without changing economy state.
            bool firstBonus = knownPayout && result.Reward == RecoveryEconomy.Payout(result.BaseReward, noResets, noSnaps, true);
            if (firstBonus) breakdown += L("  ·  include +5% prima recuperare", "  ·  includes +5% first recovery");
            Text(card, breakdown, 36, 407, 768, 31, 15, Muted);

            var next = ResultInset(card, "Recovery next step", 32, 448, 776, 36);
            Icon(next, result.NewRecovery ? RecoveryIcon.Flag : RecoveryIcon.Check, 10, 8, 20, Amber);
            Text(next, result.NewRecovery ? L("O recuperare nouă este disponibilă", "New recovery available")
                : L("Livrată la ", "Delivered to ") + result.Destination, 40, 0, 724, 36, 16, Paper).alignment = TextAlignmentOptions.MidlineLeft;
            if (result.Cooperative)
            {
                Icon(card, RecoveryIcon.Crew, 36, 499, 22, Muted);
                Text(card, result.Crew, 70, 492, 734, 42, 14, Muted);
            }

            if (result.FreeRoam)
                Button(card, L("CONTINUĂ EXPLORAREA", "CONTINUE EXPLORING"), 32, actionY, 776,
                    () => { session.FinishExplorationContract(); Show("Gameplay"); }, true, 56).interactable = result.Host;
            else
            {
                Button(card, L("URMĂTORUL CONTRACT", "NEXT CONTRACT"), 32, actionY, 776,
                    () => { session.Replay(); session.SelectContract(Mathf.Min(2, result.ContractIndex + 1)); Show("Contracts"); }, true, 54).interactable = result.Host;
                Button(card, L("REJOACĂ", "REPLAY"), 32, actionY + 66, 380,
                    () => { session.RestartContract(); Show("Lobby"); }, false, 40).interactable = result.Host;
                Button(card, L("CONTRACTE", "CONTRACTS"), 428, actionY + 66, 380,
                    () => { session.Replay(); Show("Contracts"); }, false, 40).interactable = result.Host;
                Button(card, L("VEHICULE", "VEHICLES"), 32, actionY + 116, 776,
                    () => { session.Replay(); returnPage = "Lobby"; Show("Vehicles"); }, false, 40).interactable = result.Host;
            }
            if (result.FreeRoam)
            {
                string footer = result.Host ? (session.UsingGamepad ? "A" : "Enter") + L(" · Continuă", " · Continue")
                    : L("Așteptăm gazda să continue", "Waiting for the host to continue");
                var hint = Text(card, footer, 32, actionY + 66, 776, 25, 13, Muted);
                hint.alignment = TextAlignmentOptions.Center;
            }
        }

        private RectTransform ResultInset(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = Node(name, parent, x, y, width, height);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.045f, .06f, .075f, .65f);
            image.raycastTarget = false;
            return rect;
        }

        private void ResultMedal(Transform parent, float x, string title, string detail, bool earned)
        {
            var tile = ResultInset(parent, title, x, 118, 248, 96);
            Panel(tile, 0, 0, 248, 2, earned ? Amber : new Color(.35f, .37f, .36f, .5f));
            var star = Node("Recovery badge", tile, 14, 24, 44, 44).gameObject.AddComponent<RecoveryMedalGraphic>();
            star.raycastTarget = false;
            star.color = earned ? Amber : new Color(.36f, .39f, .38f);
            Text(tile, title, 72, 16, 164, 32, 16, earned ? Paper : Muted, true);
            Text(tile, detail, 72, 50, 164, 28, 13, earned ? Amber : Muted);
        }

        private void ResultStat(Transform parent, float top, string label, string value)
        {
            Text(parent, label, 16, top, 136, 40, 14, Muted).alignment = TextAlignmentOptions.MidlineLeft;
            Text(parent, value, 150, top, 100, 40, 22, Paper, true).alignment = TextAlignmentOptions.MidlineRight;
        }
    }
}
