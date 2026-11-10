using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TowTrouble
{
    /// <summary>Owns session graphics overrides without editing the authored pipeline assets.</summary>
    public sealed class RecoveryGraphicsPreference : IDisposable
    {
        private bool initialized;
        private int previousLevel, platformLevel;
        private RenderPipelineAsset previousPipeline, platformPipelineOverride;
        private float previousLodBias, platformLodBias;
        private UniversalRenderPipelineAsset platformPipeline, performancePipeline;

        public void Apply(string platformQuality, bool performance)
        {
            if (!initialized)
            {
                previousLevel = QualitySettings.GetQualityLevel();
                previousPipeline = QualitySettings.renderPipeline;
                previousLodBias = QualitySettings.lodBias;
                int level = Array.IndexOf(QualitySettings.names, platformQuality);
                if (level >= 0 && level != previousLevel) QualitySettings.SetQualityLevel(level, true);
                else if (platformQuality != null && level < 0)
                    Debug.LogWarning("Tow Trouble quality preset is missing: " + platformQuality);
                platformLevel = QualitySettings.GetQualityLevel();
                platformPipelineOverride = QualitySettings.renderPipeline;
                platformPipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                platformLodBias = QualitySettings.lodBias;
                initialized = true;
            }
            if (platformPipeline == null) return;
            if (performance)
            {
                if (performancePipeline == null)
                {
                    bool switchQuality = platformQuality == "Switch";
                    performancePipeline = UnityEngine.Object.Instantiate(platformPipeline);
                    performancePipeline.name = "Tow Trouble " + platformQuality + " Performance (runtime)";
                    performancePipeline.hideFlags = HideFlags.DontSave;
                    performancePipeline.renderScale = Mathf.Min(switchQuality ? .75f : .85f, platformPipeline.renderScale);
                    performancePipeline.msaaSampleCount = 1;
                    performancePipeline.shadowDistance = Mathf.Min(switchQuality ? 25 : 35, platformPipeline.shadowDistance);
                    performancePipeline.shadowCascadeCount = 1;
                }
                if (QualitySettings.renderPipeline != performancePipeline) QualitySettings.renderPipeline = performancePipeline;
                QualitySettings.lodBias = Mathf.Min(1, platformLodBias);
            }
            else
            {
                if (QualitySettings.renderPipeline != platformPipeline) QualitySettings.renderPipeline = platformPipeline;
                QualitySettings.lodBias = platformLodBias;
            }
        }

        public void Dispose()
        {
            if (!initialized) return;
            // Overrides belong to a quality level. Restore it before leaving that level,
            // otherwise a future session can find a destroyed runtime clone in its slot.
            QualitySettings.SetQualityLevel(platformLevel, true);
            QualitySettings.renderPipeline = platformPipelineOverride;
            QualitySettings.lodBias = platformLodBias;
            QualitySettings.SetQualityLevel(previousLevel, true);
            QualitySettings.renderPipeline = previousPipeline;
            QualitySettings.lodBias = previousLodBias;
            if (performancePipeline != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(performancePipeline);
                else UnityEngine.Object.DestroyImmediate(performancePipeline);
            }
            performancePipeline = null;
            platformPipeline = null;
            initialized = false;
        }
    }
}
