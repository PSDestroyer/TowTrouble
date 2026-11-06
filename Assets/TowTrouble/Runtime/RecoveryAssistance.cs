using UnityEngine;

namespace TowTrouble
{
    // Advice describes observed conditions; it never changes recovery physics or eligibility.
    public static class RecoveryAssistance
    {
        public static string PullAdvice(float throttle, float speed, float slip, bool brake,
            float strugglingSeconds, WinchState winch, float alignment, bool extracting)
        {
            if (strugglingSeconds < 2f) return null;
            if (!brake && Mathf.Abs(throttle) > .35f && speed < .8f && slip > .22f)
                return "Find firmer ground";
            if (winch == WinchState.Stalled) return "Winch at limit · Reposition";
            if (extracting && alignment < .65f) return "Try pulling toward the road";
            return null;
        }

        public static bool NeedsSafePoint(float upright, float speed, float throttle, bool brake, float displaced)
            => speed < .45f && (upright < .3f || (!brake && Mathf.Abs(throttle) > .55f && displaced < .7f));

        public static string Preparation(RecoveryContract contract, GaragePerformance car)
        {
            if (contract == null) return "";
            string terrain = null;
            var material = contract.Load != null ? contract.Load.Material : RecoveryLoadMaterial.Firm;
            if ((material == RecoveryLoadMaterial.Mud || material == RecoveryLoadMaterial.Silt) && car.MudGrip < 1.15f)
                terrain = "Mud tires recommended";
            else if (material == RecoveryLoadMaterial.Snow && car.SnowGrip < 1.25f)
                terrain = "Snow tires recommended";
            else if (material == RecoveryLoadMaterial.Sand && car.SandGrip < 1.15f)
                terrain = "Sand tires recommended";
            // A lower bound from the authored blockage, not a claim about total force on a slope.
            float resistance = Mathf.Max(contract.BreakoutResistance, contract.ExtractionProfile?.BreakawayForce ?? 0);
            string cable = resistance > car.Cable ? "Stronger cable recommended" : resistance > Mathf.Max(car.Drive*4,car.WinchForce) ? "More pull and firm ground recommended" : null;
            return terrain != null && cable != null ? terrain + "\n" + cable
                : terrain ?? cable ?? "Position and terrain affect the pull";
        }

        public static bool NewRecoveryAvailable(RecoveryContract[] contracts, int before, int completionId)
        {
            if (completionId < 0 || completionId >= 31 || (before & (1 << completionId)) != 0) return false;
            var oldEntries = RecoveryBoardEntries.Select(contracts, before, false);
            var newEntries = RecoveryBoardEntries.Select(contracts, before | (1 << completionId), false);
            foreach (int entry in newEntries) if (!oldEntries.Contains(entry)) return true;
            return false;
        }
    }
}
