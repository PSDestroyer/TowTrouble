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
        private UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset originalPipeline,performancePipeline;
        private float originalLodBias;
        private void ApplyGraphicsPreference()
        {
            // Desktop quality is a runtime copy; imported render assets and other platform presets stay immutable.
            if(!Application.isPlaying)return;
            if(originalPipeline==null)
            {
                int desktop=Mathf.Max(0,QualitySettings.names.Length-1);
                if(QualitySettings.GetQualityLevel()!=desktop)QualitySettings.SetQualityLevel(desktop,true);
                originalPipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
                originalLodBias=QualitySettings.lodBias;
            }
            if(originalPipeline==null)return;
            if(Profile.quality==0)
            {
                if(performancePipeline==null){performancePipeline=Instantiate(originalPipeline);performancePipeline.name="Tow Trouble Performance (runtime)";performancePipeline.renderScale=Mathf.Min(.85f,originalPipeline.renderScale);performancePipeline.msaaSampleCount=1;performancePipeline.shadowDistance=Mathf.Min(35,originalPipeline.shadowDistance);performancePipeline.shadowCascadeCount=1;}
                if(QualitySettings.renderPipeline!=performancePipeline)QualitySettings.renderPipeline=performancePipeline;QualitySettings.lodBias=Mathf.Min(1,originalLodBias);
            }
            else {if(QualitySettings.renderPipeline!=originalPipeline)QualitySettings.renderPipeline=originalPipeline;QualitySettings.lodBias=originalLodBias;}
        }
        private void RestoreGraphicsPreference()
        {
            if(originalPipeline!=null){QualitySettings.renderPipeline=originalPipeline;QualitySettings.lodBias=originalLodBias;}
            if(performancePipeline!=null)Destroy(performancePipeline);
        }
        private void MenuConnectionFailed()
        { menuOpen = false; GarageCameraInputBlocked = false; Time.timeScale = 1; }
    }
}
