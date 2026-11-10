using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private RectTransform hitchPrompt;
        private RectTransform hitchKeycap;
        private TMP_Text hitchBinding;
        private TMP_Text hitchAction;
        private float hitchTextScale;
        private RecoveryIconGraphic hitchPointIcon;

        private void BuildHitchPrompt()
        {
            hitchPrompt = Node("Hitch action", gameplay.transform, 0, 0, 180, 38);
            hitchPrompt.anchorMin = hitchPrompt.anchorMax = Vector2.zero;
            hitchPrompt.pivot = new Vector2(.5f, 0f);
            hitchFade = hitchPrompt.gameObject.AddComponent<CanvasGroup>();
            hitchFade.alpha = 0f; hitchFade.blocksRaycasts = false;
            var background = hitchPrompt.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = Ink;
            background.raycastTarget = false;
            hitchPointIcon = Icon(hitchPrompt, RecoveryIcon.Hook, 8, 6, 26, Amber);
            hitchKeycap = Panel(hitchPrompt, 8, 6, 26, 26, Paper); hitchKeycap.name="HUD control hint";
            hitchBinding = Text(hitchKeycap, "", 0, 0, 26, 26, 16, Ink, true);
            hitchBinding.alignment = TextAlignmentOptions.Center;
            hitchAction = Text(hitchPrompt, "", 42, 0, 132, 38, 17, Paper, true);
            hitchAction.alignment = TextAlignmentOptions.MidlineLeft;
            HideHitchPrompt();
        }

        private void HideHitchPrompt()
        {
            hitchWanted = false;
        }

        private bool RefreshHitchPrompt(RecoveryWorld world, Camera camera, int slot)
        {
            if (camera == null || world == null || !world.HasContract || slot < 0 ||
                slot >= world.Drivers.Length || world.Attached(slot) || UrgentAssistance)
            {
                HideHitchPrompt();
                return false;
            }

            Vector3 hitch = world.TowPoint;
            float distance = Vector3.Distance(world.Drivers[slot].RearHitch, hitch);
            bool canAttach = distance <= RecoveryRules.AttachRange;
            if (distance > 26f)
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

            string key = canAttach ? session.UsingGamepad ? RecoveryInputPrompts.ConfirmLabel(RecoveryInputPrompts.Family(RecoveryInputDevices.Pad)) : P.ReadKey(P.hookKey, Key.F).ToString() : "";
            bool reconnecting = NeedsDeliveryReconnect(world, slot);
            string action = canAttach ? reconnecting ? "Reconnect cable" : "Attach cable" : "Move closer to attach";
            hitchKeycap.gameObject.SetActive(canAttach);
            hitchPointIcon.gameObject.SetActive(!canAttach);
            if (key != hitchBinding.text || action != hitchAction.text || hitchTextScale != P.textScale)
            {
                hitchTextScale = P.textScale;
                hitchBinding.text = key;
                hitchAction.text = action;
                float keyWidth = Mathf.Max(26f * P.textScale, hitchBinding.GetLocalizedPreferredValues(key, 300, 50).x + 10f);
                float labelWidth = hitchAction.GetLocalizedPreferredValues(action, 350, 50).x + 3f;
                float height = 38f * Mathf.Max(1, P.textScale);
                hitchKeycap.sizeDelta = hitchBinding.rectTransform.sizeDelta = new Vector2(keyWidth, height - 12f);
                hitchAction.rectTransform.anchoredPosition = new Vector2(16 + keyWidth, 0);
                hitchAction.rectTransform.sizeDelta = new Vector2(labelWidth, height);
                hitchPrompt.sizeDelta = new Vector2(25 + keyWidth + labelWidth, height);
            }

            Vector2 size = ((RectTransform)gameplay.transform).rect.size;
            float margin = hitchPrompt.rect.width * .5f + 8f;
            var safe=ScreenSafeBounds;
            hitchPrompt.anchoredPosition = new Vector2(Mathf.Clamp(viewport.x * size.x, safe.xMin+margin, safe.xMax-margin),
                Mathf.Clamp(viewport.y * size.y + 8f, safe.yMin+10f, safe.yMax-hitchPrompt.rect.height-10f));
            hitchWanted = true;
            return true;
        }
    }
}
