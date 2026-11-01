using UnityEngine;

namespace TowTrouble
{
    // Scene-owned, presentation only. The host's extraction stage triggers this
    // on each peer; these particles never change contact, forces or progression.
    public sealed class RecoveryExtractionVisuals : MonoBehaviour
    {
        [SerializeField] private int siteIndex;
        [SerializeField] private ParticleSystem soilDust, soilClods;

        public bool Matches(int index) => index == siteIndex;
        public void Configure(int index, ParticleSystem dust, ParticleSystem clods)
        { siteIndex = index; soilDust = dust; soilClods = clods; }

        public static int ParticleCount(float gain, int maximum) => Mathf.RoundToInt(Mathf.Clamp01(gain) * Mathf.Max(0, maximum));

        public void ShowRelease(Vector3 point, Vector3 exit, float gain)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            exit = Vector3.ProjectOnPlane(exit, Vector3.up).normalized;
            if (exit.sqrMagnitude < .1f) exit = Vector3.forward;
            var side = Vector3.Cross(Vector3.up, exit);
            // Deterministic scatter avoids consuming gameplay's shared Random state.
            int dust = ParticleCount(gain, 18), clods = ParticleCount(gain, 14);
            if (dust > 0 && soilDust != null) soilDust.Play(false);
            if (clods > 0 && soilClods != null) soilClods.Play(false);
            for (int i = 0; soilDust != null && i < dust; i++)
            {
                float angle = i * 2.399963f, spread = .1f + .23f * Mathf.Repeat(i * .618034f, 1);
                var radial = side * Mathf.Cos(angle) + exit * Mathf.Sin(angle);
                var particle = new ParticleSystem.EmitParams
                {
                    position = point + Vector3.up * .08f + radial * spread,
                    velocity = radial * .55f - exit * .25f + Vector3.up * .35f,
                    startSize = .35f + .25f * Mathf.Repeat(i * .381966f, 1),
                    startLifetime = .55f + .25f * Mathf.Repeat(i * .618034f, 1)
                };
                soilDust.Emit(particle, 1);
            }
            for (int i = 0; soilClods != null && i < clods; i++)
            {
                float angle = i * 2.399963f, fraction = Mathf.Repeat(i * .618034f, 1);
                var radial = side * Mathf.Cos(angle) + exit * Mathf.Sin(angle);
                var particle = new ParticleSystem.EmitParams
                {
                    position = point + Vector3.up * .08f + radial * .14f,
                    velocity = radial * .8f - exit * .35f + Vector3.up * (1.8f + fraction * .6f),
                    startSize = .07f + fraction * .09f,
                    startLifetime = .35f + fraction * .15f,
                    rotation3D = new Vector3(angle * 40, angle * 23, angle * 17)
                };
                soilClods.Emit(particle, 1);
            }
        }

        public void Clear()
        {
            if (!Application.isPlaying) return;
            if (soilDust != null) soilDust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (soilClods != null) soilClods.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnDisable() => Clear();
    }
}
