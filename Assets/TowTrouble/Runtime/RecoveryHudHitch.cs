using TMPro;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private RectTransform hitchPrompt;
        private RectTransform hitchKeycap;
        private TMP_Text hitchBinding;
        private TMP_Text hitchAction;
        private float hitchTextScale;

        private void BuildHitchPrompt()
        {
            hitchPrompt = Node("Hitch action", gameplay.transform, 0, 0, 180, 38);
            hitchPrompt.anchorMin = hitchPrompt.anchorMax = Vector2.zero;
            hitchPrompt.pivot = new Vector2(.5f, 0f);
            var background = hitchPrompt.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = Ink;
            background.raycastTarget = false;
            hitchKeycap = Panel(hitchPrompt, 8, 6, 26, 26, Paper);
            hitchBinding = Text(hitchKeycap, "", 0, 0, 26, 26, 16, Ink, true);
            hitchBinding.alignment = TextAlignmentOptions.Center;
            hitchAction = Text(hitchPrompt, "", 42, 0, 132, 38, 17, Paper, true);
            hitchAction.alignment = TextAlignmentOptions.MidlineLeft;
            HideHitchPrompt();
        }

        private void HideHitchPrompt()
        {
            if (hitchPrompt != null && hitchPrompt.gameObject.activeSelf)
                hitchPrompt.gameObject.SetActive(false);
        }

        private bool RefreshHitchPrompt(RecoveryWorld world, Camera camera, int slot)
        {
            if (camera == null || world == null || !world.HasContract || slot < 0 ||
                slot >= world.Drivers.Length || world.Attached(slot))
            {
                HideHitchPrompt();
                return false;
            }

            Vector3 hitch = world.TowPoint;
            if ((world.Drivers[slot].RearHitch - hitch).sqrMagnitude > RecoveryRules.AttachRange * RecoveryRules.AttachRange)
            {
                HideHitchPrompt();
                return false;
            }

            Vector3 viewport = camera.WorldToViewportPoint(hitch + Vector3.up * .45f);
            if (viewport.z <= 0 || viewport.x < .04f || viewport.x > .96f || viewport.y < .08f || viewport.y > .92f)
            {
                HideHitchPrompt();
                return false;
            }

            string key = session.UsingGamepad ? "A" : P.hookKey;
            string action = L("Prinde cablul", "Attach cable");
            if (key != hitchBinding.text || action != hitchAction.text || hitchTextScale != P.textScale)
            {
                hitchTextScale = P.textScale;
                hitchBinding.text = key;
                hitchAction.text = action;
                float keyWidth = Mathf.Max(26f, hitchBinding.GetPreferredValues(key, 300, 50).x + 10f);
                float labelWidth = hitchAction.GetPreferredValues(action, 350, 50).x + 3f;
                hitchKeycap.sizeDelta = hitchBinding.rectTransform.sizeDelta = new Vector2(keyWidth, 26);
                hitchAction.rectTransform.anchoredPosition = new Vector2(16 + keyWidth, 0);
                hitchAction.rectTransform.sizeDelta = new Vector2(labelWidth, 38);
                hitchPrompt.sizeDelta = new Vector2(25 + keyWidth + labelWidth, 38);
            }

            Vector2 size = ((RectTransform)gameplay.transform).rect.size;
            float margin = hitchPrompt.rect.width * .5f + 8f;
            hitchPrompt.anchoredPosition = new Vector2(Mathf.Clamp(viewport.x * size.x, margin, size.x - margin),
                Mathf.Clamp(viewport.y * size.y + 8f, 10f, size.y - hitchPrompt.rect.height - 10f));
            if (!hitchPrompt.gameObject.activeSelf) hitchPrompt.gameObject.SetActive(true);
            return true;
        }
    }
}
