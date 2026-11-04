using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    internal sealed class RecoveryTreeFade
    {
        private static readonly int Fade = Shader.PropertyToID("_TowTreeFade");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private readonly Renderer[] renderers;
        private readonly List<Slot> slots = new List<Slot>();
        private readonly List<(Renderer renderer, Material[] original)> replaced = new List<(Renderer, Material[])>();
        private readonly List<Material> owned = new List<Material>();
        private bool captured;

        internal RecoveryTreeFade(Renderer[] renderers) => this.renderers = renderers;

        internal static float Advance(float progress, bool hide, float dt) =>
            Mathf.MoveTowards(progress, hide ? 1f : 0f, dt / (hide ? .32f : .58f));

        internal void Apply(float progress)
        {
            if (progress <= 0f) { Restore(); return; }
            if (!captured)
            {
                Capture();
                captured = true;
            }
            float amount = Mathf.SmoothStep(0f, 1f, progress);
            foreach (var slot in slots) slot.Apply(amount);
        }

        private void Capture()
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var original = renderer.sharedMaterials;
                Material[] withSnow = null;
                for (int i = 0; i < original.Length; i++)
                {
                    var material = original[i];
                    if (material == null) continue;
                    bool useAlpha = !material.HasProperty(Fade);
                    if (useAlpha)
                    {
                        // The two embedded pine snow caps use stock URP Lit rather than the vegetation graphs.
                        if (material.shader.name != "Universal Render Pipeline/Lit") continue;
                        withSnow ??= (Material[])original.Clone();
                        var copy = AlphaBlendCopy(material);
                        owned.Add(copy);
                        withSnow[i] = copy;
                    }
                    slots.Add(new Slot(renderer, i, material, useAlpha));
                }
                if (withSnow != null)
                {
                    replaced.Add((renderer, original));
                    renderer.sharedMaterials = withSnow;
                }
            }
        }

        internal void Restore()
        {
            captured = false;
            foreach (var slot in slots) slot.Restore();
            slots.Clear();
            foreach (var pair in replaced)
                if (pair.renderer != null) pair.renderer.sharedMaterials = pair.original;
            replaced.Clear();
            foreach (var material in owned)
                if (Application.isPlaying) Object.Destroy(material); else Object.DestroyImmediate(material);
            owned.Clear();
        }

        private static Material AlphaBlendCopy(Material source)
        {
            var copy = new Material(source) { hideFlags = HideFlags.HideAndDontSave, renderQueue = (int)RenderQueue.Transparent };
            copy.SetOverrideTag("RenderType", "Transparent");
            copy.SetFloat("_Surface", 1f);
            copy.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            copy.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            copy.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            copy.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
            copy.SetInt("_ZWrite", 0);
            copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            copy.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return copy;
        }

        private sealed class Slot
        {
            private readonly Renderer renderer;
            private readonly int index;
            private readonly MaterialPropertyBlock original = new MaterialPropertyBlock();
            private readonly MaterialPropertyBlock working = new MaterialPropertyBlock();
            private readonly bool useAlpha;
            private readonly Color tint;

            internal Slot(Renderer renderer, int index, Material material, bool useAlpha)
            {
                this.renderer = renderer;
                this.index = index;
                this.useAlpha = useAlpha;
                renderer.GetPropertyBlock(original, index);
                // Per-material overrides take precedence over renderer-wide overrides in Unity.
                if (original.isEmpty) renderer.GetPropertyBlock(working);
                else renderer.GetPropertyBlock(working, index);
                if (useAlpha) tint = working.HasColor(BaseColor) ? working.GetColor(BaseColor) : material.GetColor(BaseColor);
            }

            internal void Apply(float amount)
            {
                if (renderer == null) return;
                if (useAlpha)
                    working.SetColor(BaseColor, new Color(tint.r, tint.g, tint.b, tint.a * (1f - amount)));
                else
                    working.SetFloat(Fade, amount);
                renderer.SetPropertyBlock(working, index);
            }

            internal void Restore()
            {
                if (renderer != null) renderer.SetPropertyBlock(original.isEmpty ? null : original, index);
            }
        }
    }
}
