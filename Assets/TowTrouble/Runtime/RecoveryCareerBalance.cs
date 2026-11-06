using UnityEngine;

namespace TowTrouble
{
    // Shared by the live catalogue and authoring. No inventory IDs or mission IDs change.
    public static class RecoveryCareerBalance
    {
        public const int FirstBonus = 25, ReplayPercent = 65;
        private static readonly int[] Introduction = { 0, 1, 4, 5, 8, 9 };
        private static readonly int[] MinimumRewards = { 600, 850, 1200, 1800, 2500 };
        private static readonly int[] ReplayRates = { 50, 55, 65, 75, 80 };
        public static int RepeatRate(int tier)=>tier<0?ReplayPercent:ReplayRates[Mathf.Clamp(tier,0,4)];
        public static int Order(int site, int tier)
        {
            int intro = System.Array.IndexOf(Introduction, site);
            return intro >= 0 ? intro : 100 + tier * 30 + site;
        }
        public static int Price(GaragePart part)
        {
            switch (part.Id)
            {
                case "engine_2": return 800;
                case "engine_3": return 2600;
                case "low_range": case "suspension_offroad": return 1000;
                case "tires_allterrain": return 400;
                case "tires_mud": case "tires_sand": return 600;
                case "tires_winter": return 800;
                case "tires_winter_pro": return 1800;
                case "cable_14": return 200;
                case "cable_18": return 1200;
                case "cable_26": return 2000;
                case "cable_36": return 4200;
                case "winch_2": return 1600;
                case "winch_3": return 4200;
            }
            if (part.Price > 0 && part.Slot == GarageSlot.Wheels)
            {
                float benefit = Mathf.Max(0, part.DryGrip - 1) * 1200 + Mathf.Max(0, part.MudGrip - 1) * 1000
                    + Mathf.Max(0, part.SandGrip - 1) * 800 + Mathf.Max(0, part.SnowGrip - 1) * 600
                    + Mathf.Max(0, 1 - part.MudResistance) * 500;
                return Mathf.RoundToInt((300 + benefit) / 50) * 50;
            }
            return part.Price;
        }
        public static void ApplyPrices(System.Collections.Generic.IEnumerable<GaragePart> parts)
        { foreach (var part in parts) part.Price = Price(part); }

        // Route speeds are calibrated against controlled driving samples, not the vehicle's top speed.
        // Time affects the agreed offer, never the time a player actually takes: waiting earns nothing.
        public static float ExpectedSeconds(float outbound, float towing, int tier, float blockage, bool obstacle,
            float outboundClimb=0,float towingClimb=0)
            => 90 + Mathf.Max(0, outbound) / 6.5f + Mathf.Max(0, towing) / 3f
                + Mathf.Clamp(tier, 0, 4) * 20 + Mathf.Max(0, blockage - 8000) / 300
                + Mathf.Max(0,outboundClimb)*.4f+Mathf.Max(0,towingClimb)
                + (obstacle ? 30 : 0);
        public static int WorldReward(int tier, float seconds)
        {
            tier = Mathf.Clamp(tier, 0, 4);
            int minimum = MinimumRewards[tier];
            float offer = 200 + seconds / 60 * (180 + tier * 35) + tier * 100;
            return Mathf.Max(minimum, Mathf.RoundToInt(offer / 50) * 50);
        }
    }
}
