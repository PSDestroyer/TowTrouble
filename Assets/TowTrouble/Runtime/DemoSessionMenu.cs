using System.Collections;
using UnityEngine;

namespace TowTrouble
{
    public sealed partial class DemoSession
    {
        private static bool returnToMain;
        public bool Leaving => leaving;
        public bool ConsumeReturnToMain() { bool value = returnToMain; returnToMain = false; return value; }
        public void CancelConnection()
        {
            Connecting = false; quickStart = false; connectDeadline = 0;
            network.ClientManager.StopConnection();
            if (IsHost) network.ServerManager.StopConnection(true);
            menuOpen = false; Time.timeScale = 1; status = "Connection cancelled. You can try again.";
        }
        public void LeaveToMain()
        {
            if (Training) EndTraining(false);
            returnToMain = true; Leave();
        }
        public void ResetProgress()
        {
            if (Connected || Connecting) return;
            Profile = RecoveryMenuSettings.WithoutProgress(Profile);
            Profile.onboarded = true;
            Profile.Save();
            ApplySettings();
        }
        public void PreviewFittedRide()
        {
            if (Connected || !Profile.starterChosen) return;
            world.PreviewVehicle(Profile.vehicle);
            world.PreviewPaint(Profile.paint);
            world.Drivers[0].GetComponent<RecoveryCustomization>()?.Apply(Profile.Build(Profile.vehicle), Profile.paint, true);
        }
        private RecoveryGraphicsPreference graphicsPreference;
        private void ApplyGraphicsPreference()
        {
            if (!Application.isPlaying) return;
            if (graphicsPreference == null) graphicsPreference = new RecoveryGraphicsPreference();
            graphicsPreference.Apply(RecoveryPlatform.GraphicsQuality, Profile.quality == 0);
        }
        private void RestoreGraphicsPreference()
        {
            graphicsPreference?.Dispose();
            graphicsPreference = null;
        }
        private void MenuConnectionFailed()
        { menuOpen = false; GarageCameraInputBlocked = false; Time.timeScale = 1; }
    }
}
