using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TowTrouble
{
    /// <summary>Small LDR colour correction in the existing LUT; no additional render pass or shared asset edits.</summary>
    public sealed class RecoverySwitchColorGrade : IDisposable
    {
        private ColorAdjustments grade;
        private float saturation, contrast, exposure;
        private bool saturationOverride, contrastOverride, exposureOverride;
        public void Apply(ColorAdjustments value, float night, float wet)
        {
            if (!RecoveryPlatform.UsesSwitchRendering || value == null) { Dispose(); return; }
            if (grade != value)
            {
                Dispose(); grade = value;
                saturation = value.saturation.value; contrast = value.contrast.value; exposure = value.postExposure.value;
                saturationOverride = value.saturation.overrideState;
                contrastOverride = value.contrast.overrideState;
                exposureOverride = value.postExposure.overrideState;
            }
            float daylight = 1 - Mathf.Clamp01(night);
            float weather = 1 - .2f * Mathf.Clamp01(wet);
            grade.saturation.Override(Mathf.Clamp(saturation + Mathf.Lerp(4, 8, daylight) * weather, -100, 100));
            grade.contrast.Override(Mathf.Clamp(contrast + Mathf.Lerp(1, 4, daylight), -100, 100));
            grade.postExposure.Override(exposure - Mathf.Lerp(.02f, .06f, daylight));
        }
        public void Dispose()
        {
            if (grade == null) return;
            grade.saturation.value = saturation; grade.saturation.overrideState = saturationOverride;
            grade.contrast.value = contrast; grade.contrast.overrideState = contrastOverride;
            grade.postExposure.value = exposure; grade.postExposure.overrideState = exposureOverride;
            grade = null;
        }
    }
}
