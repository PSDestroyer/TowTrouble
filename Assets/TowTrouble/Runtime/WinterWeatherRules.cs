using UnityEngine;

namespace TowTrouble
{
    public readonly struct WinterWeatherState
    {
        public readonly int Current, Next;
        public readonly float Blend, SecondsToNext, Snow, Gusts, Ice;
        internal WinterWeatherState(int current, int next, float blend, float remaining)
        {
            Current = current; Next = next; Blend = blend; SecondsToNext = remaining;
            Snow = Mathf.Lerp(SnowFor(current), SnowFor(next), blend);
            Gusts = Mathf.Lerp(current == 2 ? 1 : 0, next == 2 ? 1 : 0, blend);
            Ice = Mathf.Lerp(current == 3 ? 1 : .08f, next == 3 ? 1 : .08f, blend);
        }
        private static float SnowFor(int phase) => phase == 0 ? .22f : phase == 1 ? .62f : phase == 2 ? .96f : .14f;
        public string Name => WinterWeatherRules.Name(Blend < .5f ? Current : Next);
        public string Forecast => WinterWeatherRules.Name(Current) + " → " + WinterWeatherRules.Name(Next) +
            (Blend > .05f ? " · conditions changing" : " in ~" + Mathf.Max(1, Mathf.CeilToInt(SecondsToNext / 60f)) + " min");
    }

    public static class WinterWeatherRules
    {
        public const float Period = 240f, Transition = 35f;
        internal static readonly Vector3[] IceCenters = { new(117, 66.1f, 161.6f), new(-60, 53.2f, 183), new(345, 30.4f, -225), new(-140, 22.1f, -194) };
        internal static readonly Vector3[] IceDirections = { Vector3.right, new Vector3(1, 0, .18f).normalized, Vector3.forward, Vector3.forward };
        // A readable sequence, shared with the existing world clock. Menus pause that clock in solo.
        public static WinterWeatherState At(float seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int phase = Mathf.FloorToInt(seconds / Period) % 4;
            float elapsed = seconds % Period;
            return new WinterWeatherState(phase, (phase + 1) % 4,
                Mathf.SmoothStep(0, 1, Mathf.InverseLerp(Period - Transition, Period, elapsed)), Period - elapsed);
        }
        public static string Name(int phase) => phase == 0 ? "Light flurries" : phase == 1 ? "Snowfall" : phase == 2 ? "Blowing snow" : "Freeze";
        public static float Grip(Vector3 point, bool packed, WinterWeatherState weather)
        {
            if (!packed) return 1f - weather.Gusts * .05f;
            return 1f - weather.Ice * IceExposure(point) * .32f;
        }
        public static float IceExposure(Vector3 point)
        {
            float exposure = 0;
            for (int i = 0; i < IceCenters.Length; i++)
            {
                var delta = point - IceCenters[i];
                if (Mathf.Abs(delta.y) > 4) continue;
                float along = Vector3.Dot(delta, IceDirections[i]) / 7f;
                float across = Vector3.Dot(delta, Vector3.Cross(Vector3.up, IceDirections[i])) / 1.45f;
                exposure = Mathf.Max(exposure, 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(along * along + across * across)));
            }
            return exposure;
        }
    }
}
