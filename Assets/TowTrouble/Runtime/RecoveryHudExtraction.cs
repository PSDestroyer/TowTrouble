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
            var safe=ScreenSafeBounds;
            localCaptionVisible = viewport.z > 0 && position.x > Mathf.Max(side,safe.xMin+extractionTextHalfWidth+8f)
                && position.x < Mathf.Min(size.x-side,safe.xMax-extractionTextHalfWidth-8f)
                && position.y > Mathf.Max(size.y*.12f+inset,safe.yMin+8f)
                && position.y < Mathf.Min(size.y*.88f-inset,safe.yMax-marker.rectTransform.rect.height-8f);
            if (!localCaptionVisible) return false;
            var rect = marker.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = position;
            if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
            return true;
        }

        private static string ExtractionAdvice(RecoveryWorld world, bool english)
        {
            if (world.Phase != RecoveryPhase.Recover || !world.UsesArcadeExtraction || world.ClearingObstacle)
                return null;
            return ExtractionAdvice(world.ExtractionStage, world.ExtractionAlignment, world.ExtractionStalled, english);
        }

        public static string ExtractionAdvice(ExtractionStage stage, float alignment, float stalled, bool english)
        {
            if (stage == ExtractionStage.Blocked || stage >= ExtractionStage.Releasing) return null;
            if (alignment < .65f) return english ? "Pull toward the exit" : "Trage spre ieșire";
            if (stalled > 1.5f) return english ? "Find firmer ground" : "Caută teren mai ferm";
            return null;
        }
    }
}
