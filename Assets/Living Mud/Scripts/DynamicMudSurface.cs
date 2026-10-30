using System;
using UnityEngine;

namespace Dinopunch.LivingMud
{
    [Flags]
    public enum MudType {
        Watery = 1 << 0,
        Sloppy = 1 << 1,
        Dry = 1<< 2
    }

    /// <summary>
    /// Store data and provide references for a Dynamic Mud Surface
    /// </summary>
    public class DynamicMudSurface : MonoBehaviour
    {
        [Tooltip("What type of mud is this surface")]
        [SerializeField] private MudType thickness;
        public MudType Thickness => thickness;

        [Tooltip("Maximum size in units of the surface")]
        [SerializeField] float boundingBoxSizeInUnits = 10.0f;
        public float BoundingBoxSizeInUnits => boundingBoxSizeInUnits;

        [Tooltip("Use the CustomRenderTexture assigned here")]
        [SerializeField] bool OverrideMaterialStampTexture;

        [Tooltip("Reference to CustomRenderTexture stamps use on this surface")]
        [SerializeField] CustomRenderTexture customRenderTexture;

        private Material material;
        public Material Material => material;

        private Shader shader;
        public Shader Shader => shader;

        private CustomRenderTexture stampRenderTexture;
        public CustomRenderTexture StampRenderTexture => stampRenderTexture;      

        public Texture2D GetColorMap()
        {
            if (material == null)
                return null;
            else
                return (Texture2D)material.GetTexture("_Albedo");
        }

        public Color GetColor()
        {
            if (material == null || !material.HasColor("_Tint"))
                return Color.white;
            else
                return material.GetColor("_Tint");
        }

        private void OnValidate () => SetReferences();
        private void Awake() => SetReferences();

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ClearStampTexture();
        }

        public void ClearStampTexture()
        {
            if (stampRenderTexture == null)
                return;

            var previousRenderTexture = RenderTexture.active;
            RenderTexture.active = stampRenderTexture;

            Graphics.ClearRandomWriteTargets();
            Graphics.SetRenderTarget(stampRenderTexture);
            GL.Clear(true, true, Color.clear);

            var doubleBufferedTex = stampRenderTexture.GetDoubleBufferRenderTexture();
            RenderTexture.active = doubleBufferedTex;

            Graphics.ClearRandomWriteTargets();
            Graphics.SetRenderTarget(doubleBufferedTex);
            GL.Clear(true, true, Color.clear);

            RenderTexture.active = previousRenderTexture;
        }

        /// <summary>
        /// Set references for the Custom Render Texture that stores stamps applied and displayed on the surface
        /// </summary>
        // Integration hook: rebind after assigning an isolated session material.
        public void RefreshReferences() => SetReferences();

        private void SetReferences()
        {
            
            if (TryGetComponent<Renderer>(out var renderer))
            {
                material = renderer.sharedMaterial;
                shader = material.shader;
                stampRenderTexture = (CustomRenderTexture)material.GetTexture("_Stamp_Render_Texture_1");
            }
            else
            {
                Debug.LogError("This Dynamic Mud Surface does not have a renderer component");
                return;
            }

            if (OverrideMaterialStampTexture && customRenderTexture == null)
            {
                Debug.LogError("Override Material Stamp Texture is selected but no Custom Render Texture has been assigned to the Dynamic Mud Surface");
            }
            else if (OverrideMaterialStampTexture)
            {
                stampRenderTexture = customRenderTexture;
                material.SetTexture("_Stamp_Render_Texture_1", stampRenderTexture);
            }

            if (stampRenderTexture == null)
            {
                Debug.LogError("No Render Texture Assigned for Dynamic Mud Stamps");
                return;
            }
        }
    }
}
