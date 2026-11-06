using TMPro;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private RectTransform missionStatusPanel, missionStatusDivider;
        private RecoveryIconGraphic missionStatusIcon;
        private TMP_Text missionStatusTitle, missionStatusAction;
        private float missionStatusScale;

        private void BuildMissionStatus()
        {
            missionStatusPanel = Panel(gameplay.transform, 28, 22, 340, 62);
            missionStatusPanel.name = "Mission status";
            missionStatusIcon = Icon(missionStatusPanel, RecoveryIcon.Flag, 14, 13, 36, Amber);
            missionStatusDivider = Panel(missionStatusPanel, 56, 12, 1, 38, new Color(.79f, .79f, .72f, .18f));
            missionStatusTitle = Text(missionStatusPanel, "", 64, 7, 264, 20, 14, Muted);
            missionStatusTitle.alignment = TextAlignmentOptions.MidlineLeft;
            missionStatusTitle.textWrappingMode = TextWrappingModes.NoWrap;
            missionStatusAction = Text(missionStatusPanel, "", 64, 27, 264, 28, 18, Paper, true);
            missionStatusAction.alignment = TextAlignmentOptions.MidlineLeft;
            SetMissionStatus("NO ACTIVE RECOVERY", ContractControl() + " · Choose a recovery");
        }

        private void RefreshMissionStatus(RecoveryWorld world, int slot, bool atSite)
        {
            bool delivery = world.Phase == RecoveryPhase.Deliver;
            bool inside = delivery && world.Contract.ContainsDelivery(world.Casualty.transform.position);
            // In co-op a cable from either player can already be doing the towing.
            bool towing = HasTowConnection(world, slot);
            if (NeedsDeliveryReconnect(world, slot))
                atSite = Vector3.Distance(world.Drivers[slot].RearHitch, world.TowPoint) <= RecoveryRules.AttachRange;
            string instruction = MissionInstruction(world.Phase, atSite, towing,
                world.HasContract && world.ClearingObstacle, inside,
                delivery ? world.Casualty.Speed : 0, session.IsHost, ContractControl());
            string title = world.HasContract ? world.Contract.TitleFor(P)
                : newRecoveryAvailable ? "NEW RECOVERY AVAILABLE" : "NO ACTIVE RECOVERY";
            var icon = world.Phase == RecoveryPhase.Explore ? RecoveryIcon.Flag
                : world.ClearingObstacle ? RecoveryIcon.Tree
                : NeedsDeliveryReconnect(world, slot) ? atSite ? RecoveryIcon.Hook : RecoveryIcon.WaypointCar
                : delivery ? RecoveryIcon.FinishFlag
                : towing ? RecoveryIcon.Winch : atSite ? RecoveryIcon.Hook : RecoveryIcon.WaypointCar;
            if (missionStatusIcon.Kind != icon)
            { missionStatusIcon.Kind = icon; missionStatusIcon.SetVerticesDirty(); }
            SetMissionStatus(title, instruction);
        }

        private static bool HasTowConnection(RecoveryWorld world, int slot)
            => world.HasContract && (world.Attached(slot) || world.Cooperative && world.Attached(slot == 0 ? 1 : 0));

        private static bool NeedsDeliveryReconnect(RecoveryWorld world, int slot)
            => world.Phase == RecoveryPhase.Deliver && DeliveryNeedsReconnect(world.Phase,
                HasTowConnection(world, slot), world.Contract.ContainsDelivery(world.Casualty.transform.position));

        public static bool DeliveryNeedsReconnect(RecoveryPhase phase, bool towing, bool insideDelivery)
            => phase == RecoveryPhase.Deliver && !towing && !insideDelivery;

        private void SetMissionStatus(string title, string action)
        {
            float scale = Mathf.Max(1f, P.textScale);
            if (missionStatusTitle.text == title && missionStatusAction.text == action &&
                Mathf.Approximately(missionStatusScale, scale)) return;
            missionStatusTitle.text = title;
            missionStatusAction.text = action;
            missionStatusScale = scale;
            float preferred = Mathf.Max(missionStatusTitle.GetPreferredValues(title).x,
                missionStatusAction.GetPreferredValues(action).x);
            float width = Mathf.Clamp(Mathf.Ceil(preferred) + 76f * scale, 260f * scale, 440f * scale);
            float textWidth = width - 76f * scale;
            float actionHeight = Mathf.Max(26f * scale,
                Mathf.Ceil(missionStatusAction.GetPreferredValues(action, textWidth, 1000f).y) + 2f * scale);
            float height = 27f * scale + actionHeight + 8f * scale;
            missionStatusPanel.sizeDelta = new Vector2(width, height);
            missionStatusTitle.rectTransform.anchoredPosition = new Vector2(64, -7) * scale;
            missionStatusTitle.rectTransform.sizeDelta = new Vector2(textWidth, 20f * scale);
            missionStatusAction.rectTransform.anchoredPosition = new Vector2(64, -27) * scale;
            missionStatusAction.rectTransform.sizeDelta = new Vector2(textWidth, actionHeight);
            missionStatusIcon.rectTransform.anchoredPosition = new Vector2(14f * scale, -(height - 36f * scale) * .5f);
            missionStatusIcon.rectTransform.sizeDelta = Vector2.one * (36f * scale);
            missionStatusDivider.anchoredPosition = new Vector2(56f * scale, -12f * scale);
            missionStatusDivider.sizeDelta = new Vector2(scale, height - 24f * scale);
            guidanceNote.anchoredPosition = new Vector2(28, -(22 + missionStatusPanel.rect.height + 8));
        }

        public static string MissionInstruction(RecoveryPhase phase, bool atSite, bool towing,
            bool clearingObstacle, bool insideDelivery, float recoveredSpeed, bool canChoose, string boardKey)
        {
            if (phase == RecoveryPhase.Explore)
                return canChoose ? boardKey + " · Choose a recovery" : "Waiting for the host";
            if (phase == RecoveryPhase.Complete) return "Recovery complete";
            if (phase == RecoveryPhase.Deliver)
            {
                if (insideDelivery) return recoveredSpeed < 2f ? "Delivering the vehicle…" : "Stop the recovered vehicle";
                if (DeliveryNeedsReconnect(phase, towing, insideDelivery))
                    return atSite ? "Reconnect your cable to the vehicle" : "Return to the recovered vehicle";
                return "Tow the vehicle to the drop-off";
            }
            if (clearingObstacle)
                return towing ? "Pull the fallen tree clear" : atSite
                    ? "Attach your cable to the tree" : "Reach the fallen tree";
            if (towing) return "Pull the vehicle free";
            return atSite ? "Attach your cable to the vehicle" : "Reach the stranded vehicle";
        }
    }
}
