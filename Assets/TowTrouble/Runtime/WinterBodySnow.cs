using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TowTrouble
{
    [DisallowMultipleComponent]
    internal sealed class WinterBodySnow : MonoBehaviour
    {
        private static readonly int AmountsId = Shader.PropertyToID("_SnowAmounts");
        private static readonly int LayoutId = Shader.PropertyToID("_SnowWheelLayout");
        private static readonly int WorldToVehicleId = Shader.PropertyToID("_SnowWorldToVehicle");
        private static Shader shader;
        private const float SampleInterval = .1f;
        private readonly List<(Renderer Renderer, int Slot)> slots = new();
        private MaterialPropertyBlock properties;
        private RecoveryVehicle vehicle;
        private Vector4 wheelLayout;
        private Vector4 amounts;
        private float nextSample;
        private bool snowWasVisible;

        internal void UseSnowShader(Material material)
        {
            if (shader == null) shader = Resources.Load<Shader>("WinterBodySnow");
            if (shader == null || !shader.isSupported || material.shader.name != "Universal Render Pipeline/Lit" ||
                material.renderQueue >= (int)RenderQueue.AlphaTest || material.GetFloat("_Surface") != 0 ||
                material.IsKeywordEnabled("_ALPHATEST_ON"))
                return;

            var keywords = material.shaderKeywords;
            material.shader = shader;
            material.shaderKeywords = keywords;
        }

        internal void Bind(RecoveryVehicle car, GameObject assembly)
        {
            vehicle = car;
            slots.Clear();
            foreach (var renderer in assembly.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled) continue;
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                    if (materials[slot] != null && materials[slot].shader == shader)
                        slots.Add((renderer, slot));
            }

            Vector3 front = transform.InverseTransformPoint(car.WheelAnchor(0));
            Vector3 rear = transform.InverseTransformPoint(car.WheelAnchor(2));
            Vector3 opposite = transform.InverseTransformPoint(car.WheelAnchor(1));
            wheelLayout = new Vector4(front.z, rear.z, Mathf.Abs(front.x - opposite.x) * .5f,
                (front.y + rear.y) * .5f);
        }

        internal void Unbind()
        {
            slots.Clear();
            snowWasVisible = false;
        }

        private void LateUpdate()
        {
            if (slots.Count == 0 || vehicle.Body == null) return;
            if (Time.time >= nextSample)
            {
                nextSample = Time.time + SampleInterval;
                SampleSnow();
            }

            bool visible = amounts.sqrMagnitude > .0001f;
            if (!visible && !snowWasVisible) return;
            snowWasVisible = visible;
            Matrix4x4 worldToVehicle = transform.worldToLocalMatrix;
            properties ??= new MaterialPropertyBlock();
            foreach (var (renderer, slot) in slots)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(properties, slot);
                properties.SetMatrix(WorldToVehicleId, worldToVehicle);
                properties.SetVector(LayoutId, wheelLayout);
                properties.SetVector(AmountsId, visible ? amounts : Vector4.zero);
                renderer.SetPropertyBlock(properties, slot);
            }
        }

        private void SampleSnow()
        {
            float speed = vehicle.Speed;
            float motion = Mathf.InverseLerp(.35f, 6.5f, speed);
            for (int wheel = 0; wheel < 4; wheel++)
            {
                if (!vehicle.TryGetWheelContact(wheel, out var hit)) continue;
                if (WinterDrivingSurface.IsSnow(hit, out bool packed))
                {
                    float spin = Mathf.InverseLerp(.12f, .68f, vehicle.SlipAtWheel(wheel));
                    float spray = Mathf.Max(motion, spin * .85f);
                    float rate = packed ? .065f : Mathf.Lerp(.32f, .64f, Mathf.InverseLerp(.05f, .22f, vehicle.SnowSink));
                    amounts[wheel] = Mathf.MoveTowards(amounts[wheel], 1, SampleInterval * spray * rate);
                }
                else
                {
                    amounts[wheel] = Mathf.MoveTowards(amounts[wheel], 0, SampleInterval * motion * .045f);
                }
            }
        }
    }
}
