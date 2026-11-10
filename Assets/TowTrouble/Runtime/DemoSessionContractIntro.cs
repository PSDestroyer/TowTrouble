using UnityEngine;
using UnityEngine.InputSystem;

namespace TowTrouble
{
    public sealed partial class DemoSession
    {
        private RecoveryContractIntro contractIntro;
        private string introRun, pendingIntroRun;
        private int pendingIntroIndex, pendingIntroFrame;
        private float introDeadline, introPreviousTimeScale;
        private bool introOwnsPause, introSkipReleased;
        internal Vector3 ContractIntroActorPosition => contractIntro != null ? contractIntro.ActorPosition : world.Casualty.transform.position;
        public bool ContractIntroActive => contractIntro != null;
        public float ContractIntroFade => contractIntro?.Fade ?? 0;
        public bool ContractIntroCanSkip => contractIntro != null && contractIntro.CanSkip;
        public bool ContractIntroReturning => contractIntro != null && contractIntro.Returning;
        private void QueueContractIntro(int index, string run)
        {
            if (Training || run == introRun || world.Map == null || index < 0 || index >= world.AvailableContracts.Length) return;
            pendingIntroIndex = index; pendingIntroFrame=Time.frameCount; pendingIntroRun = run; introDeadline = Time.unscaledTime + 3;
        }
        private bool TickContractIntro(float delta)
        {
            if (!Connected || Leaving || Training)
            { CancelContractIntro(); return false; }
            if (pendingIntroRun != null)
            {
                if (Time.unscaledTime > introDeadline || world.Phase == RecoveryPhase.Complete) pendingIntroRun = null;
                else if (Time.frameCount>pendingIntroFrame && world.HasContract && world.RunId == pendingIntroRun && world.ContractIndex == pendingIntroIndex &&
                    world.Casualty.ModelIndex == world.Contract.TargetModel)
                {
                    CancelActiveIntro(); introRun = pendingIntroRun; pendingIntroRun = null;
                    contractIntro = new RecoveryContractIntro(world.Casualty, world.Contract, transform, Profile.reducedMotion);
                    contractIntro.ConfigureAudio(Audio?.Bank,Profile.effects);
                    introPreviousTimeScale = Time.timeScale; introOwnsPause = Solo;
                    if (introOwnsPause) Time.timeScale = 0;
                    introSkipReleased = false; ConsumeMenuInput();
                }
            }
            if (contractIntro == null) return false;
            if (!world.HasContract || world.RunId != introRun || View != "Gameplay")
            { CancelContractIntro(); return false; }
            var keyboard = Keyboard.current; var pad = RecoveryInputDevices.Pad;
            bool held = keyboard?.enterKey.isPressed == true || keyboard?.spaceKey.isPressed == true || keyboard?.escapeKey.isPressed == true ||
                RecoveryInputPrompts.Confirm(pad)?.isPressed == true || RecoveryInputPrompts.Cancel(pad)?.isPressed == true || pad?.startButton.isPressed == true;
            if (!held) introSkipReleased = true;
            if (held && introSkipReleased && contractIntro.CanSkip) { contractIntro.Skip(); ConsumeMenuInput(); }
            bool focused=Application.isFocused&&!hapticPaused;
            contractIntro.Advance(focused?Mathf.Min(delta,.1f):0,focused);
            // Every peer brakes its own car; neither the shared world nor the other player is paused in co-op.
            if (Time.unscaledTime >= nextInput) { nextInput = Time.unscaledTime + .1f; world.Drive(Vector2.zero, true, false); }
            if (contractIntro.Finished) { CancelActiveIntro(); ConsumeMenuInput(); }
            return true;
        }
        internal bool ContractIntroCamera(out Vector3 position, out Vector3 look)
        {
            if (contractIntro != null && contractIntro.ShowingIncident) { contractIntro.CameraPose(out position, out look); return true; }
            position = look = default; return false;
        }
        private void CancelActiveIntro()
        {
            contractIntro?.Dispose(); contractIntro = null;
            if (introOwnsPause) Time.timeScale = introPreviousTimeScale;
            introOwnsPause = false;
        }
        private void CancelContractIntro() { pendingIntroRun = null; CancelActiveIntro(); }
    }
}
