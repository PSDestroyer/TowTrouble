using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private RectTransform incidentOverlay;
        private Image incidentFade;
        private TMP_Text incidentTitle, incidentSkip;
        private string incidentTitleRun;
        private bool UpdateContractIntroHud()
        {
            bool active = session.ContractIntroActive;
            if (!active)
            {
                if (incidentOverlay != null && incidentOverlay.gameObject.activeSelf)
                { incidentOverlay.gameObject.SetActive(false); gameplay.SetActive(page == "Gameplay"); }
                return false;
            }
            if (incidentOverlay == null)
            {
                incidentOverlay = Stretch("Recovery incident cinema", root);
                var blocker = incidentOverlay.gameObject.AddComponent<Image>(); blocker.color = Color.clear; blocker.raycastTarget = true;
                RectTransform Band(string name, float min, float max)
                {
                    var band = Stretch(name, incidentOverlay); band.anchorMin = new Vector2(0, min); band.anchorMax = new Vector2(1, max);
                    var image = band.gameObject.AddComponent<Image>(); image.color = Color.black; image.raycastTarget = false; return band;
                }
                Band("Cinema upper band", .91f, 1);
                var bottom = Band("Cinema lower band", 0, .11f);
                incidentTitle = Text(bottom, "", 42, 12, 1060, 42, 25, Paper, true);
                incidentSkip = ControlText(Text(bottom, "", 0, 18, 420, 38, 18, Muted));
                incidentSkip.rectTransform.anchorMin = incidentSkip.rectTransform.anchorMax = incidentSkip.rectTransform.pivot = new Vector2(1, 1);
                incidentSkip.rectTransform.anchoredPosition = new Vector2(-42, -18); incidentSkip.alignment = TextAlignmentOptions.MidlineRight;
                var fade = Stretch("Cinema transition", incidentOverlay); incidentFade = fade.gameObject.AddComponent<Image>();
                incidentFade.color = Color.clear; incidentFade.raycastTarget = false;
            }
            gameplay.SetActive(false); incidentOverlay.gameObject.SetActive(true); incidentOverlay.SetAsLastSibling();
            if (incidentTitleRun != session.World.RunId)
            { incidentTitleRun = session.World.RunId; incidentTitle.text = session.World.Contract.TitleFor(P); }
            incidentSkip.text = session.ContractIntroCanSkip
                ? (session.UsingGamepad ? RecoveryInputPrompts.ConfirmLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad)) : "ENTER / ESC") + "   " + L("SARI", "SKIP") : "";
            incidentFade.color = new Color(0, 0, 0, session.ContractIntroFade);
            HideRoadTrail(); return true;
        }
    }
}
