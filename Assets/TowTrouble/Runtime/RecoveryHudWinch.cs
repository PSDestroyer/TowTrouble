using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private const float CableWarningLoad = .82f;
        private static readonly Color CableWarningTint = new Color(1f, .55f, .43f);
        private GameObject cableGroup, partnerGroup;
        private TMP_Text cableText, actionText, partnerText, cableDetails;
        private TMP_Text reelHint, payoutHint;
        private string lastReelBinding, lastPayoutBinding;
        private float hintScale;
        private UnityEngine.UI.Image cableFill, partnerFill;
        private RecoveryIconGraphic cableIcon, reelIcon, payoutIcon;
        private bool cableWarning;

        private void BuildCableHud()
        {
            var cable = Panel(gameplay.transform, 0, 0, 286, 52, Ink);
            cable.name = "Cable load";
            AnchorCableHud(cable, 26f);
            cableGroup = cable.gameObject;
            cableIcon = Icon(cable, RecoveryIcon.Winch, 12, 6, 22, Amber);
            cableText = Text(cable, "", 44, 3, 230, 26, 18, Paper, true);
            cableText.alignment = TextAlignmentOptions.MidlineLeft;
            cableFill = CableMeter(cable, 38, 262);
            reelHint = Text(cable, "", 12, 29, 94, 21, 14, Paper);
            reelHint.alignment = TextAlignmentOptions.MidlineLeft;
            payoutHint = Text(cable, "", 180, 29, 94, 21, 14, Paper);
            payoutHint.alignment = TextAlignmentOptions.MidlineRight;
            reelIcon = Icon(cable, RecoveryIcon.ReelIn, 0, 29, 21, Paper);
            payoutIcon = Icon(cable, RecoveryIcon.PayOut, 0, 29, 21, Paper);
            actionText = Text(cable, "", 12, 50, 262, 26, 16, Paper);
            actionText.alignment = TextAlignmentOptions.MidlineLeft;
            cableGroup.SetActive(false);

            var partner = Panel(gameplay.transform, 0, 0, 248, 46, Ink);
            partner.name = "Other driver's cable load";
            AnchorCableHud(partner, 26f);
            partnerGroup = partner.gameObject;
            Icon(partner, RecoveryIcon.Crew, 12, 7, 20, Muted);
            partnerText = Text(partner, "", 42, 3, 194, 28, 16, Paper);
            partnerText.alignment = TextAlignmentOptions.MidlineLeft;
            partnerFill = CableMeter(partner, 33, 224);
            partnerGroup.SetActive(false);
        }

        private static void AnchorCableHud(RectTransform rect, float bottom)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(26, bottom);
        }

        private UnityEngine.UI.Image CableMeter(Transform parent,float top,float width)
        {
            var track = Panel(parent, 12, top, width, 5, new Color(.37f,.38f,.35f));
            var rect = Stretch("Tension", track);
            var fill = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            fill.raycastTarget = false;
            rect.anchorMax = new Vector2(0, 1);
            return fill;
        }

        private void RefreshCableHud(RecoveryWorld world, int slot)
        {
            bool attached = world.HasContract && world.Attached(slot);
            float tension = attached ? Mathf.Clamp01(world.Tension(slot)) : 0;
            var frame = attached ? world.Winch(slot) : default;
            bool operating = frame.State != WinchState.Holding && frame.State != WinchState.Disconnected;
            bool quietDetach = attached && world.Drivers[slot].Speed < .45f;
            bool visible = attached && (IntroduceWinch || quietDetach || operating || tension > (cableGroup.activeSelf ? .01f : .025f));
            if (visible != cableGroup.activeSelf) cableGroup.SetActive(visible);
            cableWarning = visible && tension >= (cableWarning ? CableWarningLoad - .035f : CableWarningLoad);

            if (visible)
            {
                RefreshCableControlHints();
                string label = WinchLabel(frame.State, P.language == "en");
                string hint = null;
                if (cableWarning)
                {
                    label = L("Cablu la limită", "Cable at risk");
                    hint = frame.Length >= RecoveryWinch.MaximumLength - .01f
                        ? L("Oprește tragerea", "Ease off the throttle")
                        : L("Slăbește cablul", "Pay out cable");
                }
                else if (pullAdvice != null) hint = pullAdvice;
                else if (IntroduceWinch)
                { label = "Cable attached"; hint = "Hold " + (session.UsingGamepad ? "LB" : P.ReadKey(P.reelKey, Key.Q).ToString()) + " to reel in"; }
                if (hint == null && quietDetach && !IntroduceWinch)
                    hint = (session.UsingGamepad ? "A" : P.ReadKey(P.hookKey, Key.E).ToString()) + " · Detach cable";
                if (UrgentAssistance) hint = null;

                bool showHint = !string.IsNullOrEmpty(hint);
                var panel = (RectTransform)cableGroup.transform;
                float height = showHint ? 80f : 52f;
                if (panel.sizeDelta.y != height) panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                float panelWidth = panel.rect.width;
                cableText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth - 56f);
                actionText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth - 24f);
                if (cableText.text != label) cableText.text = label;
                if (actionText.gameObject.activeSelf != showHint) actionText.gameObject.SetActive(showHint);
                if (showHint && actionText.text != hint) actionText.text = hint;
                var tint = cableWarning ? CableWarningTint : Amber;
                cableIcon.color = cableFill.color = tint;
                payoutIcon.color = payoutHint.color = cableWarning && frame.Length < RecoveryWinch.MaximumLength - .01f ? CableWarningTint : Paper;
                actionText.color = cableWarning ? CableWarningTint : Paper;
                cableFill.rectTransform.anchorMax = new Vector2(tension, 1);
            }
            RefreshPartnerCable(world, slot == 0 ? 1 : 0);
        }

        private void RefreshCableControlHints()
        {
            bool gamepad = session.UsingGamepad;
            string reel = gamepad ? "LB" : P.reelKey;
            string payout = gamepad ? "RB" : P.extendKey;
            if (lastReelBinding == reel && lastPayoutBinding == payout && hintScale == P.textScale) return;
            lastReelBinding = reel;
            lastPayoutBinding = payout;
            hintScale = P.textScale;

            string reelKey = gamepad ? reel : P.ReadKey(reel, Key.Q).ToString();
            string payoutKey = gamepad ? payout : P.ReadKey(payout, Key.F).ToString();
            reelHint.text = "[" + reelKey + "]";
            payoutHint.text = "[" + payoutKey + "]";
            float leftWidth = Mathf.Ceil(reelHint.GetPreferredValues(reelHint.text, 1000, 40).x) + 2f;
            float rightWidth = Mathf.Ceil(payoutHint.GetPreferredValues(payoutHint.text, 1000, 40).x) + 2f;
            const float padding = 12f, iconGap = 3f, trackGap = 8f, minTrackWidth = 132f;
            float iconSize = Mathf.Clamp(21f * hintScale, 19f, 24f);
            float trackLeft = padding + leftWidth + iconGap + iconSize + trackGap;
            float rightInset = padding + rightWidth + iconGap + iconSize + trackGap;
            float width = Mathf.Max(286f, trackLeft + minTrackWidth + rightInset);
            ((RectTransform)cableGroup.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            reelHint.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, leftWidth);
            payoutHint.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rightWidth);
            float payoutLeft = width - padding - rightWidth;
            payoutHint.rectTransform.anchoredPosition = new Vector2(payoutLeft, payoutHint.rectTransform.anchoredPosition.y);
            reelIcon.rectTransform.sizeDelta = payoutIcon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            float iconTop = -39.5f + iconSize * .5f;
            reelIcon.rectTransform.anchoredPosition = new Vector2(padding + leftWidth + iconGap, iconTop);
            payoutIcon.rectTransform.anchoredPosition = new Vector2(payoutLeft - iconGap - iconSize, iconTop);
            var track = (RectTransform)cableFill.rectTransform.parent;
            track.anchoredPosition = new Vector2(trackLeft, -38);
            track.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - trackLeft - rightInset);
        }

        private void RefreshPartnerCable(RecoveryWorld world,int partnerSlot)
        {
            float tension = world.HasContract && world.Cooperative && world.Attached(partnerSlot)
                ? Mathf.Clamp01(world.Tension(partnerSlot)) : 0f;
            bool visible = tension > (partnerGroup.activeSelf ? .01f : .025f);
            if (partnerGroup.activeSelf != visible) partnerGroup.SetActive(visible);
            if (!visible) return;

            var panel = (RectTransform)partnerGroup.transform;
            float y = cableGroup.activeSelf ? 32f + ((RectTransform)cableGroup.transform).rect.height : 26f;
            panel.anchoredPosition = new Vector2(26, y);
            bool risk = tension >= CableWarningLoad;
            partnerText.text = risk ? L("Coechipier la limită", "Crew cable at risk") : L("Tensiune coechipier", "Crew cable tension");
            partnerFill.color = risk ? CableWarningTint : partnerSlot == 0 ? Blue : Orange;
            partnerFill.rectTransform.anchorMax = new Vector2(tension, 1);
        }

        private void BuildCableDetails(RectTransform card)
        {
            cableDetails = Text(card, "", 32, 548, 496, 55, 16, Muted);
            cableDetails.alignment = TextAlignmentOptions.MidlineLeft;
            RefreshCableDetails(session.World, session.LocalSlot);
        }

        private void RefreshCableDetails(RecoveryWorld world,int slot)
        {
            if (cableDetails == null) return;
            bool attached = world != null && world.HasContract && slot >= 0 && slot < 2 && world.Attached(slot);
            if (cableDetails.gameObject.activeSelf != attached) cableDetails.gameObject.SetActive(attached);
            if (!attached) return;
            var frame = world.Winch(slot);
            float capacity = world.Drivers[slot].WinchForce / 1000f;
            string controls = session.UsingGamepad ? "LB / RB" : P.reelKey + " / " + P.extendKey;
            string detach = session.UsingGamepad ? "A" : P.hookKey;
            cableDetails.text = L("Troliu ", "Winch ") + (frame.Load * capacity).ToString("0.0") + " / " +
                capacity.ToString("0.#") + " kN   ·   " + frame.Length.ToString("0.0") + " m\n" +
                controls + L("  Scurtează / slăbește", "  Reel in / pay out") + "   ·   " +
                detach + L("  Desprinde", "  Detach");
        }

        public static string WinchLabel(WinchState state, bool english)
        {
            switch (state)
            {
                case WinchState.TakingUp: return english ? "Taking up slack" : "Întinde cablul";
                case WinchState.Pulling: return english ? "Winching" : "Troliu activ";
                case WinchState.PayingOut: return english ? "Paying out" : "Dai cablu";
                case WinchState.Stalled: return english ? "Winch at limit" : "Troliu la limită";
                case WinchState.Minimum: return english ? "Minimum length" : "Lungime minimă";
                case WinchState.Maximum: return english ? "Maximum length" : "Lungime maximă";
                default: return english ? "Cable tension" : "Tensiune cablu";
            }
        }
    }
}
