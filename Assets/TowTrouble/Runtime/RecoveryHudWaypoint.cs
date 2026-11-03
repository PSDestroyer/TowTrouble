using TMPro;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private static readonly Color DeliveryTint = new Color(.68f, 1f, .57f);
        private RectTransform waypointBadge, guidanceNote;
        private TMP_Text waypointPlace, waypointCaption;
        private RecoveryIconGraphic waypointIcon, waypointArrow;
        private float waypointScale;
        private bool waypointHasHeight;

        private void BuildWaypoint()
        {
            waypointBadge = Panel(gameplay.transform, 0, 0, 190, 56);
            waypointBadge.name = "Active recovery waypoint";
            waypointBadge.anchorMin = waypointBadge.anchorMax = Vector2.zero;
            waypointBadge.pivot = new Vector2(.5f, .5f);
            waypointIcon = Icon(waypointBadge, RecoveryIcon.WaypointCar, 8, 12, 30, Amber);
            waypointPlace = Text(waypointBadge, "", 46, 4, 112, 20, 14, Muted);
            waypointPlace.alignment = TextAlignmentOptions.MidlineLeft;
            waypointCaption = Text(waypointBadge, "", 46, 24, 112, 27, 19, Paper, true);
            waypointCaption.alignment = TextAlignmentOptions.MidlineLeft;
            waypointArrow = Icon(waypointBadge, RecoveryIcon.DirectionArrow, 0, 0, 25, Paper);
            waypointArrow.rectTransform.pivot = new Vector2(.5f, .5f);
            waypointArrow.gameObject.SetActive(false);
            HideWaypoint();

            guidanceNote = Panel(gameplay.transform, 28, 22, 220, 38);
            guidanceNote.name = "Navigation hint";
            objective.rectTransform.SetParent(guidanceNote, false);
            objective.rectTransform.anchoredPosition = new Vector2(12, 0);
            objective.alignment = TextAlignmentOptions.MidlineLeft;
            textSizes[objective] = 17f;
            objective.fontSize = objective.fontSizeMax = 17f * P.textScale;
            objective.fontSizeMin = 17f * Mathf.Min(1f, P.textScale);
            HideGuidanceNote();
        }

        private void SetGuidanceNote(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                HideGuidanceNote();
                return;
            }
            if (objective.text != text || !guidanceNote.gameObject.activeSelf)
            {
                objective.text = text;
                float width = Mathf.Clamp(objective.GetPreferredValues(text).x + 28f, 125f, 540f);
                float height = 38f * Mathf.Max(1f, P.textScale);
                guidanceNote.sizeDelta = new Vector2(width, height);
                objective.rectTransform.sizeDelta = new Vector2(width - 24f, height);
                guidanceNote.gameObject.SetActive(true);
            }
        }

        private void HideGuidanceNote()
        {
            if (guidanceNote != null && guidanceNote.gameObject.activeSelf)
                guidanceNote.gameObject.SetActive(false);
        }

        private void SetWaypointContent(string place, string distance, bool delivery, bool showHeight)
        {
            var kind = delivery ? RecoveryIcon.FinishFlag : RecoveryIcon.WaypointCar;
            if (waypointIcon.Kind != kind)
            {
                waypointIcon.Kind = kind;
                waypointIcon.SetVerticesDirty();
            }
            waypointIcon.color = delivery ? DeliveryTint : Amber;
            if (waypointPlace.text != place || waypointHasHeight != showHeight || !Mathf.Approximately(P.textScale, waypointScale))
            {
                waypointPlace.text = place;
                waypointHasHeight = showHeight;
                waypointScale = P.textScale;
                float scale = Mathf.Max(1f, waypointScale);
                // Reserve enough room for a stable three/four digit reading as the car moves.
                string sample = showHeight ? L("999 m  <size=75%>·  199 m mai sus</size>", "999 m  <size=75%>·  199 m higher</size>") : "1,000 m";
                float labelWidth = Mathf.Max(waypointPlace.GetPreferredValues(place).x, waypointCaption.GetPreferredValues(sample).x);
                labelWidth = Mathf.Clamp(Mathf.Ceil(labelWidth) + 4f, 82f, 275f * scale);
                float width = 46f + labelWidth + 34f, height = 54f * scale;
                waypointBadge.sizeDelta = new Vector2(width, height);
                waypointPlace.rectTransform.anchoredPosition = new Vector2(46f, -4f * scale);
                waypointPlace.rectTransform.sizeDelta = new Vector2(labelWidth, 20f * scale);
                waypointCaption.rectTransform.anchoredPosition = new Vector2(46f, -23f * scale);
                waypointCaption.rectTransform.sizeDelta = new Vector2(labelWidth, 28f * scale);
                waypointIcon.rectTransform.anchoredPosition = new Vector2(8, -(height - 30f) * .5f);
                waypointArrow.rectTransform.anchoredPosition = new Vector2(width - 18f, -height * .5f);
            }
            if (waypointCaption.text != distance) waypointCaption.text = distance;
        }

        private void ProjectWaypoint(Camera camera, Vector3 point, bool delivery)
        {
            if (camera == null)
            {
                HideWaypoint();
                return;
            }

            if (!waypointBadge.gameObject.activeSelf) waypointBadge.gameObject.SetActive(true);
            var size = ((RectTransform)gameplay.transform).rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            var half = waypointBadge.sizeDelta * .5f;
            float lowerHud = cableGroup.activeSelf ? (partnerGroup.activeSelf ? 145f : 95f) : partnerGroup.activeSelf ? 95f : 48f;
            float upperHud = guidanceNote.gameObject.activeSelf ? guidanceNote.sizeDelta.y + 32f : 12f;
            if (!string.IsNullOrEmpty(crew.text)) upperHud = Mathf.Max(upperHud, 100f);
            float bottom = Mathf.Min(lowerHud + half.y + 8f, size.y * .45f);
            float top = Mathf.Min(upperHud + half.y + 8f, size.y * .45f);
            float side = Mathf.Min(half.x + 14f, size.x * .45f);
            var safe = Rect.MinMaxRect(side, bottom, size.x - side, size.y - top);
            bool edge = RecoveryWaypointProjection.Place(camera.WorldToViewportPoint(point),
                camera.transform.InverseTransformPoint(point), size, safe, out var position, out var angle);
            waypointBadge.anchoredPosition = position;
            if (waypointArrow.gameObject.activeSelf != edge) waypointArrow.gameObject.SetActive(edge);
            if (edge)
            {
                waypointArrow.color = delivery ? DeliveryTint : Amber;
                waypointArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void HideWaypoint()
        {
            if (waypointBadge != null && waypointBadge.gameObject.activeSelf)
                waypointBadge.gameObject.SetActive(false);
        }
    }
}
