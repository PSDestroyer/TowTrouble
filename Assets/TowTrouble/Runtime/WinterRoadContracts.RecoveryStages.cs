using UnityEngine;

namespace TowTrouble
{
    internal sealed partial class WinterRoadContracts
    {
        private Transform recoveryRearEye, firstRecoveryEye;
        private bool recoveryReady, recoveryDisconnected;
        private int recoveryStage;
        private float recoveryHold;
        private Vector3 finalPullPoint, recoverySide, recoveryAxleStart;

        private void InitializeRecoveryStage()
        {
            var task = CurrentOffer.RecoveryTask;
            recoveryReady = task == WinterRecoveryTask.Bank;
            recoveryStage = 0; recoveryHold = 0; recoveryDisconnected = false; firstRecoveryEye = null;
            finalPullPoint = rescuePullPoint;
            recoverySide = Vector3.Cross(Vector3.up, rescueExit);
            recoveryAxleStart = (pickup.WheelAnchor(blockedAxle) + pickup.WheelAnchor(blockedAxle + 1)) * .5f;
            if (task == WinterRecoveryTask.Bank) return;
            recoveryRearEye = Eye(pickup.transform, pickup.transform.InverseTransformPoint(pickup.RearHitch));
            recoveryRearEye.name = "Rear recovery tow eye";
            if (task == WinterRecoveryTask.Reposition)
                rescuePullPoint = Ground(rescue - rescueExit * 5f + recoverySide * 11f, out _);
            if (task == WinterRecoveryTask.Stabilize) pickup.SetRecoveryJammed(false);
        }

        internal void UpdateRecoveryStage(RecoveryVehicle driver, bool attached, float cableLoad)
        {
            if (Active != WinterJob.Recovery || recoveryReady || pickup == null) return;
            switch (CurrentOffer.RecoveryTask)
            {
                case WinterRecoveryTask.Stabilize:
                    recoveryHold = attached && driver.Speed < .65f && cableLoad > .06f
                        ? recoveryHold + Time.deltaTime : Mathf.Max(0, recoveryHold - Time.deltaTime);
                    if (recoveryHold >= 2.5f) { recoveryReady = true; pickup.SetRecoveryJammed(true); }
                    break;
                case WinterRecoveryTask.Turn:
                    bool aligned = Vector3.Dot(Vector3.ProjectOnPlane(pickup.transform.forward, Vector3.up).normalized, rescueExit) > .8f &&
                        Vector3.Dot(pickup.transform.up, Vector3.up) > .65f;
                    recoveryHold = reachedTow && aligned ? recoveryHold + Time.deltaTime : 0;
                    if (recoveryHold > .5f) recoveryReady = true;
                    break;
                case WinterRecoveryTask.Reposition:
                    var axle = (pickup.WheelAnchor(blockedAxle) + pickup.WheelAnchor(blockedAxle + 1)) * .5f;
                    if (recoveryStage == 0 && reachedTow && Vector3.Dot(axle - recoveryAxleStart, recoverySide) >= 1.2f)
                    {
                        recoveryStage = 1;
                        firstRecoveryEye = lastRecoveryEye;
                        rescuePullPoint = finalPullPoint;
                        Remove(currentMarker);
                        currentMarker = Zone("Second recovery pull point", rescuePullPoint, 3f);
                        Arrow(currentMarker.transform, rescuePullPoint + rescueExit * 2.9f, rescueExit);
                    }
                    if (recoveryStage == 1)
                    {
                        if (!attached) recoveryDisconnected = true;
                        if (attached && recoveryDisconnected && lastRecoveryEye != null && lastRecoveryEye != firstRecoveryEye)
                            recoveryReady = true;
                    }
                    break;
            }
        }

        private Transform lastRecoveryEye;
        internal void RecordRecoveryEye(Transform eye) => lastRecoveryEye = eye;

        internal void FixedRecoveryStage(bool attached)
        {
            if (Active != WinterJob.Recovery || recoveryReady || CurrentOffer.RecoveryTask != WinterRecoveryTask.Stabilize || TowBody == null) return;
            // Ground gravity creates the slide. A snow catch limits travel so late arrivals can still recover the car.
            Ground(TowBody.position, out var normal);
            Vector3 downhill = Vector3.ProjectOnPlane(Physics.gravity, normal);
            TowBody.AddForce(downhill * (TowBody.mass * (attached ? .08f : .32f)));
            Vector3 displacement = Vector3.ProjectOnPlane(TowBody.position - rescue, Vector3.up);
            if (displacement.magnitude > 4f)
                TowBody.AddForce((-displacement.normalized * (displacement.magnitude - 4f) * 1800f) - TowBody.linearVelocity * 900f);
        }

        private string RecoveryStageHint => recoveryReady ? null : CurrentOffer.RecoveryTask switch
        {
            WinterRecoveryTask.Stabilize => "Secure the slope · hold brake and keep cable loaded · " + Mathf.RoundToInt(recoveryHold / 2.5f * 100) + "%",
            WinterRecoveryTask.Turn => "Turn the vehicle toward the exit posts · pull from either end",
            WinterRecoveryTask.Reposition => recoveryStage == 0 ? "Pull sideways toward the orange ring to free the wheels" :
                !recoveryDisconnected ? "Side pull complete · detach, move to the next ring and reconnect at the other end" :
                "Connect to the other tow eye · then pull toward the firm bank",
            _ => null
        };
    }
}
