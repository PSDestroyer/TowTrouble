using UnityEngine;

namespace TowTrouble
{
    public sealed partial class RecoveryCamera
    {
        private Light titleSun, titleSkyFill;
        private Quaternion titleSunRotation, titleFillRotation;
        private Color titleSunColor, titleFillColor;
        private float titleSunIntensity, titleFillIntensity;
        private bool titleLightingApplied;

        private void UpdateTitleLighting(bool active)
        {
            if (!Application.isPlaying || !active)
            {
                DisableTitleLighting();
                return;
            }
            if (!titleLightingApplied)
            {
                titleSun = RenderSettings.sun;
                if (titleSun == null) return;
                titleSunRotation = titleSun.transform.rotation;
                titleSunColor = titleSun.color;
                titleSunIntensity = titleSun.intensity;
                // Reuse the existing authored fill; no extra realtime lights or shadow maps.
                foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (light.gameObject.scene == gameObject.scene && light.type == LightType.Directional && light.name == "Soft sky fill")
                    { titleSkyFill = light; break; }
                if (titleSkyFill != null)
                {
                    titleFillRotation = titleSkyFill.transform.rotation;
                    titleFillColor = titleSkyFill.color;
                    titleFillIntensity = titleSkyFill.intensity;
                }
                titleLightingApplied = true;
            }
            var car = session.World.Drivers[Mathf.Max(0, session.LocalSlot)];
            // Warm front-side sunlight reveals the hood, cabin and wheel shapes.
            // Keep it relative to the parked car in either supported menu scene.
            titleSun.transform.rotation = Quaternion.LookRotation(car.transform.TransformDirection(new Vector3(.42f, -.70f, -.58f)));
            titleSun.color = new Color(1, .89f, .74f);
            titleSun.intensity = 1.75f;
            if (titleSkyFill != null)
            {
                titleSkyFill.transform.rotation = Quaternion.LookRotation(car.transform.TransformDirection(new Vector3(-.55f, -.35f, -.76f)));
                titleSkyFill.color = new Color(1, .95f, .86f);
                titleSkyFill.intensity = .42f;
            }
        }

        private void DisableTitleLighting()
        {
            if (!titleLightingApplied) return;
            if (titleSun != null)
            {
                titleSun.transform.rotation = titleSunRotation;
                titleSun.color = titleSunColor;
                titleSun.intensity = titleSunIntensity;
            }
            if (titleSkyFill != null)
            {
                titleSkyFill.transform.rotation = titleFillRotation;
                titleSkyFill.color = titleFillColor;
                titleSkyFill.intensity = titleFillIntensity;
            }
            titleSun = titleSkyFill = null;
            titleLightingApplied = false;
        }
    }
}
