using UnityEngine;

namespace TowTrouble
{
    public static class RecoveryBodyBalance
    {
        public const float MaximumSpeed = 80f / 3.6f;
        // Player starter baselines. Recovery targets retain their authored masses.
        public static float StockMass(int model)=>model==1?960f:model==2?900f:model==3?950f:930f;
        public static float StockDrive(int model)=>(model==1?8800f:model==2?8000f:model==3?8600f:8400f)/4f;
        public static float StockSteering(int model)=>model==1?27f:model==2?31f:model==3?29f:30f;
        public static float StockSpeed(int model)
            => (model == 1 ? 56f : model == 2 ? 64f : model == 3 ? 58f : 60f) / 3.6f;

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
