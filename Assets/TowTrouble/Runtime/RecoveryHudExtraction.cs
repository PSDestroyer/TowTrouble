using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private BoxCollider extractionBodyBox;
        private RecoveryVehicle extractionBody;
        private float extractionTextHalfWidth;
        private bool localCaptionVisible;

        private bool RefreshExtractionCaption(RecoveryWorld world)
        {
            if (!world.HasContract || !world.UsesArcadeExtraction || world.ClearingObstacle) return false;
            bool released = world.ExtractionStage >= ExtractionStage.Releasing;
            if (world.ExtractionStage == ExtractionStage.Free && world.ExtractionReleasedSeconds >= 1.2f) return false;
            marker.color = released ? new Color(.65f, 1f, .48f) : Amber;
            bool terrainAware=world.Contract.ExtractionProfile.TerrainAware;
            string caption = terrainAware ? (released?L("MAȘINĂ ELIBERATĂ","CAR FREED"):world.ExtractionStage==ExtractionStage.Moving?L("MAȘINA IESE DIN BLOCAJ","CAR LEAVING THE OBSTRUCTION"):L("MAȘINĂ BLOCATĂ","CAR STUCK"))
                : released ? L("ROATĂ ELIBERATĂ", "WHEEL FREED")
                : world.ExtractionStage == ExtractionStage.Moving
                    ? L("ROATA URCĂ SPRE IEȘIRE", "WHEEL CLIMBING OUT")
                    : L("ROATĂ PRINSĂ ÎN ȘANȚ", "WHEEL CAUGHT IN RUT");
            if (marker.text != caption)
            {
                marker.text = caption;
                extractionTextHalfWidth = Mathf.Min(marker.preferredWidth, marker.rectTransform.rect.width) * .5f;
            }
            return true;
        }

        private Vector3 ExtractionLabelAnchor(RecoveryWorld world)
        {
            var vehicle = world.Casualty;
            var profile = world.Contract.ExtractionProfile;
            if (!profile.TerrainAware)
                return vehicle.WheelAnchor(Mathf.Clamp(profile.BlockedWheel, 0, 3)) + Vector3.up * 1.2f;

            if (extractionBody != vehicle)
            {
                extractionBody = vehicle;
                extractionBodyBox = vehicle.GetComponent<BoxCollider>();
            }
            if (extractionBodyBox == null) return vehicle.transform.position + Vector3.up * 2f;
            Vector3 roof = extractionBodyBox.center + Vector3.up * (extractionBodyBox.size.y * .5f);
            return vehicle.transform.TransformPoint(roof) + Vector3.up * .35f;
        }

        private bool TryPlaceExtractionCaption(Camera camera, Vector3 anchor)
        {
            var viewport = camera.WorldToViewportPoint(anchor);
            var size = ((RectTransform)gameplay.transform).rect.size;
            Vector2 position = new Vector2(viewport.x * size.x, viewport.y * size.y);
            // Allow a small dead band at the edge so camera or chassis motion cannot flicker between labels.
            float inset = localCaptionVisible ? 0f : 14f;
            float side = Mathf.Max(size.x * .06f, extractionTextHalfWidth + 8f) + inset;
            localCaptionVisible = viewport.z > 0 && position.x > side && position.x < size.x - side
                && position.y > size.y * .12f + inset && position.y < size.y * .88f - inset;
            if (!localCaptionVisible) return false;
            var rect = marker.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = position;
            if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
            return true;
        }

        private void RefreshExtractionFeedback(RecoveryWorld world)
        {
            if (!world.UsesArcadeExtraction || world.ClearingObstacle) return;
            string message = ExtractionMessage(world.ExtractionStage, world.ExtractionAlignment,
                world.ExtractionStalled, world.ExtractionReleasedSeconds, P.language == "en",world.Contract.ExtractionProfile.TerrainAware);
            if (string.IsNullOrEmpty(message)) return;
            mudFeedback.gameObject.SetActive(true);
            mudFeedback.text = message;
        }

        public static string ExtractionMessage(ExtractionStage stage, float alignment, float stalled, float releasedSeconds, bool english,bool terrainAware=false)
        {
            if (stage == ExtractionStage.Free)
                return releasedSeconds < 2 ? (english ? "CAR FREED · Ready to tow." : "MAȘINĂ ELIBERATĂ · Poți continua tractarea.") : "";
            if (stage == ExtractionStage.Releasing)
                return terrainAware?(english?"THE CAR IS CLEARING THE OBSTRUCTION":"MAȘINA IESE DIN BLOCAJ"):english ? "THE WHEEL IS CLEARING THE RUT" : "ROATA IESE DIN ȘANȚ";
            if (stage != ExtractionStage.Blocked && alignment < .65f)
                return terrainAware?(english?"POOR ANGLE · Pull toward the recovery exit.":"UNGHI NEFAVORABIL · Trage spre ieșirea din blocaj."):english ? "POOR ANGLE · Pull toward the rut exit." : "UNGHI NEFAVORABIL · Trage spre ieșirea din șanț.";
            if (stalled > 1.5f)
                return english ? "NO PROGRESS · Find firm footing and build a steady pull." : "FĂRĂ PROGRES · Caută sprijin ferm și trage constant.";
            switch (stage)
            {
                case ExtractionStage.TakingLoad:
                    return english ? "TAKING LOAD · Build the pull gradually." : "CABLU SUB SARCINĂ · Trage progresiv.";
                case ExtractionStage.Moving:
                    return english ? "THE CAR IS MOVING · Keep a steady pull." : "MAȘINA SE MIȘCĂ · Păstrează tragerea constantă.";
                default:
                    return terrainAware?(english?"CAR STUCK · Connect the cable and pull toward the exit.":"MAȘINĂ BLOCATĂ · Conectează cablul și trage spre ieșire."):english ? "WHEEL IN A RUT · Connect the cable and pull toward the trail." : "ROATĂ ÎN ȘANȚ · Conectează cablul și trage spre potecă.";
            }
        }
    }
}
