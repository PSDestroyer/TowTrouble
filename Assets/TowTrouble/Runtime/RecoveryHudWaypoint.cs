using TMPro;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private const float WaypointWidth = 246f;
        private const float WaypointHeight = 58f;
        private static readonly Color DeliveryTint = new Color(.68f, 1f, .57f);
        private RectTransform waypointBadge;
        private TMP_Text waypointCaption;
        private RecoveryIconGraphic waypointIcon;
        private RecoveryIconGraphic waypointArrow;

        private void BuildWaypoint()
        {
            waypointBadge = Panel(gameplay.transform, 0, 0, WaypointWidth, WaypointHeight);
            waypointBadge.name = "Active recovery waypoint";
            waypointBadge.anchorMin = waypointBadge.anchorMax = Vector2.zero;
            waypointBadge.pivot = new Vector2(.5f, .5f);

            waypointIcon = Icon(waypointBadge, RecoveryIcon.WaypointCar, 8, 11, 36, Amber);
            waypointCaption = Text(waypointBadge, "", 50, 2, WaypointWidth - 89, WaypointHeight - 4, 18, Paper, true);
            waypointCaption.alignment = TextAlignmentOptions.MidlineLeft;

            waypointArrow = Icon(waypointBadge, RecoveryIcon.DirectionArrow, 0, 0, 32, Paper);
            waypointArrow.rectTransform.pivot = new Vector2(.5f, .5f);
            waypointArrow.rectTransform.anchoredPosition = new Vector2(WaypointWidth - 22, -WaypointHeight * .5f);
            waypointArrow.gameObject.SetActive(false);
            HideWaypoint();
        }

        private void SetWaypointContent(string caption, bool delivery)
        {
            var kind = delivery ? RecoveryIcon.FinishFlag : RecoveryIcon.WaypointCar;
            if (waypointIcon.Kind != kind)
            {
                waypointIcon.Kind = kind;
                waypointIcon.SetVerticesDirty();
            }
            waypointIcon.color = delivery ? DeliveryTint : Amber;
            if (waypointCaption.text != caption) waypointCaption.text = caption;
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
            // Keep the entire badge clear of the top objective and lower speed/cable HUD.
            float bottomHud = winchText.gameObject.activeSelf ? 620 : cableGroup.activeSelf ? 762 : 800;
            if (partnerGroup.activeSelf) bottomHud = Mathf.Min(bottomHud, 706);
            float bottom = Mathf.Clamp(size.y - bottomHud + WaypointHeight * .5f + 10, WaypointHeight * .5f + 10, size.y * .45f);
            float top = Mathf.Min((mudFeedback.gameObject.activeSelf ? 164 : 115) + WaypointHeight * .5f + 10, size.y * .45f);
            float side = Mathf.Min(WaypointWidth * .5f + 14, size.x * .45f);
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
