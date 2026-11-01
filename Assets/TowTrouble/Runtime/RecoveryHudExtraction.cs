using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private bool RefreshExtractionMarker(RecoveryWorld world, Camera camera)
        {
            if (!world.HasContract || !world.UsesArcadeExtraction || world.ClearingObstacle) return false;
            bool released = world.ExtractionStage >= ExtractionStage.Releasing;
            if (world.ExtractionStage == ExtractionStage.Free && world.ExtractionReleasedSeconds >= 1.2f) return false;
            int wheel = Mathf.Clamp(world.Contract.ExtractionProfile.BlockedWheel, 0, 3);
            var point = world.Casualty.WheelVisual(wheel).position + Vector3.up * 1.2f;
            PlaceMarker(marker, point, camera);
            marker.color = released ? new Color(.65f, 1f, .48f) : Amber;
            bool terrainAware=world.Contract.ExtractionProfile.TerrainAware;
            marker.text = terrainAware ? (released?L("MAȘINĂ ELIBERATĂ","CAR FREED"):world.ExtractionStage==ExtractionStage.Moving?L("MAȘINA IESE DIN BLOCAJ","CAR LEAVING THE OBSTRUCTION"):L("MAȘINĂ BLOCATĂ","CAR STUCK"))
                : released ? L("ROATĂ ELIBERATĂ", "WHEEL FREED")
                : world.ExtractionStage == ExtractionStage.Moving
                    ? L("ROATA URCĂ SPRE IEȘIRE", "WHEEL CLIMBING OUT")
                    : L("ROATĂ PRINSĂ ÎN ȘANȚ", "WHEEL CAUGHT IN RUT");
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
