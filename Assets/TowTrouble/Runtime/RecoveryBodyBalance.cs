using UnityEngine;

namespace TowTrouble
{
    public static class RecoveryBodyBalance
    {
        public const float MaximumSpeed = 80f / 3.6f;
        // Scout / Hauler / Buddy / Ranger: touring, towing, light, off-road.
        public static float StockSpeed(int model)
            => (model == 1 ? 60f : model == 2 ? 72f : model == 3 ? 66f : 68f) / 3.6f;

        // Nominal pull is per wheel. Price values pull most strongly, then
        // anchoring mass and travel speed; variants with equal stats cost the same.
        public static int Price(float mass, float drive, float speed)
        {
            float pullKN = drive * 4f / 1000f;
            float value = 600f + (pullKN - 10f) * 450f + (mass - 1000f) * .9f
                + (speed * 3.6f - 54f) * 40f;
            return Mathf.Clamp(Mathf.RoundToInt(value / 100f) * 100, 1200, 6000);
        }
    }
}
