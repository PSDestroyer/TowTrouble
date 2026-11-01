using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryWorld
    {
        private RecoveryExtraction extraction;
        private readonly float[] loadedCableForces = new float[2];
        private string presentedExtractionRun;
        private bool releasePresented;
        [SerializeField] private RecoveryExtractionVisuals extractionVisuals;
        public void ConfigureExtractionVisuals(RecoveryExtractionVisuals value) { extractionVisuals = value; }
        public ExtractionStage ExtractionStage { get; private set; }
        public float ExtractionProgress { get; private set; }
        public float ExtractionAlignment { get; private set; } = 1;
        public float ExtractionStalled { get; private set; }
        public float ExtractionReleasedSeconds { get; private set; }
        public bool UsesArcadeExtraction => FreeRoam && Contract.ExtractionProfile != null && Contract.ExtractionProfile.Enabled;

        private void ResetExtraction()
        {
            if (extractionVisuals != null) extractionVisuals.Clear();
            extraction = null;
            ExtractionStage = ExtractionStage.Blocked;
            ExtractionProgress = ExtractionStalled = ExtractionReleasedSeconds = 0;
            ExtractionAlignment = 1;
            System.Array.Clear(loadedCableForces, 0, loadedCableForces.Length);
        }

        private void BeginExtraction()
        {
            ResetExtraction();
            if (!UsesArcadeExtraction) return;
            var config = Contract.ExtractionProfile;
            extraction = new RecoveryExtraction(config, casualty.transform.position,
                casualty.WheelAnchor(Mathf.Clamp(config.BlockedWheel, 0, 3)), Contract.ExitDirection);
        }

        private void TickExtraction(Vector3 totalPull)
        {
            if (extraction == null) return;
            int wheel = Mathf.Clamp(Contract.ExtractionProfile.BlockedWheel, 0, 3);
            bool contact = casualty.TryGetWheelContact(wheel, out var hit);
            bool supported = contact && Vector3.Dot(casualty.transform.up, Vector3.up) > .55f;
            var force = extraction.Step(casualty.transform.position, casualty.WheelAnchor(wheel),
                casualty.Body.linearVelocity, totalPull, casualty.Body.mass, supported,
                Phase == RecoveryPhase.Recover || Phase == RecoveryPhase.Deliver, Time.fixedDeltaTime);
            if (contact) casualty.Body.AddForceAtPosition(force, hit.point);
            ExtractionStage = extraction.Stage;
            ExtractionProgress = extraction.Progress;
            ExtractionAlignment = extraction.Alignment;
            ExtractionStalled = extraction.StalledSeconds;
            ExtractionReleasedSeconds = extraction.ReleasedSeconds;
        }

        private void PresentExtraction()
        {
            if (!HasContract || !UsesArcadeExtraction || ClearingObstacle)
            {
                if (presentedExtractionRun != null && extractionVisuals != null) extractionVisuals.Clear();
                presentedExtractionRun = null; return;
            }
            if (presentedExtractionRun != RunId)
            {
                presentedExtractionRun = RunId;
                // Joining an already-released recovery must not replay an old burst.
                releasePresented = ExtractionStage >= ExtractionStage.Releasing;
                return;
            }
            if (releasePresented || ExtractionStage < ExtractionStage.Releasing) return;
            releasePresented = true;
            int wheel = Mathf.Clamp(Contract.ExtractionProfile.BlockedWheel, 0, 3);
            // An interpolated observer may briefly miss the short suspension ray.
            // Presentation still follows the authoritative release, with a ground fallback.
            var center = casualty.WheelVisual(wheel).position;
            Vector3 point = center - Vector3.up * casualty.CurrentModel.Radius;
            if (casualty.TryGetWheelContact(wheel, out var hit)
                || Physics.Raycast(center + Vector3.up * .5f, Vector3.down, out hit, 2f, 1 << 8, QueryTriggerInteraction.Ignore)) point = hit.point;
            if (extractionVisuals != null && extractionVisuals.Matches(ContractIndex))
                extractionVisuals.ShowRelease(point, Contract.ExitDirection, RecoveryVehicle.EffectsGain);
            else casualty.ShowExtractionRelease(point);
        }
    }
}
