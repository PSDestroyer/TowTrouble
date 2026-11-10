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
        private Camera switchLens;
        private float previousSwitchFarClip;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool switchGraphicsLogged;
#endif
        private void ApplyGraphicsPreference()
        {
            if (!Application.isPlaying) return;
            if (graphicsPreference == null) graphicsPreference = new RecoveryGraphicsPreference();
            graphicsPreference.Apply(RecoveryPlatform.GraphicsQuality, Profile.quality == 0);
            ApplySwitchCameraBudget();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(RecoveryPlatform.UsesSwitchRendering&&!switchGraphicsLogged)
            {
                switchGraphicsLogged=true;
                var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                Debug.Log($"[Tow Trouble Switch] target=30fps output={Screen.width}x{Screen.height} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} scale={pipeline?.renderScale:0.00} upscale={pipeline?.upscalingFilter} HDR={pipeline?.supportsHDR} shadow={pipeline?.shadowDistance:0}m grass={QualitySettings.terrainDetailDistance:0}m trees={QualitySettings.terrainTreeDistance:0}m far={switchLens.farClipPlane:0}m");
            }
#endif
        }
        private void ApplySwitchCameraBudget()
        {
            if(!RecoveryPlatform.UsesSwitchRendering||switchLens!=null)return;
            switchLens=recoveryCamera.GetComponent<Camera>();previousSwitchFarClip=switchLens.farClipPlane;
            switchLens.farClipPlane=Mathf.Min(previousSwitchFarClip,RecoverySwitchGraphics.CameraDistance);
        }
        private void RestoreGraphicsPreference()
        {
            if(switchLens!=null)switchLens.farClipPlane=previousSwitchFarClip;
            switchLens=null;
            graphicsPreference?.Dispose();
            graphicsPreference = null;
        }
        private void MenuConnectionFailed()
        { menuOpen = false; GarageCameraInputBlocked = false; Time.timeScale = 1; }
    }
}
