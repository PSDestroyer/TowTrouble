using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    [DisallowMultipleComponent, DefaultExecutionOrder(170)]
    internal sealed class RecoveryBodyCoating : MonoBehaviour
    {
        private static readonly int SnowId = Shader.PropertyToID("_SnowAmounts");
        private static readonly int MudId = Shader.PropertyToID("_BodyMudAmounts");
        private static readonly int SandId = Shader.PropertyToID("_BodySandAmounts");
        private static readonly int LayoutId = Shader.PropertyToID("_SnowWheelLayout");
        private static readonly int WorldToVehicleId = Shader.PropertyToID("_SnowWorldToVehicle");
        private static Shader shader;
        private const float SampleInterval = .1f;
        private readonly List<(Renderer Renderer, int Slot)> slots = new();
        private readonly List<(Renderer Renderer, Material[] Original)> stockSlots = new();
        private readonly List<Material> stockMaterials = new();
        private readonly Vector3[] amounts = new Vector3[4];
        private MaterialPropertyBlock properties;
        private RecoveryVehicle vehicle;
        private Vector4 wheelLayout, snow, mud, sand;
        private float nextSample, lastSample;
        private bool needsApply;

        internal void UseShader(Material material)
        {
            // Preserve the resource GUID/key of the original DLC shader and its existing URP properties.
            if (shader == null) shader = Resources.Load<Shader>("WinterBodySnow");
            if (shader == null || !shader.isSupported || material.shader.name != "Universal Render Pipeline/Lit" ||
                material.renderQueue >= (int)RenderQueue.AlphaTest || material.GetFloat("_Surface") != 0 ||
                material.IsKeywordEnabled("_ALPHATEST_ON")) return;
            var keywords = material.shaderKeywords;
            material.shader = shader;
            material.shaderKeywords = keywords;
        }

        internal void BindStock(RecoveryVehicle car, GameObject assembly)
        {
            Unbind();
            var tires = new HashSet<Renderer>();
            for (int wheel = 0; wheel < 4; wheel++)
                foreach (var renderer in car.WheelVisual(wheel).GetComponentsInChildren<Renderer>(true)) tires.Add(renderer);
            var reused = new Dictionary<Material, Material>();
            foreach (var renderer in assembly.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || tires.Contains(renderer)) continue;
                var original = renderer.sharedMaterials; var fitted = (Material[])original.Clone(); bool changed = false;
                for (int slot = 0; slot < fitted.Length; slot++)
                {
                    var source = original[slot]; if (source == null) continue;
                    if (!reused.TryGetValue(source, out var copy))
                    {
                        copy = new Material(source) { name = source.name + " - body coating" };
                        UseShader(copy);
                        if (copy.shader != shader) { Destroy(copy); continue; }
                        stockMaterials.Add(copy); reused.Add(source, copy);
                    }
                    fitted[slot] = copy; changed = true;
                }
                if (!changed) continue;
                stockSlots.Add((renderer, original)); renderer.sharedMaterials = fitted;
            }
            Bind(car, assembly);
        }

        internal void Bind(RecoveryVehicle car, GameObject assembly)
        {
            vehicle = car; slots.Clear();
            foreach (var renderer in assembly.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                    if (materials[slot] != null && materials[slot].shader == shader) slots.Add((renderer, slot));
            }
            Vector3 front = transform.InverseTransformPoint(car.WheelAnchor(0));
            Vector3 rear = transform.InverseTransformPoint(car.WheelAnchor(2));
            Vector3 opposite = transform.InverseTransformPoint(car.WheelAnchor(1));
            wheelLayout = new Vector4(front.z, rear.z, Mathf.Abs(front.x - opposite.x) * .5f, (front.y + rear.y) * .5f);
            lastSample = Time.time; nextSample = Time.time + SampleInterval; needsApply = true;
        }

        internal void Unbind()
        {
            properties ??= new MaterialPropertyBlock();
            foreach (var (renderer, slot) in slots)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(properties, slot);
                properties.SetVector(SnowId, Vector4.zero); properties.SetVector(MudId, Vector4.zero); properties.SetVector(SandId, Vector4.zero);
                renderer.SetPropertyBlock(properties, slot);
            }
            slots.Clear();
            foreach (var (renderer, original) in stockSlots) if (renderer != null) renderer.sharedMaterials = original;
            stockSlots.Clear();
            foreach (var material in stockMaterials) if (material != null) Destroy(material);
            stockMaterials.Clear();
        }

        private void LateUpdate()
        {
            if (slots.Count == 0 || vehicle == null || vehicle.Body == null) return;
            if (Time.time >= nextSample)
            {
                float elapsed = Mathf.Min(.25f, Time.time - lastSample);
                lastSample = Time.time; nextSample = Time.time + SampleInterval;
                for (int wheel = 0; wheel < 4; wheel++)
                    if (vehicle.TryGetWheelContact(wheel, out var hit))
                        amounts[wheel] = RecoveryBodyCoatingRules.Step(amounts[wheel], RecoveryBodyCoatingRules.Classify(hit, vehicle.WinterDriving),
                            vehicle.Speed, vehicle.SlipAtWheel(wheel), elapsed);
            }
            var targetSnow = new Vector4(amounts[0].x, amounts[1].x, amounts[2].x, amounts[3].x);
            var targetMud = new Vector4(amounts[0].y, amounts[1].y, amounts[2].y, amounts[3].y);
            var targetSand = new Vector4(amounts[0].z, amounts[1].z, amounts[2].z, amounts[3].z);
            float blend = 1 - Mathf.Exp(-7f * Time.deltaTime);
            var previousSnow = snow; var previousMud = mud; var previousSand = sand;
            snow = Vector4.Lerp(snow, targetSnow, blend); mud = Vector4.Lerp(mud, targetMud, blend); sand = Vector4.Lerp(sand, targetSand, blend);
            bool changed = (snow - previousSnow).sqrMagnitude + (mud - previousMud).sqrMagnitude + (sand - previousSand).sqrMagnitude > .00000001f;
            if (!needsApply && !changed && snow.sqrMagnitude + mud.sqrMagnitude + sand.sqrMagnitude < .000001f) return;
            needsApply = false;
            properties ??= new MaterialPropertyBlock();
            var worldToVehicle = transform.worldToLocalMatrix;
            foreach (var (renderer, slot) in slots)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(properties, slot);
                properties.SetMatrix(WorldToVehicleId, worldToVehicle); properties.SetVector(LayoutId, wheelLayout);
                properties.SetVector(SnowId, snow); properties.SetVector(MudId, mud); properties.SetVector(SandId, sand);
                renderer.SetPropertyBlock(properties, slot);
            }
        }
        private void OnDestroy() => Unbind();
    }
}
