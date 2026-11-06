using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class RecoveryHud
    {
        private string assistanceRun;
        private RecoveryWorld assistanceWorld;
        private int assistanceSlot = -1, observedSnaps, completionMask;
        private bool observedAttached, firstCableUsed, firstCableIntroduced, firstSnapExplained;
        private bool snapIsLocal, safePointReady, newRecoveryAvailable;
        private float cableIntroUntil, snappedUntil, struggleSeconds;
        private float resetStillSince = -1f;
        private Vector3 resetStillPosition;
        private float lastAssistanceTime;
        private CanvasGroup exploreHintFade, hitchFade;
        private bool exploreHintWanted, hitchWanted;
        private TMP_Text exploreHintDetail;
        private string pullAdvice;
        private bool UrgentAssistance => safePointReady || Time.unscaledTime < snappedUntil;
        private bool IntroduceWinch => P.tutorial && !firstCableUsed && Time.unscaledTime < cableIntroUntil;

        private void ObserveAssistance(RecoveryWorld world, int slot)
        {
            float now = Time.unscaledTime;
            float dt = page == "Gameplay" && !session.MenuOpen
                ? Mathf.Clamp(now - lastAssistanceTime, 0f, .1f) : 0f;
            lastAssistanceTime = now;
            if (assistanceWorld != world || assistanceSlot != slot)
            {
                assistanceWorld = world; assistanceSlot = slot; assistanceRun = null;
                observedAttached = false; observedSnaps = world.Snaps;
                snappedUntil = 0; resetStillSince = -1; safePointReady = false;
                firstCableUsed = firstCableIntroduced = firstSnapExplained = false;
            }
            if (world.HasContract && assistanceRun != world.RunId)
            {
                assistanceRun = world.RunId; completionMask = P.completedMask;
                observedSnaps = world.Snaps; observedAttached = world.Attached(slot);
                struggleSeconds = 0; snappedUntil = 0; cableIntroUntil = 0;
                newRecoveryAvailable = false; resetStillSince = -1;
                if (observedAttached && !firstCableIntroduced)
                { firstCableIntroduced = true; cableIntroUntil = now + 10f; }
            }
            bool attached = world.HasContract && world.Attached(slot);
            if (world.HasContract && world.Snaps > observedSnaps)
            {
                snapIsLocal = observedAttached && !attached;
                snappedUntil = now + (P.tutorial && !firstSnapExplained ? 4f : 2.4f);
            }
            if (attached && !observedAttached && !firstCableIntroduced)
            { firstCableIntroduced = true; cableIntroUntil = now + 10f; }
            observedAttached = attached; observedSnaps = world.Snaps;
            if (attached)
            {
                var frame = world.Winch(slot);
                firstCableUsed |= frame.State == WinchState.TakingUp || frame.State == WinchState.Pulling
                    || frame.State == WinchState.Stalled || frame.State == WinchState.Minimum;
                var car = world.Drivers[slot];
                bool trying = frame.State == WinchState.Stalled || Mathf.Abs(car.ControlInput.y) > .35f
                    || (frame.Load > .05f && world.Casualty.Speed < .2f);
                struggleSeconds = trying && world.Casualty.Speed < .3f ? struggleSeconds + dt : 0;
                Vector4 slips = car.WheelSlip;
                pullAdvice = P.tutorial ? RecoveryAssistance.PullAdvice(car.ControlInput.y, car.Speed,
                    (slips.x + slips.y + slips.z + slips.w) * .25f, car.BrakeLights,
                    struggleSeconds, frame.State, world.ExtractionAlignment,
                    world.Phase == RecoveryPhase.Recover && world.UsesArcadeExtraction && !world.ClearingObstacle) : null;
            }
            else { struggleSeconds = 0; pullAdvice = null; }
            if (world.Phase == RecoveryPhase.Complete && assistanceRun == world.RunId && world.FreeRoam)
                newRecoveryAvailable = RecoveryAssistance.NewRecoveryAvailable(world.AvailableContracts,
                    completionMask, world.Contract.CompletionId);

            if (page != "Gameplay" || session.MenuOpen || !P.tutorial ||
                world.Phase == RecoveryPhase.Lobby || world.Phase == RecoveryPhase.Complete)
            { safePointReady = false; resetStillSince = -1; return; }
            var driver = world.Drivers[slot];
            if (resetStillSince < 0) { resetStillPosition = driver.transform.position; resetStillSince = now; }
            float moved = Vector3.Distance(resetStillPosition, driver.transform.position);
            bool trapped = RecoveryAssistance.NeedsSafePoint(Vector3.Dot(driver.transform.up, Vector3.up),
                driver.Speed, driver.ControlInput.y, driver.BrakeLights, moved);
            // Let normal pulling advice handle slow extractions; reset is for overturned cars
            // or a sustained unsuccessful attempt to drive with the cable detached.
            if (!trapped || (attached && Vector3.Dot(driver.transform.up, Vector3.up) >= .3f))
            { resetStillPosition = driver.transform.position; resetStillSince = now; safePointReady = false; }
            else safePointReady = now - resetStillSince >= 6f;
        }

        private bool RefreshUrgentHint(RecoveryWorld world, int slot)
        {
            if (safePointReady)
            {
                ShowExploreHint(session.UsingGamepad ? "Back" : P.ReadKey(P.resetKey, Key.R).ToString(),
                    "Return to safe point", world.HasContract && world.Resets(0) + world.Resets(1) == 0
                        ? "Using reset removes the no-reset bonus" : null);
                return true;
            }
            if (Time.unscaledTime < snappedUntil)
            {
                ShowExploreHint("", snapIsLocal ? "Cable snapped" : "Crew cable snapped",
                    P.tutorial && !firstSnapExplained ? "Pull steadily to avoid sudden loads" : null);
                return true;
            }
            if (snappedUntil > 0) { firstSnapExplained = true; snappedUntil = 0; }
            return false;
        }

        private void UpdateHintFades()
        {
            FadeHint(exploreHintFade, exploreHintWanted && page == "Gameplay");
            FadeHint(hitchFade, hitchWanted && page == "Gameplay" && !UrgentAssistance);
        }

        private static void FadeHint(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            if (visible && !group.gameObject.activeSelf) group.gameObject.SetActive(true);
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime / .16f);
            if (!visible && group.alpha <= 0f && group.gameObject.activeSelf) group.gameObject.SetActive(false);
        }
    }
}
